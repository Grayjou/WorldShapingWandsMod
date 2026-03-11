using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfSafekeepingInstant : WandOfSafekeepingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfSafekeepingSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null; // prevent sound spam during drag — played once on selection start
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
            if (Main.LocalPlayer.mouseInterface)
                return;

            if (!wandPlayer.CanStartNewSelection())
                return;

            if (!wandPlayer.Selection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartSelection(mouseTile, vertical);
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }

            wandPlayer.UpdateSelection(mouseTile);
        }
        else if (wandPlayer.Selection.IsActive)
        {
            // Don't execute if mouse released over UI
            if (Main.LocalPlayer.mouseInterface)
            {
                wandPlayer.ClearSelection();
                return;
            }

            if (wandPlayer.IsSelectionOwnedByCurrentItem())
            {
                ExecuteSafekeeping(player, wandPlayer);
            }
            wandPlayer.ClearSelection();
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(nameof(ItemID.GoldBar), 5)
            .AddRecipeGroup(nameof(ItemID.SilverBar), 10)
            .AddRecipeGroup(WandRecipeSystem.AnyGemKey, 5)
            .AddIngredient(ItemID.Obsidian, 20)
            .AddRecipeGroup(nameof(ItemID.EbonstoneBlock), 10)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
