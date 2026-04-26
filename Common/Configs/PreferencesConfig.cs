using System.ComponentModel;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Small client-side preferences: tooltips, audio, undo, feedback,
    /// stamp channeling client toggles.
    /// Client-side — each player sets their own.
    /// </summary>
    [BackgroundColor(80, 60, 100, 200)]
    public class PreferencesConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ═════════════════════════════════════════════
        //  Tooltips
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.Tooltips.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowLoreTooltips.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowLoreTooltips.Tooltip")]
        public bool ShowLoreTooltips { get; set; } = true;

        // ═════════════════════════════════════════════
        //  Audio
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.Audio.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.EnableWandSounds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.EnableWandSounds.Tooltip")]
        public bool EnableWandSounds { get; set; } = true;

        // ═════════════════════════════════════════════
        //  Undo
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.Undo.Header")]
        [DefaultValue(20)]
        [Range(1, 100)]
        [Increment(5)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.MaxUndoStackSize.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.MaxUndoStackSize.Tooltip")]
        public int MaxUndoStackSize { get; set; } = 20;

        // ═════════════════════════════════════════════
        //  Stamp Channeling (Client toggles)
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.StampChanneling.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChanneling.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChanneling.Tooltip")]
        public bool AllowChanneling { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChannelingDust.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChannelingDust.Tooltip")]
        public bool AllowChannelingDust { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChannelingSound.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AllowChannelingSound.Tooltip")]
        public bool AllowChannelingSound { get; set; } = true;

        // ═════════════════════════════════════════════
        //  Feedback
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.Feedback.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.WandVerbosity.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.WandVerbosity.Tooltip")]
        public bool WandVerbosity { get; set; } = true;

        // ═════════════════════════════════════════════
        //  Shape Defaults
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShapeDefaults.Header")]
        [DefaultValue(ShapeType.Rectangle)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.DefaultShapeType.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.DefaultShapeType.Tooltip")]
        public ShapeType DefaultShapeType { get; set; } = ShapeType.Rectangle;

        [DefaultValue(ShapeMode.Filled)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.DefaultShapeMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.DefaultShapeMode.Tooltip")]
        public ShapeMode DefaultShapeMode { get; set; } = ShapeMode.Filled;

        // ═════════════════════════════════════════════
        //  Rain Fill Visual Effects
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.RainFillEffects.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.RainFillSummonsClouds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.RainFillSummonsClouds.Tooltip")]
        public bool RainFillSummonsClouds { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.RainFillSpawnDusts.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.RainFillSpawnDusts.Tooltip")]
        public bool RainFillSpawnDusts { get; set; } = true;

        [DefaultValue(LavaRainStyle.Embers)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.LavaRainStyle.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.LavaRainStyle.Tooltip")]
        public LavaRainStyle LavaRainStyle { get; set; } = LavaRainStyle.Embers;

        // ═════════════════════════════════════════════
        //  Block Exhaustion Behavior (building wands)
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Header")]
        [DefaultValue(BlockExhaustionMode.NextBlock)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Tooltip")]
        public BlockExhaustionMode BlockExhaustion { get; set; } = BlockExhaustionMode.NextBlock;
    }
}
