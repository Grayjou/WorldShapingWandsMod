using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Client-side configuration â€” personal audio, visual, and UI preferences.
    /// Each player sets their own; changes take effect immediately.
    /// </summary>
    [BackgroundColor(120, 60, 30, 200)]
    public class WandClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  Tooltips
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [Header("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.Tooltips.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.ShowLoreTooltips.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.ShowLoreTooltips.Tooltip")]
        public bool ShowLoreTooltips { get; set; } = true;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  Audio
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [Header("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.Audio.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.EnableWandSounds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.EnableWandSounds.Tooltip")]
        public bool EnableWandSounds { get; set; } = true;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  Overlay
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [Header("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.Overlay.Header")]
        [DefaultValue(OverlayRenderMode.Auto)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.OverlayRenderMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.OverlayRenderMode.Tooltip")]
        public OverlayRenderMode OverlayRenderMode { get; set; } = OverlayRenderMode.Auto;

        /// <summary>
        /// Fill opacity for selected tiles in the overlay (0.1â€“1.0).
        /// Must always be at least 0.1 higher than <see cref="NegativeSpaceAlpha"/>.
        /// </summary>
        [DefaultValue(0.18f)]
        [Range(0.1f, 1.0f)]
        [Increment(0.02f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.ShapeOverlayAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.ShapeOverlayAlpha.Tooltip")]
        public float ShapeOverlayAlpha { get; set; } = 0.18f;

        /// <summary>
        /// Fill opacity for negative-space tiles (bounding box minus shape) in the overlay (0.0â€“1.0).
        /// Set to 0 to disable. Must always be at least 0.1 below <see cref="ShapeOverlayAlpha"/>.
        /// The constraint is enforced live in rendering, not in the config UI.
        /// </summary>
        [DefaultValue(0.05f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.02f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.NegativeSpaceAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.NegativeSpaceAlpha.Tooltip")]
        public float NegativeSpaceAlpha { get; set; } = 0.05f;

        /// <summary>
        /// The base color used for the selection overlay (fill, outline, cursor highlight).
        /// Opacity is controlled separately by <see cref="ShapeOverlayAlpha"/>.
        /// The alpha channel of this color is ignored — use the alpha sliders above instead.
        /// Default: LimeGreen (50, 205, 50).
        /// </summary>
        [DefaultValue(typeof(Color), "50, 205, 50, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.SelectionOverlayColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.SelectionOverlayColor.Tooltip")]
        public Color SelectionOverlayColor { get; set; } = new Color(50, 205, 50, 255);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  Undo
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [Header("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.Undo.Header")]
        [DefaultValue(20)]
        [Range(1, 100)]
        [Increment(5)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.MaxUndoStackSize.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.MaxUndoStackSize.Tooltip")]
        public int MaxUndoStackSize { get; set; } = 20;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  Feedback
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        [Header("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.Feedback.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.WandVerbosity.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandClientConfig.WandVerbosity.Tooltip")]
        public bool WandVerbosity { get; set; } = true;

        /// <summary>
        /// Returns the effective negative space alpha, clamped so it is always
        /// at least 0.1 below <see cref="ShapeOverlayAlpha"/>.
        /// </summary>
        public float GetEffectiveNegativeSpaceAlpha()
        {
            float maxAllowed = ShapeOverlayAlpha - 0.1f;
            return Math.Max(0f, Math.Min(NegativeSpaceAlpha, maxAllowed));
        }
    }
}
