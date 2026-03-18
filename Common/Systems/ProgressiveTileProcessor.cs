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
            if (Main.netMode == NetmodeID.MultiplayerClient)
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

            // Slope-only: same-type tile, just apply the slope change
            if (info.IsSlopeOnly)
            {
                if (op.OverwriteSlope)
                    ApplyBuildSlope(info.Position.X, info.Position.Y, op.SlopeType);
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
                Enums.SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpRight,
                Enums.SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpLeft,
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
                    Main.NewText(Get("NoTilesReplaced"), WandColors.MsgInfo);
                }
                break;

            case OperationType.Building:
                if (op.TotalProcessed > 0)
                {
                    op.UndoManager.CommitAction(op.UndoAction);
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
    }

    /// <summary>Pre-validated tile info for progressive building (placement + replacement).</summary>
    public struct TileBuildingInfo
    {
        public Point Position;
        public bool IsReplacement;      // true = existing tile to replace; false = empty tile to place
        public bool SuppressDrops;
        public bool IsGrassSeed;        // true = grass seed conversion (don't destroy substrate)
        public bool IsSlopeOnly;        // true = same-type tile, only apply slope change
    }

    /// <summary>Pre-validated wall info for progressive wall building (placement + replacement).</summary>
    public struct WallBuildingInfo
    {
        public Point Position;
        public bool IsReplacement;      // true = existing wall to replace; false = empty slot to place
        public bool SuppressDrops;
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
    }
}
