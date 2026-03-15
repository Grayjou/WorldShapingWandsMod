using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfSafekeepingStamp : WandOfSafekeepingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new Color(100, 200, 255); // Light blue — Stamp
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfSafekeepingInstant>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // 1st click — start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("StampClickEnd"), WandColors.MsgPrompt);
            return false;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            // 2nd click — set end point and lock shape
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText(Get("StampClickLock"), WandColors.MsgConfirm);
            return false;
        }
        else if (!wandPlayer.IsStampLocked)
        {
            // 3rd click — lock the stamp template
            wandPlayer.LockStamp(mouseTile);
            Main.NewText(Get("StampLocked", "apply"), Color.LimeGreen);
            return false;
        }
        else
        {
            // 4th+ click — execute at current position, keep stamp
            wandPlayer.MoveStampTo(mouseTile);
            ExecuteSafekeeping(player, wandPlayer);
            return false;
        }
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);

        if (Main.myPlayer != player.whoAmI)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        // While stamp is locked, continuously move the preview to follow the mouse
        if (wandPlayer.IsStampLocked && wandPlayer.Selection.IsActive)
        {
            Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
            wandPlayer.MoveStampTo(mouseTile);
        }
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfSafekeepingInstant>(1)
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.SilverBar, 10)
            .AddCustomShimmerResult(ItemID.Amethyst, 5)
            .AddCustomShimmerResult(ItemID.Obsidian, 20)
            .AddCustomShimmerResult(ItemID.EbonstoneBlock, 10)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}
