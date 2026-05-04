using System;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.UI;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Static factory for the Wand of Coating's paint-color swatch grid (31 vanilla
/// paints + 1 "Ignore" swatch, 32 slots total).
/// </summary>
/// <remarks>
/// Extracted from <c>CoatingSettingsPanel.OnInitialize</c> in S6 2026-04-24
/// (W-S6-3, S+2 prep step 1) per the SessionPlan_WSW_Next3Sessions §S+2 Task 1.
/// The Phase C migration to <c>PaintColorSection</c> requires
/// the grid to be re-parentable into a <c>WandSubPanel</c> at runtime;
/// extraction makes that re-parenting a one-line operation in the host's
/// <c>SetBody</c> call.
///
/// <para>Body shape is verbatim from the original inline code: same 32-slot
/// (31 paint + 1 Ignore-sentinel) layout, same <c>UIPaintColorButton</c>
/// per-cell construction, same per-cell click handler binding to
/// <paramref name="onSelect"/>. Only the row arithmetic is parameterised on
/// <paramref name="columns"/>; the original constants <c>SwatchSize=22f</c> and
/// <c>SwatchGap=4f</c> are kept as defaults so the existing call site is a
/// byte-for-byte equivalent.</para>
///
/// <para>The Ignore-swatch label resolves through <see cref="Language.GetTextValue(string)"/>
/// against <c>Mods.WorldShapingWandsMod.UI.Coating.Ignore</c> — same key the
/// original inline code consulted via the panel's local <c>L(...)</c> helper.</para>
/// </remarks>
public static class CoatingPaintColorGrid
{
    // === DevTunables (v1.2, 2026-04-25 S3 — DesignDoc_PopoutFrameworkV1_2 §B Track 2) ===
    // v1.1 (S2) had these as plain `public const float`. v1.2 (S3) promotes them
    // to Func-backed properties bound to the DevTunable system at mod load via
    // `Common/Debug/PopoutTunables.cs`. The literal-fallback Funcs (() => 22f /
    // () => 4f) keep RELEASE behaviour identical to v1.1; only DEBUG builds with
    // PopoutTunables.RegisterAll() bound get live `/dev set` propagation.
    //
    // The literal compile-time constants `DefaultSwatchSizeLiteral` /
    // `DefaultSwatchGapLiteral` are kept for Build()'s default arg values
    // (default-arg expressions MUST be compile-time constants). The properties
    // are what callers SHOULD read for tunability flow-through.
    public const float DefaultSwatchSizeLiteral = 22f;
    public const float DefaultSwatchGapLiteral  = 4f;
    public const int   SwatchCount = 32;

    private static System.Func<float> _defaultSwatchSizeFn = static () => DefaultSwatchSizeLiteral;
    private static System.Func<float> _defaultSwatchGapFn  = static () => DefaultSwatchGapLiteral;

    public static float DefaultSwatchSize => _defaultSwatchSizeFn();
    public static float DefaultSwatchGap  => _defaultSwatchGapFn();

    /// <summary>
    /// DEBUG-only tunables binding entry point. Called from
    /// <c>PopoutTunables.RegisterAll</c>. Swaps the internal Funcs from
    /// literal-fallbacks to live DevTunable-backed lookups. RELEASE builds
    /// keep the literal fallbacks (semantically identical to v1.1's consts).
    /// </summary>
    internal static void BindTunables(
        System.Func<float> defaultSwatchSizeFn,
        System.Func<float> defaultSwatchGapFn)
    {
        if (defaultSwatchSizeFn != null) _defaultSwatchSizeFn = defaultSwatchSizeFn;
        if (defaultSwatchGapFn  != null) _defaultSwatchGapFn  = defaultSwatchGapFn;
    }

    private const string IgnoreLabelKey = "Mods.WorldShapingWandsMod.UI.Coating.Ignore";

