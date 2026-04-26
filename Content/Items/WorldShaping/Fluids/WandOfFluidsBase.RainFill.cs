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
// Rain Fill (BasinFillSolver + cosmetic clouds) — partial of WandOfFluidsBase. See WandOfFluidsBase.cs for the class header & overrides.
public abstract partial class WandOfFluidsBase
{
    // ── Rain Fill ────────────────────────────────────────────────────

    /// <summary>
    /// Number of tiles to pad on each side of the cloud spawn columns.
    /// Without padding, thick-line shapes (e.g. a 5-tile Cardinal Line) drop
    /// dust right up to the edge column but the rim of the dust sprite falls
    /// outside the cloud column, producing a visible "no-rain" gap above the
    /// outermost dust column. Extending the column list by this many tiles on
    /// each side keeps clouds visually centered over the wet area.
    /// </summary>
    private const int CloudColumnPadding = 12;

    /// <summary>
    /// Rain Fill: seeds raindrop positions from the topmost shape tile per column,
    /// runs the BasinFillSolver to determine where water would settle, then places
    /// liquid at every fill coordinate.
    ///
    /// The selection is expanded by 1 tile horizontally and 1 tile below so the
    /// solver can detect terrain walls and floors that border the shape.
    ///
    /// Non-rectangular shapes (ellipse, diamond, etc.) are handled by computing
    /// per-column seed positions from the actual shape tile set, not the bbox top row.
    /// </summary>
    private static void ExecuteRainFill(Player player, List<Point> positions, WandOfFluidsSettings settings,
        WandPlayer wandPlayer = null)
    {
        if (positions.Count == 0)
        {
            ShowNullResult(wandPlayer, "FluidsNoTiles", WandColors.MsgWarning);
            return;
        }

        // ── Step 1: Compute the bounding box of the shape positions ──
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

        // ── Step 2: Expand the analysis region ──
        // +1 tile in each horizontal direction and +1 tile below.
        // This lets the solver detect solid walls/floors at the shape boundary.
        int expandedMinX = Math.Max(0, minX - 1);
        int expandedMinY = minY; // No expansion above — rain seeds from top of shape
        int expandedMaxX = Math.Min(Main.maxTilesX - 1, maxX + 1);
        int expandedMaxY = Math.Min(Main.maxTilesY - 1, maxY + 1);

        int gridWidth = expandedMaxX - expandedMinX + 1;
        int gridHeight = expandedMaxY - expandedMinY + 1;

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Main.NewText(Get("FluidsInvalidRegion"), WandColors.MsgWarning);
            return;
        }

        // ── Step 3: Extract the tile grid ──
        // 0 = air (can hold water), 1 = solid (boundary)
        var grid = new int[gridHeight, gridWidth];

        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                int worldX = expandedMinX + col;
                int worldY = expandedMinY + row;

                if (!WorldGen.InWorld(worldX, worldY, 1))
                {
                    grid[row, col] = 1; // Out of world = solid
                    continue;
                }

