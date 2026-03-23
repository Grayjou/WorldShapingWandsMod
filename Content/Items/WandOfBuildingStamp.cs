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
    public class WandOfBuildingStamp : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
        public override Color ModeColor => new Color(100, 200, 255); // Light blue — Stamp (repeatable)
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingInstant>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                // 1st click — start selection
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText(Get("StampClickEnd"), Color.Cyan);
                return false;
            }
            else if (!wandPlayer.Selection.IsLocked)
            {
                // 2nd click — set end point and lock shape
                wandPlayer.UpdateSelection(mouseTile);
                wandPlayer.LockSelection();
                Main.NewText(Get("StampClickLock"), Color.Yellow);
                return false;
            }
            else if (!wandPlayer.IsStampLocked)
            {
                // 3rd click — lock the stamp template at current mouse position
                if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
                wandPlayer.LockStamp(mouseTile);
                Main.NewText(Get("StampLocked", "place"), Color.LimeGreen);
                return false;
            }
            else
            {
                // 4th+ click — execute at current mouse position, keep stamp
                if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
                if (IsOnLocalCooldown()) return false;
                wandPlayer.MoveStampTo(mouseTile);
                ExecuteBuilding(player, wandPlayer);
                // Don't clear — keep the stamp for repeated use
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
                Point mouseTile = GeometryHelper.GetMouseTile();
                wandPlayer.MoveStampTo(mouseTile);
            }
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
