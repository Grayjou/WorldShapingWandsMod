using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfWiringSelect : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select (caution)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click again to wire.", Color.Yellow);
            return false; // Don't consume the wand
        }
        else
        {
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteWiring(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfWiringInstant>(1)
            .AddCustomShimmerResult(ItemID.WireKite, 1)
            .AddCustomShimmerResult(ItemID.Wire, 50)
            .AddCustomShimmerResult(ItemID.Actuator, 10)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}