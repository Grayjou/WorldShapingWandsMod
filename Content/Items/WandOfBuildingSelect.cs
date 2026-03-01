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
        public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select (caution)
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingConfirm>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText("Selection started. Click again to place.", Color.Cyan);
                return false; // Don't consume the wand
            }
            else
            {
                wandPlayer.UpdateSelection(mouseTile);
                ExecuteBuilding(player, wandPlayer);
                wandPlayer.ClearSelection();
                return false; // Don't consume the wand
            }
        }
    }
}