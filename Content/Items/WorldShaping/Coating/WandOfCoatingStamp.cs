using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Stamp (FourClick) mode for the Wand of Coating.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// Overrides GetChannelFrames for shorter channel time ("Free Paint Picasso" experience).
/// </summary>
public class WandOfCoatingStamp : WandOfCoatingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => WandColors.Coating.Stamp;
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfCoatingInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;   // Keep wand visually held during stamp channeling
        Item.UseSound = null;  // Prevent sound spam — channeling feedback is via dust/charge sound
    }

    /// <summary>
    /// Coating wands use a shorter channel time for the "Free Paint Picasso" experience.
    /// Set to 0 in config for instant channeling (no charge delay).
    /// </summary>
    protected override int GetChannelFrames()
    {
        var config = WandConfigs.Stamp;
        return config?.CoatingStampChannelFrames ?? 20;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfCoatingInstant>();
}
