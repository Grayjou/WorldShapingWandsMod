using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfReplacementBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Replacement";

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
    /// Resolves the source and target items for replacement, ensuring they are different tiles.
    /// Source = first inventory item matching OldObject category.
    /// Target = first inventory item matching NewObject category, skipping items with same createTile as source.
    /// </summary>
    private static (Item source, Item target) ResolveReplacementPair(Player player, WandOfReplacementSettings settings)
    {
        // Find source item (what tiles to look for in the world)
        var srcCondition = ItemTypeHelper.GetConditions(settings.OldObject);
        Item sourceItem = ItemTypeHelper.FindFirstItem(player, srcCondition);

        if (sourceItem == null)
            return (null, null);

        ushort sourceCreateTile = (ushort)sourceItem.createTile;

        // Find target item, but skip items that would place the same tile as source
        Item targetItem = null;
        if (settings.NewObject != ObjectType.Air)
        {
            var tgtCondition = ItemTypeHelper.GetConditions(settings.NewObject);
            // Use a condition that also excludes items with the same createTile as source
            targetItem = ItemTypeHelper.FindFirstItem(player, item =>
                tgtCondition(item) && (ushort)item.createTile != sourceCreateTile);
        }

        return (sourceItem, targetItem);
    }

    protected void ExecuteReplacement(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.ReplacementSettings;
        var selection = wandPlayer.Selection;
        var config = ModContent.GetInstance<WandConfig>();

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
                    Main.NewText($"Source and target are the same tile ({anyTarget.Name}).", WandColors.MsgInfo);
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
            selection.VerticalFirst
        );

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        // Count tiles that match the specific source tile type
        int needed = 0;
        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            var t = Main.tile[tile.X, tile.Y];

            if (t.HasTile && t.TileType == sourceType)
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
                Main.NewText($"Need {needed} {targetItem.Name}, have {available}.", WandColors.MsgError);
                return;
            }
        }

        var undoMgr = player.GetModPlayer<UndoManager>();
        string srcName = sourceItem.Name;
        string tgtName = settings.NewObject == ObjectType.Air ? "Air" : targetItem.Name;
        var action = undoMgr.BeginAction($"Replace {srcName} → {tgtName}");

        int replaced = 0;

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

            var t = Main.tile[tile.X, tile.Y];

            // Match only the specific source tile type
            if (!t.HasTile || t.TileType != sourceType) continue;

            // Check pick power
            if (!player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
            if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

            action.AddSnapshot(tile);

            // Preserve slope/half-block from the old tile (vanilla replacement behavior)
            var oldSlope = t.Slope;
            bool oldHalf = t.IsHalfBlock;

            // Remove old tile (drop the item)
            WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: false);

            // Place new tile (unless target is Air = erase)
            if (settings.NewObject != ObjectType.Air)
            {
                WorldGen.PlaceTile(tile.X, tile.Y, targetType, mute: true, forced: false, plr: player.whoAmI);

                // Restore the original slope/half-block
                var placed = Main.tile[tile.X, tile.Y];
                if (placed.HasTile)
                {
                    placed.Slope = oldSlope;
                    placed.IsHalfBlock = oldHalf;
                }
            }

            replaced++;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendTileSquare(-1, tile.X, tile.Y);
        }

        if (replaced > 0)
        {
            undoMgr.CommitAction(action);

            // Consume target items (unless Air or infinite resource mode)
            if (settings.NewObject != ObjectType.Air && targetCondition != null && shouldConsume)
                ItemTypeHelper.ConsumeItems(player.inventory, targetCondition, replaced);

            Main.NewText($"Replaced {replaced}: {srcName} → {tgtName}" + (shouldConsume ? "" : " (no items consumed)"), WandColors.MsgReplacement);
        }
        else
        {
            Main.NewText("No tiles could be replaced.", WandColors.MsgInfo);
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
}