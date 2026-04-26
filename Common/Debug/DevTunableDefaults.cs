#if DEBUG
using System;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// Central registration of all dev-tunable parameters.
/// Each registration returns a <c>Func&lt;T&gt;</c> that reads the live value.
/// Store the <c>Func</c> in a static field near the code that uses it.
/// <para>
/// Add new tunables here as you need to calibrate new parameters.
/// Delete registrations after baking final values into production code.
/// </para>
/// </summary>
/// <remarks>
/// Design reference: <c>dev_notes/planning/DebugPipelinePlan.md</c> §2.3
/// <para>
/// Previously calibrated tunables (ModeProj, TWP, TWS, Molding, Safekeeping,
/// Fluids positioning, Colors) were baked on 2026-04-18. Archive:
/// <c>dev_notes/reference/ArchivedDevTunableValues.md</c>
/// </para>
/// </remarks>
public static class DevTunableDefaults
{
    /// <summary>
    /// Registers all dev tunables. Called from <see cref="DevTunable.Initialize"/>.
    /// </summary>
    public static void RegisterAll()
    {
        // ── Rain Liquid Dust System (active — being tuned) ──
        Content.Projectiles.RainDustTunables.RegisterAll();

        // ── Popout Framework v1.2 chrome / sizing / placement (S3, 2026-04-25) ──
        // Cavendish DesignDoc_PopoutFrameworkV1_2 §B Track 2: GrayJou can live-tune
        // popout body fallback dimensions, chrome padding, header height, and
        // initial-placement X/Y offsets, plus paint-grid swatch size + gap.
        PopoutTunables.RegisterAll();
    }
}
#endif
