using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Organizes shape types into UI pages for the shape selector panel.
/// Page 1 contains all current shapes (backwards-compatible).
/// Future pages will contain multi-point and complex shapes.
/// </summary>
/// <remarks>
/// Navigation between pages uses arrow buttons or scroll wheel on the shape panel.
/// The current page index is stored per-player and resets on world exit.
/// Shapes on pages beyond page 1 may be restricted to certain confirmation modes
/// (e.g. multi-point shapes cannot be used in Instant/OneClick mode).
/// </remarks>
public static class ShapePages
{
    /// <summary>
    /// All shape pages. Each inner array contains the shape types shown on that page.
    /// Page indices are 0-based internally but displayed as 1-based to users.
    /// </summary>
    public static readonly ShapeType[][] Pages = new[]
    {
        // Page 1: Current bounding-box shapes (all require 2 points)
        new[]
        {
            ShapeType.Rectangle,
            ShapeType.Ellipse,
            ShapeType.Diamond,
            ShapeType.Triangle,
            ShapeType.Elbow,
            ShapeType.CardinalLine,
            ShapeType.StraightLine,
        },

        // Future pages will be added here when multi-point shapes are implemented:
        // Page 2: Curve shapes (3-4 points)
        //   ShapeType.Arc, ShapeType.ArcDonut, ShapeType.Bezier
        // Page 3: Complex shapes (N points)
        //   ShapeType.Polygon, ShapeType.Spiral
    };

    /// <summary>
    /// Total number of pages currently available.
    /// </summary>
    public static int PageCount => Pages.Length;

    /// <summary>
    /// Returns the shapes on the specified page (0-based index).
    /// Returns the first page if the index is out of range.
    /// </summary>
    public static ShapeType[] GetPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= Pages.Length)
            return Pages[0];
        return Pages[pageIndex];
    }

    /// <summary>
    /// Finds which page a shape type is on, and its index within that page.
    /// Returns (-1, -1) if the shape is not found on any page.
    /// </summary>
    public static (int PageIndex, int ShapeIndex) FindShape(ShapeType shape)
    {
        for (int p = 0; p < Pages.Length; p++)
        {
            for (int s = 0; s < Pages[p].Length; s++)
            {
                if (Pages[p][s] == shape)
                    return (p, s);
            }
        }
        return (-1, -1);
    }

    /// <summary>
    /// Returns true if the given shape type exists on any page.
    /// </summary>
    public static bool ContainsShape(ShapeType shape) =>
        FindShape(shape).PageIndex >= 0;

    /// <summary>
    /// Returns true if there are multiple pages available.
    /// Used by the UI to decide whether to show page navigation controls.
    /// </summary>
    public static bool HasMultiplePages => Pages.Length > 1;
}
