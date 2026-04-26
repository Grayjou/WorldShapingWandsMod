using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Wiring.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// This file only defines mode identity, SetDefaults overrides, and the recipe.
/// </summary>
public class WandOfWiringInstant : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null; // prevent sound spam during drag — played once on selection start
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
