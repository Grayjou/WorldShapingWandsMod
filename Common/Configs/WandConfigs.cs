using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Configs
{
    /// <summary>
    /// Static shorthand accessors for all granular config classes.
    /// Reduces boilerplate at call sites: <c>WandConfigs.Resources.InfiniteTileThreshold</c>
    /// instead of <c>ModContent.GetInstance&lt;ResourcesConfig&gt;().InfiniteTileThreshold</c>.
    /// Each property is a thin wrapper around <see cref="ModContent.GetInstance{T}"/>.
    /// </summary>
    public static class WandConfigs
    {
        // ═════════════════════════════════════════════
        //  Server-side configs
        // ═════════════════════════════════════════════

        public static ResourcesConfig Resources => ModContent.GetInstance<ResourcesConfig>();
        public static SandboxConfig Sandbox => ModContent.GetInstance<SandboxConfig>();
        public static LimitsConfig Limits => ModContent.GetInstance<LimitsConfig>();
        public static PerformanceConfig Performance => ModContent.GetInstance<PerformanceConfig>();
        public static StampConfig Stamp => ModContent.GetInstance<StampConfig>();
        public static TorchWheelConfig TorchWheel => ModContent.GetInstance<TorchWheelConfig>();
        public static CarefreeConfig Carefree => ModContent.GetInstance<CarefreeConfig>();

        // ═════════════════════════════════════════════
        //  Client-side configs
        // ═════════════════════════════════════════════

        public static OverlayConfig Overlay => ModContent.GetInstance<OverlayConfig>();
        public static CanvasOverlayConfig CanvasOverlay => ModContent.GetInstance<CanvasOverlayConfig>();
        public static PreferencesConfig Preferences => ModContent.GetInstance<PreferencesConfig>();
    }
}
