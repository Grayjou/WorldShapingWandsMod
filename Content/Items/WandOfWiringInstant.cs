using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfWiringInstant : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant (dangerous)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        // All logic handled in HoldItem for instant/drag mode
        return false;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);

        if (Main.myPlayer != player.whoAmI)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);

        if (Main.mouseLeft)
        {
            // Don't start selection if mouse is over UI
            if (Main.LocalPlayer.mouseInterface)
                return;

            // Don't restart selection immediately after cancellation
            if (!wandPlayer.CanStartNewSelection())
                return;

            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
            }
            wandPlayer.UpdateSelection(mouseTile);
        }
        else if (wandPlayer.Selection.IsActive)
        {
            // Mouse released - execute only if this wand started the selection
            if (wandPlayer.IsSelectionOwnedByCurrentItem())
            {
                ExecuteWiring(player, wandPlayer);
            }
            wandPlayer.ClearSelection();
        }
    }
}
