using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract partial class WandOfBuildingBase
    {
        /// <summary>
        /// Wall placement path. Walls use createWall, PlaceWall, KillWall — 
        /// a fundamentally different API from tile placement.
        /// Supports replace mode (replacing existing walls) and standard placement.
        /// </summary>
        private void ExecuteWallBuilding(Player player, WandPlayer wandPlayer,
            WandOfBuildingSettings settings, SelectionState selection,
            ResourcesConfig config, SandboxConfig sandbox, PerformanceConfig perfConfig, PreferencesConfig clientCfg)
        {
            var condition = ItemTypeHelper.GetConditions(PlaceType.Wall);

            // InventoryView v1 (S6 2026-04-22): honor the wall choice if the player
            // set one via the panel. Stale choices fall back to the broad scan.
            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition, settings.ChosenWallItemType);
            if (sourceIndex < 0)
            {
                Main.NewText(Get("NoWallItemFound"), Color.Red);
                return;
            }

            // ── ExhaustMode / Choice: narrow `condition` to the FIRST wall item type ──
            //
            // Mirror of the TileExecution narrowing (post-S8 GrayJou Letter #9 fix).
            // Two narrowing triggers:
            //   (a) ExhaustMode != NextBlock — user disabled substitution.
            //   (b) Choice was honored — user chosen a wall via the InventoryView panel.
            // See TileExecution.cs for the full rationale; the same authoritative-choice
            // semantics apply here so the WoB Wall mode also stops silently substituting
            // a scan-order-first wall in place of the user's chosen wall.
            var exhaustModeWall = WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock;
            Item initialWallItem = player.inventory[sourceIndex];
            bool choiceHonoredWall = settings.ChosenWallItemType.HasValue
                                  && initialWallItem.type == settings.ChosenWallItemType.Value;

            // 2026-04-23 Session 1 (Letter #10 §6 bug): Interrupt + stale-choice hard-stop.
            // Mirror of the TileExecution guard. Interrupt must refuse when the wall choice
            // is set but missing, instead of silently substituting the scan-order-first wall.
            //
            // 2026-04-23 Session 2 (Letter #11 ack): extend to Cancel per GrayJou's
            // explicit "Yes" to Cavendish's open question. Choice = authoritative intent;
            // only NextBlock may substitute silently.
            if ((exhaustModeWall == BlockExhaustionMode.Interrupt
                    || exhaustModeWall == BlockExhaustionMode.Cancel)
                && settings.ChosenWallItemType.HasValue
                && !choiceHonoredWall)
            {
                Main.NewText(Get("ChosenMissingUnderInterrupt"), Color.Red);
                return;
            }

            if (exhaustModeWall != BlockExhaustionMode.NextBlock || choiceHonoredWall)
            {
                int ChosenWallItemType = initialWallItem.type;
                condition = i => !i.IsAir && i.type == ChosenWallItemType;
            }

            // S10 (Letter #10 polish — ghost-choice toast, mirror of TileExecution
            // hookpoint): rate-limited chat hint when the wall choice was bypassed
            // because it wasn't in inventory. See GhostChoiceToast for throttling
            // semantics (~2 s per (choice, fallback) pair).
            Common.UI.InventoryView.GhostChoiceToast.TryEmit(
                settings.ChosenWallItemType, initialWallItem.type);

            // ── Multiplayer: send packet to server instead of executing locally ──
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Item mpItem = player.inventory[sourceIndex];
                BuildingPacketHandler.SendBuildingOperation(
                    selection.StartTile, selection.EndTile,
                    settings.Shape.Shape, settings.Shape.FillMode,
                    settings.Shape.Thickness, settings.Shape.EqualDimensions,
                    selection.VerticalFirst, player.whoAmI,
                    PlaceType.Wall, settings.Slope, settings.OverwriteSlope,
                    WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock, player.TileReplacementEnabled,
                    (short)mpItem.type, (short)mpItem.placeStyle,
                    settings.Shape.Slice, settings.Shape.ConnectDiameter,
                    settings.Shape.InvertSelection,
                    settings.PaintSprayer.IsActive(), settings.Actuation);
                return;
            }

            var context = settings.Shape.ToShapeContext(
                selection.StartTile, selection.EndTile, selection.VerticalFirst);

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

            // Filter by active tile selection (Select Wand integration)
            var swp = player.GetModPlayer<DelimitationWandPlayer>();
            tilesToProcess = swp.FilterBySelection(tilesToProcess);

            bool shouldConsume = true;
            if (config.IsInfiniteForPlaceType(PlaceType.Wall))
            {
                // Count only the specific wall item type — not all wall items combined
                Item wallSourceItem = player.inventory[sourceIndex];
                Func<Item, bool> wallInfCond = i => !i.IsAir && i.type == wallSourceItem.type;
                ItemTypeHelper.CountItems(player.inventory, wallInfCond, out int grandTotal);
                int threshold = config.GetThresholdForPlaceType(PlaceType.Wall);
                if (threshold == 0)
                    shouldConsume = false;
                else if (grandTotal >= threshold)
                    shouldConsume = false;
            }

            if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Cancel)
            {
                Item firstItem = player.inventory[sourceIndex];
                Func<Item, bool> checkCond = i => !i.IsAir && i.type == firstItem.type;
                bool hasInfinite = ItemTypeHelper.CountItems(player.inventory, checkCond, out int totalAvailable);
                if (!hasInfinite && totalAvailable < tilesToProcess.Length)
                {
                    Main.NewText(Get("NeedHave", tilesToProcess.Length, firstItem.Name, totalAvailable), Color.Red);
                    return;
                }
            }

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building (Walls)");

            bool replaceMode = player.TileReplacementEnabled;

            // Branch: progressive batching for large wall operations
            bool useProgressive = perfConfig.EnableProgressiveMode
                && tilesToProcess.Length > perfConfig.ProgressiveBatchSize;

            if (useProgressive)
            {
                ExecuteWallBuildingProgressive(
                    player, settings, config, sandbox, perfConfig, condition, tilesToProcess,
                    shouldConsume, replaceMode, undoMgr, action);
                return;
            }

            ExecuteWallBuildingInstant(
                player, settings, config, sandbox, perfConfig, clientCfg, condition, tilesToProcess,
                shouldConsume, replaceMode, undoMgr, action);
        }

        /// <summary>
        /// Progressive batching path for wall building. Pre-validates walls and enqueues
        /// non-replacement walls for instant processing; replacements go to the
        /// <see cref="ProgressiveTileProcessor"/> queue.
        /// </summary>
        private void ExecuteWallBuildingProgressive(
            Player player,
            WandOfBuildingSettings settings,
            ResourcesConfig config,
            SandboxConfig sandbox,
            PerformanceConfig perfConfig,
            Func<Item, bool> condition,
            Point[] tilesToProcess,
            bool shouldConsume,
            bool replaceMode,
            UndoManager undoMgr,
            UndoAction action)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            // Pre-validate walls and snapshot them for progressive processing
            var buildWalls = new List<ProgressiveTileProcessor.WallBuildingInfo>();
            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsWallProtected(tile.X, tile.Y)) continue;

                var t = Main.tile[tile.X, tile.Y];

                if (t.WallType != WallID.None)
                {
                    if (!replaceMode) continue;

                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0) continue;
                    Item srcItem = player.inventory[idx];
                    ushort wallType = (ushort)srcItem.createWall;
                    if (t.WallType == wallType) continue;

                    if (TileHelper.WouldTileLoseSupport(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);
                    buildWalls.Add(new ProgressiveTileProcessor.WallBuildingInfo
                    {
                        Position = tile,
                        IsReplacement = true,
                        SuppressDrops = sandbox.EffectiveSuppressDrops,
                        PaintSprayer = settings.PaintSprayer
                    });
                }
                else
                {
                    action.AddSnapshot(tile);
                    buildWalls.Add(new ProgressiveTileProcessor.WallBuildingInfo
                    {
                        Position = tile,
                        IsReplacement = false,
                        SuppressDrops = true,
                        PaintSprayer = settings.PaintSprayer
                    });
                }
            }

            if (buildWalls.Count == 0)
            {
                ShowNullResult(wandPlayer, "NoWallsPlaced", Color.Gray);
                return;
            }

            // -- Unbatching Phase 1: Instant placement for non-replacement walls --
            // Walls placed into empty slots never call KillWall and can skip
            // progressive batching. Only replacements need batched processing.
            var immediateWalls = buildWalls.Where(w => !w.IsReplacement).ToList();
            var batchWalls = buildWalls.Where(w => w.IsReplacement).ToList();

            int instantWallPlaced = 0;
            var instantWallPositions = new List<Point>();

            if (immediateWalls.Count > 0)
            {
                bool savedGenW = WorldGen.gen;

                foreach (var info in immediateWalls)
                {
                    int x = info.Position.X, y = info.Position.Y;

                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Interrupt) break;
                        continue;
                    }
                    Item srcItem = player.inventory[idx];
                    ushort wallType = (ushort)srcItem.createWall;

                    WorldGen.gen = true;
                    WorldGen.PlaceWall(x, y, wallType, mute: true);
                    if (Main.tile[x, y].WallType == wallType)
                    {
                        ApplyPaintSprayerWall(player, x, y, shouldConsume, info.PaintSprayer);
                        instantWallPlaced++;
                        if (shouldConsume) ItemTypeHelper.ConsumeItems(player.inventory,
                            i => !i.IsAir && i.type == srcItem.type, 1);
                        instantWallPositions.Add(info.Position);
                    }
                    WorldGen.gen = savedGenW;
                }

                if (instantWallPositions.Count > 0)
                {
                    foreach (var pos in instantWallPositions)
                        Framing.WallFrame(pos.X, pos.Y);
                    BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(instantWallPositions));
                }
            }

            if (batchWalls.Count > 0)
            {
                int batchSize = perfConfig.ProgressiveBatchSize;
                float interval = perfConfig.ProgressiveInterval;

                ProgressiveTileProcessor.EnqueueWallBuilding(
                    player, batchWalls, action, undoMgr,
                    batchSize, interval, shouldConsume, condition,
                    vacuumItems: sandbox.VacuumItems);

                int batches = (int)Math.Ceiling((double)batchWalls.Count / batchSize);
                float totalTime = (batches - 1) * interval;

                string msg = instantWallPlaced > 0
                    ? $"Placed {instantWallPlaced} wall(s) instantly, replacing {batchWalls.Count} in {batches} wave(s) (~{totalTime:F1}s)"
                    : $"Replacing {batchWalls.Count} wall(s) in {batches} wave(s) (~{totalTime:F1}s)";
                if (!shouldConsume) msg += " (no items consumed)";
                Main.NewText(msg, Color.Cyan);
            }
            else
            {
                undoMgr.CommitAction(action);

                string detail = $"Placed {instantWallPlaced} wall(s)";
                if (!shouldConsume) detail += " (no items consumed)";
                Main.NewText(detail, Color.Green);
            }
        }

        /// <summary>
        /// Instant (non-progressive) path for wall building. Processes all walls
        /// synchronously in a single frame.
        /// </summary>
        private void ExecuteWallBuildingInstant(
            Player player,
            WandOfBuildingSettings settings,
            ResourcesConfig config,
            SandboxConfig sandbox,
            PerformanceConfig perfConfig,
            PreferencesConfig clientCfg,
            Func<Item, bool> condition,
            Point[] tilesToProcess,
            bool shouldConsume,
            bool replaceMode,
            UndoManager undoMgr,
            UndoAction action)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            int placed = 0;
            int replaced = 0;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            bool wasGen = WorldGen.gen;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsWallProtected(tile.X, tile.Y)) continue;

                var t = Main.tile[tile.X, tile.Y];

                int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                if (idx < 0)
                {
                    if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                    continue;
                }

                Item srcItem = player.inventory[idx];
                ushort wallType = (ushort)srcItem.createWall;

                if (t.WallType != WallID.None)
                {
                    // Wall already exists
                    if (!replaceMode) continue;
                    if (t.WallType == wallType) continue; // same wall, skip

                    // Preserve tiles that depend on the wall for support (torches, candles,
                    // banners, paintings, etc.). KillWall would silently destroy them.
                    if (TileHelper.WouldTileLoseSupport(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);
                    WorldGen.gen = sandbox.EffectiveSuppressDrops;
                    WorldGen.KillWall(tile.X, tile.Y, fail: false);
                    if (t.WallType == WallID.None)
                    {
                        WorldGen.PlaceWall(tile.X, tile.Y, wallType, mute: true);
                        if (t.WallType == wallType)
                        {
                            ApplyPaintSprayerWall(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
                            replaced++;
                            if (shouldConsume) ItemTypeHelper.ConsumeItems(player.inventory,
                                i => !i.IsAir && i.type == srcItem.type, 1);
                            affectedPositions.Add(tile);
                        }
                    }
                    WorldGen.gen = wasGen;
                }
                else
                {
                    // Empty wall slot — place
                    action.AddSnapshot(tile);
                    WorldGen.gen = true;
                    WorldGen.PlaceWall(tile.X, tile.Y, wallType, mute: true);
                    if (t.WallType == wallType)
                    {
                        ApplyPaintSprayerWall(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
                        placed++;
                        if (shouldConsume) ItemTypeHelper.ConsumeItems(player.inventory,
                            i => !i.IsAir && i.type == srcItem.type, 1);
                        affectedPositions.Add(tile);
                    }
                    WorldGen.gen = wasGen;
                }
            }

            WorldGen.gen = wasGen;

            if (affectedPositions.Count > 0)
            {
                // Wall framing for proper visual merging
                foreach (var pos in affectedPositions)
                    Framing.WallFrame(pos.X, pos.Y);
                BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(affectedPositions));

                // Vacuum: collect scattered wall drops into the player's inventory.
                // Wall replacement (KillWall) creates item drops when SuppressDrops is false.
                if (sandbox.VacuumItems && !sandbox.EffectiveSuppressDrops && replaced > 0)
                {
                    var bounds = BulkTileOperations.ComputeBounds(affectedPositions);
                    BulkTileOperations.VacuumItemsInArea(player, bounds);
                }
            }

            int total = placed + replaced;
            if (total > 0)
            {
                undoMgr.CommitAction(action);

                // Play completion sound for wall operations.
                if (clientCfg?.EnableWandSounds == true)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, player.Center);

                string detail = placed > 0 && replaced > 0 ? $"Placed {placed}, replaced {replaced} walls"
                    : replaced > 0 ? $"Replaced {replaced} walls" : $"Placed {placed} walls";
                if (!shouldConsume) detail += " (no items consumed)";
                if (interrupted) detail += " — ran out of walls";
                Main.NewText(detail, Color.Cyan);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoWallsPlaced", Color.Gray);
            }
        }
    }
}
