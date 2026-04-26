using System.ComponentModel;
using Terraria.ModLoader.Config;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// TorchWheel projectile tuning — spacing, limits, backtrack depth,
    /// underwater torch behavior, animation, and photosensitivity settings.
    /// Server-authoritative — the host controls these in multiplayer.
    /// </summary>
    [BackgroundColor(120, 80, 20, 200)]
    public class TorchWheelConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ═════════════════════════════════════════════
        //  Torch Wheel — Spacing & Limits
        // ═════════════════════════════════════════════

        [Header("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheel.Header")]
        [DefaultValue(12)]
        [Range(1, 50)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelSpacingS.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelSpacingS.Tooltip")]
        public int TorchWheelSpacingS { get; set; } = 12;

        [DefaultValue(8)]
        [Range(1, 50)]
        [Increment(1)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelSpacingD.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelSpacingD.Tooltip")]
        public int TorchWheelSpacingD { get; set; } = 8;

        [DefaultValue(50)]
        [Range(1, 500)]
        [Increment(10)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxTorches.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxTorches.Tooltip")]
        public int TorchWheelMaxTorches { get; set; } = 50;

        /// <summary>
        /// Maximum number of tiles (steps) the Torch Wheel projectile can traverse
        /// before terminating.
        /// </summary>
        [DefaultValue(150)]
        [Range(50, 2000)]
        [Increment(50)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxTiles.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxTiles.Tooltip")]
        public int TorchWheelMaxTiles { get; set; } = 150;

        /// <summary>
        /// Maximum number of steps the Torch Wheel traces backward after the
        /// forward pass ends. Backtracking fills the gap left by the sliding-
        /// window warm-up near the first torch.
        /// <para>-1 = unlimited (capped internally at MaxTiles).</para>
        /// </summary>
        [DefaultValue(20)]
        [Range(-1, 500)]
        [Increment(5)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxBacktrackSteps.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelMaxBacktrackSteps.Tooltip")]
        public int TorchWheelMaxBacktrackSteps { get; set; } = 20;

        // ═════════════════════════════════════════════
        //  Torch Wheel — Behavior
        // ═════════════════════════════════════════════

        /// <summary>
        /// When enabled, the Torch Wheel placement projectiles can damage critters
        /// and break pots/plants on contact. Disable for a purely passive placement tool.
        /// <para>
        /// Affects all 4 Torch Wheel projectiles (solid placement, platform placement,
        /// flying solid, flying platform) uniformly.
        /// </para>
        /// </summary>
        [Header("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.Behavior.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelFriendly.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.TorchWheelFriendly.Tooltip")]
        public bool TorchWheelFriendly { get; set; } = false;

        // ═════════════════════════════════════════════
        //  Torch Wheel — Smart Torch Selection
        // ═════════════════════════════════════════════

        /// <summary>
        /// When the currently selected torch is not waterproof and the Torch Wheel
        /// encounters an underwater position, search the player's inventory for
        /// a waterproof torch to use instead. If off, underwater positions are skipped.
        /// </summary>
        [Header("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SmartTorchSelection.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.UnderwaterTorchLookup.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.UnderwaterTorchLookup.Tooltip")]
        public bool UnderwaterTorchLookup { get; set; } = true;

        /// <summary>
        /// When the current torch is not waterproof and no waterproof torch is found
        /// in inventory, convert regular torches into waterproof torches automatically.
        /// Uses Coral Torch in Ocean biome and evil-appropriate torch elsewhere.
        /// </summary>
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.AutoWaterproofTorches.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.AutoWaterproofTorches.Tooltip")]
        public bool AutoWaterproofTorches { get; set; } = false;

        /// <summary>
        /// Which waterproof torch to use when auto-converting in Ocean biome areas.
        /// </summary>
        [DefaultValue(OceanWaterproofTorch.CoralTorch)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.OceanWaterproofTorch.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.OceanWaterproofTorch.Tooltip")]
        public OceanWaterproofTorch OceanWaterproofTorch { get; set; } = OceanWaterproofTorch.CoralTorch;

        /// <summary>
        /// Which waterproof torch to use when auto-converting outside Ocean biome areas.
        /// </summary>
        [DefaultValue(NonOceanWaterproofTorch.EvilTorch)]
        [DrawTicks]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.NonOceanWaterproofTorch.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.NonOceanWaterproofTorch.Tooltip")]
        public NonOceanWaterproofTorch NonOceanWaterproofTorch { get; set; } = NonOceanWaterproofTorch.EvilTorch;

        /// <summary>
        /// When the player holds a regular torch and biome torch is active, search
        /// the inventory for the actual biome torch item first. If found, consume
        /// the biome torch directly instead of converting a regular torch. This
        /// avoids wasting biome torch stacks the player has collected.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SmartBiomeTorchLookup.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SmartBiomeTorchLookup.Tooltip")]
        public bool SmartBiomeTorchLookup { get; set; } = true;

        /// <summary>
        /// When enabled, underwater torch conversion will not produce Ichor or Cursed
        /// torches before Hardmode, since those require Hardmode materials to craft.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.EvilTorchRequiresHardmode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.EvilTorchRequiresHardmode.Tooltip")]
        public bool EvilTorchRequiresHardmode { get; set; } = true;

        /// <summary>
        /// When 'Evil Torch Requires Hardmode' blocks an evil torch conversion pre-Hardmode,
        /// substitute a Coral Torch instead. Has no effect if the Hardmode gate is off or
        /// if the world is already in Hardmode.
        /// </summary>
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SubstituteCoralTorchPreHardmode.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SubstituteCoralTorchPreHardmode.Tooltip")]
        public bool SubstituteCoralTorchPreHardmode { get; set; } = false;

        // ═════════════════════════════════════════════
        //  Torch Wheel — Visual & Safety
        // ═════════════════════════════════════════════

        /// <summary>
        /// When enabled, the Torch Wheel projectile follows a smooth velocity-based
        /// trajectory with gentle bobbing, creating an organic "firefly" feel.
        /// When disabled, the projectile uses legacy lerp smoothing (snaps more
        /// closely to the logical tile position each frame).
        /// </summary>
        [Header("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.VisualAndSafety.Header")]
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SmoothVisualPath.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.SmoothVisualPath.Tooltip")]
        public bool SmoothVisualPath { get; set; } = true;

        /// <summary>
        /// Animate the Torch Wheel projectile sprite (cycles color from grey to gold).
        /// When disabled, the projectile uses a static sprite with no flicker.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.AnimateTorchWheel.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.AnimateTorchWheel.Tooltip")]
        public bool AnimateTorchWheel { get; set; } = true;

        /// <summary>
        /// Show a chat warning on login when Outline Spacing (S) is set below 6,
        /// advising about potential photosensitivity issues from high-frequency flicker.
        /// The warning is suppressed when Animate Torch Wheel is disabled.
        /// </summary>
        [DefaultValue(true)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.PhotosensitivityWarning.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.TorchWheelConfig.PhotosensitivityWarning.Tooltip")]
        public bool PhotosensitivityWarning { get; set; } = true;
    }
}
