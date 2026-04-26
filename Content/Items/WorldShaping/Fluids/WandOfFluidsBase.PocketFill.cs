using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Algorithms;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Projectiles;
using static WorldShapingWandsMod.Common.Utilities.Msg;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Items;


/// <summary>
/// Abstract base class for all Wand of Fluids variants.
/// Handles liquid placement, draining, rain fill, and pocket fill operations.
/// Four concrete subclasses (Instant, Select, Confirm, Stamp) provide mode behavior.
/// </summary>
// Pocket Fill (BasinFillSolver, FillAllPockets) — partial of WandOfFluidsBase. See WandOfFluidsBase.cs for the class header & overrides.
public abstract partial class WandOfFluidsBase
{
    // ── Pocket Fill ──────────────────────────────────────────────────

    /// <summary>
    /// Fills sealed enclosed cavities within the selection area.
    /// Same as Rain Fill but includes sealed pockets below overhangs —
    /// every run enclosed by solid blocks on both ends acts as a base.
    /// Uses the BasinFillSolver with FillAllPockets = true.
    /// </summary>
    private static void ExecutePocketFill(Player player, List<Point> positions, WandOfFluidsSettings settings,
        WandPlayer wandPlayer = null)
    {
        if (positions.Count == 0)
        {
            ShowNullResult(wandPlayer, "FluidsNoTiles", WandColors.MsgWarning);
            return;
        }

        // ── Step 1: Compute bounding box ──
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        var shapeSet = new HashSet<(int x, int y)>();
        foreach (var p in positions)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
            shapeSet.Add((p.X, p.Y));
        }

        // ── Step 2: Expand analysis region (+1 horiz, +1 below) ──
        int expandedMinX = Math.Max(0, minX - 1);
        int expandedMinY = minY;
        int expandedMaxX = Math.Min(Main.maxTilesX - 1, maxX + 1);
        int expandedMaxY = Math.Min(Main.maxTilesY - 1, maxY + 1);

        int gridWidth = expandedMaxX - expandedMinX + 1;
        int gridHeight = expandedMaxY - expandedMinY + 1;

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Main.NewText(Get("FluidsInvalidRegion"), WandColors.MsgWarning);
            return;
        }

        // ── Step 3: Extract tile grid ──
        var grid = new int[gridHeight, gridWidth];

        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                int worldX = expandedMinX + col;
                int worldY = expandedMinY + row;

                if (!WorldGen.InWorld(worldX, worldY, 1))
                {
                    grid[row, col] = 1;
                    continue;
                }

                var tile = Main.tile[worldX, worldY];
                // Solid tiles AND tiles with existing liquid are treated as barriers.
                // Treat Bubble blocks as barriers as well so pocket-fill respects shells.
                bool isTileBubble = tile.HasTile && tile.TileType == TileID.Bubble;
                bool isSolid = (tile.HasTile && (WorldGen.SolidTile(tile) || isTileBubble)) || tile.LiquidAmount > 0;
                grid[row, col] = isSolid ? 1 : 0;
            }
        }

        // ── Step 4: Run solver with FillAllPockets ──
        var matrix = RunMatrix.FromGrid(grid);
        var solver = new WaterFillSolver(matrix) { FillAllPockets = true };

        // Pocket Fill still uses shape-aware seeding for reachability analysis,
        // but the key difference is that ALL enclosed base runs are valid, not just
        // top-reachable ones.
        var seedPositions = new HashSet<(int x, int y)>();
        for (int worldX = minX; worldX <= maxX; worldX++)
        {
            for (int worldY = minY; worldY <= maxY; worldY++)
            {
                if (shapeSet.Contains((worldX, worldY)))
                {
                    int localX = worldX - expandedMinX;
                    int localY = worldY - expandedMinY;
                    if (localX >= 0 && localX < gridWidth && localY >= 0 && localY < gridHeight
                        && grid[localY, localX] == 0)
                    {
                        seedPositions.Add((localX, localY));
                    }
                    break; // Only need topmost per column for reachability
                }
            }
        }

        solver.Solve(seedPositions.Count > 0 ? seedPositions : null);
        var waterCoords = solver.GetWaterCoordinates();

        if (waterCoords.Count == 0)
        {
            ShowNullResult(wandPlayer, "FluidsNoPockets",
                WandColors.MsgWarning);
            return;
        }

        // ── Step 5: Place liquid (clipped to shape) ──
        int placed = 0;
        byte liquidType = (byte)settings.LiquidType;

        foreach (var (localX, localY) in waterCoords)
        {
            int worldX = expandedMinX + localX;
            int worldY = expandedMinY + localY;

            if (!shapeSet.Contains((worldX, worldY)))
                continue;

            if (!WorldGen.InWorld(worldX, worldY, 1))
                continue;

            var tile = Main.tile[worldX, worldY];
            if (tile.HasTile && WorldGen.SolidTile(tile))
                continue;

            // Handle different-liquid mixing
            if (tile.LiquidAmount > 0 && tile.LiquidType != liquidType)
            {
                int aboveY = worldY - 1;
                if (WorldGen.InWorld(worldX, aboveY, 1))
                {
                    var aboveTile = Main.tile[worldX, aboveY];
                    if (!aboveTile.HasTile || !WorldGen.SolidTile(aboveTile))
                    {
                        aboveTile.LiquidType = liquidType;
                        aboveTile.LiquidAmount = 255;

                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.sendWater(worldX, aboveY);
                        else
                            Liquid.AddWater(worldX, aboveY);

                        placed++;
                    }
                }
                continue;
            }

            tile.LiquidType = liquidType;
            tile.LiquidAmount = 255;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.sendWater(worldX, worldY);
            else
                Liquid.AddWater(worldX, worldY);

            placed++;
        }

        Main.NewText(Get("FluidsPocketFilled", placed, waterCoords.Count),
            WandColors.MsgFluids);
    }
}
