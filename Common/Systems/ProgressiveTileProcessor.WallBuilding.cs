using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Partial class: Wall Building batch processing (Enqueue + ProcessBatch).
/// </summary>
public partial class ProgressiveTileProcessor
{
    /// <summary>
    /// Enqueues a wall building (placement + replacement) operation to be processed progressively.
    /// Walls will be placed/replaced in batches with proper sounds and effects.
    /// </summary>
    public static void EnqueueWallBuilding(
        Player player,
        List<WallBuildingInfo> walls,
        UndoAction undoAction,
        UndoManager undoManager,
        int batchSize,
        float intervalSeconds,
        bool shouldConsume,
        Func<Item, bool> buildCondition,
        bool vacuumItems = true)
    {
        if (walls.Count == 0) return;

        var op = new ProgressiveOperation
        {
            Type = OperationType.WallBuilding,
            Player = player,
            WallBuildingTiles = walls,
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
            VacuumItems = vacuumItems
        };

        _activeOperations.Add(op);
    }

    private static bool ProcessWallBuildingBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.WallBuildingTiles.Count);
        var batchPositions = new List<Point>();
        int batchPlaced = 0;

        bool wasGen = WorldGen.gen;

        // Pre-compute the full operation bounds from ALL wall tiles (not just this batch)
        // so that vacuum sweeps capture drops from wall replacement paths.
        bool wantVacuum = op.VacuumItems
            && op.WallBuildingTiles.Count > 0
            && !op.WallBuildingTiles[op.CurrentIndex].SuppressDrops;
        Rectangle fullBounds = Rectangle.Empty;
        if (wantVacuum)
            fullBounds = BulkTileOperations.ComputeBounds(
                op.WallBuildingTiles.ConvertAll(t => t.Position));

        // Periodic vacuum interval: sweep every N tile destructions within a batch
        // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
        const int VacuumSweepInterval = 200;
        int tilesSinceVacuum = 0;

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.WallBuildingTiles[i];

            if (!WorldGen.InWorld(info.Position.X, info.Position.Y, 1)) continue;

            // Re-find source wall item for each tile (supports exhaustion)
            int idx = ItemTypeHelper.FindFirstItemIndex(op.Player, op.BuildCondition);
            if (idx < 0)
                continue; // no more items

            Item srcItem = op.Player.inventory[idx];
            ushort wallType = (ushort)srcItem.createWall;
            var t = Main.tile[info.Position.X, info.Position.Y];

            if (info.IsReplacement && t.WallType != WallID.None)
            {
                // Replace existing wall
                if (t.WallType == wallType) continue; // same wall, skip

                WorldGen.gen = info.SuppressDrops;
                WorldGen.KillWall(info.Position.X, info.Position.Y, fail: false);
                if (t.WallType == WallID.None)
                {
                    WorldGen.PlaceWall(info.Position.X, info.Position.Y, wallType, mute: true);
                    if (t.WallType == wallType)
                    {
                        WandOfBuildingBase.ApplyPaintSprayerWall(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                        batchPlaced++;
                        tilesSinceVacuum++;
                        if (op.ShouldConsume)
                            ItemTypeHelper.ConsumeItems(op.Player.inventory,
                                it => !it.IsAir && it.type == srcItem.type, 1);
                        batchPositions.Add(info.Position);
                    }
                }
                WorldGen.gen = wasGen;
            }
            else if (t.WallType == WallID.None)
            {
                // Place on empty slot
                WorldGen.gen = true;
                WorldGen.PlaceWall(info.Position.X, info.Position.Y, wallType, mute: true);
                if (t.WallType == wallType)
                {
                    WandOfBuildingBase.ApplyPaintSprayerWall(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                    batchPlaced++;
                    if (op.ShouldConsume)
                        ItemTypeHelper.ConsumeItems(op.Player.inventory,
                            it => !it.IsAir && it.type == srcItem.type, 1);
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
            foreach (var pos in batchPositions)
                Framing.WallFrame(pos.X, pos.Y);
            BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(batchPositions));

            // Final vacuum sweep for this batch: catch remaining items from the
            // last group of wall destructions and from frame updates.
            if (wantVacuum)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
            }
        }

        op.TotalProcessed += batchPlaced;
        op.CurrentIndex = end;
        return op.CurrentIndex >= op.WallBuildingTiles.Count;
    }
}
