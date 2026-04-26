using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Safekeeping.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// This file only defines mode identity, SetDefaults overrides, and the recipe.
/// </summary>
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