                var tile = Main.tile[worldX, worldY];
                // Solid tiles AND tiles with existing liquid are treated as barriers.
                // Bubble blocks (TileID.Bubble) should also act as barriers for basin
                // viability, even though they may not be reported as "solid" by
                // WorldGen.SolidTile. Treat them as solid here so the solver respects
                // bubble shells when computing water fill.
                bool isTileBubble = tile.HasTile && tile.TileType == TileID.Bubble;
                bool isSolid = (tile.HasTile && (WorldGen.SolidTile(tile) || isTileBubble)) || tile.LiquidAmount > 0;
                grid[row, col] = isSolid ? 1 : 0;
            }
        }

        // ── Step 4: Compute per-column seed positions ──
        // For each column that has at least one shape tile, find the topmost shape tile.
        // Convert to local grid coordinates for the solver.
        var seedPositions = new HashSet<(int x, int y)>();

        for (int worldX = minX; worldX <= maxX; worldX++)
        {
            int topY = int.MaxValue;
            for (int worldY = minY; worldY <= maxY; worldY++)
            {
                if (shapeSet.Contains((worldX, worldY)))
                {
                    topY = worldY;
                    break;
                }
            }

            if (topY == int.MaxValue)
                continue; // No shape tile in this column — skip (ellipse/diamond gap)

            // Convert to local grid coords
            int localX = worldX - expandedMinX;
            int localY = topY - expandedMinY;

            // Only seed if the tile at that position is air (not solid)
            if (localX >= 0 && localX < gridWidth && localY >= 0 && localY < gridHeight
                && grid[localY, localX] == 0)
            {
                seedPositions.Add((localX, localY));
            }
        }

        if (seedPositions.Count == 0)
        {
#if DEBUG
            // Generate synthetic seed positions at the top of each column in the shape
            // so we can spawn clouds even when there are no natural seed points.
            var synthSeeds = new HashSet<(int x, int y)>();
            foreach (var (wx, wy) in shapeSet)
            {
                int localX = wx - expandedMinX;
                int localY = wy - expandedMinY;
                if (!synthSeeds.Any(s => s.x == localX))
                    synthSeeds.Add((localX, localY));
            }
            if (synthSeeds.Count > 0)
            {
                SpawnRainClouds(player, synthSeeds, expandedMinX, expandedMinY, settings.LiquidType);
                Main.NewText("[DEBUG] Rain clouds spawned (synthetic seeds — debug mode)", WandColors.MsgWarning);
                return;
            }
#endif
            ShowNullResult(wandPlayer, "FluidsNoSeeds",
                WandColors.MsgWarning);
            return;
        }

        // ── Step 5: Run the BasinFillSolver ──
        var matrix = RunMatrix.FromGrid(grid);
        var solver = new WaterFillSolver(matrix);
        solver.Solve(seedPositions);

        var waterCoords = solver.GetWaterCoordinates();

        if (waterCoords.Count == 0)
        {
#if DEBUG
            // Debug mode: skip liquid placement but still spawn clouds
            // so rain animations and dust ratios can be iterated without needing basins.
            SpawnRainClouds(player, seedPositions, expandedMinX, expandedMinY, settings.LiquidType);
            Main.NewText("[DEBUG] Rain clouds spawned (no basins — debug mode)", WandColors.MsgWarning);
            return;
#else
            ShowNullResult(wandPlayer, "FluidsNoBasins", WandColors.MsgWarning);
            return;
#endif
        }

        // ── Step 6: Map local coords back to world coords and clip to shape ──
        // Only place liquid at positions that are within the original shape selection.
        int placed = 0;
        byte liquidType = (byte)settings.LiquidType;

        foreach (var (localX, localY) in waterCoords)
        {
            int worldX = expandedMinX + localX;
            int worldY = expandedMinY + localY;

            // Clip to the original shape — don't fill the expanded border tiles
            if (!shapeSet.Contains((worldX, worldY)))
                continue;

            if (!WorldGen.InWorld(worldX, worldY, 1))
                continue;

            var tile = Main.tile[worldX, worldY];

            // Double-check: don't overwrite solid tiles
            if (tile.HasTile && WorldGen.SolidTile(tile))
                continue;

            // If there's a different liquid already present, spawn 1 tile above
            // so vanilla mixing mechanics apply (obsidian, honey blocks, etc.)
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

        Main.NewText(Get("FluidsRainFilled", placed, seedPositions.Count, waterCoords.Count),
            WandColors.MsgFluids);

        // ── Step 7: Spawn decorative rain clouds above seeded columns ──
        SpawnRainClouds(player, seedPositions, expandedMinX, expandedMinY, settings.LiquidType);
    }

    /// <summary>
    /// Pads a sorted column list by adding <paramref name="padding"/> extra columns
    /// on each side. This ensures cloud sprites visually cover the full wet area,
    /// preventing "no-rain" gaps at the edges of thick-line shapes.
    /// </summary>
    /// <param name="sortedColumns">Sorted list of world-X column indices to pad.</param>
    /// <param name="padding">Number of extra columns to add on each side.</param>
    /// <returns>A new list with padding columns prepended and appended.</returns>
    private static List<int> PadColumnList(List<int> sortedColumns, int padding)
    {
        if (sortedColumns.Count == 0 || padding <= 0)
            return sortedColumns;

        int leftEdge = sortedColumns[0];
        int rightEdge = sortedColumns[sortedColumns.Count - 1];

        var padded = new List<int>(sortedColumns.Count + padding * 2);

        for (int p = padding; p >= 1; p--)
            padded.Add(leftEdge - p);

        padded.AddRange(sortedColumns);

        for (int p = 1; p <= padding; p++)
            padded.Add(rightEdge + p);

        return padded;
    }

    /// <summary>
    /// Spawns cosmetic <see cref="RainCloudProjectile"/> instances above the seeded columns.
    /// Uses a spacing algorithm to prevent clouds from clustering:
    /// <list type="bullet">
    ///   <item>If the previous column spawned a cloud, skip this column.</item>
    ///   <item>If one column was skipped, 50% chance to spawn.</item>
    ///   <item>If two or more columns were skipped, always spawn.</item>
    /// </list>
    /// Clouds have a random Y offset with magnitude in [4,16) pixels, sign flipped randomly,
    /// which avoids clustering near the center line while keeping natural variation.
    /// Capped at <see cref="MaxClouds"/> clouds maximum.
    /// </summary>
    private static void SpawnRainClouds(
        Player player, HashSet<(int x, int y)> seedPositions,
        int expandedMinX, int expandedMinY, LiquidTypeSelection liquidType)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // Respect client preference — skip cloud spawning if disabled
        if (!WandConfigs.Preferences.RainFillSummonsClouds)
            return;

        const int MaxClouds = 100;
        const float CloudOffsetY = 48f;
        const float MinYOffset = 4f;   // Minimum distance from baseline (pixels)
        const float MaxYOffset = 16f;  // Maximum distance from baseline (pixels)

        // ── Collect unique world-X columns and find the topmost seed Y ──
        var columns = new HashSet<int>();
        int topWorldY = int.MaxValue;

        foreach (var (localX, localY) in seedPositions)
        {
            int worldX = expandedMinX + localX;
            int worldY = expandedMinY + localY;
            columns.Add(worldX);
            if (worldY < topWorldY) topWorldY = worldY;
        }

        // ── Sort columns and apply horizontal padding ──
        var colList = new List<int>(columns);
        colList.Sort();
        colList = PadColumnList(colList, CloudColumnPadding);

        // ── Spawn clouds with spacing logic ──
        int cloudType = ModContent.ProjectileType<RainCloudProjectile>();
        int cloudsSpawned = 0;

        // Track consecutive columns without a cloud spawn.
        // Start at 2 so the first column is eligible to spawn.
        int consecutiveSkips = 2;

        for (int i = 0; i < colList.Count && cloudsSpawned < MaxClouds; i++)
        {
            bool shouldSpawn = consecutiveSkips switch
            {
                >= 2 => true,                   // Two+ columns skipped → always spawn
                1    => Main.rand.NextBool(),   // One column skipped   → 50% chance
                _    => false                   // Just spawned          → skip
            };

            if (!shouldSpawn)
            {
                consecutiveSkips++;
                continue;
            }

            int worldX = colList[i];

            // NewProjectile() expects the projectile CENTER (subtracts width/2 internally).
            // Tile center in world pixels = worldX * 16 + 8. No manual width-half shift.
            float spawnX = worldX * 16f + 8f;

            float magnitude = Main.rand.NextFloat(MinYOffset, MaxYOffset);
            float randomYOffset = magnitude * (Main.rand.NextBool() ? 1f : -1f);
            float spawnY = topWorldY * 16f - CloudOffsetY + randomYOffset;

            int variant = Main.rand.Next(3);
            Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem),
                new Vector2(spawnX, spawnY),
                Vector2.Zero,
                cloudType,
                0, 0f,
                player.whoAmI,
                ai0: variant,
                ai1: 0f,
                ai2: (float)liquidType);

            cloudsSpawned++;
            consecutiveSkips = 0;
        }
    }
}