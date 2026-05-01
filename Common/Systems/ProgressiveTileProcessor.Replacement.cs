using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Partial class: Replacement batch processing (Enqueue + ProcessBatch).
/// </summary>
public partial class ProgressiveTileProcessor
{
    /// <summary>
    /// Enqueues a replacement operation to be processed progressively over time.
    /// Tiles will be replaced in batches with proper drops, sounds, and effects.
    /// </summary>
    public static void EnqueueReplacement(
        Player player,
        List<TileReplacementInfo> tiles,
        UndoAction undoAction,
        UndoManager undoManager,
        int batchSize,
        float intervalSeconds,
        bool shouldConsume,
        Func<Item, bool> targetCondition,
        Item sourceItem,
        Item targetItem,
        ObjectType newObjectType,
        bool vacuumItems = true)
    {
        if (tiles.Count == 0) return;

        var op = new ProgressiveOperation
        {
            Type = OperationType.Replacement,
            Player = player,
            ReplacementTiles = tiles,
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
            VacuumItems = vacuumItems
        };

        _activeOperations.Add(op);
    }

    private static bool ProcessReplacementBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.ReplacementTiles.Count);
        var batchPositions = new List<Point>();
        int batchReplaced = 0;

        // Pre-compute the full operation bounds from ALL tiles (not just this batch)
        // so that vacuum sweeps capture cascaded drops from multi-tile objects.
        bool wantVacuum = op.VacuumItems
            && op.ReplacementTiles.Count > 0
            && !op.ReplacementTiles[op.CurrentIndex].SuppressDrops;
        Rectangle fullBounds = Rectangle.Empty;
        if (wantVacuum)
            fullBounds = BulkTileOperations.ComputeBounds(
                op.ReplacementTiles.ConvertAll(t => t.Position));

        // Periodic vacuum interval: sweep every N tile destructions within a batch
        // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
        const int VacuumSweepInterval = 200;
        int tilesSinceVacuum = 0;

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.ReplacementTiles[i];
            var t = Main.tile[info.Position.X, info.Position.Y];

            // Verify tile still matches (might have changed between batches)
            if (!t.HasTile || !ItemTypeHelper.IsTileVariantOf(t.TileType, info.SourceType))
                continue;

            // Preserve slope/half-block from the old tile
            var oldSlope = t.Slope;
            bool oldHalf = t.IsHalfBlock;

            // Save old paint color for PreservePaint
            byte oldPaintColor = t.TileColor;

            bool didReplace = false;

            if (!info.IsErase)
            {
                // TileID.Dirt == 0: WorldGen.ReplaceTile treats tileType=0 as "no tile",
                // so skip it for Dirt targets and use KillTile+PlaceTile instead.
                if (info.TargetType != 0 && WorldGen.ReplaceTile(info.Position.X, info.Position.Y, info.TargetType, 0))
                {
                    didReplace = true;
                }
                else if (WorldGen.CanKillTile(info.Position.X, info.Position.Y))
                {
                    // Fallback: KillTile + PlaceTile
                    WorldGen.KillTile(info.Position.X, info.Position.Y,
                        fail: false, effectOnly: false, noItem: info.SuppressDrops);

                    if (!Main.tile[info.Position.X, info.Position.Y].HasTile)
                    {
                        WorldGen.PlaceTile(info.Position.X, info.Position.Y,
                            info.TargetType, mute: true, forced: false,
                            plr: op.Player.whoAmI);
                        didReplace = Main.tile[info.Position.X, info.Position.Y].HasTile;
                    }
                }
            }
            else
            {
                // Erase to Air
                if (WorldGen.CanKillTile(info.Position.X, info.Position.Y))
                {
                    WorldGen.KillTile(info.Position.X, info.Position.Y,
                        fail: false, effectOnly: false, noItem: info.SuppressDrops);
                    didReplace = !Main.tile[info.Position.X, info.Position.Y].HasTile;
                }
            }

            if (didReplace)
            {
                // Restore the original slope/half-block
                var placed = Main.tile[info.Position.X, info.Position.Y];
                if (placed.HasTile)
                {
                    placed.Slope = oldSlope;
                    placed.IsHalfBlock = oldHalf;
                }

                if (info.PaintSprayer.IsActive() && placed.HasTile)
                {
                    // PreservePaint wins over PaintSprayer
                    if (info.PreservePaint && oldPaintColor > 0)
                        placed.TileColor = oldPaintColor;
                    else
                        WandOfBuildingBase.ApplyPaintSprayerTile(op.Player, info.Position.X, info.Position.Y, op.ShouldConsume, info.PaintSprayer);
                }
                else if (info.PreservePaint && oldPaintColor > 0 && placed.HasTile)
                {
                    placed.TileColor = oldPaintColor;
                }

                batchReplaced++;
                batchPositions.Add(info.Position);
                tilesSinceVacuum++;
            }

            // Periodic vacuum sweep within the batch to prevent 400-item cap overflow.
            if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
                tilesSinceVacuum = 0;
            }
        }

        // Batch frame update + network sync for this batch
        // In MP, per-tile messages already handle server sync — only frame update.
        if (batchPositions.Count > 0)
        {
            op.AffectedPositions.AddRange(batchPositions);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                BulkTileOperations.FinalizeFrameOnly(batchPositions);
            else
                BulkTileOperations.FinalizeBatch(batchPositions);

            // Final vacuum sweep for this batch: catch remaining items from the
            // last group of tile destructions and from FinalizeBatch's frame updates.
            if (wantVacuum)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
            }
        }

        op.TotalProcessed += batchReplaced;
        op.CurrentIndex = end;
        return op.CurrentIndex >= op.ReplacementTiles.Count;
    }
}
