using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementSelect : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select (caution)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = System.Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            System.Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickAgain", "replace"), Color.MediumPurple);
            return false; // Don't consume the wand
        }
        else
        {
            if (IsOnLocalCooldown()) return false;
            wandPlayer.UpdateSelection(mouseTile);
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