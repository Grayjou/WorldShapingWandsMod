using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfWiringSelect : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(100, 200, 255); // Light blue
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click again to wire.", Color.Yellow);
            return true;
        }
        else
        {
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteWiring(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }
}