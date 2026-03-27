using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using static WorldShapingWandsMod.Common.Utilities.Msg;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract class WandOfBuildingBase : BaseCyclingWand
    {
        public override string WandBaseName => "Wand of Building";
        public override string WandLore => "The Deity of Creation lets you manifest order and existence at will.";

        protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

        public override bool? UseItem(Player player)
        {
            // Don't do anything if the mouse is over UI
            if (Main.LocalPlayer.mouseInterface)
                return false;

            var wandPlayer = player.GetModPlayer<WandPlayer>();

            // Clear incompatible selections (e.g., a 2-step selection on a 2-click wand).
            // Skip for OneClick — instant wands manage their own lifecycle in HoldItem.
            if (WandSelectionMode != SelectionMode.OneClick)
                wandPlayer.EnsureSelectionCompatibility(WandSelectionMode);

            if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
                return false;

            Point mouseTile = GeometryHelper.GetMouseTile();
            return HandleUseItem(player, wandPlayer, mouseTile);
        }

        public override void HoldItem(Player player)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            // Only cancel on right-click in the WORLD, not when clicking in inventory/UI.
            // Without this guard, right-clicking the wand in inventory (to cycle mode)
            // also triggers cancel-selection, which eats the click.
            if (!Main.LocalPlayer.mouseInterface
                && wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
            {
                CancelSelection(wandPlayer);
                Main.mouseRightRelease = false;
            }

            // Display cursor item icon for the block that will be placed
            var condition = ItemTypeHelper.GetConditions(wandPlayer.BuildingSettings.Object);
            Item blockItem = ItemTypeHelper.FindFirstItem(player, condition);
            if (blockItem != null)
            {
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = blockItem.type;
                player.cursorItemIconPush = 26;
            }
        }

        protected virtual void CancelSelection(WandPlayer wandPlayer)
        {
            wandPlayer.CancelSelection(WandColors.CancelBuilding, wandPlayer.BuildingSettings.Shape);
        }

        protected void ExecuteBuilding(Player player, WandPlayer wandPlayer)
        {
            var settings = wandPlayer.BuildingSettings;
            var selection = wandPlayer.GetVisualSelection();
            var config = ModContent.GetInstance<WandServerConfig>();
            var clientCfg = ModContent.GetInstance<WandClientConfig>();

            // Wall mode uses a completely separate placement path
            if (settings.Object == PlaceType.Wall)
            {
                ExecuteWallBuilding(player, wandPlayer, settings, selection, config, clientCfg);
                return;
            }

            // ── Multiplayer: send packet to server instead of executing locally ──
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Find source item for the packet (client-side pre-validation)
                var mpBaseCondition = ItemTypeHelper.GetConditions(settings.Object);
                Func<Item, bool> mpCondition = item => mpBaseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);
                int mpIdx = ItemTypeHelper.FindFirstItemIndex(player, mpCondition);
                if (mpIdx < 0)
                {
                    Main.NewText(Get("NoSuitableItem", settings.Object), Color.Red);
                    return;
                }

                Item mpItem = player.inventory[mpIdx];
                BuildingPacketHandler.SendBuildingOperation(
                    selection.StartTile, selection.EndTile,
                    settings.Shape.Shape, settings.Shape.FillMode,
                    settings.Shape.Thickness, settings.Shape.EqualDimensions,
                    selection.VerticalFirst, player.whoAmI,
                    settings.Object, settings.Slope, settings.OverwriteSlope,
                    settings.ExhaustionMode, player.TileReplacementEnabled,
                    (short)mpItem.type, (short)mpItem.placeStyle,
                    settings.Shape.Slice, settings.Shape.ConnectDiameter,
                    settings.Shape.InvertSelection,
                    settings.PaintSprayer, settings.Actuation);
                return;
            }

            // Get the condition for the selected object type, filtering out multi-tile objects
            // so they are silently skipped (e.g., Furnace is ignored, Wood is used instead).
            var baseCondition = ItemTypeHelper.GetConditions(settings.Object);
            Func<Item, bool> condition = item => baseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);

            // Find the first matching placement item (block item or tile wand)
            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition);
            if (sourceIndex < 0)
            {
                Main.NewText(Get("NoSuitableItem", settings.Object), Color.Red);
                return;
            }

            Item initialSourceItem = player.inventory[sourceIndex];

            // Build shape context (includes slice & connectDiameter from settings)
            var context = settings.Shape.ToShapeContext(
                selection.StartTile, selection.EndTile, selection.VerticalFirst);

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

            // Sort gravity-affected blocks bottom-to-top so they settle correctly
            // instead of cascading downward as each tile is placed from the top.
            if (initialSourceItem.createTile >= TileID.Dirt && Main.tileSand[initialSourceItem.createTile])
            {
                Array.Sort(tilesToProcess, (a, b) => b.Y.CompareTo(a.Y));
            }

            int required = tilesToProcess.Length;

            // --- Cancel mode: pre-check total stock before touching anything ---
            if (settings.ExhaustionMode == BlockExhaustionMode.Cancel)
            {
                Item firstSourceItem = player.inventory[sourceIndex];
                bool usingTileWandForCheck = firstSourceItem.tileWand >= 0;
                Func<Item, bool> checkCond = usingTileWandForCheck
                    ? i => !i.IsAir && i.type == firstSourceItem.tileWand
                    : i => !i.IsAir && condition(i);

                bool hasInfinite = ItemTypeHelper.CountItems(player.inventory, checkCond, out int totalAvailable);
                if (!hasInfinite && totalAvailable < required)
                {
                    string itemName = usingTileWandForCheck
                        ? Lang.GetItemNameValue(firstSourceItem.tileWand)
                        : firstSourceItem.Name;
                    Main.NewText(Get("NeedHave", required, itemName, totalAvailable), Color.Red);
                    return;
                }
            }

            // Determine if consumption is needed (per-type check)
            // Must count the SPECIFIC item type (or tile wand ammo) — not the broad category.
            // Using the category-wide condition would sum all solid tiles (dirt + acidwood + stone…)
            // and incorrectly exceed the infinite threshold even when the individual item is scarce.
            bool shouldConsume = true;
            if (config.IsInfiniteForPlaceType(settings.Object))
            {
                bool usingTileWandForInf = initialSourceItem.tileWand >= 0;
                Func<Item, bool> infCheckCond = usingTileWandForInf
                    ? i => !i.IsAir && i.type == initialSourceItem.tileWand
                    : i => !i.IsAir && i.type == initialSourceItem.type;

                ItemTypeHelper.CountItems(player.inventory, infCheckCond, out int grandTotal);
                int threshold = config.GetThresholdForPlaceType(settings.Object);
                if (threshold == 0)
                    shouldConsume = false;
                else if (grandTotal >= threshold)
                    shouldConsume = false;
            }

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building");

            bool replaceMode = player.TileReplacementEnabled;

            // Branch: progressive batching for large operations
            bool useProgressive = config.EnableProgressiveMode
                && tilesToProcess.Length > config.ProgressiveBatchSize;

            if (useProgressive)
            {
                // Pre-validate tiles and snapshot them for progressive processing
                var buildTiles = new List<ProgressiveTileProcessor.TileBuildingInfo>();
                foreach (Point tile in tilesToProcess)
                {
                    if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                    if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                    var existingTile = Main.tile[tile.X, tile.Y];
                    if (existingTile.HasTile)
                    {
                        // Grass seed mode: don't destroy the substrate, just convert
                        if (settings.Object == PlaceType.GrassSeed)
                        {
                            // Grass seeds convert existing substrates (dirt→grass, mud→jungle grass, etc.)
                            // They are NOT replacements — they modify the surface of the block.
                            action.AddSnapshot(tile);
                            buildTiles.Add(new ProgressiveTileProcessor.TileBuildingInfo
                            {
                                Position = tile,
                                IsReplacement = false, // Not a replacement — it's a conversion
                                SuppressDrops = true,
                                IsGrassSeed = true,
                                PaintSprayer = settings.PaintSprayer,
                                Actuation = settings.Actuation
                            });
                            continue;
                        }

                        // Same tile type? Apply slope only — don't destroy+rebuild.
                        // This check runs BEFORE replaceMode: slope correction is non-destructive
                        // (no item consumed, no tile removed) so it should always be allowed.
                        int idx0 = ItemTypeHelper.FindFirstItemIndex(player, condition);
                        if (idx0 >= 0)
                        {
                            Item src0 = player.inventory[idx0];
                            if (existingTile.TileType == (ushort)src0.createTile)
                            {
                                // Same type — only queue for slope overwrite, never destroy.
                                if (settings.OverwriteSlope && NeedsSlopeChange(tile.X, tile.Y, settings.Slope))
                                {
                                    action.AddSnapshot(tile);
                                    buildTiles.Add(new ProgressiveTileProcessor.TileBuildingInfo
                                    {
                                        Position = tile,
                                        IsReplacement = false,
                                        SuppressDrops = true,
                                        IsSlopeOnly = true,
                                        PaintSprayer = settings.PaintSprayer,
                                        Actuation = settings.Actuation
                                    });
                                }
                                continue;
                            }

                            // Substrate-variant skip: if the existing tile is a grass/moss
                            // variant of the target substrate (e.g. JungleGrass is a variant
                            // of Mud), don't replace it — the user almost certainly does not
                            // want to strip the grass coating off tiles they're filling "with mud".
                            if (ItemTypeHelper.IsTileVariantOf(existingTile.TileType, src0.createTile))
                                continue;
                        }

                        if (!replaceMode) continue;

                        if (!config.EffectiveBypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
                        if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

                        action.AddSnapshot(tile);
                        buildTiles.Add(new ProgressiveTileProcessor.TileBuildingInfo
                        {
                            Position = tile,
                            IsReplacement = true,
                            SuppressDrops = config.EffectiveSuppressDrops,
                            PaintSprayer = settings.PaintSprayer,
                            Actuation = settings.Actuation
                        });
                    }
                    else
                    {
                        action.AddSnapshot(tile);
                        buildTiles.Add(new ProgressiveTileProcessor.TileBuildingInfo
                        {
                            Position = tile,
                            IsReplacement = false,
                            SuppressDrops = true,
                            PaintSprayer = settings.PaintSprayer,
                            Actuation = settings.Actuation
                        });
                    }
                }

                if (buildTiles.Count == 0)
                {
                    Main.NewText(Get("NoTilesPlaced"), Color.Gray);
                    return;
                }

                // -- Unbatching Phase 1: Instant placement for non-replacement tiles --
                // Tiles that place into empty space, apply slope-only changes, or convert
                // grass seeds are safe to process synchronously — they never call KillTile.
                // Only true replacements (IsReplacement=true) need progressive batching.
                var immediateTiles = buildTiles.Where(t => !t.IsReplacement).ToList();
                var batchTiles = buildTiles.Where(t => t.IsReplacement).ToList();

                int instantPlaced = 0;
                var instantPositions = new List<Point>();

                if (immediateTiles.Count > 0)
                {
                    bool savedGen = WorldGen.gen;

                    foreach (var info in immediateTiles)
                    {
                        int x = info.Position.X, y = info.Position.Y;

                        if (info.IsSlopeOnly)
                        {
                            if (settings.OverwriteSlope)
                                ApplySlope(x, y, settings.Slope);
                            instantPlaced++;
                            instantPositions.Add(info.Position);
                            continue;
                        }

                        int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                        if (idx < 0)
                        {
                            if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) break;
                            continue;
                        }
                        Item srcItem = player.inventory[idx];
                        ushort tType = (ushort)srcItem.createTile;

                        WorldGen.gen = true;
                        if (WorldGen.PlaceTile(x, y, tType, mute: true, forced: false,
                            plr: player.whoAmI, style: srcItem.placeStyle))
                        {
                            if (settings.OverwriteSlope)
                                ApplySlope(x, y, settings.Slope);
                            ApplyActuation(x, y, info.Actuation);
                            if (info.PaintSprayer)
                                ApplyPaintSprayerTile(player, x, y, shouldConsume);
                            instantPlaced++;
                            if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                            instantPositions.Add(info.Position);
                        }
                        WorldGen.gen = savedGen;
                    }

                    if (instantPositions.Count > 0)
                        BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(instantPositions));
                }

                if (batchTiles.Count > 0)
                {
                    int batchSize = config.ProgressiveBatchSize;
                    float interval = config.ProgressiveInterval;

                    ProgressiveTileProcessor.EnqueueBuilding(
                        player, batchTiles, action, undoMgr,
                        batchSize, interval, shouldConsume, condition,
                        settings.OverwriteSlope, settings.Slope,
                        vacuumItems: config.VacuumItems);

                    int batches = (int)Math.Ceiling((double)batchTiles.Count / batchSize);
                    float totalTime = (batches - 1) * interval;

                    string msg = instantPlaced > 0
                        ? $"Placed {instantPlaced} tile(s) instantly, replacing {batchTiles.Count} in {batches} wave(s) (~{totalTime:F1}s)"
                        : $"Replacing {batchTiles.Count} tile(s) in {batches} wave(s) (~{totalTime:F1}s)";
                    if (!shouldConsume) msg += " (no items consumed)";
                    Main.NewText(msg, Color.Cyan);
                }
                else
                {
                    // All tiles were non-replacement — commit undo action immediately
                    undoMgr.CommitAction(action);

                    string detail = $"Placed {instantPlaced} tile(s)";
                    if (!shouldConsume) detail += " (no items consumed)";
                    Main.NewText(detail, Color.Green);
                }
                return;
            }

            // Instant mode: process all at once
            int placed = 0;
            int replaced = 0;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            // WorldGen.gen suppresses per-tile sounds, dust, gore, AND tile drops.
            // For placements (empty → tile) this is always fine (no drops expected).
            // For replacements (tile → tile), we only suppress when config says so,
            // otherwise the old tile should drop its items naturally.
            bool wasGen = WorldGen.gen;
            bool suppressDrops = config.EffectiveSuppressDrops;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

                // Skip protected positions
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                var existingTile = Main.tile[tile.X, tile.Y];

                if (existingTile.HasTile)
                {
                    // Re-lookup source item for this tile (supports NextBlock)
                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                        continue; // NextBlock: no more items, skip remaining
                    }

                    Item srcItem = player.inventory[idx];
                    ushort tType = (ushort)srcItem.createTile;

                    // === GRASS SEED SPECIAL PATH ===
                    // Grass seeds convert existing substrates (dirt→grass, mud→jungle, etc.)
                    // They don't destroy the underlying block — they modify its surface.
                    if (settings.Object == PlaceType.GrassSeed)
                    {
                        action.AddSnapshot(tile);
                        WorldGen.gen = true;
                        if (WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false,
                            plr: player.whoAmI, style: srcItem.placeStyle))
                        {
                            ApplyActuation(tile.X, tile.Y, settings.Actuation);
                            if (settings.PaintSprayer)
                                ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume);
                            placed++;
                            if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                            affectedPositions.Add(tile);
                        }
                        WorldGen.gen = wasGen;
                        continue;
                    }

                    // Same tile type? Check if we need a slope change.
                    // Don't destroy+rebuild just to change the slope — apply in-place.
                    // This check runs BEFORE replaceMode: slope correction is non-destructive
                    // (no item consumed, no tile removed) so it should always be allowed.
                    if (existingTile.TileType == tType)
                    {
                        // Tile is the same type — but if OverwriteSlope is on and the slope differs,
                        // apply the new slope in-place without consuming an item.
                        if (settings.OverwriteSlope && NeedsSlopeChange(tile.X, tile.Y, settings.Slope))
                        {
                            action.AddSnapshot(tile);
                            ApplySlope(tile.X, tile.Y, settings.Slope);
                            affectedPositions.Add(tile);
                            replaced++;
                        }
                        continue; // Either slope was applied or nothing to do
                    }

                    // Substrate-variant skip: if the existing tile is a grass/moss
                    // variant of the target substrate (e.g. JungleGrass is a variant
                    // of Mud), don't replace it — the user almost certainly does not
                    // want to strip the grass coating off tiles they're filling "with mud".
                    if (ItemTypeHelper.IsTileVariantOf(existingTile.TileType, tType))
                        continue;

                    if (!replaceMode) continue;

                    if (!config.EffectiveBypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
                    if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);

                    // Replace path: only suppress effects when drops are suppressed
                    WorldGen.gen = suppressDrops;

                    bool didReplace = false;
                    // TileID.Dirt == 0: WorldGen.ReplaceTile treats tileType=0 as "no tile",
                    // so skip it and go straight to KillTile+PlaceTile for Dirt targets.
                    if (tType != 0 && WorldGen.ReplaceTile(tile.X, tile.Y, tType, srcItem.placeStyle))
                    {
                        didReplace = true;
                    }
                    else
                    {
                        WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: suppressDrops);
                        if (!Main.tile[tile.X, tile.Y].HasTile &&
                            WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI, style: srcItem.placeStyle))
                        {
                            didReplace = true;
                        }
                    }

                    // Restore gen for next iteration
                    WorldGen.gen = wasGen;

                    if (didReplace)
                    {
                        if (settings.OverwriteSlope)
                            ApplySlope(tile.X, tile.Y, settings.Slope);
                        ApplyActuation(tile.X, tile.Y, settings.Actuation);
                        if (settings.PaintSprayer)
                            ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume);
                        replaced++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        affectedPositions.Add(tile);
                    }
                }
                else
                {
                    // Re-lookup source item for this tile (supports NextBlock)
                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                        continue; // NextBlock: no more items, skip remaining
                    }

                    Item srcItem = player.inventory[idx];
                    ushort tType = (ushort)srcItem.createTile;

                    // Placement path: always suppress effects (no drops from empty space)
                    WorldGen.gen = true;

                    action.AddSnapshot(tile);
                    if (WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI, style: srcItem.placeStyle))
                    {
                        if (settings.OverwriteSlope)
                            ApplySlope(tile.X, tile.Y, settings.Slope);
                        ApplyActuation(tile.X, tile.Y, settings.Actuation);
                        if (settings.PaintSprayer)
                            ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume);
                        placed++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        affectedPositions.Add(tile);
                    }

                    WorldGen.gen = wasGen;
                }
            }

            // Restore WorldGen.gen state (safety — already restored per-iteration)
            WorldGen.gen = wasGen;

            // Batch network sync: one packet instead of N per-tile messages
            if (affectedPositions.Count > 0)
            {
                BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(affectedPositions));

                // Vacuum: collect scattered tile drops into the player's inventory.
                // Tile replacement (KillTile) creates item drops when SuppressDrops is false.
                if (config.VacuumItems && !config.EffectiveSuppressDrops && replaced > 0)
                {
                    var bounds = BulkTileOperations.ComputeBounds(affectedPositions);
                    BulkTileOperations.VacuumItemsInArea(player, bounds);
                }
            }

            int totalChanged = placed + replaced;
            if (totalChanged > 0)
            {
                undoMgr.CommitAction(action);

                // Play completion sound — provides audio feedback even when
                // SuppressDrops is ON (which silences all per-tile sounds).
                if (clientCfg?.EnableWandSounds == true)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, player.Center);

                string detail;
                if (placed > 0 && replaced > 0)
                    detail = $"Placed {placed}, replaced {replaced}";
                else if (replaced > 0)
                    detail = $"Replaced {replaced}";
                else
                    detail = $"Placed {placed}";

                if (!shouldConsume) detail += " (no items consumed)";
                if (interrupted) detail += " — ran out of blocks";
                Main.NewText(detail, Color.Cyan);
            }
            else
            {
                Main.NewText(Get("NoTilesPlaced"), Color.Gray);
            }
        }

        /// <summary>
        /// Wall placement path. Walls use createWall, PlaceWall, KillWall — 
        /// a fundamentally different API from tile placement.
        /// Supports replace mode (replacing existing walls) and standard placement.
        /// </summary>
        private void ExecuteWallBuilding(Player player, WandPlayer wandPlayer,
            WandOfBuildingSettings settings, SelectionState selection, WandServerConfig config, WandClientConfig clientCfg)
        {
            var condition = ItemTypeHelper.GetConditions(PlaceType.Wall);

            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition);
            if (sourceIndex < 0)
            {
                Main.NewText(Get("NoWallItemFound"), Color.Red);
                return;
            }

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
                    settings.ExhaustionMode, player.TileReplacementEnabled,
                    (short)mpItem.type, (short)mpItem.placeStyle,
                    settings.Shape.Slice, settings.Shape.ConnectDiameter,
                    settings.Shape.InvertSelection,
                    settings.PaintSprayer, settings.Actuation);
                return;
            }

            var context = settings.Shape.ToShapeContext(
                selection.StartTile, selection.EndTile, selection.VerticalFirst);

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);
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

            if (settings.ExhaustionMode == BlockExhaustionMode.Cancel)
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
            bool useProgressive = config.EnableProgressiveMode
                && tilesToProcess.Length > config.ProgressiveBatchSize;

            if (useProgressive)
            {
                // Pre-validate walls and snapshot them for progressive processing
                var buildWalls = new List<ProgressiveTileProcessor.WallBuildingInfo>();
                foreach (Point tile in tilesToProcess)
                {
                    if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                    if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

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
                            SuppressDrops = config.EffectiveSuppressDrops,
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
                    Main.NewText(Get("NoWallsPlaced"), Color.Gray);
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
                            if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) break;
                            continue;
                        }
                        Item srcItem = player.inventory[idx];
                        ushort wallType = (ushort)srcItem.createWall;

                        WorldGen.gen = true;
                        WorldGen.PlaceWall(x, y, wallType, mute: true);
                        if (Main.tile[x, y].WallType == wallType)
                        {
                            if (info.PaintSprayer)
                                ApplyPaintSprayerWall(player, x, y, shouldConsume);
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
                    int batchSize = config.ProgressiveBatchSize;
                    float interval = config.ProgressiveInterval;

                    ProgressiveTileProcessor.EnqueueWallBuilding(
                        player, batchWalls, action, undoMgr,
                        batchSize, interval, shouldConsume, condition,
                        vacuumItems: config.VacuumItems);

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
                return;
            }

            // Instant mode: process all at once
            int placed = 0;
            int replaced = 0;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            bool wasGen = WorldGen.gen;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                var t = Main.tile[tile.X, tile.Y];

                int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                if (idx < 0)
                {
                    if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
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
                    WorldGen.gen = config.EffectiveSuppressDrops;
                    WorldGen.KillWall(tile.X, tile.Y, fail: false);
                    if (t.WallType == WallID.None)
                    {
                        WorldGen.PlaceWall(tile.X, tile.Y, wallType, mute: true);
                        if (t.WallType == wallType)
                        {
                            if (settings.PaintSprayer)
                                ApplyPaintSprayerWall(player, tile.X, tile.Y, shouldConsume);
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
                        if (settings.PaintSprayer)
                            ApplyPaintSprayerWall(player, tile.X, tile.Y, shouldConsume);
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
                if (config.VacuumItems && !config.EffectiveSuppressDrops && replaced > 0)
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
                Main.NewText(Get("NoWallsPlaced"), Color.Gray);
            }
        }

        /// <summary>
        /// Consumes one item from the player's inventory for the given source item.
        /// Handles tile wand ammo vs. direct consumption.
        /// </summary>
        private static void ConsumeOneItem(Player player, Item sourceItem, Func<Item, bool> placeCondition)
        {
            bool usingTileWand = sourceItem.tileWand >= 0;
            Func<Item, bool> consumeCond = usingTileWand
                ? i => !i.IsAir && i.type == sourceItem.tileWand
                : i => !i.IsAir && i.type == sourceItem.type;

            ItemTypeHelper.ConsumeItems(player.inventory, consumeCond, 1);
        }

        /// <summary>
        /// Checks if the tile at (x, y) has a different slope than the requested one.
        /// Used to determine if an in-place slope update is needed when the tile type matches.
        /// </summary>
        private static bool NeedsSlopeChange(int x, int y, SlopeType slope)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return false;

            return slope switch
            {
                SlopeType.Default => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.Solid,
                SlopeType.VerticalHalf => !tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.Solid,
                SlopeType.BottomRight => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeDownLeft,
                SlopeType.BottomLeft => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeDownRight,
                SlopeType.TopRight => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeUpRight,
                SlopeType.TopLeft => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeUpLeft,
                _ => false
            };
        }

        /// <summary>
        /// Applies the selected slope/half-block setting to a placed tile.
        /// Always applies the selected slope type, including forcing full-block for SlopeType.Default.
        /// </summary>
        private static void ApplySlope(int x, int y, SlopeType slope)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;

            if (slope == SlopeType.Default)
            {
                // Force full block (clear any slope/half-block)
                tile.IsHalfBlock = false;
                tile.Slope = Terraria.ID.SlopeType.Solid;
            }
            else if (slope == SlopeType.VerticalHalf)
            {
                tile.IsHalfBlock = true;
                tile.Slope = Terraria.ID.SlopeType.Solid; // clear any slope when setting half-block
            }
            else
            {
                tile.IsHalfBlock = false;

                tile.Slope = slope switch
                {
                    SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                    SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                    SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpRight,
                    SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpLeft,
                    _ => Terraria.ID.SlopeType.Solid
                };
            }

            // Update tile frame so collision and visuals are immediately correct
            // (without this, sloped platforms act as solid blocks until an adjacent tile update)
            WorldGen.SquareTileFrame(x, y);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Right-click
            {
                var wandPlayer = player.GetModPlayer<WandPlayer>();
                if (wandPlayer.Selection.IsActive)
                {
                    CancelSelection(wandPlayer);
                }
                else if (Main.myPlayer == player.whoAmI)
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            // Only the Instant variant has a craftable recipe.
            // Other modes are obtained via right-click cycling in inventory.
        }

        // ── Paint Sprayer helper ──────────────────────────────────────

        /// <summary>
        /// Finds the first paint item in the player's inventory and returns its paint ID (1–30).
        /// Returns 0 if no paint is found.
        /// </summary>
        internal static byte FindPaintInInventory(Player player)
        {
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && item.paint > 0)
                    return item.paint;
            }
            return 0;
        }

        /// <summary>
        /// Applies paint from inventory to a freshly placed tile at (x, y).
        /// Consumes one paint item unless infinite resources are active.
        /// </summary>
        internal static void ApplyPaintSprayerTile(Player player, int x, int y, bool shouldConsume, HashSet<int> changedSlots = null)
        {
            byte paintId = FindPaintInInventory(player);
            if (paintId == 0) return;

            WorldGen.paintTile(x, y, paintId, true);

            if (shouldConsume)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item item = player.inventory[i];
                    if (!item.IsAir && item.paint == paintId)
                    {
                        item.stack--;
                        if (item.stack <= 0) item.TurnToAir();
                        changedSlots?.Add(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Applies paint from inventory to a freshly placed wall at (x, y).
        /// Consumes one paint item unless infinite resources are active.
        /// </summary>
        internal static void ApplyPaintSprayerWall(Player player, int x, int y, bool shouldConsume, HashSet<int> changedSlots = null)
        {
            byte paintId = FindPaintInInventory(player);
            if (paintId == 0) return;

            WorldGen.paintWall(x, y, paintId, true);

            if (shouldConsume)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item item = player.inventory[i];
                    if (!item.IsAir && item.paint == paintId)
                    {
                        item.stack--;
                        if (item.stack <= 0) item.TurnToAir();
                        changedSlots?.Add(i);
                        break;
                    }
                }
            }
        }

        // ── Actuation helper ─────────────────────────────────────────

        /// <summary>
        /// Applies the actuation setting to a tile at (x, y).
        /// <c>null</c> = ignore (leave as-is), <c>true</c> = actuate, <c>false</c> = de-actuate.
        /// </summary>
        internal static void ApplyActuation(int x, int y, bool? actuation)
        {
            if (actuation == null) return;
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;
            tile.IsActuated = actuation.Value;
        }
    }
}