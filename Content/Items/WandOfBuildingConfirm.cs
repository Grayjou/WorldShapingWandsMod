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
        public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingStamp>();

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                Main.NewText("Selection started. Click to set end point.", Color.Cyan);
                return false; // Don't consume the wand
            }
            else if (!wandPlayer.Selection.IsLocked)
            {
                wandPlayer.UpdateSelection(mouseTile);
                wandPlayer.LockSelection();
                Main.NewText("End point set. Click again to confirm.", Color.Yellow);
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
    }
}