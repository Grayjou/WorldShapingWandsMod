using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    public class WandConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide; // Can be ServerSide if you want per-world

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResources.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableInfiniteResource.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableInfiniteResource.Tooltip")]
        public bool EnableInfiniteResource { get; set; }

        [DefaultValue(0)]
        [Range(0, 10000)]
        [Increment(100)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResourceAmount.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResourceAmount.Tooltip")]
        public int InfiniteResourceAmount { get; set; }

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.SelectionLimits.Header")]
        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SmallSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SmallSelectionCap.Tooltip")]
        public int SmallSelectionCap { get; set; } = 1000;

        [DefaultValue(200)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BigSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BigSelectionCap.Tooltip")]
        public int BigSelectionCap { get; set; } = 200;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Sandbox.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SuppressDrops.Tooltip")]
        public bool SuppressDrops { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BypassPickaxePower.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BypassPickaxePower.Tooltip")]
        public bool BypassPickaxePower { get; set; }

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.ProgressiveMode.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableProgressiveMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableProgressiveMode.Tooltip")]
        public bool EnableProgressiveMode { get; set; } = true;

        [DefaultValue(400)]
        [Range(50, 2000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ProgressiveBatchSize.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ProgressiveBatchSize.Tooltip")]
        public int ProgressiveBatchSize { get; set; } = 400;

        [DefaultValue(0.3f)]
        [Range(0.1f, 2.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ProgressiveInterval.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ProgressiveInterval.Tooltip")]
        public float ProgressiveInterval { get; set; } = 0.3f;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Tooltips.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ShowLoreTooltips.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.ShowLoreTooltips.Tooltip")]
        public bool ShowLoreTooltips { get; set; } = true;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Audio.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableWandSounds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableWandSounds.Tooltip")]
        public bool EnableWandSounds { get; set; } = true;
    }
}