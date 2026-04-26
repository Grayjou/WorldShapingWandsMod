using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Replacement.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfReplacementInstant : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null; // prevent sound spam during drag
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
