using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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
        var wandPlayer = player.GetModPlayer<WandPlayer>();
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

        // Show the replacement pair in cursor
        var (sourceItem, targetItem) = FindReplacementPair(player);
        if (sourceItem != null && targetItem != null)
        {
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = targetItem.type;
            player.cursorItemIconPush = 26;
        }
    }

    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.ClearSelection();
        Main.NewText("Selection cancelled.", Color.Yellow);
    }

    /// <summary>
    /// Finds the first two different tile-creating items in inventory.
    /// First = what to replace, Second = what to replace with.
    /// </summary>
    protected (Item source, Item target) FindReplacementPair(Player player)
    {
        Item source = null;
        Item target = null;

        for (int i = 0; i < 58; i++)
        {
            Item item = player.inventory[i];
            if (item.IsAir || item.createTile < 0) continue;

            if (source == null)
            {
                source = item;
            }
            else if (item.createTile != source.createTile)
            {
                target = item;
                break;
            }
        }

        return (source, target);
    }

    protected void ExecuteReplacement(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.ReplacementSettings;
        var selection = wandPlayer.Selection;

        var (sourceItem, targetItem) = FindReplacementPair(player);

        if (sourceItem == null || targetItem == null)
        {
            Main.NewText("Need two different block types in inventory.", Color.Red);
            return;
        }

        ushort sourceType = (ushort)sourceItem.createTile;
        ushort targetType = (ushort)targetItem.createTile;

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

        // Count how many tiles we need to replace
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
            Main.NewText($"No {sourceItem.Name} tiles found in selection.", Color.Gray);
            return;
        }

        // Check if we have enough target blocks
        ItemTypeHelper.CountItems(player.inventory, 
            item => item.createTile == targetType, out int available);

        if (available < needed)
        {
            Main.NewText($"Need {needed} {targetItem.Name}, have {available}.", Color.Red);
            return;
        }

        var undoMgr = player.GetModPlayer<UndoManager>();
        var action = undoMgr.BeginAction($"Replace {sourceItem.Name} → {targetItem.Name}");

        int replaced = 0;

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

            var t = Main.tile[tile.X, tile.Y];
            if (!t.HasTile || t.TileType != sourceType) continue;

            // Check if we can break this tile
            if (!player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
            if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

            action.AddSnapshot(tile);

            // Remove old tile
            WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: true);

            // Place new tile
            WorldGen.PlaceTile(tile.X, tile.Y, targetType, mute: true, forced: false, plr: player.whoAmI);

            replaced++;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendTileSquare(-1, tile.X, tile.Y);
        }

        if (replaced > 0)
        {
            undoMgr.CommitAction(action);

            // Consume target items
            int toConsume = replaced;
            for (int i = 0; i < 58 && toConsume > 0; i++)
            {
                Item item = player.inventory[i];
                if (item.IsAir || item.createTile != targetType) continue;

                int take = Math.Min(item.stack, toConsume);
                item.stack -= take;
                toConsume -= take;
                if (item.stack <= 0) item.TurnToAir();
            }

            Main.NewText($"Replaced {replaced} tiles: {sourceItem.Name} → {targetItem.Name}", Color.MediumPurple);
        }
        else
        {
            Main.NewText("No tiles could be replaced.", Color.Gray);
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
                ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
            }
            return false;
        }
        return true;
    }
}