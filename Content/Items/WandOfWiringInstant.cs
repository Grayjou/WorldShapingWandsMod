using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
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
        Point mouseTile = GeometryHelper.GetMouseTile();

        if (Main.mouseLeft)
        {
            // Don't start selection if mouse is over UI
            // IsMouseOverUI() checks both mouseInterface AND panel hover,
            // because HoldItem runs before UI.Update sets mouseInterface.
            if (IsMouseOverUI())
            {

                return;
            }

            // Don't restart selection immediately after cancellation
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

            // Right-click during a drag cancels the selection without executing.
            if (Main.mouseRight && wandPlayer.InstantSelection.IsActive)
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelWiring, wandPlayer.WiringSettings.Shape);
                return;
            }
        }
        else if (wandPlayer.InstantSelection.IsActive)
        {
            // Don't execute if mouse released over UI (e.g. NPC shop)
            if (IsMouseOverUI())
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelWiring, wandPlayer.WiringSettings.Shape);
                return;
            }

            // Mouse released - execute only if this wand started the selection
            if (wandPlayer.IsInstantSelectionOwnedByCurrentItem() && !IsOnLocalCooldown())
            {
                ExecuteWiring(player, wandPlayer);
            }
            wandPlayer.ClearInstantSelection();
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.WireKite, 1)
            .AddIngredient(ItemID.Wire, 50)
            .AddRecipeGroup(nameof(ItemID.CopperBar), 5)
            .AddRecipeGroup(nameof(ItemID.IronBar), 5)
            .AddRecipeGroup(nameof(ItemID.SilverBar), 2)
            .AddIngredient(ItemID.Glass, 10)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
