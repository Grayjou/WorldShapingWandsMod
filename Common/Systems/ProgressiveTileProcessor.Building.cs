using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Partial class: Building batch processing (Enqueue + ProcessBatch + helpers).
/// </summary>
public partial class ProgressiveTileProcessor
{
    /// <summary>
    /// Enqueues a building (placement + replacement) operation to be processed progressively.
    /// Tiles will be placed/replaced in batches with proper sounds and effects.
    /// </summary>
    public static void EnqueueBuilding(
        Player player,
        List<TileBuildingInfo> tiles,
        UndoAction undoAction,
        UndoManager undoManager,
        int batchSize,
        float intervalSeconds,
        bool shouldConsume,
        Func<Item, bool> buildCondition,
        bool overwriteSlope,
        Enums.SlopeType slopeType,
        bool vacuumItems = true)
    {
        if (tiles.Count == 0) return;

        var op = new ProgressiveOperation
        {
            Type = OperationType.Building,
            Player = player,
            BuildingTiles = tiles,
            UndoAction = undoAction,
            UndoManager = undoManager,
            BatchSize = batchSize,
            IntervalTicks = (int)(intervalSeconds * 60f),
            TicksUntilNextBatch = 0,
            CurrentIndex = 0,
            AffectedPositions = new List<Point>(),
            TotalProcessed = 0,
            ShouldConsume = shouldConsume,
            BuildCondition = buildCondition,
            OverwriteSlope = overwriteSlope,
            SlopeType = slopeType,
            VacuumItems = vacuumItems
        };

        _activeOperations.Add(op);
    }

