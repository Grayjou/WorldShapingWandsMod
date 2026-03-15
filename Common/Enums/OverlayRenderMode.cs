namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Controls how the selection overlay is rendered for large shapes.
/// </summary>
public enum OverlayRenderMode
{
    /// <summary>
    /// Default behavior: shows bounding-box outline while dragging large shapes,
    /// then fades in the full shape once the mouse stops.
    /// </summary>
    Auto,

    /// <summary>
    /// Always render the full shape overlay, even for very large shapes.
    /// May cause lag during dragging if the shape is extremely large.
    /// </summary>
    AlwaysFullShape,

    /// <summary>
    /// Always show only the bounding-box outline, never rasterize the full shape.
    /// Lightest on performance.
    /// </summary>
    AlwaysBoundingBox
}
