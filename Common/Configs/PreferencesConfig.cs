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
        //  Wand Panel Buttons
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.WandPanelButtons.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowHelpButton.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowHelpButton.Tooltip")]
        public bool ShowHelpButton { get; set; } = true;

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowInfoButton.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowInfoButton.Tooltip")]
        public bool ShowInfoButton { get; set; } = false;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowTooltipButton.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowTooltipButton.Tooltip")]
        public bool ShowTooltipButton { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowLongTooltips.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.ShowLongTooltips.Tooltip")]
        public bool ShowLongTooltips { get; set; } = true;

        [DefaultValue(WandPanelDragMode.HandleOrAnywhere)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.WandPanelDragMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.WandPanelDragMode.Tooltip")]
        public WandPanelDragMode WandPanelDragMode { get; set; } = WandPanelDragMode.HandleOrAnywhere;

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.TransformMode.Header")]
        [DefaultValue(TransformAnchorTMOff.Pivot)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.TransformAnchorTMOff.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.TransformAnchorTMOff.Tooltip")]
        public TransformAnchorTMOff TransformAnchorTMOff { get; set; } = TransformAnchorTMOff.Pivot;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AlwaysShowPivot.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.AlwaysShowPivot.Tooltip")]
        public bool AlwaysShowPivot { get; set; } = true;

        // ═════════════════════════════════════════════
        //  Block Exhaustion Behavior (building wands)
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Header")]
        [DefaultValue(BlockExhaustionMode.NextBlock)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.PreferencesConfig.BlockExhaustion.Tooltip")]
        public BlockExhaustionMode BlockExhaustion { get; set; } = BlockExhaustionMode.NextBlock;

        // ═════════════════════════════════════════════
        //  Planification
        // ═════════════════════════════════════════════

        [Header("Planification")]
        [DefaultValue(true)]
        public bool SharedPlanificationRenderConfig { get; set; } = true;

        [DefaultValue(false)]
        public bool UseFirstStencilColorForAll { get; set; } = false;
    }
}
