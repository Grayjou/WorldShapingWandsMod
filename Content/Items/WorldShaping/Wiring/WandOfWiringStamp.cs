using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Stamp (FourClick) mode for the Wand of Wiring.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// This file only defines mode identity, SetDefaults overrides, and the recipe.
/// </summary>
public class WandOfWiringStamp : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new Color(100, 200, 255); // Light blue — Stamp
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // Keep wand visually held during stamp channeling
        Item.UseSound = null;  // Prevent sound spam — channeling feedback is via dust/charge sound
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfWiringInstant>();
}
