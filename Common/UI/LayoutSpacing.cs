namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Shared layout math helpers for panel/sub-UI construction code.
/// Keeps spacing intent explicit at call sites.
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
}
