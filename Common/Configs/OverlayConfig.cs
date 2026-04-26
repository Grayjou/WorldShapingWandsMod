using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Selection overlay colors, opacity, rendering mode, per-family color toggles.
    /// Client-side — each player sets their own.
    /// </summary>
    [BackgroundColor(120, 60, 30, 200)]
    public class OverlayConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ═════════════════════════════════════════════
        //  Overlay
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.Overlay.Header")]
        [DefaultValue(OverlayRenderMode.Auto)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayRenderMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayRenderMode.Tooltip")]
        public OverlayRenderMode OverlayRenderMode { get; set; } = OverlayRenderMode.Auto;

        /// <summary>
        /// Fill opacity for selected tiles in the overlay (0.1–1.0).
        /// Must always be at least 0.1 higher than <see cref="NegativeSpaceAlpha"/>.
        /// </summary>
        [DefaultValue(0.18f)]
        [Range(0.1f, 1.0f)]
        [Increment(0.02f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.ShapeOverlayAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.ShapeOverlayAlpha.Tooltip")]
        public float ShapeOverlayAlpha { get; set; } = 0.18f;

        /// <summary>
        /// Fill opacity for negative-space tiles (bounding box minus shape) in the overlay (0.0–1.0).
        /// Set to 0 to disable. Must always be at least 0.1 below <see cref="ShapeOverlayAlpha"/>.
        /// </summary>
        [DefaultValue(0.05f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.02f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.NegativeSpaceAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.NegativeSpaceAlpha.Tooltip")]
        public float NegativeSpaceAlpha { get; set; } = 0.05f;

        /// <summary>
        /// The base color used for the selection overlay (fill, outline, cursor highlight).
        /// Opacity is controlled separately by <see cref="ShapeOverlayAlpha"/>.
        /// Default: LimeGreen (50, 205, 50).
        /// </summary>
        [DefaultValue(typeof(Color), "50, 205, 50, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.SelectionOverlayColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.SelectionOverlayColor.Tooltip")]
        public Color SelectionOverlayColor { get; set; } = new Color(50, 205, 50, 255);

        /// <summary>
        /// When true, the selection overlay uses per-family colors instead of
        /// the single <see cref="SelectionOverlayColor"/>.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.UsePerFamilyOverlayColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.UsePerFamilyOverlayColor.Tooltip")]
        public bool UsePerFamilyOverlayColor { get; set; } = true;

        /// <summary>
        /// Minimum brightness for the overlay color, applied at the first selection step.
        /// </summary>
        [DefaultValue(0.6f)]
        [Range(0.3f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayBrightnessMin.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayBrightnessMin.Tooltip")]
        public float OverlayBrightnessMin { get; set; } = 0.6f;

        /// <summary>
        /// Maximum brightness for the overlay color, applied at the final selection step.
        /// </summary>
        [DefaultValue(1.0f)]
        [Range(0.3f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayBrightnessMax.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.OverlayBrightnessMax.Tooltip")]
        public float OverlayBrightnessMax { get; set; } = 1.0f;

        /// <summary>
        /// When true, shows a preview of torch tiling positions.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.ShowTilingPreview.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.ShowTilingPreview.Tooltip")]
        public bool ShowTilingPreview { get; set; } = true;

        /// <summary>
        /// Controls how the stamp overlay is positioned when dragging.
        /// <b>Precise</b>: Snaps to tile grid (no sub-pixel offset).
        /// <b>Smooth</b>: Applies sub-pixel offset for smooth mouse-following.
        /// </summary>
        // S6 2026-04-24 (W-S6-1, default re-flip): Smooth is restored as
        // the default after GrayJou's S8 verbatim verdict on Cavendish-side
        // ("v3 works perfectly", "re-flip to Smooth acceptable", "no paused
        // bug") closed G-1. Precise remains available as opt-out for users
        // who prefer the old per-tile snap aesthetic. The defensive-Precise
        // default (S3 W-S3-1) and its conservative retention through S4
        // (W-S4-1 v3 ship) served their purpose — the v3 world-space ease
        // is now the canonical render path.
        [DefaultValue(StampRenderMode.Smooth)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.StampRenderMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.OverlayConfig.StampRenderMode.Tooltip")]
        public StampRenderMode StampRenderMode { get; set; } = StampRenderMode.Smooth;

        // ─── W-S4-1 (S4 2026-04-24) cleanup note ─────────────────────────────
        // The former W-S3-1 `DebugDualDraw` field + its localization block were
        // removed here per the easy-to-remove contract (grep token: `W-S3-1
        // dual-draw`). Verdict (β) was returned in Letter #3, the diagnostic
        // served its purpose, and v3 replaces the smooth path entirely.

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
