using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;
using static WorldShapingWandsMod.Common.Utilities.Msg;

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
public partial class ProgressiveTileProcessor : ModSystem
{
    /// <summary>Active batch operations being processed over time.</summary>
    private static readonly List<ProgressiveOperation> _activeOperations = new();

    /// <summary>Whether any progressive operations are currently in progress.</summary>
    public static bool HasActiveOperations => _activeOperations.Count > 0;

    // ===== Enqueue + ProcessBatch methods are in partial class files =====
    // ProgressiveTileProcessor.Dismantling.cs
    // ProgressiveTileProcessor.Replacement.cs
    // ProgressiveTileProcessor.Building.cs
    // ProgressiveTileProcessor.WallBuilding.cs
    // ProgressiveTileProcessor.WallReplacement.cs

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
                    op.BatchesCompleted++;
                    PlayBatchSound(op);
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
            case OperationType.Building:
                return ProcessBuildingBatch(op);
            case OperationType.WallBuilding:
                return ProcessWallBuildingBatch(op);
            case OperationType.WallReplacement:
                return ProcessWallReplacementBatch(op);
            default:
                return true;
        }
    }

    /// <summary>
    /// Plays a per-batch progress sound for building operations.
    /// Building: alternates Item168 (odd batches) and Tink (even batches) for a
    /// workshop-like rhythm. Dismantling plays a quieter Tink. Other operation
    /// types are silent per-batch (only play completion sound).
    /// </summary>
    private static void PlayBatchSound(ProgressiveOperation op)
    {
        var config = WandConfigs.Preferences;
        if (config?.EnableWandSounds != true) return;

        switch (op.Type)
        {
            case OperationType.Building:
            case OperationType.WallBuilding:
                // Alternate Item168 (hammer hit) on odd batches and Tink on even batches
                // to create a rhythmic construction sound.
                if (op.BatchesCompleted % 2 == 1)
                    SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, op.Player.Center);
                else
                    SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.4f }, op.Player.Center);
                break;

            case OperationType.Dismantling:
                // Quieter per-batch feedback during progressive dismantling
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.3f }, op.Player.Center);
                break;

            // Replacement and WallReplacement: silent per-batch (completion sound only)
        }
    }

    /// <summary>
    /// Called when all batches of an operation have been processed.
    /// Commits the undo action and shows the final message.
    /// </summary>
    private static void FinalizeOperation(ProgressiveOperation op)
    {
        var config = WandConfigs.Preferences;
        bool playSound = config?.EnableWandSounds ?? true;

        switch (op.Type)
        {
            case OperationType.Dismantling:
                if (op.UndoAction.Snapshots.Count > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    if (playSound)
                        SoundEngine.PlaySound(SoundID.Tink, op.Player.Center);
                    Main.NewText($"Destroyed {op.TotalProcessed} tile(s) (progressive)",
                        Color.OrangeRed);
                }
                break;

            case OperationType.Replacement:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    if (playSound)
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, op.Player.Center);

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
                    Main.NewText(Get("NoTilesReplaced"), WandColors.MsgInfo);
                }
                break;

            case OperationType.Building:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    if (playSound)
                        SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, op.Player.Center);
                    Main.NewText(
                        $"Placed {op.TotalProcessed} tile(s)" +
                        (op.ShouldConsume ? "" : " (no items consumed)") +
                        " (progressive)",
                        Color.Cyan);
                }
                else
                {
                    Main.NewText(Get("NoTilesPlaced"), WandColors.MsgInfo);
                }
                break;

            case OperationType.WallBuilding:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    if (playSound)
                        SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, op.Player.Center);
                    Main.NewText(
                        $"Placed {op.TotalProcessed} wall(s)" +
                        (op.ShouldConsume ? "" : " (no items consumed)") +
                        " (progressive)",
                        Color.Cyan);
                }
                else
                {
                    Main.NewText(Get("NoWallsPlaced"), WandColors.MsgInfo);
                }
                break;

            case OperationType.WallReplacement:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
                    if (playSound)
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, op.Player.Center);

                    // Consume target wall items all at once at the end
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
                        $"Replaced {op.TotalProcessed} walls: {srcIcon} → {tgtIcon}" +
                        (op.ShouldConsume ? "" : " (no items consumed)") +
                        " (progressive)",
                        WandColors.MsgReplacement);
                }
                else
                {
                    Main.NewText(Get("NoWallsReplaced"), WandColors.MsgInfo);
                }
                break;
        }
    }

    public override void OnWorldUnload()
    {
        _activeOperations.Clear();
    }

    // ===== Data types =====

    private enum OperationType { Dismantling, Replacement, Building, WallBuilding, WallReplacement }

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
        public ushort TargetType;   // target tile type; 0 = TileID.Dirt when IsErase is false
        public bool IsErase;        // true = erase to Air (ignore TargetType)
        public bool SuppressDrops;
        public Settings.PaintSprayerSource PaintSprayer;
        public bool PreservePaint;
    }

    /// <summary>Pre-validated tile info for progressive building (placement + replacement).</summary>
    public struct TileBuildingInfo
    {
        public Point Position;
        public bool IsReplacement;      // true = existing tile to replace; false = empty tile to place
        public bool SuppressDrops;
        public bool IsGrassSeed;        // true = grass seed conversion (don't destroy substrate)
        public bool IsSlopeOnly;        // true = same-type tile, only apply slope change
        public Settings.PaintSprayerSource PaintSprayer;
        public bool? Actuation;
    }

    /// <summary>Pre-validated wall info for progressive wall building (placement + replacement).</summary>
    public struct WallBuildingInfo
    {
        public Point Position;
        public bool IsReplacement;      // true = existing wall to replace; false = empty slot to place
        public bool SuppressDrops;
        public Settings.PaintSprayerSource PaintSprayer;
    }

    /// <summary>Pre-validated wall info for progressive wall replacement.</summary>
    public struct WallReplacementInfo
    {
        public Point Position;
        public ushort SourceWallType;
        public ushort TargetWallType;   // 0 when IsErase is true
        public bool IsErase;            // true = erase wall to nothing
        public bool SuppressDrops;
        public bool HasHangingObject;   // true = foreground tile depends on this wall for support
        public Settings.PaintSprayerSource PaintSprayer;
        public bool PreservePaint;
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

        // Building data
        public List<TileBuildingInfo> BuildingTiles;
        public List<WallBuildingInfo> WallBuildingTiles;
        public Func<Item, bool> BuildCondition;     // condition to find source items
        public bool OverwriteSlope;
        public Enums.SlopeType SlopeType;

        // Wall replacement data
        public List<WallReplacementInfo> WallReplacementTiles;
        public ushort WallTargetType;  // target wall type for wall replacement

        // Shared state
        public UndoAction UndoAction;
        public UndoManager UndoManager;
        public int BatchSize;
        public int IntervalTicks;
        public int TicksUntilNextBatch;
        public int CurrentIndex;
        public List<Point> AffectedPositions;
        public int TotalProcessed;
        public bool VacuumItems; // Whether to vacuum scattered drops after each batch
        public int BatchesCompleted; // Counts completed batches — used for alternating sound
    }
}
