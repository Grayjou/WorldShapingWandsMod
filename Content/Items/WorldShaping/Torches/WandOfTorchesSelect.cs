using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Select (TwoClick) mode for the Wand of Torches.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfTorchesSelect : WandOfTorchesBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => WandColors.Torches.Select;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfTorchesConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfTorchesInstant>();
}
