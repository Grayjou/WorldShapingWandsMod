using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;
using System;

namespace WorldShapingWandsMod.Content.Items
{
    public class WandOfBuildingConfirm : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
        public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingStamp>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText(Get("SelectStartClickEnd"), Color.Cyan);
                return false; // Don't consume the wand
            }
            else if (!wandPlayer.Selection.IsLocked)
            {
                wandPlayer.UpdateSelection(mouseTile);
                wandPlayer.LockSelection();
                Main.NewText(Get("EndPointSet"), Color.Yellow);
                return false; // Don't consume the wand
            }
            else
            {
                ExecuteBuilding(player, wandPlayer);
                wandPlayer.ClearSelection();
                return false; // Don't consume the wand
            }
        }

        public override bool CanUseItem(Player player)
        {
            return base.CanUseItem(player);
        }

        public override void AddRecipes()
        {
            WandRecipeConditions.Register(Type);
            CreateRecipe()
                .AddIngredient<WandOfBuildingInstant>(1)
                .AddCustomShimmerResult(ItemID.Wood, 10)
                .AddCustomShimmerResult(ItemID.GrayBrick, 10)
                .AddCustomShimmerResult(ItemID.RedBrick, 10)
                .AddCustomShimmerResult(ItemID.Rope, 20)
                .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
                .AddCondition(WandRecipeConditions.NonCraftable)
                .Register();
        }
    }
}