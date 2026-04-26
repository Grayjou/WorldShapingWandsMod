using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Instant (OneClick) mode for the Wand of Torches.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfTorchesInstant : WandOfTorchesBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => WandColors.Torches.Instant;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfTorchesSelect>();

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
            .AddRecipeGroup(nameof(ItemID.IronBar), 10)
            .AddIngredient(ItemID.Torch, 50)
            .AddIngredient(ItemID.Gel, 20)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
