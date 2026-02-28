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
    /// Creates a new ShapeInfo with the specified parameters.
    /// </summary>
    public ShapeInfo(ShapeType shape, ShapeMode fillMode, int thickness = 1)
    {
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
    }

    /// <summary>
    /// Creates a default ShapeInfo (Rectangle, Filled, thickness 1).
    /// </summary>
    public static ShapeInfo Default => new(ShapeType.Rectangle, ShapeMode.Filled, 1);

    /// <summary>
    /// Returns a human-readable description of this shape configuration.
    /// </summary>
    public string GetDescription()
    {
        return FillMode switch
        {
            ShapeMode.Filled => $"{Shape} - Filled",
            ShapeMode.Hollow => $"{Shape} - Hollow",
            ShapeMode.Outline => Thickness switch
            {
                0 => $"{Shape} - Outline (Slim)",
                1 => $"{Shape} - Outline (Standard)",
                _ => $"{Shape} - Outline ({Thickness})"
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
    /// Uses default biases and vertical first settings.
    /// </summary>
    public ShapeContext ToShapeContext(Point start, Point end)
    {
        int effectiveThickness = FillMode == ShapeMode.Outline ? Thickness : 0;
        return new ShapeContext(start, end, FillMode, effectiveThickness,
            HorizontalBias.None, VerticalBias.None, false);
    }
}