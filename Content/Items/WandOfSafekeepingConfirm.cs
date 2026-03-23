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

public class WandOfSafekeepingConfirm : WandOfSafekeepingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfSafekeepingStamp>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickEnd"), WandColors.MsgPrompt);
            return false;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText(Get("ClickToConfirmOrCancel"), WandColors.MsgConfirm);
            return false;
        }
        else
        {
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            if (IsOnLocalCooldown()) return false;
            ExecuteSafekeeping(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false;
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
