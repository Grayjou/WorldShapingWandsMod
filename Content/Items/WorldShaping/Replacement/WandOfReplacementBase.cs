using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfReplacementBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Replacement/{Name}";
    public override string WandBaseName => "Wand of Replacement";
    public override string WandLore => Get("LoreReplacement");

    // ── Template Method Pattern ────────────────────────────────────────
    protected override WandFamily Family => WandFamily.Replacement;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
        => WandAction.Replacement;

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ModContent.ItemType<WandOfBuildingInstant>(), 1)
            .AddCustomShimmerResult(ModContent.ItemType<WandOfDismantlingInstant>(), 1)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteReplacement(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.ReplacementSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Show cursor icons: source → target
        DrawReplacementCursorIcons(player, wandPlayer);
    }

    public override bool? UseItem(Player player)
    {
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        TemplateHoldItem(player);
    }

    /// <summary>
    /// Shows the target tile icon next to the cursor.
    /// Note: Terraria's cursorItemIcon only supports a single item.
    /// The source tile is identified by the first matching item in inventory,
    /// so the cursor shows what it will be REPLACED WITH.
    /// </summary>
    private void DrawReplacementCursorIcons(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.ReplacementSettings;

        // Resolve source and target items
        var (sourceItem, targetItem) = ResolveReplacementPair(player, settings);

        // Show target item (what tiles will become) as the cursor icon.
        // Terraria only supports one cursorItemIcon, so we show the target.
        // The source is implicitly "the first [OldObject] item in your inventory".
        if (targetItem != null)
        {
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = targetItem.type;
            player.cursorItemIconPush = 26;
        }
        else if (settings.NewObject == ObjectType.Air && sourceItem != null)
        {
            // Target is Air (erase mode) — show source item so player knows what gets erased
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = sourceItem.type;
            player.cursorItemIconPush = 26;
        }
    }

    /// <summary>
    /// Resolves the source and target items for replacement, ensuring they are different tiles/walls.
    /// Source = first inventory item matching OldObject category.
    /// Target = first inventory item matching NewObject category, skipping items with same placement as source.
    /// </summary>
    private static (Item source, Item target) ResolveReplacementPair(Player player, WandOfReplacementSettings settings)
    {
        // Find source item (what tiles/walls to look for in the world).
        // SameType mode is target-driven: the target IV choice is authoritative.
        var srcCondition = ItemTypeHelper.GetConditions(settings.OldObject);
        int? sourceChoice = settings.SameTypeMode
            ? settings.GetChosenTargetItemType(settings.NewObject)
            : settings.GetChosenSourceItemType(settings.OldObject);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition, sourceChoice);

        if (sourceItem == null)
            return (null, null);

        bool isWallMode = settings.OldObject == ObjectType.Wall;

        // Find target item, but skip items that would place the same tile/wall as source.
        // SameTypeMode: target tracks the source choice (the wand is placing the same
        // type it just removed). Otherwise: honor ChosenTargetItemType.
        // S8 2026-04-22 (Cavendish Response #5 §9.1): when SameTypeMode is ON, track
        // the *resolved* sourceItem.type — NOT settings.ChosenSourceItemType. If the
        // source choice ghosted (item exhausted) and the pre-pass fell back to a different
        // type, the original choice field is stale; using it would have the wand remove B
        // and try to place A. Coherence violation. Tracking sourceItem.type means
        // "Same Type" always genuinely matches what we're actually picking up.
        Item targetItem = null;
        if (settings.NewObject != ObjectType.Air)
        {
            var tgtCondition = ItemTypeHelper.GetConditions(settings.NewObject);
            int? targetChoice = settings.GetChosenTargetItemType(settings.NewObject);

            if (isWallMode)
            {
                ushort sourceWall = (ushort)sourceItem.createWall;
                targetItem = ItemTypeHelper.FindFirstItem(player,
                    item => tgtCondition(item) && (ushort)item.createWall != sourceWall,
                    targetChoice);
            }
            else
            {
                ushort sourceCreateTile = (ushort)sourceItem.createTile;
                targetItem = ItemTypeHelper.FindFirstItem(player,
                    item => tgtCondition(item) && (ushort)item.createTile != sourceCreateTile,
                    targetChoice);
            }
        }

        return (sourceItem, targetItem);
    }

    protected void ExecuteReplacement(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.ReplacementSettings;
        var selection = wandPlayer.GetVisualSelection();
        var config = WandConfigs.Resources;
        var sandbox = WandConfigs.Sandbox;
        var perfConfig = WandConfigs.Performance;
        var clientCfg = WandConfigs.Preferences;

        // Wall replacement uses a completely separate path
        if (settings.OldObject == ObjectType.Wall || settings.NewObject == ObjectType.Wall)
        {
            ExecuteWallReplacement(player, wandPlayer, settings, selection, config, sandbox, perfConfig, clientCfg);
            return;
        }

        // --- Resolve source item: identifies the SPECIFIC tile type to find in the world ---
        // SameType mode is target-driven: the target IV choice is authoritative.
        var srcCondition = ItemTypeHelper.GetConditions(settings.OldObject);
        int? sourceChoice = settings.SameTypeMode
            ? settings.GetChosenTargetItemType(settings.NewObject)
            : settings.GetChosenSourceItemType(settings.OldObject);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition, sourceChoice);
        if (sourceItem == null)
        {
            Main.NewText(Get("NoSourceItems", settings.OldObject), WandColors.MsgError);
            return;
        }
        ushort sourceType = (ushort)sourceItem.createTile;

        // --- Resolve target: find item matching NewObject, but skip same tile as source ---
        Item targetItem = null;
        Func<Item, bool> targetCondition = null;
        ushort targetType = 0;
        if (settings.NewObject != ObjectType.Air)
        {
            var baseTgtCondition = ItemTypeHelper.GetConditions(settings.NewObject);
            // Exclude items that would place the same tile as source
            targetCondition = item => baseTgtCondition(item) && (ushort)item.createTile != sourceType;
            // SameTypeMode choices target to source choice (target tracks source).
            // S8 2026-04-22 (Cavendish Response #5 §9.1): track *resolved* sourceItem.type
            // not the choice field, so target stays coherent when source choice ghosts.
            int? targetChoice = settings.GetChosenTargetItemType(settings.NewObject);
            targetItem = ItemTypeHelper.FindFirstItem(player, targetCondition, targetChoice);

            if (targetItem == null)
            {
                // Check if there ARE items of the target type but they're all the same as source
                Item anyTarget = ItemTypeHelper.FindFirstItem(player, baseTgtCondition);
                if (anyTarget != null && (ushort)anyTarget.createTile == sourceType)
                    Main.NewText(Get("SameSourceTarget", anyTarget.type), WandColors.MsgInfo);
                else
                    Main.NewText(Get("NoTargetItems", settings.NewObject), WandColors.MsgError);
                return;
            }
            targetType = (ushort)targetItem.createTile;
        }

        // --- MP: send packet to server and return early ---
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ReplacementPacketHandler.SendReplacementOperation(
                selection.StartTile, selection.EndTile,
                settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                settings.OldObject, settings.NewObject,
                sourceType, targetType,
                (short)(targetItem?.type ?? 0), isWallMode: false,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection, settings.PaintSprayer);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swp.FilterBySelection(invertedTiles);

        // Count tiles that match the specific source tile type (including variants like grass→dirt)
        int needed = 0;
        foreach (Point tile in invertedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsTileProtected(tile.X, tile.Y)) continue;
            var t = Main.tile[tile.X, tile.Y];

            if (t.HasTile && ItemTypeHelper.IsTileVariantOf(t.TileType, sourceType))
                needed++;
        }

        if (needed == 0)
        {
            Main.NewText(Get("NoSourceInSelection", sourceItem.Name), WandColors.MsgInfo);
            return;
        }

        // Determine if consumption is needed (same rules as building wand)
        // Count only the SPECIFIC target item type — not all items in the category.
        bool shouldConsume = true;
        if (settings.NewObject != ObjectType.Air && config != null && config.IsInfiniteForObjectType(settings.NewObject))
        {
            Func<Item, bool> infCheckCond = i => !i.IsAir && i.type == targetItem.type;
            ItemTypeHelper.CountItems(player.inventory, infCheckCond, out int grandTotal);
            int threshold = config.GetThresholdForObjectType(settings.NewObject);
            if (threshold == 0)
                shouldConsume = false;
            else if (grandTotal >= threshold)
                shouldConsume = false;
        }

        // Check target item availability (non-Air, when consumption is needed)
        if (settings.NewObject != ObjectType.Air && shouldConsume)
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int available);
            if (available < needed)
            {
                Main.NewText($"Need {needed} [i:{targetItem.type}], have {available}.", WandColors.MsgError);
                return;
            }
        }

        var undoMgr = player.GetModPlayer<UndoManager>();
        string srcName = sourceItem.Name;
        string tgtName = settings.NewObject == ObjectType.Air ? "Air" : targetItem.Name;
        var action = undoMgr.BeginAction($"Replace {srcName} → {tgtName}");

        int replaced = 0;
        var affectedPositions = new List<Point>();

        // Pre-validate all tiles and snapshot them
        var validTiles = new List<ProgressiveTileProcessor.TileReplacementInfo>();
        foreach (Point tile in invertedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsTileProtected(tile.X, tile.Y)) continue;

            var t = Main.tile[tile.X, tile.Y];
            if (!t.HasTile || !ItemTypeHelper.IsTileVariantOf(t.TileType, sourceType)) continue;
            if (!sandbox.EffectiveBypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;

            // Use WorldGen.ReplaceTile first — it handles tiles under multi-tile objects
            // (chests, dressers, furniture) without destroying the object above.
            // Fall back to KillTile+PlaceTile only if ReplaceTile doesn't apply (e.g., erase to Air).
            // For validation: ReplaceTile targets always pass; Air targets need CanKillTile.
            if (settings.NewObject == ObjectType.Air && !WorldGen.CanKillTile(tile.X, tile.Y))
                continue;

            action.AddSnapshot(tile);

            validTiles.Add(new ProgressiveTileProcessor.TileReplacementInfo
            {
                Position = tile,
                SourceType = sourceType,
                TargetType = targetType,
                IsErase = settings.NewObject == ObjectType.Air,
                SuppressDrops = sandbox.EffectiveSuppressDrops,
                PaintSprayer = settings.PaintSprayer,
                PreservePaint = settings.PreservePaint
            });
        }

        if (validTiles.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesReplaced", WandColors.MsgInfo);
            return;
        }

        // Branch: progressive mode (with drops/sounds) vs instant mode (silent, no drops)
        bool useProgressive = perfConfig != null && perfConfig.EnableProgressiveMode;

        if (useProgressive)
        {
            // Progressive: enqueue batches for timed processing
            int batchSize = perfConfig.ProgressiveBatchSize;
            float interval = perfConfig.ProgressiveInterval;

            ProgressiveTileProcessor.EnqueueReplacement(
                player, validTiles, action, undoMgr, batchSize, interval,
                shouldConsume, targetCondition, sourceItem, targetItem, settings.NewObject,
                vacuumItems: sandbox.VacuumItems);

            int batches = (int)Math.Ceiling((double)validTiles.Count / batchSize);
            float totalTime = (batches - 1) * interval;
            string srcIcon = $"[i:{sourceItem.type}]";
            string tgtIcon = settings.NewObject == ObjectType.Air ? "Air" : $"[i:{targetItem.type}]";
            Main.NewText(
                $"Replacing {validTiles.Count}: {srcIcon} → {tgtIcon} in {batches} wave(s) (~{totalTime:F1}s)" +
                (shouldConsume ? "" : " (no items consumed)"),
                WandColors.MsgReplacement);
        }
        else
        {
            // Instant: process all at once
            // In single-player, WorldGen.gen controls per-tile sounds/dust/gore AND item drops.
            // Only set gen=true when we actually want to suppress drops:
            //   - SuppressDrops=true → gen=true (no items created)
            //   - SuppressDrops=false → gen=false (items spawn; vacuum collects them if enabled)
            // In multiplayer: always leave gen=false so each KillTile/PlaceTile/ReplaceTile
            // sends its own TileManipulation message to the server.
            bool isMultiplayer = Main.netMode == NetmodeID.MultiplayerClient;
            bool wantVacuum = sandbox.VacuumItems && !sandbox.EffectiveSuppressDrops;
            bool wasGen = WorldGen.gen;
            if (!isMultiplayer && sandbox.EffectiveSuppressDrops)
                WorldGen.gen = true;

            // Pre-compute full operation bounds for periodic vacuum sweeps
            Rectangle fullOperationBounds = Rectangle.Empty;
            if (wantVacuum)
                fullOperationBounds = BulkTileOperations.ComputeBounds(
                    validTiles.ConvertAll(t => t.Position));

            // Periodic vacuum: sweep every N tile replacements to prevent
            // hitting Terraria's 400-item ground cap (Main.maxItems).
            const int VacuumSweepInterval = 200;
            int tilesSinceVacuum = 0;

            foreach (var info in validTiles)
            {
                var t = Main.tile[info.Position.X, info.Position.Y];

                // Verify tile still matches (might have changed between validation and execution)
                if (!t.HasTile || !ItemTypeHelper.IsTileVariantOf(t.TileType, info.SourceType))
                    continue;

                // Preserve slope/half-block from the old tile (vanilla replacement behavior)
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
                        // Fallback: KillTile + PlaceTile for cases ReplaceTile can't handle
                        WorldGen.KillTile(info.Position.X, info.Position.Y,
                            fail: false, effectOnly: false, noItem: sandbox.EffectiveSuppressDrops);

                        if (!Main.tile[info.Position.X, info.Position.Y].HasTile)
                        {
                            WorldGen.PlaceTile(info.Position.X, info.Position.Y,
                                info.TargetType, mute: true, forced: false, plr: player.whoAmI);
                            didReplace = Main.tile[info.Position.X, info.Position.Y].HasTile;
                        }
                    }
                }
                else
                {
                    // Erase to Air: must use KillTile
                    if (WorldGen.CanKillTile(info.Position.X, info.Position.Y))
                    {
                        WorldGen.KillTile(info.Position.X, info.Position.Y,
                            fail: false, effectOnly: false, noItem: sandbox.EffectiveSuppressDrops);
                        didReplace = !Main.tile[info.Position.X, info.Position.Y].HasTile;
                    }
                }

                if (didReplace)
                {
                    var placed = Main.tile[info.Position.X, info.Position.Y];
                    if (placed.HasTile)
                    {
                        placed.Slope = oldSlope;
                        placed.IsHalfBlock = oldHalf;
                    }

                    // Paint logic: PreservePaint wins over PaintSprayer.
                    // If PreservePaint is ON and the tile had paint → re-apply old paint.
                    // If PaintSprayer is ON → paint only if the tile was NOT already painted
                    //   (or if PreservePaint is OFF, paint all tiles).
                    if (placed.HasTile)
                    {
                        if (settings.PreservePaint && oldPaintColor > 0)
                        {
                            placed.TileColor = oldPaintColor;
                        }
                        else
                        {
                            WandOfBuildingBase.ApplyPaintSprayerTile(player, info.Position.X, info.Position.Y, shouldConsume, settings.PaintSprayer);
                        }
                    }

                    replaced++;
                    affectedPositions.Add(info.Position);
                    tilesSinceVacuum++;
                }

                // Periodic vacuum sweep to prevent 400-item cap overflow.
                if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                    tilesSinceVacuum = 0;
                }
            }

            WorldGen.gen = wasGen;

            if (affectedPositions.Count > 0)
            {
                if (isMultiplayer)
                    BulkTileOperations.FinalizeFrameOnly(affectedPositions);
                else
                    BulkTileOperations.FinalizeBatch(affectedPositions);

                // Final vacuum sweep: catch remaining items from the last group
                // and from FinalizeBatch's cascading frame updates.
                if (wantVacuum)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                }
            }

            if (replaced > 0)
            {
                undoMgr.CommitAction(action);

                // Play completion sound — always when wand sounds enabled.
                if (clientCfg?.EnableWandSounds == true)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, player.Center);
                }

                if (settings.NewObject != ObjectType.Air && targetCondition != null && shouldConsume)
                    ItemTypeHelper.ConsumeItems(player.inventory, targetCondition, replaced);

                string srcIcon = $"[i:{sourceItem.type}]";
                string tgtIcon = settings.NewObject == ObjectType.Air ? "Air" : $"[i:{targetItem.type}]";
                Main.NewText($"Replaced {replaced}: {srcIcon} → {tgtIcon}" +
                    (shouldConsume ? "" : " (no items consumed)"), WandColors.MsgReplacement);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoTilesReplaced", WandColors.MsgInfo);
            }
        }
    }

    /// <summary>
    /// Wall-to-wall replacement path. Uses WallType, KillWall, PlaceWall, and WallFrame
    /// instead of the tile-based replacement API.
    /// </summary>
    private void ExecuteWallReplacement(Player player, WandPlayer wandPlayer,
        WandOfReplacementSettings settings, SelectionState selection,
        ResourcesConfig config, SandboxConfig sandbox, PerformanceConfig perfConfig, PreferencesConfig clientCfg)
    {
        // Both source and target must be Wall (or target can be Air to erase walls)
        if (settings.OldObject != ObjectType.Wall)
        {
            Main.NewText(Get("SourceMustBeWall"), WandColors.MsgError);
            return;
        }

        // Find source wall item.
        // SameType mode is target-driven for walls as well.
        var srcCondition = ItemTypeHelper.GetConditions(ObjectType.Wall);
        int? sourceWallChoice = settings.SameTypeMode
            ? settings.GetChosenTargetItemType(settings.NewObject)
            : settings.GetChosenSourceItemType(ObjectType.Wall);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition, sourceWallChoice);
        if (sourceItem == null)
        {
            Main.NewText(Get("NoWallSourceItems"), WandColors.MsgError);
            return;
        }
        ushort sourceWallType = (ushort)sourceItem.createWall;

        // Find target wall item (unless target is Air — erase walls)
        Item targetItem = null;
        Func<Item, bool> targetCondition = null;
        ushort targetWallType = 0;
        bool eraseMode = settings.NewObject == ObjectType.Air;

        if (!eraseMode)
        {
            if (settings.NewObject != ObjectType.Wall)
            {
                Main.NewText(Get("TargetMustBeWallOrAir"), WandColors.MsgError);
                return;
            }

            var baseTgtCondition = ItemTypeHelper.GetConditions(ObjectType.Wall);
            targetCondition = item => baseTgtCondition(item) && (ushort)item.createWall != sourceWallType;
            // SameTypeMode choices target to source choice (target tracks source).
            // S8 2026-04-22 (Cavendish Response #5 §9.1): track *resolved* sourceItem.type
            // not the choice field, so target stays coherent when source choice ghosts.
            int? targetChoice = settings.GetChosenTargetItemType(ObjectType.Wall);
            targetItem = ItemTypeHelper.FindFirstItem(player, targetCondition, targetChoice);

            if (targetItem == null)
            {
                Item anyTarget = ItemTypeHelper.FindFirstItem(player, baseTgtCondition);
                if (anyTarget != null && (ushort)anyTarget.createWall == sourceWallType)
                    Main.NewText(Get("SameWallSourceTarget", anyTarget.type), WandColors.MsgInfo);
                else
                    Main.NewText(Get("NoWallTargetItems"), WandColors.MsgError);
                return;
            }
            targetWallType = (ushort)targetItem.createWall;
        }

        // --- MP: send packet to server and return early ---
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ReplacementPacketHandler.SendReplacementOperation(
                selection.StartTile, selection.EndTile,
                settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                settings.OldObject, settings.NewObject,
                sourceWallType, eraseMode ? (ushort)0 : targetWallType,
                (short)(targetItem?.type ?? 0), isWallMode: true,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection, settings.PaintSprayer);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swpWall = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swpWall.FilterBySelection(invertedTiles);

        // Count walls that match
        int needed = 0;
        foreach (Point tile in invertedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsWallProtected(tile.X, tile.Y)) continue;
            if (Main.tile[tile.X, tile.Y].WallType == sourceWallType)
                needed++;
        }

        if (needed == 0)
        {
            Main.NewText(Get("NoSourceWallsInSelection", sourceItem.Name), WandColors.MsgInfo);
            return;
        }

        // Check target availability — count only the specific wall item type
        bool shouldConsume = true;
        if (!eraseMode && config != null && config.IsInfiniteForObjectType(ObjectType.Wall))
        {
            Func<Item, bool> wallInfCond = i => !i.IsAir && i.type == targetItem.type;
            ItemTypeHelper.CountItems(player.inventory, wallInfCond, out int grandTotal);
            int threshold = config.GetThresholdForObjectType(ObjectType.Wall);
            if (threshold == 0)
                shouldConsume = false;
            else if (grandTotal >= threshold)
                shouldConsume = false;
        }

        if (!eraseMode && shouldConsume)
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int available);
            if (available < needed)
            {
                Main.NewText(Get("NeedTargetItems", needed, targetItem.type, available), WandColors.MsgError);
                return;
            }
        }

        var undoMgr = player.GetModPlayer<UndoManager>();
        string srcName = sourceItem.Name;
        string tgtName = eraseMode ? "Air" : targetItem.Name;
        var action = undoMgr.BeginAction($"Replace Wall {srcName} → {tgtName}");

        bool isMultiplayer = Main.netMode == NetmodeID.MultiplayerClient;
        bool suppressDrops = sandbox.EffectiveSuppressDrops;

        // Pre-validate all walls and snapshot them
        var validWalls = new List<ProgressiveTileProcessor.WallReplacementInfo>();
        foreach (Point tile in invertedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsWallProtected(tile.X, tile.Y)) continue;

            var t = Main.tile[tile.X, tile.Y];
            if (t.WallType != sourceWallType) continue;

            // Check if there's a hanging object (torch, banner, painting, etc.)
            // that depends on this wall for support. Instead of skipping the tile,
            // we flag it so the hanging object is destroyed (with drops) before
            // the wall is replaced.
            bool hasHanging = TileHelper.WouldTileLoseSupport(tile.X, tile.Y);

            action.AddSnapshot(tile);

            validWalls.Add(new ProgressiveTileProcessor.WallReplacementInfo
            {
                Position = tile,
                SourceWallType = sourceWallType,
                TargetWallType = eraseMode ? (ushort)0 : targetWallType,
                IsErase = eraseMode,
                SuppressDrops = suppressDrops,
                HasHangingObject = hasHanging,
                PaintSprayer = settings.PaintSprayer,
                PreservePaint = settings.PreservePaint
            });
        }

        if (validWalls.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoWallsReplaced", WandColors.MsgInfo);
            return;
        }

        // Branch: progressive mode (with drops/sounds) vs instant mode (silent)
        bool useProgressive = perfConfig != null && perfConfig.EnableProgressiveMode;

        if (useProgressive)
        {
            // Progressive: enqueue batches for timed processing
            int batchSize = perfConfig.ProgressiveBatchSize;
            float interval = perfConfig.ProgressiveInterval;

            ProgressiveTileProcessor.EnqueueWallReplacement(
                player, validWalls, action, undoMgr, batchSize, interval,
                shouldConsume, targetCondition, sourceItem, targetItem,
                eraseMode ? ObjectType.Air : ObjectType.Wall,
                targetWallType,
                vacuumItems: sandbox.VacuumItems);

            int batches = (int)Math.Ceiling((double)validWalls.Count / batchSize);
            float totalTime = (batches - 1) * interval;
            string srcIcon2 = $"[i:{sourceItem.type}]";
            string tgtIcon2 = eraseMode ? "Air" : $"[i:{targetItem.type}]";
            Main.NewText(
                $"Replacing {validWalls.Count} walls: {srcIcon2} → {tgtIcon2} in {batches} wave(s) (~{totalTime:F1}s)" +
                (shouldConsume ? "" : " (no items consumed)"),
                WandColors.MsgReplacement);
        }
        else
        {
            // Instant: process all at once
            int replaced = 0;
            var affectedPositions = new List<Point>();
            bool wantVacuum = sandbox.VacuumItems && !suppressDrops;

            // Pre-compute full operation bounds for periodic vacuum sweeps
            Rectangle fullOperationBounds = Rectangle.Empty;
            if (wantVacuum)
                fullOperationBounds = BulkTileOperations.ComputeBounds(
                    validWalls.ConvertAll(w => w.Position));

            bool wasGen = WorldGen.gen;

            // Periodic vacuum: sweep every N wall replacements to prevent
            // hitting Terraria's 400-item ground cap (Main.maxItems).
            const int VacuumSweepInterval = 200;
            int tilesSinceVacuum = 0;

            foreach (var info in validWalls)
            {
                var t = Main.tile[info.Position.X, info.Position.Y];

                // Verify wall still matches
                if (t.WallType != info.SourceWallType) continue;

                // Save old wall paint color for PreservePaint
                byte oldWallColor = t.WallColor;

                // Destroy hanging objects (torches, banners, paintings) that depend
                // on this wall for support. They get their natural drops (unless suppressed).
                if (info.HasHangingObject && t.HasTile)
                {
                    if (!isMultiplayer)
                        WorldGen.gen = suppressDrops;
                    WorldGen.KillTile(info.Position.X, info.Position.Y,
                        fail: false, effectOnly: false, noItem: suppressDrops);
                    WorldGen.gen = wasGen;
                }

                // Only suppress wall drops when config says so.
                if (!isMultiplayer)
                    WorldGen.gen = suppressDrops;

                if (info.IsErase)
                {
                    WorldGen.KillWall(info.Position.X, info.Position.Y, fail: false);
                    if (t.WallType == WallID.None)
                    {
                        replaced++;
                        affectedPositions.Add(info.Position);
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
                            if (settings.PreservePaint && oldWallColor > 0)
                            {
                                t.WallColor = oldWallColor;
                            }
                            else
                            {
                                WandOfBuildingBase.ApplyPaintSprayerWall(player, info.Position.X, info.Position.Y, shouldConsume, settings.PaintSprayer);
                            }
                            replaced++;
                            affectedPositions.Add(info.Position);
                            tilesSinceVacuum++;
                        }
                    }
                }

                WorldGen.gen = wasGen;

                // Periodic vacuum sweep to prevent 400-item cap overflow.
                if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                    tilesSinceVacuum = 0;
                }
            }

            if (affectedPositions.Count > 0)
            {
                foreach (var pos in affectedPositions)
                    Framing.WallFrame(pos.X, pos.Y);

                if (isMultiplayer)
                    BulkTileOperations.FinalizeFrameOnly(affectedPositions);
                else
                    BulkTileOperations.FinalizeBatch(affectedPositions);

                // Final vacuum sweep: catch remaining items from the last group
                // and from FinalizeBatch's cascading frame updates.
                if (wantVacuum)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                }
            }

            if (replaced > 0)
            {
                undoMgr.CommitAction(action);

                // Play completion sound — always when wand sounds enabled.
                if (clientCfg?.EnableWandSounds == true)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, player.Center);
                }

                if (!eraseMode && targetCondition != null && shouldConsume)
                    ItemTypeHelper.ConsumeItems(player.inventory, targetCondition, replaced);

                string srcIcon = $"[i:{sourceItem.type}]";
                string tgtIcon = eraseMode ? "Air" : $"[i:{targetItem.type}]";
                Main.NewText($"Replaced {replaced} walls: {srcIcon} → {tgtIcon}" +
                    (shouldConsume ? "" : " (no items consumed)"), WandColors.MsgReplacement);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoWallsReplaced", WandColors.MsgInfo);
            }
        }
    }



    public override void AddRecipes()
    {
        // Only the Instant variant has a craftable recipe.
        // Other modes are obtained via right-click cycling in inventory.
    }

}