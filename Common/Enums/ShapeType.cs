namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the geometric shape to be drawn or filled.
/// Half-shape variants (e.g. half-ellipse, half-diamond) are produced
/// by combining a base shape with <see cref="SliceMode"/>, not via
/// separate enum values.
/// </summary>
public enum ShapeType : byte
{
    Rectangle = 0,
    Ellipse = 1,
    Diamond = 2,
    Triangle = 3,
    Elbow = 4,
    CardinalLine = 5,
    StraightLine = 6,

    /// <summary>
    /// User-defined custom shape captured from the Wand of Molding.
    /// When selected, Stamp wands use the mold's tile set instead of
    /// a parametric shape. The mold shape ignores ShapeMode/Thickness/Slice
    /// — it is always exactly the captured tile pattern.
    /// </summary>
    Mold = 7,
}