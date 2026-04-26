using System.ComponentModel;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Canvas overlay colors and alpha for Delimitation and Molding wands.
    /// Client-side — each player sets their own.
    /// </summary>
    [BackgroundColor(100, 80, 30, 200)]
    public class CanvasOverlayConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ═════════════════════════════════════════════
        //  Canvas Overlay (Delimitation Wand three-layer model)
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasOverlay.Header")]
        [DefaultValue(0.2f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasOutsideAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasOutsideAlpha.Tooltip")]
        public float CanvasOutsideAlpha { get; set; } = 0.2f;

        [DefaultValue(0.4f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasFillAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasFillAlpha.Tooltip")]
        public float CanvasFillAlpha { get; set; } = 0.4f;

        [DefaultValue(0.4f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasTileSelectionAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasTileSelectionAlpha.Tooltip")]
        public float CanvasTileSelectionAlpha { get; set; } = 0.4f;

        [DefaultValue(typeof(Color), "0, 0, 0, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasOutsideColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasOutsideColor.Tooltip")]
        public Color CanvasOutsideColor { get; set; } = new Color(0, 0, 0, 255);

        [DefaultValue(typeof(Color), "255, 255, 255, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasFillColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasFillColor.Tooltip")]
        public Color CanvasFillColor { get; set; } = new Color(255, 255, 255, 255);

        [DefaultValue(typeof(Color), "128, 128, 0, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasTileSelectionColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.CanvasTileSelectionColor.Tooltip")]
        public Color CanvasTileSelectionColor { get; set; } = new Color(128, 128, 0, 255);

        // ═════════════════════════════════════════════
        //  Molding Canvas Overlay (Mold Wand three-layer model)
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingCanvasOverlay.Header")]
        [DefaultValue(0.2f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingOutsideAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingOutsideAlpha.Tooltip")]
        public float MoldingOutsideAlpha { get; set; } = 0.2f;

        [DefaultValue(0.4f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingCanvasFillAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingCanvasFillAlpha.Tooltip")]
        public float MoldingCanvasFillAlpha { get; set; } = 0.4f;

        [DefaultValue(0.4f)]
        [Range(0.0f, 1.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingTileSelectionAlpha.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingTileSelectionAlpha.Tooltip")]
        public float MoldingTileSelectionAlpha { get; set; } = 0.4f;

        [DefaultValue(typeof(Color), "0, 0, 0, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingOutsideColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingOutsideColor.Tooltip")]
        public Color MoldingOutsideColor { get; set; } = new Color(0, 0, 0, 255);

        [DefaultValue(typeof(Color), "200, 255, 255, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingCanvasColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingCanvasColor.Tooltip")]
        public Color MoldingCanvasColor { get; set; } = new Color(200, 255, 255, 255);

        [DefaultValue(typeof(Color), "0, 180, 180, 255")]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingTileSelectionColor.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CanvasOverlayConfig.MoldingTileSelectionColor.Tooltip")]
        public Color MoldingTileSelectionColor { get; set; } = new Color(0, 180, 180, 255);
    }
}
