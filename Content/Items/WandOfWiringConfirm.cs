using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfWiringConfirm : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringStamp>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click to set end point.", Color.Yellow);
            return false; // Don't consume the wand
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText("Click again to confirm wiring.", Color.Cyan);
            return false; // Don't consume the wand
        }
        else
        {
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