using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Processes tile operations in timed batches instead of all at once.
/// When progressive mode is enabled, tiles are killed/replaced in waves
/// (e.g., 400 tiles every 0.3 seconds) so the player sees a satisfying
/// demolition effect with proper tile drops, sounds, and dust — without
/// the overwhelming cacophony of doing everything in a single frame.
/// 
/// <para>When progressive mode is disabled, operations execute silently
/// and instantly using <c>WorldGen.gen = true</c> (no drops, no sounds).</para>
/// </summary>
public class ProgressiveTileProcessor : ModSystem
{
    /// <summary>Active batch operations being processed over time.</summary>
    private static readonly List<ProgressiveOperation> _activeOperations = new();

    /// <summary>Whether any progressive operations are currently in progress.</summary>
    public static bool HasActiveOperations => _activeOperations.Count > 0;

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
        float intervalSeconds)
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
            TotalProcessed = 0
        };

        _activeOperations.Add(op);
    }

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
        ObjectType newObjectType)
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
            NewObjectType = newObjectType
        };

        _activeOperations.Add(op);
    }

    public override void PostUpdateWorld()
    {
        if (_activeOperations.Count == 0) return;

        // Process all active operations; remove completed ones
        for (int i = _activeOperations.Count - 1; i >= 0; i--)
        {
            var op = _activeOperations[i];
            op.TicksUntilNextBatch--;

            if (op.TicksUntilNextBatch <= 0)
            {
                bool completed = ProcessBatch(op);
                if (completed)
                {
                    FinalizeOperation(op);
                    _activeOperations.RemoveAt(i);
                }
                else
                {
                    op.TicksUntilNextBatch = op.IntervalTicks;
                }
            }
        }
    }

    /// <summary>
    /// Processes one batch of tiles for the given operation.
    /// Returns true when all tiles have been processed.
    /// </summary>
    private static bool ProcessBatch(ProgressiveOperation op)
    {
        switch (op.Type)
        {
            case OperationType.Dismantling:
                return ProcessDismantlingBatch(op);
            case OperationType.Replacement:
                return ProcessReplacementBatch(op);
            default:
                return true;
        }
    }

    private static bool ProcessDismantlingBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.DismantlingTiles.Count);
        var batchPositions = new List<Point>();

        for (int i = op.CurrentIndex; i < end; i++)
        {
            var info = op.DismantlingTiles[i];

            if (info.DestroyTile)
            {
                WorldGen.KillTile(info.Position.X, info.Position.Y,
                    fail: false, effectOnly: false, noItem: info.SuppressDrops);
                op.TotalProcessed++;
                batchPositions.Add(info.Position);
            }

            if (info.DestroyWall)
            {
                WorldGen.KillWall(info.Position.X, info.Position.Y);
                if (!info.DestroyTile)
                    batchPositions.Add(info.Position);
            }
        }

        // Batch frame update + network sync for this batch's affected tiles
        if (batchPositions.Count > 0)
        {
            op.AffectedPositions.AddRange(batchPositions);
            BulkTileOperations.FinalizeBatch(batchPositions);
        }

        op.CurrentIndex = end;
        return op.CurrentIndex >= op.DismantlingTiles.Count;
    }

    private static bool ProcessReplacementBatch(ProgressiveOperation op)
    {
        int end = Math.Min(op.CurrentIndex + op.BatchSize, op.ReplacementTiles.Count);
        var batchPositions = new List<Point>();
        int batchReplaced = 0;

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

            bool didReplace = false;

            if (info.TargetType > 0)
            {
                // Try ReplaceTile first — handles tiles under multi-tile objects (chests, etc.)
                if (WorldGen.ReplaceTile(info.Position.X, info.Position.Y, info.TargetType, 0))
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

                batchReplaced++;
                batchPositions.Add(info.Position);
            }
        }

        // Batch frame update + network sync for this batch
        if (batchPositions.Count > 0)
        {
            op.AffectedPositions.AddRange(batchPositions);
            BulkTileOperations.FinalizeBatch(batchPositions);
        }

        op.TotalProcessed += batchReplaced;
        op.CurrentIndex = end;
        return op.CurrentIndex >= op.ReplacementTiles.Count;
    }

    /// <summary>
    /// Called when all batches of an operation have been processed.
    /// Commits the undo action and shows the final message.
    /// </summary>
    private static void FinalizeOperation(ProgressiveOperation op)
    {
        switch (op.Type)
        {
            case OperationType.Dismantling:
                if (op.UndoAction.Snapshots.Count > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    Main.NewText($"Destroyed {op.TotalProcessed} tile(s) (progressive)",
                        Color.OrangeRed);
                }
                break;

            case OperationType.Replacement:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);

                    // Consume target items all at once at the end
                    if (op.NewObjectType != Enums.ObjectType.Air
                        && op.TargetCondition != null && op.ShouldConsume)
                    {
                        ItemTypeHelper.ConsumeItems(op.Player.inventory,
                            op.TargetCondition, op.TotalProcessed);
                    }

                    string srcIcon = op.SourceItem != null ? $"[i:{op.SourceItem.type}]" : "?";
                    string tgtIcon = op.NewObjectType == Enums.ObjectType.Air
                        ? "Air"
                        : (op.TargetItem != null ? $"[i:{op.TargetItem.type}]" : "?");
                    Main.NewText(
                        $"Replaced {op.TotalProcessed}: {srcIcon} → {tgtIcon}" +
                        (op.ShouldConsume ? "" : " (no items consumed)") +
                        " (progressive)",
                        WandColors.MsgReplacement);
                }
                else
                {
                    Main.NewText("No tiles could be replaced.", WandColors.MsgInfo);
                }
                break;
        }
    }

    public override void OnWorldUnload()
    {
        _activeOperations.Clear();
    }

    // ===== Data types =====

    private enum OperationType { Dismantling, Replacement }

    /// <summary>Pre-validated tile info for progressive destruction.</summary>
    public struct TileDismantlingInfo
    {
        public Point Position;
        public bool DestroyTile;
        public bool DestroyWall;
        public bool SuppressDrops;
    }

    /// <summary>Pre-validated tile info for progressive replacement.</summary>
    public struct TileReplacementInfo
    {
        public Point Position;
        public ushort SourceType;
        public ushort TargetType; // 0 = Air (erase only)
        public bool SuppressDrops;
    }

    private class ProgressiveOperation
    {
        public OperationType Type;
        public Player Player;

        // Dismantling data
        public List<TileDismantlingInfo> DismantlingTiles;

        // Replacement data
        public List<TileReplacementInfo> ReplacementTiles;
        public bool ShouldConsume;
        public Func<Item, bool> TargetCondition;
        public Item SourceItem;
        public Item TargetItem;
        public ObjectType NewObjectType;

        // Shared state
        public UndoAction UndoAction;
        public UndoManager UndoManager;
        public int BatchSize;
        public int IntervalTicks;
        public int TicksUntilNextBatch;
        public int CurrentIndex;
        public List<Point> AffectedPositions;
        public int TotalProcessed;
    }
}
