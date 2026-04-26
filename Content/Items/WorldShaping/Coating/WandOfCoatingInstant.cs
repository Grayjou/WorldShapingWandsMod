using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Coating.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
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

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(nameof(ItemID.GoldBar), 5)
            .AddRecipeGroup(nameof(ItemID.SilverBar), 10)
            .AddIngredient(ItemID.Paintbrush, 1)
            .AddIngredient(ItemID.PaintScraper, 1)
            .AddIngredient(ItemID.PaintRoller, 1)
            .AddIngredient(ItemID.CyanPaint, 1000)
            .AddIngredient(ItemID.VioletPaint, 1000)
            .AddIngredient(ItemID.YellowPaint, 1000)
            .AddIngredient(ItemID.WhitePaint, 1000)
            .AddIngredient(ItemID.BlackPaint, 1000)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
