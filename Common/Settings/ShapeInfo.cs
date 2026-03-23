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
    /// How the shape is sliced to produce a half-shape.
    /// Shapes compute partial geometry natively based on this value.
    /// </summary>
    public SliceMode Slice { get; set; }

    /// <summary>
    /// When true and slicing is active on a hollow shape, the diameter edge
    /// (flat side) is drawn. When false, the diameter edge is omitted,
    /// leaving an open-sided shape.
    /// </summary>
    public bool ConnectDiameter { get; set; }

    /// <summary>
    /// When true, inverts the selection — tiles inside the shape become unselected,
    /// and tiles in the bounding rectangle outside the shape become the selection.
    /// Hidden/disabled for line shapes (CardinalLine, StraightLine, Elbow).
    /// </summary>
    public bool InvertSelection { get; set; }

    /// <summary>
    /// Creates a new ShapeInfo with the specified parameters.
    /// </summary>
    public ShapeInfo(ShapeType shape, ShapeMode fillMode, int thickness = 1,
        bool equalDimensions = false, SliceMode slice = SliceMode.Full,
        bool connectDiameter = true, bool invertSelection = false)
    {
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
        EqualDimensions = equalDimensions;
        Slice = slice;
        ConnectDiameter = connectDiameter;
        InvertSelection = invertSelection;
    }

    /// <summary>
    /// Creates a default ShapeInfo (Rectangle, Filled, thickness 1, no equal dimensions).
    /// </summary>
    public static ShapeInfo Default => new(ShapeType.Rectangle, ShapeMode.Filled, 1, false, SliceMode.Full, true, false);

    /// <summary>
    /// Returns true if inversion is supported for the current shape type.
    /// Line shapes (CardinalLine, StraightLine, Elbow) cannot be inverted because
    /// they don't have a meaningful bounding-box complement.
    /// </summary>
    public bool SupportsInversion => ShapeSupportsInversion(Shape);

    /// <summary>
    /// Static helper: returns true if inversion is supported for the given shape type.
    /// Used by both instance <see cref="SupportsInversion"/> and server-side packet handling.
    /// </summary>
    public static bool ShapeSupportsInversion(ShapeType shape) => true;

    /// <summary>
    /// Returns true if the selection should actually be inverted — only when the
    /// toggle is on AND the shape supports it.
    /// </summary>
    public bool ShouldInvert => InvertSelection && SupportsInversion;

    /// <summary>
    /// Returns a human-readable description of this shape configuration.
    /// </summary>
    public string GetDescription()
    {
        string desc = FillMode switch
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

        if (Slice != SliceMode.Full)
            desc += $" [{Slice}]";

        return desc;
    }

    /// <summary>
    /// Clamps thickness to valid ranges using the configured maximum.
    /// </summary>
    public void Validate()
    {
        int max = Terraria.ModLoader.ModContent.GetInstance<Configs.WandServerConfig>()?.MaxOutlineThickness ?? 10;
        Thickness = (int)MathHelper.Clamp(Thickness, 0, max);
    }

    /// <summary>
    /// Creates a ShapeContext from this ShapeInfo with the given start and end points.
    /// Uses default biases. The verticalFirst parameter controls Elbow/Elbow axis order
    /// and must match the selection's VerticalFirst to avoid preview/execution mismatches.
    /// </summary>
    public ShapeContext ToShapeContext(Point start, Point end, bool verticalFirst = false)
    {
        // Pass thickness for all modes. CardinalLine uses thickness in Filled mode
        // for its circular brush. Other shapes ignore thickness in Filled mode.
        return new ShapeContext(start, end, FillMode, Thickness,
            HorizontalBias.None, VerticalBias.None, verticalFirst, EqualDimensions, Slice, ConnectDiameter);
    }

    /// <summary>
    /// Applies inversion to a tile set if <see cref="ShouldInvert"/> is true.
    /// Returns the original tiles unchanged if inversion is not active.
    /// When inverted, returns all tiles in the bounding rectangle that are NOT in the original set.
    /// </summary>
    public Point[] ApplyInversion(Point[] tiles, ShapeContext context)
    {
        if (!ShouldInvert)
            return tiles;

        var originalSet = new System.Collections.Generic.HashSet<Point>(tiles);
        var bounds = context.GetBounds();
        var inverted = new System.Collections.Generic.List<Point>();

        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                var pt = new Point(x, y);
                if (!originalSet.Contains(pt))
                    inverted.Add(pt);
            }
        }

        return inverted.ToArray();
    }
}