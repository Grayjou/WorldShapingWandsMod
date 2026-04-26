using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Stamp channeling timing and behavior. Server-authoritative because
    /// it affects execution timing (exploitable if client-controlled in MP).
    /// </summary>
    [BackgroundColor(100, 60, 30, 200)]
    public class StampConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Stamp Channeling
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampChanneling.Header")]
        [DefaultValue(100)]
        [Range(10, 300)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampChannelFrames.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampChannelFrames.Tooltip")]
        public int StampChannelFrames { get; set; } = 100;

        [DefaultValue(10)]
        [Range(1, 60)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampRepeatFrames.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampRepeatFrames.Tooltip")]
        public int StampRepeatFrames { get; set; } = 10;

        [DefaultValue(20)]
        [Range(0, 300)]
        [Increment(5)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.CoatingStampChannelFrames.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.CoatingStampChannelFrames.Tooltip")]
        public int CoatingStampChannelFrames { get; set; } = 20;

        /// <summary>
        /// When true, the first click of a Stamp wand immediately executes the
        /// operation (the classic "click-and-done" behavior). When false, the
        /// first click only begins channeling — the operation executes after
        /// the full channel duration.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampExecuteOnClick.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.StampConfig.StampExecuteOnClick.Tooltip")]
        public bool StampExecuteOnClick { get; set; } = true;
    }
}
