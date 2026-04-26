using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items
{
    /// <summary>
    /// Select (TwoClick) mode for the Wand of Building.
    /// All mode-specific input logic lives in BaseCyclingWand's template methods.
    /// </summary>
    public class WandOfBuildingSelect : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
        public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingConfirm>();

        public override void AddRecipes() => RegisterNonInstantRecipe<WandOfBuildingInstant>();
    }
}