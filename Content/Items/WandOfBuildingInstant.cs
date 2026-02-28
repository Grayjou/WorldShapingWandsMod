using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using System;

namespace WorldShapingWandsMod.Content.Items
{
    public class WandOfBuildingInstant : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
        public override Color ModeColor => new Color(100, 255, 100); // Light green
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingSelect>();

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.channel = true; // needed for drag detection
        }

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            if (!wandPlayer.Selection.IsActive)
            {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
            }
            // Update selection in HoldItem continuously
            wandPlayer.UpdateSelection(mouseTile);
            return true;
        }

        public override void HoldItem(Player player)
        {
            base.HoldItem(player); // handles right-click cancel
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (!wandPlayer.Selection.IsActive)
                return;

            // Update selection while holding left click
            if (Main.mouseLeft)
            {
                Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
                wandPlayer.UpdateSelection(mouseTile);
            }

            // On release, execute
            if (!Main.mouseLeft && wandPlayer.Selection.IsActive)
            {
                ExecuteBuilding(player, wandPlayer);
                wandPlayer.ClearSelection();
            }
        }
    }
}