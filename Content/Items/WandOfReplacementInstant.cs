using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Utilities;
using System;

namespace WorldShapingWandsMod.Content.Items;

public class WandOfReplacementInstant : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant (dangerous)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null; // prevent sound spam during drag — played once on selection start
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
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }
            wandPlayer.UpdateSelection(mouseTile);
        }
        else if (wandPlayer.Selection.IsActive)
        {
            // Don't execute if mouse released over UI (e.g. NPC shop)
            if (Main.LocalPlayer.mouseInterface)
            {
                wandPlayer.ClearSelection();
                return;
            }

            // Mouse released - execute only if this wand started the selection
            if (wandPlayer.IsSelectionOwnedByCurrentItem())
            {
                ExecuteReplacement(player, wandPlayer);
            }
            wandPlayer.ClearSelection();
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(WandRecipeSystem.AnyWandOfBuildingKey, 1)
            .AddRecipeGroup(WandRecipeSystem.AnyWandOfDismantlingKey, 1)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
