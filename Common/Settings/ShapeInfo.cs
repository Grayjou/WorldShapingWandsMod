using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
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
    /// (S2 2026-04-30 — DesignDoc_HalfShapeOrientationFlipToggle.md #IOP)
    /// When true, the kept half of a sliced shape is flipped relative to the
    /// drag direction (i.e. opposite of <see cref="SliceHelper.IsStartAbove" /> /
    /// <see cref="SliceHelper.IsStartLeft" />). Default false. Only meaningful
    /// when <see cref="Slice" /> is a half mode. Propagated to
    /// <see cref="ShapeContext.InvertHalfOrientation" /> via <see cref="ToShapeContext" />.
    /// </summary>
    public bool InvertHalfOrientation { get; set; }

    /// <summary>
    /// (S2 2026-05-24 Session 2 — FromCenterShapeOption.md)
    /// Controls whether the shape is drawn outward from the initial click point
    /// (which acts as the center) rather than as a drag corner.
    /// Off = default bbox drag. Odd = symmetric around cursor (odd dims).
    /// Even = cursor is corner of central 2×2 block (even dims).
    /// Only meaningful for shapes that support it: Rectangle, Diamond, Ellipse.
    /// See <see cref="SupportsDrawFromCenter" />.
    /// </summary>
    public DrawFromCenterMode DrawFromCenter { get; set; }

    /// <summary>
    /// Creates a new ShapeInfo with the specified parameters.
    /// </summary>
    public ShapeInfo(ShapeType shape, ShapeMode fillMode, int thickness = 1,
        bool equalDimensions = false, SliceMode slice = SliceMode.Full,
        bool connectDiameter = true, bool invertSelection = false,
        bool invertHalfOrientation = false,
        DrawFromCenterMode drawFromCenter = DrawFromCenterMode.Off)
    {
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
        EqualDimensions = equalDimensions;
        Slice = slice;
        ConnectDiameter = connectDiameter;
        InvertSelection = invertSelection;
        InvertHalfOrientation = invertHalfOrientation;
        DrawFromCenter = drawFromCenter;
    }

    /// <summary>
    /// Creates a default ShapeInfo using the player's preferred shape type and mode
    /// from PreferencesConfig, falling back to Rectangle/Filled if config is unavailable.
    /// </summary>
    public static ShapeInfo Default
    {
        get
        {
            var config = ModContent.GetInstance<PreferencesConfig>();
            if (config != null)
                return new(config.DefaultShapeType, config.DefaultShapeMode, 1, false, SliceMode.Full, true, false, false, DrawFromCenterMode.Off);
            return new(ShapeType.Rectangle, ShapeMode.Filled, 1, false, SliceMode.Full, true, false, false, DrawFromCenterMode.Off);
        }
    }

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
    public static bool ShapeSupportsInversion(ShapeType shape) => shape switch
    {
        ShapeType.Mold => false,
        _ => true,
    };

    /// <summary>
    /// Returns true if the selection should actually be inverted — only when the
    /// toggle is on AND the shape supports it.
    /// </summary>
    public bool ShouldInvert => InvertSelection && SupportsInversion;

    /// <summary>
    /// Returns true if DrawFromCenter is meaningful for the current shape.
    /// Rectangle, Diamond, and Ellipse support a true bbox-center expansion.
    /// Line shapes (Elbow, CardinalLine, StraightLine) and special shapes
    /// (Triangle, Mold, MagicWandRead) do not.
    /// </summary>
    public bool SupportsDrawFromCenter => ShapeSupportsDrawFromCenter(Shape);

    /// <summary>
    /// Static helper for server-side packet handling.
    /// </summary>
    public static bool ShapeSupportsDrawFromCenter(ShapeType shape) => shape switch
    {
        ShapeType.Rectangle => true,
        ShapeType.Diamond   => true,
        ShapeType.Ellipse   => true,
        _                   => false
    };

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
        int max = Configs.WandConfigs.Limits?.MaxOutlineThickness ?? 10;
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
        // DrawFromCenter is guarded: unsupported shapes receive Off so GetBounds()
        // never misapplies the center logic to e.g. Triangle or Elbow.
        return new ShapeContext(start, end, FillMode, Thickness,
            HorizontalBias.None, VerticalBias.None, verticalFirst, EqualDimensions, Slice, ConnectDiameter,
            InvertHalfOrientation, SupportsDrawFromCenter ? DrawFromCenter : DrawFromCenterMode.Off);
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