    private static bool ProcessBuildingBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.BuildingTiles.Count);
        var batchPositions = new List<Point>();
        int batchPlaced = 0;

        bool wasGen = WorldGen.gen;

        // Pre-compute the full operation bounds from ALL tiles (not just this batch)
        // so that vacuum sweeps capture cascaded drops from replacement tiles.
        bool wantVacuum = op.VacuumItems
            && op.BuildingTiles.Count > 0
            && !op.BuildingTiles[op.CurrentIndex].SuppressDrops;
        Rectangle fullBounds = Rectangle.Empty;
        if (wantVacuum)
            fullBounds = BulkTileOperations.ComputeBounds(
                op.BuildingTiles.ConvertAll(t => t.Position));

        // Periodic vacuum interval: sweep every N tile destructions within a batch
        // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
        const int VacuumSweepInterval = 200;
        int tilesSinceVacuum = 0;

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.BuildingTiles[i];

            if (!WorldGen.InWorld(info.Position.X, info.Position.Y, 1)) continue;

            // Re-find source item for each tile (supports NextBlock exhaustion)
            int idx = ItemTypeHelper.FindFirstItemIndex(op.Player, op.BuildCondition);
            if (idx < 0)
                continue; // no more items, skip remaining

            Item srcItem = op.Player.inventory[idx];
            ushort tType = (ushort)srcItem.createTile;
            var existingTile = Main.tile[info.Position.X, info.Position.Y];

            // Same-type in-place edit: apply slope, actuation, and/or paint without destroying the tile.
            // (IsSlopeOnly is a legacy name — it now covers all three in-place modifications.)
            if (info.IsSlopeOnly)
            {
                if (op.OverwriteSlope)
                    ApplyBuildSlope(info.Position.X, info.Position.Y, op.SlopeType);
                WandOfBuildingBase.ApplyActuation(info.Position.X, info.Position.Y, info.Actuation);
                WandOfBuildingBase.ApplyPaintSprayerTile(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                batchPlaced++;
                batchPositions.Add(info.Position);
                continue;
            }

            // Grass seed conversion: don't destroy substrate, just call PlaceTile to convert
            if (info.IsGrassSeed && existingTile.HasTile)
            {
                WorldGen.gen = true;
                if (WorldGen.PlaceTile(info.Position.X, info.Position.Y, tType,
                    mute: true, forced: false, plr: op.Player.whoAmI,
                    style: srcItem.placeStyle))
                {
                    WandOfBuildingBase.ApplyActuation(info.Position.X, info.Position.Y, info.Actuation);
                    WandOfBuildingBase.ApplyPaintSprayerTile(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                    batchPlaced++;
                    if (op.ShouldConsume)
                        ConsumeOneBuildItem(op.Player, srcItem, op.BuildCondition);
                    batchPositions.Add(info.Position);
                }
                WorldGen.gen = wasGen;
                continue;
            }

            if (info.IsReplacement && existingTile.HasTile)
            {
                // Replacement path
                WorldGen.gen = info.SuppressDrops;

                bool didReplace = false;
                // TileID.Dirt == 0: WorldGen.ReplaceTile treats tileType=0 as "no tile",
                // so skip it and go straight to KillTile+PlaceTile for Dirt targets.
                if (tType != 0 && WorldGen.ReplaceTile(info.Position.X, info.Position.Y, tType, srcItem.placeStyle))
                {
                    didReplace = true;
                }
                else
                {
                    WorldGen.KillTile(info.Position.X, info.Position.Y,
                        fail: false, effectOnly: false, noItem: info.SuppressDrops);
                    if (!Main.tile[info.Position.X, info.Position.Y].HasTile &&
                        WorldGen.PlaceTile(info.Position.X, info.Position.Y, tType,
                            mute: true, forced: false, plr: op.Player.whoAmI,
                            style: srcItem.placeStyle))
                    {
                        didReplace = true;
                    }
                }

                WorldGen.gen = wasGen;

                if (didReplace)
                {
                    if (op.OverwriteSlope)
                        ApplyBuildSlope(info.Position.X, info.Position.Y, op.SlopeType);
                    WandOfBuildingBase.ApplyActuation(info.Position.X, info.Position.Y, info.Actuation);
                    WandOfBuildingBase.ApplyPaintSprayerTile(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                    batchPlaced++;
                    tilesSinceVacuum++;
                    if (op.ShouldConsume)
                        ConsumeOneBuildItem(op.Player, srcItem, op.BuildCondition);
                    batchPositions.Add(info.Position);
                }
            }
            else if (!existingTile.HasTile)
            {
                // Placement path
                WorldGen.gen = true;

                if (WorldGen.PlaceTile(info.Position.X, info.Position.Y, tType,
                    mute: true, forced: false, plr: op.Player.whoAmI,
                    style: srcItem.placeStyle))
                {
                    if (op.OverwriteSlope)
                        ApplyBuildSlope(info.Position.X, info.Position.Y, op.SlopeType);
                    WandOfBuildingBase.ApplyActuation(info.Position.X, info.Position.Y, info.Actuation);
                    WandOfBuildingBase.ApplyPaintSprayerTile(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                    batchPlaced++;
                    if (op.ShouldConsume)
                        ConsumeOneBuildItem(op.Player, srcItem, op.BuildCondition);
                    batchPositions.Add(info.Position);
                }

                WorldGen.gen = wasGen;
            }

            // Periodic vacuum sweep within the batch to prevent 400-item cap overflow.
            if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
                tilesSinceVacuum = 0;
            }
        }

        if (batchPositions.Count > 0)
        {
            op.AffectedPositions.AddRange(batchPositions);
            BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(batchPositions));

            // Final vacuum sweep for this batch: catch remaining items from the
            // last group of tile destructions and from FinalizeBatch's frame updates.
            if (wantVacuum)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
            }
        }

        op.TotalProcessed += batchPlaced;
        op.CurrentIndex = end;
        return op.CurrentIndex >= op.BuildingTiles.Count;
    }

    /// <summary>
    /// Applies slope/half-block settings for progressive building operations.
    /// </summary>
    private static void ApplyBuildSlope(int x, int y, Enums.SlopeType slope)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return;

        if (slope == Enums.SlopeType.Default)
        {
            tile.IsHalfBlock = false;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else if (slope == Enums.SlopeType.VerticalHalf)
        {
            tile.IsHalfBlock = true;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else
        {
            tile.IsHalfBlock = false;
            tile.Slope = slope switch
            {
                Enums.SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                Enums.SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                Enums.SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpLeft, // These are inverted, don't ask me why
                Enums.SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpRight, // These are inverted, don't ask me why
                _ => Terraria.ID.SlopeType.Solid
            };
        }
        WorldGen.SquareTileFrame(x, y);
    }

    /// <summary>
    /// Consumes one item for building. Handles tile wand ammo vs direct consumption.
    /// </summary>
    private static void ConsumeOneBuildItem(Player player, Item sourceItem, Func<Item, bool> placeCondition)
    {
        bool usingTileWand = sourceItem.tileWand >= 0;
        Func<Item, bool> consumeCond = usingTileWand
            ? i => !i.IsAir && i.type == sourceItem.tileWand
            : i => !i.IsAir && i.type == sourceItem.type;

        ItemTypeHelper.ConsumeItems(player.inventory, consumeCond, 1);
    }
}
