using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Server-authoritative safety gates for Magic Wand Read execution behavior.
    /// Defaults are conservative to prevent accidental large-area Instant/Select apply flows.
    /// </summary>
    [BackgroundColor(70, 90, 140, 200)]
    public class MagicWandReadSafetyConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.Safety.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadInInstantMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadInInstantMode.Tooltip")]
        public bool AllowReadInInstantMode { get; set; } = false;

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadInSelectMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadInSelectMode.Tooltip")]
        public bool AllowReadInSelectMode { get; set; } = false;

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadWithoutCanvas.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.MagicWandReadSafetyConfig.AllowReadWithoutCanvas.Tooltip")]
        public bool AllowReadWithoutCanvas { get; set; } = false;
    }
}
