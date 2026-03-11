using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Contains shape configuration information for wand operations.
/// </summary>
public struct ShapeInfo
{
    /// <summary>The geometric shape type.</summary>
    public ShapeType Shape { get; set; }

    /// <summary>How the shape should be filled.</summary>
    public ShapeMode FillMode { get; set; }

    /// <summary>The thickness for outline/hollow shapes.</summary>
    public int Thickness { get; set; }

    /// <summary>
    /// When true, forces equal width and height dimensions.
    /// Turns rectangles into squares, ellipses into circles, diamonds into square diamonds, etc.
    /// </summary>
    public bool EqualDimensions { get; set; }

    /// <summary>
    /// Creates a new ShapeInfo with the specified parameters.
    /// </summary>
    public ShapeInfo(ShapeType shape, ShapeMode fillMode, int thickness = 1, bool equalDimensions = false)
    {
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
        EqualDimensions = equalDimensions;
    }

    /// <summary>
    /// Creates a default ShapeInfo (Rectangle, Filled, thickness 1, no equal dimensions).
    /// </summary>
    public static ShapeInfo Default => new(ShapeType.Rectangle, ShapeMode.Filled, 1, false);

    /// <summary>
    /// Returns a human-readable description of this shape configuration.
    /// </summary>
    public string GetDescription()
    {
        return FillMode switch
        {
            ShapeMode.Filled => $"{Shape} - Filled",
            ShapeMode.Hollow => Thickness switch
            {
                0 => $"{Shape} - Hollow (Slim)",
                1 => $"{Shape} - Hollow (Standard)",
                _ => $"{Shape} - Hollow ({Thickness})"
            },
            _ => $"{Shape} - Unknown"
        };
    }

    /// <summary>
    /// Clamps thickness to valid ranges.
    /// </summary>
    public void Validate()
    {
        Thickness = (int)MathHelper.Clamp(Thickness, 0, 50);
    }

    /// <summary>
    /// Creates a ShapeContext from this ShapeInfo with the given start and end points.
    /// Uses default biases. The verticalFirst parameter controls Elbow/Elbow axis order
    /// and must match the selection's VerticalFirst to avoid preview/execution mismatches.
    /// </summary>
    public ShapeContext ToShapeContext(Point start, Point end, bool verticalFirst = false)
    {
        // Use thickness for both Hollow and Outline modes
        int effectiveThickness = (FillMode == ShapeMode.Hollow) ? Thickness : 0;
        return new ShapeContext(start, end, FillMode, effectiveThickness,
            HorizontalBias.None, VerticalBias.None, verticalFirst, EqualDimensions);
    }
}