using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Fluids.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfFluidsConfirm : WandOfFluidsBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => WandColors.Fluids.Confirm;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfFluidsStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfFluidsInstant>();
}
