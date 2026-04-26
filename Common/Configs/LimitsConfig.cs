using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Selection dimension caps, outline thickness, cooldowns, distance limits —
    /// anything that bounds the scope or frequency of operations.
    /// Server-authoritative — the host controls these in multiplayer.
    /// </summary>
    [BackgroundColor(30, 60, 120, 200)]
    public class LimitsConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Selection Limits
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.SelectionLimits.Header")]
        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.SmallSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.SmallSelectionCap.Tooltip")]
        public int SmallSelectionCap { get; set; } = 1000;

        [DefaultValue(200)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.BigSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.BigSelectionCap.Tooltip")]
        public int BigSelectionCap { get; set; } = 200;

        [DefaultValue(400)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.HollowSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.HollowSelectionCap.Tooltip")]
        public int HollowSelectionCap { get; set; } = 400;

        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.CoatingSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.CoatingSelectionCap.Tooltip")]
        public int CoatingSelectionCap { get; set; } = 1000;

        // Molding and Delimitation are also cheap, instant, no-batching operations
        // (canvas / filter manipulation — no tile mutation per cell). They share
        // the generous Coating-tier limit by default.
        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MoldingSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MoldingSelectionCap.Tooltip")]
        public int MoldingSelectionCap { get; set; } = 1000;

        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.DelimitationSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.DelimitationSelectionCap.Tooltip")]
        public int DelimitationSelectionCap { get; set; } = 1000;

        // ═════════════════════════════════════════════
        //  Operation Limits
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.OperationLimits.Header")]
        [DefaultValue(10)]
        [Range(1, 100)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MaxOutlineThickness.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MaxOutlineThickness.Tooltip")]
        public int MaxOutlineThickness { get; set; } = 10;

        /// <summary>
        /// Server-side cooldown between wand operations, in game ticks (60 ticks = 1 second).
        /// Prevents autoclicker or click-spam abuse. 0 disables the cooldown.
        /// Applied per-player on the server for MP; checked client-side in SP.
        /// </summary>
        [DefaultValue(12)]
        [Range(0, 120)]
        [Increment(6)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.OperationCooldownTicks.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.OperationCooldownTicks.Tooltip")]
        public int OperationCooldownTicks { get; set; } = 12;

        /// <summary>
        /// Maximum distance (in tiles) from the closest edge of the selection
        /// bounding box at which a Confirm or Stamp click is accepted.
        /// Prevents accidental long-range confirms after teleporting.
        /// 0 disables the distance check.
        /// </summary>
        [DefaultValue(50)]
        [Range(0, 500)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MaxConfirmDistance.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.MaxConfirmDistance.Tooltip")]
        public int MaxConfirmDistance { get; set; } = 50;

        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.SafekeepingClearThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.LimitsConfig.SafekeepingClearThreshold.Tooltip")]
        public int SafekeepingClearThreshold { get; set; } = 50;
    }
}
