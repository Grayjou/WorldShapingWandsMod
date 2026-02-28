using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementConfirm : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(220, 200, 255);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementInstant>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = System.Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            System.Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click to set end point.", Color.MediumPurple);
            return true;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText("Click again to confirm replacement.", Color.Yellow);
            return true;
        }
        else
        {
            ExecuteReplacement(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }
}