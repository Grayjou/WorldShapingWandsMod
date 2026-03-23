using System.ComponentModel;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Server-authoritative configuration — affects gameplay balance, world state, and performance.
    /// In multiplayer the host sets these and they sync to all clients.
    /// </summary>
    [BackgroundColor(30, 60, 120, 200)]
    public class WandServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Infinite Resources
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteResources.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableInfiniteResource.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableInfiniteResource.Tooltip")]
        public bool EnableInfiniteResource { get; set; }

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteTileThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteTileThreshold.Tooltip")]
        public int InfiniteTileThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWallThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWallThreshold.Tooltip")]
        public int InfiniteWallThreshold { get; set; } = 999;

        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteGrassSeedThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteGrassSeedThreshold.Tooltip")]
        public int InfiniteGrassSeedThreshold { get; set; } = 50;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWireThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWireThreshold.Tooltip")]
        public int InfiniteWireThreshold { get; set; } = 999;

        [DefaultValue(999)]
        [Range(0, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteActuatorThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteActuatorThreshold.Tooltip")]
        public int InfiniteActuatorThreshold { get; set; } = 999;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteTiles.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteTiles.Tooltip")]
        public InfiniteOverride InfiniteTiles { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWalls.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWalls.Tooltip")]
        public InfiniteOverride InfiniteWalls { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteGrassSeeds.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteGrassSeeds.Tooltip")]
        public InfiniteOverride InfiniteGrassSeeds { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWires.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteWires.Tooltip")]
        public InfiniteOverride InfiniteWires { get; set; } = InfiniteOverride.Default;

        [DefaultValue(InfiniteOverride.Default)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteActuators.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.InfiniteActuators.Tooltip")]
        public InfiniteOverride InfiniteActuators { get; set; } = InfiniteOverride.Default;

        // ═════════════════════════════════════════════
        //  Selection Limits
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SelectionLimits.Header")]
        [DefaultValue(1000)]
        [Range(10, 10000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SmallSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SmallSelectionCap.Tooltip")]
        public int SmallSelectionCap { get; set; } = 1000;

        [DefaultValue(200)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.BigSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.BigSelectionCap.Tooltip")]
        public int BigSelectionCap { get; set; } = 200;

        [DefaultValue(400)]
        [Range(10, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.HollowSelectionCap.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.HollowSelectionCap.Tooltip")]
        public int HollowSelectionCap { get; set; } = 400;

        // ═════════════════════════════════════════════
        //  Sandbox Options
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.Sandbox.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SuppressDrops.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SuppressDrops.Tooltip")]
        public bool SuppressDrops { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.BypassPickaxePower.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.BypassPickaxePower.Tooltip")]
        public bool BypassPickaxePower { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AllowDemonAltarDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AllowDemonAltarDestruction.Tooltip")]
        public bool AllowDemonAltarDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AllowDelicateTileDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AllowDelicateTileDestruction.Tooltip")]
        public bool AllowDelicateTileDestruction { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.IgnoreLockedKeyRequirements.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.IgnoreLockedKeyRequirements.Tooltip")]
        public bool IgnoreLockedKeyRequirements { get; set; }

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AutoOpenChestsOnDestruction.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.AutoOpenChestsOnDestruction.Tooltip")]
        public bool AutoOpenChestsOnDestruction { get; set; }

        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.VacuumItems.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.VacuumItems.Tooltip")]
        public bool VacuumItems { get; set; } = true;

        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableUndoCommand.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableUndoCommand.Tooltip")]
        public bool EnableUndoCommand { get; set; } = false;

        [DefaultValue(10)]
        [Range(1, 100)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.MaxOutlineThickness.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.MaxOutlineThickness.Tooltip")]
        public int MaxOutlineThickness { get; set; } = 10;

        /// <summary>
        /// Server-side cooldown between wand operations, in game ticks (60 ticks = 1 second).
        /// Prevents autoclicker or click-spam abuse. 0 disables the cooldown.
        /// Applied per-player on the server for MP; checked client-side in SP.
        /// </summary>
        [DefaultValue(12)]
        [Range(0, 120)]
        [Increment(6)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.OperationCooldownTicks.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.OperationCooldownTicks.Tooltip")]
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
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.MaxConfirmDistance.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.MaxConfirmDistance.Tooltip")]
        public int MaxConfirmDistance { get; set; } = 50;

        // ═════════════════════════════════════════════
        //  Progressive Mode
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.ProgressiveMode.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableProgressiveMode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.EnableProgressiveMode.Tooltip")]
        public bool EnableProgressiveMode { get; set; } = true;

        [DefaultValue(400)]
        [Range(50, 2000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.ProgressiveBatchSize.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.ProgressiveBatchSize.Tooltip")]
        public int ProgressiveBatchSize { get; set; } = 400;

        [DefaultValue(0.3f)]
        [Range(0.1f, 2.0f)]
        [Increment(0.05f)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.ProgressiveInterval.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.ProgressiveInterval.Tooltip")]
        public float ProgressiveInterval { get; set; } = 0.3f;

        // ═════════════════════════════════════════════
        //  Safekeeping
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.Safekeeping.Header")]
        [DefaultValue(50)]
        [Range(0, 10000)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SafekeepingClearThreshold.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandServerConfig.SafekeepingClearThreshold.Tooltip")]
        public int SafekeepingClearThreshold { get; set; } = 50;

        // ═════════════════════════════════════════════
        //  Helper methods (migrated from WandConfig)
        // ═════════════════════════════════════════════

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
