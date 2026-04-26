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
/// Partial class: Wall Replacement batch processing (Enqueue + ProcessBatch).
/// </summary>
public partial class ProgressiveTileProcessor
{
    /// <summary>
    /// Enqueues a wall replacement operation to be processed progressively over time.
    /// Walls will be replaced in batches with proper drops, sounds, and effects.
    /// Hanging objects (torches, banners, etc.) that depend on the wall are destroyed
    /// and dropped before the wall is replaced.
    /// </summary>
    public static void EnqueueWallReplacement(
        Player player,
        List<WallReplacementInfo> walls,
        UndoAction undoAction,
        UndoManager undoManager,
        int batchSize,
        float intervalSeconds,
        bool shouldConsume,
        Func<Item, bool> targetCondition,
        Item sourceItem,
        Item targetItem,
        ObjectType newObjectType,
        ushort targetWallType,
        bool vacuumItems = true)
    {
        if (walls.Count == 0) return;

        var op = new ProgressiveOperation
        {
            Type = OperationType.WallReplacement,
            Player = player,
            WallReplacementTiles = walls,
            UndoAction = undoAction,
            UndoManager = undoManager,
            BatchSize = batchSize,
            IntervalTicks = (int)(intervalSeconds * 60f),
            TicksUntilNextBatch = 0,
            CurrentIndex = 0,
            AffectedPositions = new List<Point>(),
            TotalProcessed = 0,
            ShouldConsume = shouldConsume,
            TargetCondition = targetCondition,
            SourceItem = sourceItem,
            TargetItem = targetItem,
            NewObjectType = newObjectType,
            WallTargetType = targetWallType,
            VacuumItems = vacuumItems
        };

        _activeOperations.Add(op);
    }

    private static bool ProcessWallReplacementBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.WallReplacementTiles.Count);
        var batchPositions = new List<Point>();
        int batchReplaced = 0;

        bool wasGen = WorldGen.gen;

        // Pre-compute the full operation bounds from ALL wall tiles (not just this batch)
        // so that vacuum sweeps capture drops from hanging objects outside the batch.
        bool wantVacuum = op.VacuumItems
            && op.WallReplacementTiles.Count > 0
            && !op.WallReplacementTiles[op.CurrentIndex].SuppressDrops;
        Rectangle fullBounds = Rectangle.Empty;
        if (wantVacuum)
            fullBounds = BulkTileOperations.ComputeBounds(
                op.WallReplacementTiles.ConvertAll(t => t.Position));

        // Periodic vacuum interval: sweep every N tile destructions within a batch
        // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
        const int VacuumSweepInterval = 200;
        int tilesSinceVacuum = 0;

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.WallReplacementTiles[i];

            if (!WorldGen.InWorld(info.Position.X, info.Position.Y, 1)) continue;

            var t = Main.tile[info.Position.X, info.Position.Y];

            // Verify wall still matches (might have changed between batches)
            if (t.WallType != info.SourceWallType) continue;

            // Save old wall paint color for PreservePaint
            byte oldWallColor = t.WallColor;

            // Destroy hanging objects (torches, banners, etc.) that depend on the wall
            if (info.HasHangingObject && t.HasTile)
            {
                WorldGen.KillTile(info.Position.X, info.Position.Y,
                    fail: false, effectOnly: false, noItem: info.SuppressDrops);
                tilesSinceVacuum++;
            }

            // Only suppress wall drops when config says so
            if (Main.netMode != NetmodeID.MultiplayerClient)
                WorldGen.gen = info.SuppressDrops;

            if (info.IsErase)
            {
                WorldGen.KillWall(info.Position.X, info.Position.Y, fail: false);
                if (t.WallType == WallID.None)
                {
                    batchReplaced++;
                    batchPositions.Add(info.Position);
                    tilesSinceVacuum++;
                }
            }
            else
            {
                WorldGen.KillWall(info.Position.X, info.Position.Y, fail: false);
                if (t.WallType == WallID.None)
                {
                    WorldGen.PlaceWall(info.Position.X, info.Position.Y, info.TargetWallType, mute: true);
                    if (t.WallType == info.TargetWallType)
                    {
                        // Paint logic: PreservePaint wins over PaintSprayer
                        if (info.PreservePaint && oldWallColor > 0)
                            t.WallColor = oldWallColor;
                        else
                            WandOfBuildingBase.ApplyPaintSprayerWall(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                        batchReplaced++;
                        batchPositions.Add(info.Position);
                        tilesSinceVacuum++;
                    }
                }
            }

            WorldGen.gen = wasGen;

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

            if (Main.netMode == NetmodeID.MultiplayerClient)
                BulkTileOperations.FinalizeFrameOnly(batchPositions);
            else
                BulkTileOperations.FinalizeBatch(batchPositions);

            // Final vacuum sweep for this batch: catch remaining items from the
            // last group of destructions and from FinalizeBatch's frame updates.
            if (wantVacuum)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
            }
        }

        op.TotalProcessed += batchReplaced;
        op.CurrentIndex = end;
        return op.CurrentIndex >= op.WallReplacementTiles.Count;
    }
}
