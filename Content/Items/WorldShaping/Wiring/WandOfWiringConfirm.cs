using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Wiring.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfWiringConfirm : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfWiringInstant>();
}