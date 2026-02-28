using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using System;

namespace WorldShapingWandsMod.Content.Items
{
    public class WandOfBuildingSelect : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
        public override Color ModeColor => new Color(255, 200, 100); // Orange-yellow
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingConfirm>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText("Selection started. Click again to place.", Color.Cyan);
                return true;
            }
            else
            {
                wandPlayer.UpdateSelection(mouseTile);
                ExecuteBuilding(player, wandPlayer);
                wandPlayer.ClearSelection();
                return true;
            }
        }
    }
}