using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfWiringConfirm : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(100, 255, 150); // Mint green
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringInstant>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click to set end point.", Color.Yellow);
            return true;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText("Click again to confirm wiring.", Color.Cyan);
            return true;
        }
        else
        {
            ExecuteWiring(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }
}