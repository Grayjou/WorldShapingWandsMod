using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Progressive batching tuning. Will grow when Progression System adds
    /// per-tier batch sizes.
    /// Server-authoritative — the host controls these in multiplayer.
    /// </summary>
    [BackgroundColor(60, 60, 100, 200)]
    public class PerformanceConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Progressive Mode
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.ProgressiveMode.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.EnableProgressiveMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.EnableProgressiveMode.Tooltip")]
        public bool EnableProgressiveMode { get; set; } = true;

        [DefaultValue(400)]
        [Range(50, 2000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.ProgressiveBatchSize.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.ProgressiveBatchSize.Tooltip")]
        public int ProgressiveBatchSize { get; set; } = 400;

        [DefaultValue(0.3f)]
        [Range(0.1f, 2.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.ProgressiveInterval.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PerformanceConfig.ProgressiveInterval.Tooltip")]
        public float ProgressiveInterval { get; set; } = 0.3f;
    }
}
