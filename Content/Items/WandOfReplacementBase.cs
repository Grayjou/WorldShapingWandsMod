using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfReplacementBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Replacement";
    public override string WandLore => "The Deity of Transmutation lets you reweave the threads of what already is.";

    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

    public override bool? UseItem(Player player)
    {
        // Don't do anything if the mouse is over UI
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    public override void HoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        if (wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false;
        }

        // Show cursor icons: source → target
        DrawReplacementCursorIcons(player, wandPlayer);
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

    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(WandColors.CancelReplacement, wandPlayer.ReplacementSettings.Shape);
    }

    /// <summary>
    /// Resolves the source and target items for replacement, ensuring they are different tiles/walls.
    /// Source = first inventory item matching OldObject category.
    /// Target = first inventory item matching NewObject category, skipping items with same placement as source.
    /// </summary>
    private static (Item source, Item target) ResolveReplacementPair(Player player, WandOfReplacementSettings settings)
    {
        // Find source item (what tiles/walls to look for in the world)
        var srcCondition = ItemTypeHelper.GetConditions(settings.OldObject);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition);

        if (sourceItem == null)
            return (null, null);

        bool isWallMode = settings.OldObject == ObjectType.Wall;

        // Find target item, but skip items that would place the same tile/wall as source
        Item targetItem = null;
        if (settings.NewObject != ObjectType.Air)
        {
            var tgtCondition = ItemTypeHelper.GetConditions(settings.NewObject);

            if (isWallMode)
            {
                ushort sourceWall = (ushort)sourceItem.createWall;
                targetItem = ItemTypeHelper.FindFirstItem(player, item =>
                    tgtCondition(item) && (ushort)item.createWall != sourceWall);
            }
            else
            {
                ushort sourceCreateTile = (ushort)sourceItem.createTile;
                targetItem = ItemTypeHelper.FindFirstItem(player, item =>
                    tgtCondition(item) && (ushort)item.createTile != sourceCreateTile);
            }
        }

        return (sourceItem, targetItem);
    }

    protected void ExecuteReplacement(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.ReplacementSettings;
        var selection = wandPlayer.Selection;
        var config = ModContent.GetInstance<WandConfig>();

        // Wall replacement uses a completely separate path
        if (settings.OldObject == ObjectType.Wall || settings.NewObject == ObjectType.Wall)
        {
            ExecuteWallReplacement(player, wandPlayer, settings, selection, config);
            return;
        }

        // --- Resolve source item: identifies the SPECIFIC tile type to find in the world ---
        var srcCondition = ItemTypeHelper.GetConditions(settings.OldObject);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition);
        if (sourceItem == null)
        {
            Main.NewText($"No {settings.OldObject} items in inventory to identify source tile.", WandColors.MsgError);
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
            targetItem = ItemTypeHelper.FindFirstItem(player, targetCondition);

            if (targetItem == null)
            {
                // Check if there ARE items of the target type but they're all the same as source
                Item anyTarget = ItemTypeHelper.FindFirstItem(player, baseTgtCondition);
                if (anyTarget != null && (ushort)anyTarget.createTile == sourceType)
                    Main.NewText($"Source and target are the same tile [i:{anyTarget.type}].", WandColors.MsgInfo);
                else
                    Main.NewText($"No {settings.NewObject} items found in inventory.", WandColors.MsgError);
                return;
            }
            targetType = (ushort)targetItem.createTile;
        }

        var context = new ShapeContext(
            selection.StartTile,
            selection.EndTile,
            settings.Shape.FillMode,
            settings.Shape.Thickness,
            HorizontalBias.None,
            VerticalBias.None,
            selection.VerticalFirst,
            settings.Shape.EqualDimensions
        );

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        // Count tiles that match the specific source tile type (including variants like grass→dirt)
        int needed = 0;
        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;
            var t = Main.tile[tile.X, tile.Y];

            if (t.HasTile && ItemTypeHelper.IsTileVariantOf(t.TileType, sourceType))
                needed++;
        }

        if (needed == 0)
        {
            Main.NewText($"No {sourceItem.Name} found in selection.", WandColors.MsgInfo);
            return;
        }

        // Determine if consumption is needed (same rules as building wand)
        bool shouldConsume = true;
        if (settings.NewObject != ObjectType.Air && config != null && config.EnableInfiniteResource)
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int grandTotal);
            if (config.InfiniteResourceAmount == 0)
                shouldConsume = false;
            else if (grandTotal >= config.InfiniteResourceAmount)
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
        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

            var t = Main.tile[tile.X, tile.Y];
            if (!t.HasTile || !ItemTypeHelper.IsTileVariantOf(t.TileType, sourceType)) continue;
            if (!config.BypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;

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
                TargetType = settings.NewObject != ObjectType.Air ? targetType : (ushort)0,
                SuppressDrops = config.SuppressDrops
            });
        }

        if (validTiles.Count == 0)
        {
            Main.NewText("No tiles could be replaced.", WandColors.MsgInfo);
            return;
        }

        // Branch: progressive mode (with drops/sounds) vs instant mode (silent, no drops)
        bool useProgressive = config != null && config.EnableProgressiveMode;

        if (useProgressive)
        {
            // Progressive: enqueue batches for timed processing
            int batchSize = config.ProgressiveBatchSize;
            float interval = config.ProgressiveInterval;

            ProgressiveTileProcessor.EnqueueReplacement(
                player, validTiles, action, undoMgr, batchSize, interval,
                shouldConsume, targetCondition, sourceItem, targetItem, settings.NewObject);

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
            // In single-player, suppress sounds/effects via WorldGen.gen for clean operation.
            // In multiplayer, do NOT set WorldGen.gen — let KillTile/PlaceTile send per-tile
            // network messages so the server properly processes each replacement.
            bool isMultiplayer = Main.netMode == NetmodeID.MultiplayerClient;
            bool wasGen = WorldGen.gen;

            if (!isMultiplayer)
                WorldGen.gen = true;

            foreach (var info in validTiles)
            {
                var t = Main.tile[info.Position.X, info.Position.Y];

                // Verify tile still matches (might have changed between validation and execution)
                if (!t.HasTile || !ItemTypeHelper.IsTileVariantOf(t.TileType, info.SourceType))
                    continue;

                // Preserve slope/half-block from the old tile (vanilla replacement behavior)
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
                        // Fallback: KillTile + PlaceTile for cases ReplaceTile can't handle
                        WorldGen.KillTile(info.Position.X, info.Position.Y,
                            fail: false, effectOnly: false, noItem: config.SuppressDrops);

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
                            fail: false, effectOnly: false, noItem: config.SuppressDrops);
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

                    replaced++;
                    affectedPositions.Add(info.Position);
                }
            }

            if (!isMultiplayer)
                WorldGen.gen = wasGen;

            if (affectedPositions.Count > 0)
                BulkTileOperations.FinalizeBatch(affectedPositions);

            if (replaced > 0)
            {
                undoMgr.CommitAction(action);

                // Play ManaCrystalPickup (SoundID.Item29) at low volume — only when drops
                // are suppressed (otherwise natural tile break/place sounds play),
                // and only when wand sounds are enabled in config.
                if (config.SuppressDrops && config.EnableWandSounds)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, player.Center); // ManaCrystalPickup
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
                Main.NewText("No tiles could be replaced.", WandColors.MsgInfo);
            }
        }
    }

    /// <summary>
    /// Wall-to-wall replacement path. Uses WallType, KillWall, PlaceWall, and WallFrame
    /// instead of the tile-based replacement API.
    /// </summary>
    private void ExecuteWallReplacement(Player player, WandPlayer wandPlayer,
        WandOfReplacementSettings settings, SelectionState selection, WandConfig config)
    {
        // Both source and target must be Wall (or target can be Air to erase walls)
        if (settings.OldObject != ObjectType.Wall)
        {
            Main.NewText("Source type must be Wall for wall replacement.", WandColors.MsgError);
            return;
        }

        // Find source wall item
        var srcCondition = ItemTypeHelper.GetConditions(ObjectType.Wall);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition);
        if (sourceItem == null)
        {
            Main.NewText("No wall items in inventory to identify source wall.", WandColors.MsgError);
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
                Main.NewText("Target type must be Wall or Air for wall replacement.", WandColors.MsgError);
                return;
            }

            var baseTgtCondition = ItemTypeHelper.GetConditions(ObjectType.Wall);
            targetCondition = item => baseTgtCondition(item) && (ushort)item.createWall != sourceWallType;
            targetItem = ItemTypeHelper.FindFirstItem(player, targetCondition);

            if (targetItem == null)
            {
                Item anyTarget = ItemTypeHelper.FindFirstItem(player, baseTgtCondition);
                if (anyTarget != null && (ushort)anyTarget.createWall == sourceWallType)
                    Main.NewText($"Source and target are the same wall [i:{anyTarget.type}].", WandColors.MsgInfo);
                else
                    Main.NewText("No wall items found for target.", WandColors.MsgError);
                return;
            }
            targetWallType = (ushort)targetItem.createWall;
        }

        var context = new ShapeContext(
            selection.StartTile, selection.EndTile,
            settings.Shape.FillMode, settings.Shape.Thickness,
            HorizontalBias.None, VerticalBias.None,
            selection.VerticalFirst, settings.Shape.EqualDimensions);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        // Count walls that match
        int needed = 0;
        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;
            if (Main.tile[tile.X, tile.Y].WallType == sourceWallType)
                needed++;
        }

        if (needed == 0)
        {
            Main.NewText($"No {sourceItem.Name} walls found in selection.", WandColors.MsgInfo);
            return;
        }

        // Check target availability
        bool shouldConsume = true;
        if (!eraseMode && config != null && config.EnableInfiniteResource)
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int grandTotal);
            if (config.InfiniteResourceAmount == 0)
                shouldConsume = false;
            else if (grandTotal >= config.InfiniteResourceAmount)
                shouldConsume = false;
        }

        if (!eraseMode && shouldConsume)
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
        string tgtName = eraseMode ? "Air" : targetItem.Name;
        var action = undoMgr.BeginAction($"Replace Wall {srcName} → {tgtName}");

        int replaced = 0;
        var affectedPositions = new List<Point>();

        bool isMultiplayer = Main.netMode == NetmodeID.MultiplayerClient;
        bool wasGen = WorldGen.gen;

        if (!isMultiplayer)
            WorldGen.gen = true;

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

            var t = Main.tile[tile.X, tile.Y];
            if (t.WallType != sourceWallType) continue;

            action.AddSnapshot(tile);

            if (eraseMode)
            {
                WorldGen.KillWall(tile.X, tile.Y, fail: false);
                if (t.WallType == WallID.None)
                {
                    replaced++;
                    affectedPositions.Add(tile);
                }
            }
            else
            {
                WorldGen.KillWall(tile.X, tile.Y, fail: false);
                if (t.WallType == WallID.None)
                {
                    WorldGen.PlaceWall(tile.X, tile.Y, targetWallType, mute: true);
                    if (t.WallType == targetWallType)
                    {
                        replaced++;
                        affectedPositions.Add(tile);
                    }
                }
            }
        }

        if (!isMultiplayer)
            WorldGen.gen = wasGen;

        if (affectedPositions.Count > 0)
        {
            foreach (var pos in affectedPositions)
                Framing.WallFrame(pos.X, pos.Y);
            BulkTileOperations.FinalizeBatch(affectedPositions);
        }

        if (replaced > 0)
        {
            undoMgr.CommitAction(action);

            // Play ManaCrystalPickup (SoundID.Item29) at low volume — only when drops
            // are suppressed and wand sounds enabled.
            if (config.SuppressDrops && config.EnableWandSounds)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, player.Center); // ManaCrystalPickup
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
            Main.NewText("No walls could be replaced.", WandColors.MsgInfo);
        }
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive)
            {
                CancelSelection(wandPlayer);
            }
            else
            {
                // Only toggle UI on the client
                if (Main.myPlayer == player.whoAmI)
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
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
}