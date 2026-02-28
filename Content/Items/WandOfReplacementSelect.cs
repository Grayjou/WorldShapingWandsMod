using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementSelect : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(200, 150, 255);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = System.Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            System.Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText("Selection started. Click again to replace.", Color.MediumPurple);
            return true;
        }
        else
        {
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteReplacement(player, wandPlayer);
            wandPlayer.ClearSelection();
            return true;
        }
    }
}