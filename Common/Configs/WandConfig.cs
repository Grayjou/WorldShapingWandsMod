using System.ComponentModel;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

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

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteTileThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteTileThreshold.Tooltip")]
        public int InfiniteTileThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWallThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWallThreshold.Tooltip")]
        public int InfiniteWallThreshold { get; set; } = 999;

        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteGrassSeedThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteGrassSeedThreshold.Tooltip")]
        public int InfiniteGrassSeedThreshold { get; set; } = 50;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWireThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWireThreshold.Tooltip")]
        public int InfiniteWireThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteActuatorThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteActuatorThreshold.Tooltip")]
        public int InfiniteActuatorThreshold { get; set; } = 999;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteTiles.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteTiles.Tooltip")]
        public InfiniteOverride InfiniteTiles { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWalls.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWalls.Tooltip")]
        public InfiniteOverride InfiniteWalls { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteGrassSeeds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteGrassSeeds.Tooltip")]
        public InfiniteOverride InfiniteGrassSeeds { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWires.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteWires.Tooltip")]
        public InfiniteOverride InfiniteWires { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteActuators.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteActuators.Tooltip")]
        public InfiniteOverride InfiniteActuators { get; set; } = InfiniteOverride.Default;

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

        [DefaultValue(400)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.HollowSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.HollowSelectionCap.Tooltip")]
        public int HollowSelectionCap { get; set; } = 400;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Sandbox.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SuppressDrops.Tooltip")]
        public bool SuppressDrops { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BypassPickaxePower.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.BypassPickaxePower.Tooltip")]
        public bool BypassPickaxePower { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AllowDemonAltarDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AllowDemonAltarDestruction.Tooltip")]
        public bool AllowDemonAltarDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AllowDelicateTileDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AllowDelicateTileDestruction.Tooltip")]
        public bool AllowDelicateTileDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.IgnoreLockedKeyRequirements.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.IgnoreLockedKeyRequirements.Tooltip")]
        public bool IgnoreLockedKeyRequirements { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AutoOpenChestsOnDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.AutoOpenChestsOnDestruction.Tooltip")]
        public bool AutoOpenChestsOnDestruction { get; set; }

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.VacuumItems.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.VacuumItems.Tooltip")]
        public bool VacuumItems { get; set; } = true;

        [DefaultValue(10)]
        [Range(1, 100)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.MaxOutlineThickness.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.MaxOutlineThickness.Tooltip")]
        public int MaxOutlineThickness { get; set; } = 10;

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

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Safekeeping.Header")]
        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SafekeepingClearThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.SafekeepingClearThreshold.Tooltip")]
        public int SafekeepingClearThreshold { get; set; } = 50;
        // =============================================
        //  Carefree Mode
        // =============================================

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeMode.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableCarefreeMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableCarefreeMode.Tooltip")]
        public bool EnableCarefreeMode { get; set; }

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeSuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeSuppressDrops.Tooltip")]
        public bool CarefreeSuppressDrops { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeAllowDemonAltarDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeAllowDemonAltarDestruction.Tooltip")]
        public bool CarefreeAllowDemonAltarDestruction { get; set; } = true;

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeAllowDelicateTileDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.CarefreeAllowDelicateTileDestruction.Tooltip")]
        public bool CarefreeAllowDelicateTileDestruction { get; set; } = true;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Audio.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableWandSounds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableWandSounds.Tooltip")]
        public bool EnableWandSounds { get; set; } = true;

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.Overlay.Header")]
        [DefaultValue(OverlayRenderMode.Auto)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.OverlayRenderMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.OverlayRenderMode.Tooltip")]
        public OverlayRenderMode OverlayRenderMode { get; set; } = OverlayRenderMode.Auto;

        /// <summary>
        /// Returns the per-type threshold for infinite resource mode.
        /// If the threshold is 0, the resource is always infinite (no minimum stack required).
        /// Otherwise, the player must have at least this many items for consumption to be skipped.
        /// </summary>
        public int GetThresholdForPlaceType(PlaceType placeType)
        {
            return placeType switch
            {
                PlaceType.Solid => InfiniteTileThreshold,
                PlaceType.Platform => InfiniteTileThreshold,
                PlaceType.Rope => InfiniteTileThreshold,
                PlaceType.Rail => InfiniteTileThreshold,
                PlaceType.PlantPot => InfiniteTileThreshold,
                PlaceType.GrassSeed => InfiniteGrassSeedThreshold,
                PlaceType.Wall => InfiniteWallThreshold,
                _ => InfiniteTileThreshold
            };
        }

        /// <summary>
        /// Returns the per-type threshold for infinite resource mode (ObjectType variant).
        /// </summary>
        public int GetThresholdForObjectType(ObjectType objectType)
        {
            return objectType switch
            {
                ObjectType.Tile => InfiniteTileThreshold,
                ObjectType.Platform => InfiniteTileThreshold,
                ObjectType.Rope => InfiniteTileThreshold,
                ObjectType.PlanterBox => InfiniteTileThreshold,
                ObjectType.Rail => InfiniteTileThreshold,
                ObjectType.Seeds => InfiniteGrassSeedThreshold,
                ObjectType.Wall => InfiniteWallThreshold,
                _ => InfiniteTileThreshold
            };
        }

        /// <summary>
        /// Resolves an InfiniteOverride value against the master toggle.
        /// ForceOff → false, ForceOn → true, Default → follows EnableInfiniteResource.
        /// </summary>
        private bool ResolveOverride(InfiniteOverride ov)
        {
            return ov switch
            {
                InfiniteOverride.ForceOff => false,
                InfiniteOverride.ForceOn => true,
                _ => EnableInfiniteResource // Default
            };
        }

        /// <summary>
        /// Checks if infinite resource mode is active for the given PlaceType.
        /// Uses the 3-state per-type override: ForceOff always disables,
        /// ForceOn always enables, Default follows the master toggle.
        /// </summary>
        public bool IsInfiniteForPlaceType(PlaceType placeType)
        {
            return placeType switch
            {
                PlaceType.Solid => ResolveOverride(InfiniteTiles),
                PlaceType.Platform => ResolveOverride(InfiniteTiles),
                PlaceType.Rope => ResolveOverride(InfiniteTiles),
                PlaceType.Rail => ResolveOverride(InfiniteTiles),
                PlaceType.PlantPot => ResolveOverride(InfiniteTiles),
                PlaceType.GrassSeed => ResolveOverride(InfiniteGrassSeeds),
                PlaceType.Wall => ResolveOverride(InfiniteWalls),
                _ => ResolveOverride(InfiniteTiles) // fallback
            };
        }

        /// <summary>
        /// Checks if infinite resource mode is active for the given ObjectType.
        /// Used by the replacement wand which operates on ObjectType rather than PlaceType.
        /// </summary>
        public bool IsInfiniteForObjectType(ObjectType objectType)
        {
            return objectType switch
            {
                ObjectType.Tile => ResolveOverride(InfiniteTiles),
                ObjectType.Platform => ResolveOverride(InfiniteTiles),
                ObjectType.Rope => ResolveOverride(InfiniteTiles),
                ObjectType.PlanterBox => ResolveOverride(InfiniteTiles),
                ObjectType.Rail => ResolveOverride(InfiniteTiles),
                ObjectType.Seeds => ResolveOverride(InfiniteGrassSeeds),
                ObjectType.Wall => ResolveOverride(InfiniteWalls),
                ObjectType.Air => false, // Air never consumes
                _ => ResolveOverride(InfiniteTiles) // fallback
            };
        }

        /// <summary>
        /// Checks if infinite resource mode is active for wires.
        /// </summary>
        public bool IsInfiniteForWires => ResolveOverride(InfiniteWires);

        /// <summary>
        /// Checks if infinite resource mode is active for actuators.
        /// </summary>
        public bool IsInfiniteForActuators => ResolveOverride(InfiniteActuators);
    }
}