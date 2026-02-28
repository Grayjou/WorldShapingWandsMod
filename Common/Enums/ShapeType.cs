namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the geometric shape to be drawn or filled.
/// </summary>
public enum ShapeType : byte
{
    Rectangle = 0,
    Ellipse = 1,
    Diamond = 2,
    Triangle = 3,
    Line = 4,
    // Future: Parallelogram, Polygon, etc.
}