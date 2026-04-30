namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines how a shape is sliced to produce a half-shape.
/// The specific half (top/bottom or left/right) is determined by the
/// drag direction (Start vs End), matching how triangles and half-ellipses
/// already determine their orientation.
///
/// <para><b>S12 2026-04-29</b> — added <see cref="QuickHalfHorizontal"/> and
/// <see cref="QuickHalfVertical"/> per
/// <c>dev_notes/architecture/HalfShapeQuickSlice.md</c>. The "Quick" variants
/// interpret the user's drag bbox as the HALF SHAPE'S bbox (not the full
/// shape's bbox), saving the user from dragging twice as far to get the
/// intended result. Implementation: <c>SliceHelper.PreExpandForQuickSlice</c>
/// runs once before shape rasterization, mirrors the End point across the
/// slice axis, downgrades Slice to the corresponding non-quick value, and
/// the existing slicing pipeline runs unchanged.</para>
/// </summary>
public enum SliceMode : byte
{
    /// <summary>No slicing — full shape.</summary>
    Full = 0,

    /// <summary>
    /// Slice horizontally through the center, keeping one half.
    /// Drag bbox = the FULL underlying shape (mathematical model).
    /// Which half (top or bottom) is determined by the drag direction:
    ///   - Start.Y &lt;= End.Y → keep top half (flat side on bottom)
    ///   - Start.Y &gt; End.Y  → keep bottom half (flat side on top)
    /// </summary>
    HalfHorizontal = 1,

    /// <summary>
    /// Slice vertically through the center, keeping one half.
    /// Drag bbox = the FULL underlying shape (mathematical model).
    /// Which half (left or right) is determined by the drag direction:
    ///   - Start.X &lt;= End.X → keep left half (flat side on right)
    ///   - Start.X &gt; End.X  → keep right half (flat side on left)
    /// </summary>
    HalfVertical = 2,

    /// <summary>
    /// (S12 2026-04-29) Same as <see cref="HalfHorizontal"/> but the drag
    /// bbox describes the HALF SHAPE itself — the underlying full shape is
    /// twice as tall as the user dragged. Cursor travel matches result;
    /// loses the ½-tile rounding distinction in exchange for ergonomics.
    /// Pre-expansion in <c>SliceHelper.PreExpandForQuickSlice</c>.
    /// </summary>
    QuickHalfHorizontal = 3,

    /// <summary>
    /// (S12 2026-04-29) Same as <see cref="HalfVertical"/> but the drag
    /// bbox describes the HALF SHAPE itself — the underlying full shape is
    /// twice as wide as the user dragged. See <see cref="QuickHalfHorizontal"/>
    /// for the rationale.
    /// </summary>
    QuickHalfVertical = 4,
}
