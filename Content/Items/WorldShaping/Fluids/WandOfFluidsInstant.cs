using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Fluids.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfFluidsInstant : WandOfFluidsBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => WandColors.Fluids.Instant;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfFluidsSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // needed for drag detection
        Item.UseSound = null;  // prevent sound spam during drag
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(nameof(ItemID.GoldBar), 5)
            .AddRecipeGroup(nameof(ItemID.SilverBar), 10)
            .AddIngredient(ItemID.Glass, 30)
            .AddIngredient(ItemID.WaterBucket, 3)
            .AddIngredient(ItemID.LavaBucket, 3)
            .AddIngredient(ItemID.HoneyBucket, 3)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
