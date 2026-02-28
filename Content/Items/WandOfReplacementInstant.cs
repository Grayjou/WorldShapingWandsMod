using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementInstant : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(180, 100, 255);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = System.Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            System.Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
        }
        wandPlayer.UpdateSelection(mouseTile);
        return true;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        if (!wandPlayer.Selection.IsActive) return;

        if (Main.mouseLeft)
        {
            Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
            wandPlayer.UpdateSelection(mouseTile);
        }

        if (!Main.mouseLeft && wandPlayer.Selection.IsActive)
        {
            ExecuteReplacement(player, wandPlayer);
            wandPlayer.ClearSelection();
        }
    }
}