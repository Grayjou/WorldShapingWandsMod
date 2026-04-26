using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Select (TwoClick) mode for the Wand of Wiring.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfWiringSelect : WandOfWiringBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfWiringConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfWiringInstant>();
}