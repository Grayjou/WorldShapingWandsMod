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
}