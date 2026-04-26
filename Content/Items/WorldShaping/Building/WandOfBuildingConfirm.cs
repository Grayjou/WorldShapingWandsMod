using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items
{
    /// <summary>
    /// Confirm (ThreeClick) mode for the Wand of Building.
    /// All mode-specific input logic lives in BaseCyclingWand's template methods.
    /// </summary>
    public class WandOfBuildingConfirm : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
        public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingStamp>();

        public override void AddRecipes() => RegisterNonInstantRecipe<WandOfBuildingInstant>();
    }
}