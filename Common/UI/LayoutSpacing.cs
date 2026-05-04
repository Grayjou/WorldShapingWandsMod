using System;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Shared layout math helpers for panel/sub-UI construction code.
/// Keeps spacing intent explicit at call sites.
///
/// Horizontal convention (added C-S4 2026-05-03):
///   Use <see cref="AddHorizontalSpace"/> to build a row's intrinsic width element by element.
///   Use <see cref="FitHorizontalSpace"/> to fold each finished row width into the panel's
///   running max width. Apply panel padding once after all rows are folded.
///
/// See: dev_notes/inbox/DesignDoc_FitHorizontalSpace_Helper.md §4 for the full convention.
/// </summary>
internal static class LayoutSpacing
{
    /// <summary>
    /// Returns <paramref name="currentSize"/> + <paramref name="elementSize"/> + <paramref name="bottomPadding"/>.
    /// </summary>
    public static int AddVerticalSpace(int currentSize, int elementSize, int bottomPadding)
        => currentSize + elementSize + bottomPadding;

    /// <summary>
    /// Float overload for existing UI layout code that uses pixel values as floats.
    /// </summary>
    public static float AddVerticalSpace(float currentSize, float elementSize, float bottomPadding)
        => currentSize + elementSize + bottomPadding;

    // ── Horizontal layout helpers ─────────────────────────────────────

    /// <summary>
    /// Accumulates an element's width into a running row width.
    /// Use this to build row widths element by element; call <see cref="FitHorizontalSpace"/>
    /// once per row to fold the completed row into the panel's max width.
    /// </summary>
    /// <param name="currentWidth">Running row width so far.</param>
    /// <param name="elementWidth">Width of the next element in the row.</param>
    /// <param name="elementPadding">Optional leading gap before this element (e.g. inter-button gap). Default 0.</param>
    /// <returns><paramref name="currentWidth"/> + <paramref name="elementWidth"/> + <paramref name="elementPadding"/>.</returns>
    public static float AddHorizontalSpace(float currentWidth, float elementWidth, float elementPadding = 0f)
        => currentWidth + elementWidth + elementPadding;

    /// <summary>
    /// Folds a completed row's width into the panel's running max width (a simple max).
    /// Panel padding is NOT included here; add it once after all rows are folded.
    /// Rows that intentionally consume panel padding should skip this call and
    /// clamp themselves against the already-established panel width.
    /// </summary>
    /// <param name="currentWidth">Panel's current max width.</param>
    /// <param name="rowWidth">This row's intrinsic width (built with <see cref="AddHorizontalSpace"/>).</param>
    /// <returns>max(<paramref name="currentWidth"/>, <paramref name="rowWidth"/>).</returns>
    public static float FitHorizontalSpace(float currentWidth, float rowWidth)
        => MathF.Max(currentWidth, rowWidth);
}
