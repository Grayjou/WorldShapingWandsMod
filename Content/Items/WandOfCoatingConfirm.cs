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

public class WandOfCoatingConfirm : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => WandColors.Coating.Confirm;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingStamp>();

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
            ExecuteCoating(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false;
        }
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfCoatingInstant>(1)
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.SilverBar, 10)
            .AddCustomShimmerResult(ItemID.Paintbrush, 1)
            .AddCustomShimmerResult(ItemID.PaintScraper, 1)
            .AddCustomShimmerResult(ItemID.PaintRoller, 1)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}
