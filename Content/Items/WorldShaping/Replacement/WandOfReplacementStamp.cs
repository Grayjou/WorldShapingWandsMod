using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Stamp (FourClick) mode for the Wand of Replacement.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfReplacementStamp : WandOfReplacementBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new Color(100, 200, 255); // Light blue — Stamp
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfReplacementInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // Keep wand visually held during stamp channeling
        Item.UseSound = null;  // Prevent sound spam
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfReplacementInstant>();
}
