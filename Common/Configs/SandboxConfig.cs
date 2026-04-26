using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// World-protection toggles and sandbox behavior overrides.
    /// Server-authoritative — the host controls these in multiplayer.
    /// </summary>
    [BackgroundColor(120, 40, 40, 200)]
    public class SandboxConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Sandbox Options
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.Sandbox.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.SuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.SuppressDrops.Tooltip")]
        public bool SuppressDrops { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.BypassPickaxePower.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.BypassPickaxePower.Tooltip")]
        public bool BypassPickaxePower { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AllowDemonAltarDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AllowDemonAltarDestruction.Tooltip")]
        public bool AllowDemonAltarDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AllowDelicateTileDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AllowDelicateTileDestruction.Tooltip")]
        public bool AllowDelicateTileDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.IgnoreLockedKeyRequirements.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.IgnoreLockedKeyRequirements.Tooltip")]
        public bool IgnoreLockedKeyRequirements { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AutoOpenChestsOnDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.AutoOpenChestsOnDestruction.Tooltip")]
        public bool AutoOpenChestsOnDestruction { get; set; }

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.VacuumItems.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.VacuumItems.Tooltip")]
        public bool VacuumItems { get; set; } = true;

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.EnableUndoCommand.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.SandboxConfig.EnableUndoCommand.Tooltip")]
        public bool EnableUndoCommand { get; set; } = false;

        // ═════════════════════════════════════════════
        //  Effective* helpers (Carefree-aware)
        // ═════════════════════════════════════════════

        private bool IsCarefree => WandConfigs.Carefree?.EnableCarefreeMode ?? false;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveSuppressDrops =>
            IsCarefree ? (WandConfigs.Carefree?.CarefreeSuppressDrops ?? true) : SuppressDrops;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveBypassPickaxePower =>
            IsCarefree || BypassPickaxePower;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveAllowDemonAltarDestruction =>
            IsCarefree ? (WandConfigs.Carefree?.CarefreeAllowDemonAltarDestruction ?? true) : AllowDemonAltarDestruction;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveAllowDelicateTileDestruction =>
            IsCarefree ? (WandConfigs.Carefree?.CarefreeAllowDelicateTileDestruction ?? true) : AllowDelicateTileDestruction;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveIgnoreLockedKeyRequirements =>
            IsCarefree || IgnoreLockedKeyRequirements;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool EffectiveAutoOpenChestsOnDestruction =>
            IsCarefree || AutoOpenChestsOnDestruction;
    }
}
