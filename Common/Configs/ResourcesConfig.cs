using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Infinite resource thresholds and per-type overrides.
    /// Server-authoritative — the host controls these in multiplayer.
    /// </summary>
    [BackgroundColor(40, 80, 40, 200)]
    public class ResourcesConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Infinite Resources
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteResources.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.EnableInfiniteResource.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.EnableInfiniteResource.Tooltip")]
        public bool EnableInfiniteResource { get; set; }

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteTileThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteTileThreshold.Tooltip")]
        public int InfiniteTileThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWallThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWallThreshold.Tooltip")]
        public int InfiniteWallThreshold { get; set; } = 999;

        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteGrassSeedThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteGrassSeedThreshold.Tooltip")]
        public int InfiniteGrassSeedThreshold { get; set; } = 50;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWireThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWireThreshold.Tooltip")]
        public int InfiniteWireThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteActuatorThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteActuatorThreshold.Tooltip")]
        public int InfiniteActuatorThreshold { get; set; } = 999;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteTiles.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteTiles.Tooltip")]
        public InfiniteOverride InfiniteTiles { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWalls.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWalls.Tooltip")]
        public InfiniteOverride InfiniteWalls { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteGrassSeeds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteGrassSeeds.Tooltip")]
        public InfiniteOverride InfiniteGrassSeeds { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWires.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteWires.Tooltip")]
        public InfiniteOverride InfiniteWires { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteActuators.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.ResourcesConfig.InfiniteActuators.Tooltip")]
        public InfiniteOverride InfiniteActuators { get; set; } = InfiniteOverride.Default;

        // ═════════════════════════════════════════════
        //  Helper methods (Carefree-aware)
        // ═════════════════════════════════════════════

        private bool IsCarefree => WandConfigs.Carefree?.EnableCarefreeMode ?? false;

        /// <summary>
        /// Returns the per-type threshold for infinite resource mode.
        /// If the threshold is 0, the resource is always infinite (no minimum stack required).
        /// Otherwise, the player must have at least this many items for consumption to be skipped.
        /// </summary>
        public int GetThresholdForPlaceType(PlaceType placeType)
        {
            if (IsCarefree) return 0;
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
            if (IsCarefree) return 0;
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
            if (IsCarefree) return true;
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
            if (IsCarefree && objectType != ObjectType.Air) return true;
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
        public bool IsInfiniteForWires => IsCarefree || ResolveOverride(InfiniteWires);

        /// <summary>
        /// Checks if infinite resource mode is active for actuators.
        /// </summary>
        public bool IsInfiniteForActuators => IsCarefree || ResolveOverride(InfiniteActuators);
    }
}
