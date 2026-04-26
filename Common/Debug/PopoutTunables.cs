#if DEBUG
using System;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// DevTunable registrations for the Popout Framework v1.2 chrome / sizing knobs.
/// All parameters can be adjusted in real-time via <c>/dev set CollapsedPopoutHost.X</c>
/// or <c>/dev set CoatingPaintColorGrid.X</c>. Live edits affect the NEXT
/// <c>OpenWith</c> call (already-open popouts don't resize live — see
/// <c>DesignDoc_PopoutFrameworkV1_2 §B.4</c>).
/// <para>
/// Design reference:
/// <c>dev_notes/inbox/addressed/Cavendish 2026-04-25_Session_3/DesignDoc_PopoutFrameworkV1_2.md §B Track 2</c>
/// </para>
/// </summary>
internal static class PopoutTunables
{
    // ── CollapsedPopoutHost chrome (sizing/padding) ────────────────
    public static Func<float> DefaultBodyWidth;
    public static Func<float> DefaultBodyHeight;
    public static Func<float> HorizontalPadding;
    public static Func<float> VerticalPadding;
    public static Func<float> HeaderHeight;

    // ── CollapsedPopoutHost initial-placement nudges (NEW v1.2) ──
    public static Func<float> PopoutXOffset;
    public static Func<float> PopoutYOffset;

    // ── CollapsedPopoutHost first-spawn safe placement (NEW v1.3 §C) ──
    public static Func<float> SafeSpawnMargin;

    // ── CoatingPaintColorGrid swatch dimensions ────────────────────
    public static Func<float> SwatchSize;
    public static Func<float> SwatchGap;

    /// <summary>
    /// Registers all popout-framework tunables and binds them into the
    /// host / grid via their <c>BindTunables</c> entry points. Called from
    /// <see cref="DevTunableDefaults.RegisterAll"/>.
    /// </summary>
    public static void RegisterAll()
    {
        // ── Host chrome ──
        DefaultBodyWidth  = DevTunable.RegisterFloat("CollapsedPopoutHost.DefaultBodyWidth",  200f, "Popout body fallback width (px) when body has no intrinsic Width.Pixels",  min:  50f, max: 500f);
        DefaultBodyHeight = DevTunable.RegisterFloat("CollapsedPopoutHost.DefaultBodyHeight",  80f, "Popout body fallback height (px) when body has no intrinsic Height.Pixels", min:  40f, max: 400f);
        HorizontalPadding = DevTunable.RegisterFloat("CollapsedPopoutHost.HorizontalPadding",   6f, "Popout horizontal inner padding (px, applied each side)",                   min:   0f, max:  32f);
        VerticalPadding   = DevTunable.RegisterFloat("CollapsedPopoutHost.VerticalPadding",     6f, "Popout vertical inner padding (px, applied below header & at bottom)",      min:   0f, max:  32f);
        HeaderHeight      = DevTunable.RegisterFloat("CollapsedPopoutHost.HeaderHeight",       22f, "Popout header strip height (px)",                                            min:  12f, max:  40f);

        // ── Initial-placement nudges ──
        PopoutXOffset = DevTunable.RegisterFloat("CollapsedPopoutHost.PopoutXOffset", 0f, "Additive X offset (px) applied to the popout's first-open and saved-restore positions", min: -200f, max: 200f);
        PopoutYOffset = DevTunable.RegisterFloat("CollapsedPopoutHost.PopoutYOffset", 0f, "Additive Y offset (px) applied to the popout's first-open and saved-restore positions", min: -200f, max: 200f);

        // ── First-spawn safe placement (v1.3 §C) ──
        SafeSpawnMargin = DevTunable.RegisterFloat("CollapsedPopoutHost.SafeSpawnMargin", 16f, "Margin (px) between owning parent panel and first-time popout placement (v1.3 §C)", min: 0f, max: 64f);

        // ── Grid swatch dimensions ──
        SwatchSize = DevTunable.RegisterFloat("CoatingPaintColorGrid.DefaultSwatchSize", 22f, "Paint colour swatch button edge length (px)", min: 16f, max: 32f);
        SwatchGap  = DevTunable.RegisterFloat("CoatingPaintColorGrid.DefaultSwatchGap",   4f, "Paint colour swatch inter-button gap (px)",   min:  0f, max: 12f);

        // ── Wire into consumers ──
        CollapsedPopoutHost.BindTunables(
            defaultBodyWidthFn:  DefaultBodyWidth,
            defaultBodyHeightFn: DefaultBodyHeight,
            horizontalPaddingFn: HorizontalPadding,
            verticalPaddingFn:   VerticalPadding,
            headerHeightFn:      HeaderHeight,
            popoutXOffsetFn:     PopoutXOffset,
            popoutYOffsetFn:     PopoutYOffset,
            safeSpawnMarginFn:   SafeSpawnMargin);

        CoatingPaintColorGrid.BindTunables(
            defaultSwatchSizeFn: SwatchSize,
            defaultSwatchGapFn:  SwatchGap);
    }
}
#endif
