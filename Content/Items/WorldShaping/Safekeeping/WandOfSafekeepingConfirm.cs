using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Safekeeping.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfSafekeepingConfirm : WandOfSafekeepingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfSafekeepingStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfSafekeepingInstant>();
}