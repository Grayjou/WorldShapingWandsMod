#if DEBUG
using System;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// DevTunable registrations for live-tunable UI sizing knobs. Adjust at
/// runtime via <c>/dev set CoatingPaintColorGrid.X</c>. Live edits affect the
/// NEXT panel construction (already-realised elements don't resize live).
///
/// <para>(Phase C 2026-04-29) The <c>CollapsedPopoutHost.*</c> tunable block
/// — chrome sizing, padding, header height, X/Y offsets, safe-spawn margin —
/// was retired together with the host itself when the PaintColor section
/// migrated to the unified <see cref="WandSubPanel"/> substrate. The new
/// popout chrome's sizing is fixed by <c>WandSubPanel.ChromeHeight</c> +
/// <c>ChromeButtonSize</c> + <c>ChromePadding</c>; the safe-spawn margin is
/// hard-coded in <c>PaintColorSection.ResolveSpawnPosition</c> at the legacy
/// 16f default. Re-introducing tunable knobs is a future polish item if any
/// of those constants need iteration in the field.</para>
///
/// <para>The swatch dimensions (<c>CoatingPaintColorGrid.*</c>) survive
/// Phase C unchanged — they parameterise the swatch grid itself, not the
/// popout chrome around it.</para>
/// </summary>
internal static class PopoutTunables
{
    // ── CoatingPaintColorGrid swatch dimensions ────────────────────
    public static Func<float> SwatchSize;
    public static Func<float> SwatchGap;

    /// <summary>
    /// Registers all UI tunables surviving Phase C and binds them into the
    /// grid via its <c>BindTunables</c> entry point. Called from
    /// <see cref="DevTunableDefaults.RegisterAll"/>.
    /// </summary>
    public static void RegisterAll()
    {
        // ── Grid swatch dimensions ──
        SwatchSize = DevTunable.RegisterFloat("CoatingPaintColorGrid.DefaultSwatchSize", 22f, "Paint colour swatch button edge length (px)", min: 16f, max: 32f);
        SwatchGap  = DevTunable.RegisterFloat("CoatingPaintColorGrid.DefaultSwatchGap",   4f, "Paint colour swatch inter-button gap (px)",   min:  0f, max: 12f);

        // ── Wire into consumers ──
        CoatingPaintColorGrid.BindTunables(
            defaultSwatchSizeFn: SwatchSize,
            defaultSwatchGapFn:  SwatchGap);
    }
}
#endif
