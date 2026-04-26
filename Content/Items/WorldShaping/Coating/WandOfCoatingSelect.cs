using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Select (TwoClick) mode for the Wand of Coating.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfCoatingSelect : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => WandColors.Coating.Select;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfCoatingInstant>();
}
