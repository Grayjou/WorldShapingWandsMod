using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Coating.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfCoatingConfirm : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => WandColors.Coating.Confirm;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfCoatingInstant>();
}
