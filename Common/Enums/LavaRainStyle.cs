namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Visual style for the cosmetic lava rain particles emitted by
/// <see cref="WorldShapingWandsMod.Content.Projectiles.RainCloudProjectile"/> when
/// Rain Fill / Pocket Fill places lava.
/// <para>
/// Originally lava rain layered both an Embers core (dust 127, OrangeTorch) and a
/// SolarFlare secondary (dust 259) — the combined readout was visually noisy. Session
/// 2026-04-20 S1 removed the SolarFlare layer; client testing in 2026-04-21 S1 found
/// that the SolarFlare style read cleanly *on its own*, so the choice is now exposed
/// to the player as a Preferences toggle rather than baked into the wand.
/// </para>
/// </summary>
/// <remarks>
/// Default: <see cref="Embers"/> (preserves shipped 1.0.0 visuals).
/// </remarks>
public enum LavaRainStyle : byte
{
    /// <summary>Bright glowing embers only (dust 127, OrangeTorch). Crisp, lower density. Default.</summary>
    Embers = 0,

    /// <summary>SolarFlare blobs only (dust 259). Slower, larger, dreamier rain.</summary>
    SolarFlare = 1,

    /// <summary>Both layers combined — denser, busier visual. Pre-2026-04-20 behaviour.</summary>
    Both = 2,
}
