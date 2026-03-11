using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using System;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Context containing all parameters needed for shape generation.
/// </summary>
public struct ShapeContext
{
    public Point Start { get; set; }
    public Point End { get; set; }
    public ShapeMode Mode { get; set; }
    public int Thickness { get; set; }
    public HorizontalBias HBias { get; set; }
    public VerticalBias VBias { get; set; }
    public bool VerticalFirst { get; set; }

    /// <summary>
    /// When true, forces equal width and height — rectangles become squares,
    /// ellipses become circles, etc. The larger dimension is used, centered
    /// on the original bounding box.
    /// </summary>
    public bool EqualDimensions { get; set; }

    public ShapeContext(Point start, Point end)
    {
        Start = start;
        End = end;
        Mode = ShapeMode.Filled;
        Thickness = 1;
        HBias = HorizontalBias.None;
        VBias = VerticalBias.None;
        VerticalFirst = false;
        EqualDimensions = false;
    }

    public ShapeContext(Point start, Point end, ShapeMode mode, int thickness, 
        HorizontalBias hBias, VerticalBias vBias, bool verticalFirst, bool equalDimensions = false)
    {
        Start = start;
        End = end;
        Mode = mode;
        Thickness = thickness;
        HBias = hBias;
        VBias = vBias;
        VerticalFirst = verticalFirst;
        EqualDimensions = equalDimensions;
    }

    /// <summary>
    /// Returns the bounding rectangle for this shape context.
    /// When <see cref="EqualDimensions"/> is true, the rectangle is expanded
    /// to a square using the larger dimension, centered on the original bounds.
    /// </summary>
    public Rectangle GetBounds()
    {
        int minX = Math.Min(Start.X, End.X);
        int minY = Math.Min(Start.Y, End.Y);
        int maxX = Math.Max(Start.X, End.X);
        int maxY = Math.Max(Start.Y, End.Y);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        if (EqualDimensions)
        {
            int size = Math.Max(width, height);
            int centerX = (minX + maxX) / 2;
            int centerY = (minY + maxY) / 2;
            minX = centerX - size / 2;
            minY = centerY - size / 2;
            width = size;
            height = size;
        }

        return new Rectangle(minX, minY, width, height);
    }

    public Vector2 GetCenter()
    {
        var bounds = GetBounds();
        return new Vector2(
            bounds.X + bounds.Width / 2f,
            bounds.Y + bounds.Height / 2f
        );
    }

    public ShapeContext With(
        ShapeMode? mode = null,
        int? thickness = null,
        HorizontalBias? hBias = null,
        VerticalBias? vBias = null,
        bool? verticalFirst = null,
        bool? equalDimensions = null)
    {
        return new ShapeContext(
            Start, End,
            mode ?? Mode,
            thickness ?? Thickness,
            hBias ?? HBias,
            vBias ?? VBias,
            verticalFirst ?? VerticalFirst,
            equalDimensions ?? EqualDimensions
        );
    }
}