    /// <summary>
    /// Build the 32-slot paint-color swatch grid as a single container element.
    /// </summary>
    /// <param name="columns">
    /// Column count for the grid layout. Row count is computed as
    /// <c>ceil(SwatchCount / columns)</c>. Original Coating panel uses 8.
    /// </param>
    /// <param name="containerInnerWidth">
    /// <b>[Obsolete since v1.1 popout framework, 2026-04-25 S2]</b> Formerly
    /// the available drawable width of the parent in pixels (parent width
    /// minus its own horizontal padding); used to bake a centring offset into
    /// every swatch's <c>Left.Pixels</c>. That bake-in was the root of Bug A
    /// (Diagnosis_CoatingPopoutMismatch §A): when the grid was re-parented
    /// into a 200px popout host, the baked 48px offset for a 300px parent
    /// pushed half the swatches off the right edge.
    /// <para>The grid now wraps its swatches in an inner strip of intrinsic
    /// width (<c>totalWidth</c>) and <c>HAlign=0.5f</c>, so it self-centres in
    /// whatever container it lives in (300px Coating panel, 200px popout host,
    /// or any future re-parent). The parameter is retained for source-compat
    /// only and is ignored.</para>
    /// </param>
    /// <param name="onSelect">
    /// Per-swatch click callback. Receives the paint byte
    /// (<c>0..30</c> = vanilla colour index; <c>WandOfCoatingBase.IgnorePaintColor</c>
    /// for the Ignore swatch).
    /// </param>
    /// <param name="buttons">
    /// Out array of every constructed <see cref="UIPaintColorButton"/>
    /// indexed by slot (<c>0..SwatchCount-1</c>). Caller can use this to
    /// drive selected-state visuals.
    /// </param>
    /// <param name="swatchSize">Per-cell pixel size. Default 22f matches WSW convention.</param>
    /// <param name="swatchGap">Per-cell pixel gap. Default 4f matches WSW convention.</param>
    /// <returns>
    /// A new <see cref="UIElement"/> container with full parent-width
    /// (<c>Width.Set(0f, 1f)</c>) and auto-computed <c>Height</c> based on the
    /// row count. Caller is responsible for setting <c>Top</c> on the returned
    /// container before appending it to its parent.
    /// </returns>
    public static UIElement Build(
        int columns,
        float containerInnerWidth,
        Action<byte> onSelect,
        out UIPaintColorButton[] buttons,
        float swatchSize = DefaultSwatchSizeLiteral,
        float swatchGap = DefaultSwatchGapLiteral)
    {
        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns,
                "Column count must be positive.");
        }
        if (onSelect is null)
        {
            throw new ArgumentNullException(nameof(onSelect));
        }

        // Bug-A fix (Diagnosis_CoatingPopoutMismatch §2 / DesignDoc_PopoutFrameworkV1_1 §3.1,
        // 2026-04-25 S2): button positions are now strip-relative and the strip itself
        // self-centres in its parent via HAlign=0.5f. The grid no longer "bakes" a parent-
        // width assumption into each button's Left.Pixels — so it renders correctly at
        // ANY container width (300px in the Coating panel, 200-ish in the popout host,
        // anything in the future). The `containerInnerWidth` parameter is therefore
        // unused; retained only for signature stability and marked Obsolete in XML doc.
        _ = containerInnerWidth; // intentional discard — see XmlDoc on the parameter.

        int rows = (SwatchCount + columns - 1) / columns;

        float totalWidth = 0f;
        for (int i = 0; i < columns; i++)
            totalWidth = LayoutSpacing.AddHorizontalSpace(totalWidth, swatchSize, i == 0 ? 0f : swatchGap);
        // Tighter row-major height: rows of (size + gap) minus the trailing inter-row gap
        // so the body's reported Height matches the visible content tightly. The popout
        // host sizes its panel from this Height.Pixels so accuracy here is load-bearing.
        float gridHeight = rows * (swatchSize + swatchGap) - swatchGap;

        var container = new UIElement();
        // v1.2 (S3, DesignDoc_PopoutFrameworkV1_2 §B Invariant I-8): the body
        // declares its intrinsic preferred Width.Pixels = totalWidth so the
        // popout host can derive panel size from `body.Width.Pixels` (instead
        // of relying on the now-removed DefaultBodyWidth literal). Self-centres
        // in any parent (300px coating panel, ~204px popout body slot) via
        // HAlign=0.5f — so this is a strict superset of v1.1's `Set(0f, 1f)`
        // semantics in the panel context (visually identical: a 204px-wide
        // grid centred in 300px panel vs. a 300px-wide container holding a
        // 204px-wide strip centred via HAlign=0.5f).
        container.Width.Set(totalWidth, 0f);
        container.Height.Set(gridHeight, 0f);
        container.HAlign = 0.5f;

        // INNER strip — sub-container of EXACT grid width that auto-centres in
        // its parent. Buttons are positioned strip-relative so they know nothing
        // about the outer container width. This is what makes the grid trivially
        // re-parentable into the popout host.
        var strip = new UIElement();
        strip.Width.Set(totalWidth, 0f);
        strip.Height.Set(gridHeight, 0f);
        strip.HAlign = 0.5f;
        container.Append(strip);

        buttons = new UIPaintColorButton[SwatchCount];

        Color[] paintColors = PaintPalette.Colors;
        string[] paintColorNames = PaintPalette.Names;
        string ignoreLabel = Language.GetTextValue(IgnoreLabelKey);

        for (int i = 0; i < SwatchCount; i++)
        {
            int arrayIndex = i;
            byte paintByte = (byte)(i < 31 ? i : WandOfCoatingBase.IgnorePaintColor);
            Color swatchColor = i < 31 ? paintColors[i] : Color.Transparent;
            string swatchName = i < 31 ? paintColorNames[i] : ignoreLabel;

            int col = i % columns;
            int row = i / columns;
            float sx = col * (swatchSize + swatchGap);   // strip-relative — no startX bake
            float sy = row * (swatchSize + swatchGap);

            var btn = new UIPaintColorButton(paintByte, swatchColor, swatchName);
            btn.Width.Set(swatchSize, 0f);
            btn.Height.Set(swatchSize, 0f);
            btn.Left.Set(sx, 0f);
            btn.Top.Set(sy, 0f);
            btn.OnLeftClick += (_, _) => onSelect(paintByte);
            buttons[arrayIndex] = btn;
            strip.Append(btn);
        }

        return container;
    }
}
