using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Stamp (FourClick) mode for the Wand of Torches.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfTorchesStamp : WandOfTorchesBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => WandColors.Torches.Stamp;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfTorchesInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // Keep wand visually held during stamp channeling
        Item.UseSound = null;  // Prevent sound spam
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfTorchesInstant>();
}
