namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Controls when the shape preview overlay is displayed.
/// </summary>
public enum PreviewMode
{
    /// <summary>
    /// Show preview only when holding a wand item (default behavior).
    /// </summary>
    Default,

    /// <summary>
    /// Always show preview, regardless of held item.
    /// </summary>
    Forced
}