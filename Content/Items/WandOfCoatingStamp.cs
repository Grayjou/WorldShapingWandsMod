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

public class WandOfCoatingStamp : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => WandColors.Coating.Stamp;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingInstant>();

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
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            wandPlayer.LockStamp(mouseTile);
            Main.NewText(Get("StampLocked", "apply"), Color.LimeGreen);
            return false;
        }
        else
        {
            // 4th+ click — execute at current position, keep stamp
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            if (IsOnLocalCooldown()) return false;
            wandPlayer.MoveStampTo(mouseTile);
            ExecuteCoating(player, wandPlayer);
            return false;
        }
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);

        if (Main.myPlayer != player.whoAmI)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (wandPlayer.IsStampLocked && wandPlayer.Selection.IsActive)
        {
            Point mouseTile = GeometryHelper.GetMouseTile();
            wandPlayer.MoveStampTo(mouseTile);
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
