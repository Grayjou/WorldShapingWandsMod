using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Select (TwoClick) mode for the Wand of Fluids.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfFluidsSelect : WandOfFluidsBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => WandColors.Fluids.Select;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfFluidsConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfFluidsInstant>();
}
