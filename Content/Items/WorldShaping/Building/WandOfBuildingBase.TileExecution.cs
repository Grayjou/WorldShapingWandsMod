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
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract partial class WandOfBuildingBase
    {
        protected void ExecuteBuilding(Player player, WandPlayer wandPlayer)
        {
            var settings = wandPlayer.BuildingSettings;
            var selection = wandPlayer.GetVisualSelection();
            var config = WandConfigs.Resources;
            var sandbox = WandConfigs.Sandbox;
            var perfConfig = WandConfigs.Performance;
            var clientCfg = WandConfigs.Preferences;

            // Wall mode uses a completely separate placement path
            if (settings.Object == PlaceType.Wall)
            {
                ExecuteWallBuilding(player, wandPlayer, settings, selection, config, sandbox, perfConfig, clientCfg);
                return;
            }

            // Empty object mode: edit existing tiles' attributes only (slope/actuation),
            // without placing/replacing tiles or consuming resources.
            if (settings.Object == PlaceType.None)
            {
                bool canApplySlope = settings.OverwriteSlope;
                bool canApplyActuation = settings.Actuation != null;
                if (!canApplySlope && !canApplyActuation)
                {
                    Main.NewText("Nothing to apply — set Slope or Actuation first.", Color.Gray);
                    return;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    BuildingPacketHandler.SendBuildingOperation(
                        selection.StartTile, selection.EndTile,
                        settings.Shape.Shape, settings.Shape.FillMode,
                        settings.Shape.Thickness, settings.Shape.EqualDimensions,
                        selection.VerticalFirst, player.whoAmI,
                        settings.Object, settings.Slope, settings.OverwriteSlope,
                        WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock, false,
                        0, 0,
                        settings.Shape.Slice, settings.Shape.ConnectDiameter,
                        settings.Shape.InvertSelection,
                        paintSprayer: false, actuation: settings.Actuation);
                    return;
                }

                ExecuteTileAttributeOnlyInstant(player, wandPlayer, settings, selection, clientCfg);
                return;
            }

            // ── Multiplayer: send packet to server instead of executing locally ──
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Find source item for the packet (client-side pre-validation)
                var mpBaseCondition = ItemTypeHelper.GetConditions(settings.Object);
                Func<Item, bool> mpCondition = item => mpBaseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);
                int mpIdx = ItemTypeHelper.FindFirstItemIndex(player, mpCondition, settings.GetChosenTileItemType(settings.Object));
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
                    WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock, player.TileReplacementEnabled,
                    (short)mpItem.type, (short)mpItem.placeStyle,
                    settings.Shape.Slice, settings.Shape.ConnectDiameter,
                    settings.Shape.InvertSelection,
                    settings.PaintSprayer.IsActive(), settings.Actuation);
                return;
            }

            // Get the condition for the selected object type, filtering out multi-tile objects
            // so they are silently skipped (e.g., Furnace is ignored, Wood is used instead).
            var baseCondition = ItemTypeHelper.GetConditions(settings.Object);
            Func<Item, bool> condition = item => baseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);

            // Find the first matching placement item (block item or tile wand).
            // InventoryView v1 (S6 2026-04-22): if the player has chosen a tile type
            // via the InventoryView panel, prefer that exact item before the broad
            // scan order. Stale choices (item gone, mode mismatch) fall back silently.
            // (S1 2026-04-26 fix): choice is keyed by PlaceType so Platform mode
            // cannot bleed a Solid-mode choice through.
            int? chosenTileItemType = settings.GetChosenTileItemType(settings.Object);
            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition, chosenTileItemType);
            if (sourceIndex < 0)
            {
                Main.NewText(Get("NoSuitableItem", settings.Object), Color.Red);
                return;
            }

            Item initialSourceItem = player.inventory[sourceIndex];

            // ── ExhaustMode / Choice: narrow `condition` to the FIRST source item type ──
            //
            // Two reasons to narrow (post-S8 GrayJou Letter #9 fix):
            //   (a) ExhaustMode != NextBlock — the user explicitly disabled substitution
            //       so the broad category condition ("any solid block") would defeat that.
            //   (b) `choiceHonored` — the user chosen a specific item via the InventoryView panel
            //       AND that choice is currently in inventory (FindFirstItemIndex above returned
            //       the chosen item). Choices are AUTHORITATIVE: the user clicked a slot saying
            //       "place this exact item", so NextBlock substitution must be suppressed for
            //       this operation. If the choice runs out mid-operation the per-iteration
            //       lookup will return -1 and ExhaustMode's existing skip/break logic takes
            //       over (NextBlock with a narrowed condition behaves like "skip remaining",
            //       which is the correct fallback — silently substituting cactus for chosen
            //       Adamantite Ore would be a worse outcome).
            //
            // If the choice was stale (item gone), FindFirstItemIndex fell through to the broad
            // scan and `initialSourceItem` is something else; choiceHonored stays false and
            // we retain pre-choice substitution behaviour for that operation.
            //
            // This single narrowing point is what makes the fix surgical: the narrowed
            // `condition` propagates to every downstream FindFirstItemIndex call (slope
            // pre-check, immediate-tile loop, batched replacement loop, instant placement
            // path) AND to ProgressiveTileProcessor via the BuildCondition field, so we
            // don't have to thread the choice through ~8 separate call sites.
            var exhaustMode = WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock;
            bool choiceHonored = chosenTileItemType.HasValue
                              && initialSourceItem.type == chosenTileItemType.Value;

            // 2026-04-23 Session 1 (Letter #10 §6 bug): Interrupt + stale-choice hard-stop.
            // GrayJou intent: "Using Exhaust Mode Interrupt, and I have the view open
            // with a chosen item that I currently don't have in my inventory, it uses
            // the next available item. This isn't intuitive at all, it should not proceed."
            // Interpretation: Interrupt = "use ONLY my choice; stop the moment it runs out".
            // If the choice isn't even present at execute time, that's "already out", so we
            // refuse before any placement runs. NextBlock keeps silent substitution
            // (that's its whole point — the ghost-choice toast still fires as a hint).
            //
            // 2026-04-23 Session 2 (Letter #11 ack): Cavendish's S1 patch left an open
            // question — should Cancel ALSO hard-refuse on stale choice? GrayJou: "Yes."
            // Both Interrupt and Cancel treat the choice as authoritative intent; the only
            // mode that may silently substitute is NextBlock. So the guard now covers
            // both. The pre-existing broad stock pre-check downstream is now a strict
            // backstop (it would only trip on stock-shortage of the substitute under
            // NextBlock, since Cancel exits here first whenever the choice is stale).
            if ((exhaustMode == BlockExhaustionMode.Interrupt
                    || exhaustMode == BlockExhaustionMode.Cancel)
                && chosenTileItemType.HasValue
                && !choiceHonored)
            {
                Main.NewText(Get("ChosenMissingUnderInterrupt"), Color.Red);
                return;
            }

            if (exhaustMode != BlockExhaustionMode.NextBlock || choiceHonored)
            {
                // Narrow placement lookup to the exact chosen placement item.
                // IMPORTANT: for tile wands this must stay the wand item type (not ammo type),
                // otherwise downstream placement lookups resolve to ammo blocks (e.g., Wood)
                // and place the wrong tile instead of the wand's createTile.
                // Ammo consumption remains handled separately by ConsumeOneItem / stock checks.
                int chosenPlacementItemType = initialSourceItem.type;
                condition = i => !i.IsAir && i.type == chosenPlacementItemType;
            }

            // S10 (Letter #10 polish — ghost-choice toast, second on Cavendish's
            // re-ranked Phase 2 polish list): if the user chosen a specific
            // tile type but it wasn't in inventory at execute time, surface a
            // rate-limited chat hint so the substitution isn't silent. The
            // toast is keyed and throttled in GhostChoiceToast so a long
            // click-burst against a stale choice only fires once per ~2 s.
            Common.UI.InventoryView.GhostChoiceToast.TryEmit(
                chosenTileItemType, initialSourceItem.type);

            // Build shape context (includes slice & connectDiameter from settings)
            var context = settings.Shape.ToShapeContext(
                selection.StartTile, selection.EndTile, selection.VerticalFirst);

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

            // Filter by active tile selection (Select Wand integration)
            var swp = player.GetModPlayer<DelimitationWandPlayer>();
            tilesToProcess = swp.FilterBySelection(tilesToProcess);

            // Sort gravity-affected blocks bottom-to-top so they settle correctly
            // instead of cascading downward as each tile is placed from the top.
            if (initialSourceItem.createTile >= TileID.Dirt && Main.tileSand[initialSourceItem.createTile])
            {
                Array.Sort(tilesToProcess, (a, b) => b.Y.CompareTo(a.Y));
            }

            int required = tilesToProcess.Length;

            // --- Cancel mode: pre-check total stock before touching anything ---
            if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Cancel)
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
            bool useProgressive = perfConfig.EnableProgressiveMode
                && tilesToProcess.Length > perfConfig.ProgressiveBatchSize;

            if (useProgressive)
            {
                ExecuteTileBuildingProgressive(
                    player, settings, config, sandbox, perfConfig, condition, tilesToProcess,
                    shouldConsume, replaceMode, undoMgr, action);
                return;
            }

            ExecuteTileBuildingInstant(
                player, settings, config, sandbox, perfConfig, clientCfg, condition, tilesToProcess,
                shouldConsume, replaceMode, undoMgr, action);
        }

        private void ExecuteTileAttributeOnlyInstant(
            Player player,
            WandPlayer wandPlayer,
            WandOfBuildingSettings settings,
            SelectionState selection,
            PreferencesConfig clientCfg)
        {
            var context = settings.Shape.ToShapeContext(
                selection.StartTile, selection.EndTile, selection.VerticalFirst);

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

            var swp = player.GetModPlayer<DelimitationWandPlayer>();
            tilesToProcess = swp.FilterBySelection(tilesToProcess);

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building");
            var affectedPositions = new List<Point>();

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1))
                    continue;
                if (SafekeepingSystem.IsTileProtected(tile.X, tile.Y))
                    continue;

                var existingTile = Main.tile[tile.X, tile.Y];
                if (!existingTile.HasTile)
                    continue;

                bool needsSlope = settings.OverwriteSlope && NeedsSlopeChange(tile.X, tile.Y, settings.Slope);
                bool needsActuation = settings.Actuation != null && existingTile.IsActuated != settings.Actuation.Value;

                if (!needsSlope && !needsActuation)
                    continue;

                action.AddSnapshot(tile);
                if (needsSlope)
                    ApplySlope(tile.X, tile.Y, settings.Slope);
                if (needsActuation)
                    ApplyActuation(tile.X, tile.Y, settings.Actuation);

                affectedPositions.Add(tile);
            }

            if (affectedPositions.Count > 0)
            {
                undoMgr.CommitAction(action);
                BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(affectedPositions));

                if (clientCfg?.EnableWandSounds == true)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, player.Center);

                Main.NewText($"Updated {affectedPositions.Count} tile attribute(s).", Color.Cyan);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoTilesPlaced", Color.Gray);
            }
        }

        /// <summary>
        /// Progressive batching path for tile building. Pre-validates tiles and enqueues
        /// non-replacement tiles for instant processing; replacements go to the
        /// <see cref="ProgressiveTileProcessor"/> queue.
        /// </summary>
        private void ExecuteTileBuildingProgressive(
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
            // Pre-validate tiles and snapshot them for progressive processing
            var buildTiles = new List<ProgressiveTileProcessor.TileBuildingInfo>();
            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsTileProtected(tile.X, tile.Y)) continue;

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
                        if (existingTile.TileType == (ushort)src0.createTile
                            && ItemTypeHelper.IsSameTileStyle(existingTile, src0.placeStyle))
                        {
                            // Same type AND same style — apply in-place modifications (slope/actuation/paint).
                            // Never destroy+rebuild. IsSlopeOnly is a legacy name; it now covers all three.
                            bool sameNeedsSlope     = settings.OverwriteSlope && NeedsSlopeChange(tile.X, tile.Y, settings.Slope);
                            bool sameNeedsActuation = settings.Actuation != null;
                            bool sameNeedsPaint     = settings.PaintSprayer.IsActive();

                            if (sameNeedsSlope || sameNeedsActuation || sameNeedsPaint)
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
                        // Guard: skip this check when types match (same-type-different-style
                        // should fall through to replacement, e.g., Stone Platform → Solar Platform).
                        if (existingTile.TileType != (ushort)src0.createTile
                            && ItemTypeHelper.IsTileVariantOf(existingTile.TileType, src0.createTile))
                            continue;
                    }

                    if (!replaceMode) continue;

                    if (!sandbox.EffectiveBypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
                    if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);
                    buildTiles.Add(new ProgressiveTileProcessor.TileBuildingInfo
                    {
                        Position = tile,
                        IsReplacement = true,
                        SuppressDrops = sandbox.EffectiveSuppressDrops,
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
                ShowNullResult(wandPlayer, "NoTilesPlaced", Color.Gray);
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
                        if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Interrupt) break;
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
                        ApplyPaintSprayerTile(player, x, y, shouldConsume, info.PaintSprayer);
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
                int batchSize = perfConfig.ProgressiveBatchSize;
                float interval = perfConfig.ProgressiveInterval;

                ProgressiveTileProcessor.EnqueueBuilding(
                    player, batchTiles, action, undoMgr,
                    batchSize, interval, shouldConsume, condition,
                    settings.OverwriteSlope, settings.Slope,
                    vacuumItems: sandbox.VacuumItems);

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
        }

        /// <summary>
        /// Instant (non-progressive) path for tile building. Processes all tiles
        /// synchronously in a single frame.
        /// </summary>
        private void ExecuteTileBuildingInstant(
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
            int placed = 0;
            int replaced = 0;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            // WorldGen.gen suppresses per-tile sounds, dust, gore, AND tile drops.
            // For placements (empty → tile) this is always fine (no drops expected).
            // For replacements (tile → tile), we only suppress when config says so,
            // otherwise the old tile should drop its items naturally.
            bool wasGen = WorldGen.gen;
            bool suppressDrops = sandbox.EffectiveSuppressDrops;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

                // Skip protected positions
                if (SafekeepingSystem.IsTileProtected(tile.X, tile.Y)) continue;

                var existingTile = Main.tile[tile.X, tile.Y];

                if (existingTile.HasTile)
                {
                    // Re-lookup source item for this tile (supports NextBlock)
                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
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
                            ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
                            placed++;
                            if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                            affectedPositions.Add(tile);
                        }
                        WorldGen.gen = wasGen;
                        continue;
                    }

                    // Same tile type? Apply in-place modifications (slope, actuation, paint) without
                    // destroying + rebuilding. None of these consume the tile item.
                    // This check runs BEFORE replaceMode: these are all non-destructive edits.
                    if (existingTile.TileType == tType
                        && ItemTypeHelper.IsSameTileStyle(existingTile, srcItem.placeStyle))
                    {
                        bool needsSlope      = settings.OverwriteSlope && NeedsSlopeChange(tile.X, tile.Y, settings.Slope);
                        bool needsActuation  = settings.Actuation != null;
                        bool needsPaint      = settings.PaintSprayer.IsActive();

                        if (needsSlope || needsActuation || needsPaint)
                        {
                            action.AddSnapshot(tile);
                            if (needsSlope)     ApplySlope(tile.X, tile.Y, settings.Slope);
                            if (needsActuation) ApplyActuation(tile.X, tile.Y, settings.Actuation);
                            if (needsPaint)     ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
                            affectedPositions.Add(tile);
                            replaced++;
                        }
                        continue; // In-place edits done (or nothing to do) — never destroy+rebuild same type
                    }

                    // Substrate-variant skip: if the existing tile is a grass/moss
                    // variant of the target substrate (e.g. JungleGrass is a variant
                    // of Mud), don't replace it — the user almost certainly does not
                    // want to strip the grass coating off tiles they're filling "with mud".
                    // Guard: skip this check when types match (same-type-different-style
                    // should fall through to replacement, e.g., Stone Platform → Solar Platform).
                    if (existingTile.TileType != tType
                        && ItemTypeHelper.IsTileVariantOf(existingTile.TileType, tType))
                        continue;

                    if (!replaceMode) continue;

                    if (!sandbox.EffectiveBypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
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
                        ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
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
                        if ((WandConfigs.Preferences?.BlockExhaustion ?? BlockExhaustionMode.NextBlock) == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
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
                        ApplyPaintSprayerTile(player, tile.X, tile.Y, shouldConsume, settings.PaintSprayer);
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
                if (sandbox.VacuumItems && !sandbox.EffectiveSuppressDrops && replaced > 0)
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
                ShowNullResult(player.GetModPlayer<WandPlayer>(), "NoTilesPlaced", Color.Gray);
            }
        }
    }
}
