using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using System;

namespace WorldShapingWandsMod.Content.Items
{
    public class WandOfBuildingConfirm : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
        public override Color ModeColor => new Color(255, 100, 100); // Reddish
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingInstant>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText("Selection started. Click to set end point.", Color.Cyan);
                return true;
            }
            else if (!wandPlayer.Selection.IsLocked)
            {
                wandPlayer.UpdateSelection(mouseTile);
                wandPlayer.LockSelection();
                Main.NewText("End point set. Click again to confirm.", Color.Yellow);
                return true;
            }
            else
            {
                ExecuteBuilding(player, wandPlayer);
                wandPlayer.ClearSelection();
                return true;
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                var wandPlayer = player.GetModPlayer<WandPlayer>();
                if (wandPlayer.Selection.IsActive)
                {
                    wandPlayer.ClearSelection();
                    Main.NewText("Selection cancelled.", Color.Yellow);
                }
                return false;
            }
            return true;
        }
    }
}