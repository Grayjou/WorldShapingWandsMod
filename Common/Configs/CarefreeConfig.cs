using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Carefree Mode master toggle and per-feature overrides. Server-authoritative
    /// because it bypasses balance gates.
    /// </summary>
    [BackgroundColor(60, 120, 60, 200)]
    public class CarefreeConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Carefree Mode
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeMode.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.EnableCarefreeMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.EnableCarefreeMode.Tooltip")]
        public bool EnableCarefreeMode { get; set; }

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeSuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeSuppressDrops.Tooltip")]
        public bool CarefreeSuppressDrops { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeAllowDemonAltarDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeAllowDemonAltarDestruction.Tooltip")]
        public bool CarefreeAllowDemonAltarDestruction { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeAllowDelicateTileDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.CarefreeConfig.CarefreeAllowDelicateTileDestruction.Tooltip")]
        public bool CarefreeAllowDelicateTileDestruction { get; set; } = true;
    }
}
