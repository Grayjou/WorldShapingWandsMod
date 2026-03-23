using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementConfirm : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementStamp>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = System.Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            System.Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickEnd"), Color.MediumPurple);
            return false; // Don't consume the wand
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText(Get("ClickToConfirm"), Color.Yellow);
            return false; // Don't consume the wand
        }
        else
        {
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            if (IsOnLocalCooldown()) return false;
            ExecuteReplacement(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfReplacementInstant>(1)
            .AddCustomShimmerResult(ModContent.ItemType<WandOfBuildingInstant>(), 1)
            .AddCustomShimmerResult(ModContent.ItemType<WandOfDismantlingInstant>(), 1)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}