using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Stamp (FourClick) mode for the Wand of Fluids.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfFluidsStamp : WandOfFluidsBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => WandColors.Fluids.Stamp;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfFluidsInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // Keep wand visually held during stamp channeling
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfFluidsInstant>();
}
