using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfCoatingInstant : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => WandColors.Coating.Instant;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        return false; // All logic in HoldItem
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
            if (IsMouseOverUI())
            {
                DebugIsMouseOverUI("WandOfCoatingInstant blocked");
                return;
            }

            if (!wandPlayer.CanStartNewSelection())
                return;

            if (!wandPlayer.InstantSelection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartInstantSelection(mouseTile, vertical);
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }

            wandPlayer.UpdateInstantSelection(mouseTile);

            if (Main.mouseRight && wandPlayer.InstantSelection.IsActive)
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelCoating, wandPlayer.CoatingSettings.Shape);
                return;
            }
        }
        else if (wandPlayer.InstantSelection.IsActive)
        {
            if (IsMouseOverUI())
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelCoating, wandPlayer.CoatingSettings.Shape);
                return;
            }

            if (wandPlayer.IsInstantSelectionOwnedByCurrentItem())
            {
                ExecuteCoating(player, wandPlayer);
            }
            wandPlayer.ClearInstantSelection();
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(nameof(ItemID.GoldBar), 5)
            .AddRecipeGroup(nameof(ItemID.SilverBar), 10)
            .AddIngredient(ItemID.Paintbrush, 1)
            .AddIngredient(ItemID.PaintScraper, 1)
            .AddIngredient(ItemID.PaintRoller, 1)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
