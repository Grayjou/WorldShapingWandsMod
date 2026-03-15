namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines how a shape is sliced to produce a half-shape.
/// The specific half (top/bottom or left/right) is determined by the
/// drag direction (Start vs End), matching how triangles and half-ellipses
/// already determine their orientation.
/// </summary>
public enum SliceMode : byte
{
    /// <summary>No slicing — full shape.</summary>
    Full = 0,

    /// <summary>
    /// Slice horizontally through the center, keeping one half.
    /// Which half (top or bottom) is determined by the drag direction:
    ///   - Start.Y &lt;= End.Y → keep top half (flat side on bottom)
    ///   - Start.Y &gt; End.Y  → keep bottom half (flat side on top)
    /// </summary>
    HalfHorizontal = 1,

    /// <summary>
    /// Slice vertically through the center, keeping one half.
    /// Which half (left or right) is determined by the drag direction:
    ///   - Start.X &lt;= End.X → keep left half (flat side on right)
    ///   - Start.X &gt; End.X  → keep right half (flat side on left)
    /// </summary>
    HalfVertical = 2,
}
