using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Torches.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfTorchesConfirm : WandOfTorchesBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => WandColors.Torches.Confirm;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfTorchesStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfTorchesInstant>();
}
