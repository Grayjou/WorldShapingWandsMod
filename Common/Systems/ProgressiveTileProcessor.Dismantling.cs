using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Partial class: Dismantling batch processing (Enqueue + ProcessBatch).
/// </summary>
public partial class ProgressiveTileProcessor
{
    /// <summary>
    /// Enqueues a destruction operation to be processed progressively over time.
    /// Tiles will be destroyed in batches with proper drops, sounds, and effects.
    /// </summary>
    public static void EnqueueDismantling(
        Player player,
        List<TileDismantlingInfo> tiles,
        UndoAction undoAction,
        UndoManager undoManager,
        int batchSize,
        float intervalSeconds,
        bool vacuumItems = true)
    {
        if (tiles.Count == 0) return;

        var op = new ProgressiveOperation
        {
            Type = OperationType.Dismantling,
            Player = player,
            DismantlingTiles = tiles,
            UndoAction = undoAction,
            UndoManager = undoManager,
            BatchSize = batchSize,
            IntervalTicks = (int)(intervalSeconds * 60f), // 60 ticks per second
            TicksUntilNextBatch = 0, // Process first batch immediately
            CurrentIndex = 0,
            AffectedPositions = new List<Point>(),
            TotalProcessed = 0,
            VacuumItems = vacuumItems
        };

        _activeOperations.Add(op);
    }

    private static bool ProcessDismantlingBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.DismantlingTiles.Count);
        var batchPositions = new List<Point>();

        // Pre-compute the full operation bounds from ALL tiles (not just this batch)
        // so that vacuum sweeps capture cascaded drops from multi-tile objects
        // (trees, bamboo, etc.) that spawn items outside the explicit tile set.
        bool wantVacuum = op.VacuumItems
            && op.DismantlingTiles.Count > 0
            && !op.DismantlingTiles[op.CurrentIndex].SuppressDrops;
        Rectangle fullBounds = Rectangle.Empty;
        if (wantVacuum)
            fullBounds = BulkTileOperations.ComputeBounds(
                op.DismantlingTiles.ConvertAll(t => t.Position));

        // Periodic vacuum interval: sweep every N tile destructions within a batch
        // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
        // Each KillTile can cascade via SquareTileFrame to destroy additional
        // unsupported tiles, multiplying the item count beyond the explicit tile count.
        const int VacuumSweepInterval = 200;
        int tilesSinceVacuum = 0;

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.DismantlingTiles[i];

            if (info.DestroyTile)
            {
                // Re-check at execution time: tile may now be killable because
                // objects above it were destroyed in a prior batch or earlier in this batch.
                if (!Main.tile[info.Position.X, info.Position.Y].HasTile)
                {
                    // Already gone (part of a multi-tile object that collapsed)
                }
                else if (!WorldGen.CanKillTile(info.Position.X, info.Position.Y))
                {
                    // Still can't kill — skip
                }
                else
                {
                    WorldGen.KillTile(info.Position.X, info.Position.Y,
                        fail: false, effectOnly: false, noItem: info.SuppressDrops);
                    op.TotalProcessed++;
                    batchPositions.Add(info.Position);
                    tilesSinceVacuum++;
                }
            }

            if (info.DestroyWall)
            {
                WorldGen.KillWall(info.Position.X, info.Position.Y);
                if (!info.DestroyTile)
                    batchPositions.Add(info.Position);
            }

            // Periodic vacuum sweep within the batch to prevent 400-item cap overflow.
            if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
                tilesSinceVacuum = 0;
            }
        }

        // Batch frame update + network sync for this batch's affected tiles
        // In MP, per-tile KillTile/KillWall messages already handle server sync,
        // so only do frame updates (no BatchNetworkSync to avoid dual-sync).
        if (batchPositions.Count > 0)
        {
            op.AffectedPositions.AddRange(batchPositions);
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
                BulkTileOperations.FinalizeFrameOnly(batchPositions);
            else
                BulkTileOperations.FinalizeBatch(batchPositions);

            // Final vacuum sweep for this batch: catch remaining items from the
            // last group of tile destructions and from FinalizeBatch's frame updates
            // (which can cascade-destroy additional unsupported tiles).
            if (wantVacuum)
            {
                BulkTileOperations.VacuumItemsInArea(op.Player, fullBounds);
            }
        }

        op.CurrentIndex = end;
        return op.CurrentIndex >= op.DismantlingTiles.Count;
    }
}
