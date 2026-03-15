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

    /// <summary>
    /// How the shape is sliced to produce a half-shape.
    /// Shapes compute partial geometry natively based on this value.
    /// The specific half is determined by Start/End drag direction.
    /// </summary>
    public SliceMode Slice { get; set; }

    /// <summary>
    /// When true and slicing is active on a hollow shape, the diameter edge
    /// (flat side) is drawn. When false, the diameter edge is omitted,
    /// leaving an open-sided shape (e.g. 3-sided rectangle, open half-circle).
    /// Only meaningful when <see cref="Slice"/> != <see cref="SliceMode.Full"/>
    /// and <see cref="Mode"/> == <see cref="ShapeMode.Hollow"/>.
    /// </summary>
    public bool ConnectDiameter { get; set; }

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
        Slice = SliceMode.Full;
        ConnectDiameter = true;
    }

    public ShapeContext(Point start, Point end, ShapeMode mode, int thickness, 
        HorizontalBias hBias, VerticalBias vBias, bool verticalFirst, bool equalDimensions = false,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true)
    {
        Start = start;
        End = end;
        Mode = mode;
        Thickness = thickness;
        HBias = hBias;
        VBias = vBias;
        VerticalFirst = verticalFirst;
        EqualDimensions = equalDimensions;
        Slice = slice;
        ConnectDiameter = connectDiameter;
    }

    /// <summary>
    /// Returns the bounding rectangle for this shape context.
    /// When <see cref="EqualDimensions"/> is true, the rectangle is expanded
    /// to a square using the larger dimension, anchored at <see cref="Start"/>.
    /// The Start corner stays fixed while the square extends in the direction of End,
    /// eliminating the integer-truncation jitter that occurred with center-based expansion.
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

            // Anchor the square at Start — extend in the direction of End.
            // This keeps the Start corner fixed, so the origin never shifts
            // due to integer truncation as the selection grows by 1 tile.
            if (End.X >= Start.X)
            {
                // End is to the right of Start: anchor left edge at Start.X
                minX = Start.X;
            }
            else
            {
                // End is to the left of Start: anchor right edge at Start.X
                minX = Start.X - size + 1;
            }

            if (End.Y >= Start.Y)
            {
                // End is below Start: anchor top edge at Start.Y
                minY = Start.Y;
            }
            else
            {
                // End is above Start: anchor bottom edge at Start.Y
                minY = Start.Y - size + 1;
            }

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
        bool? equalDimensions = null,
        SliceMode? slice = null,
        bool? connectDiameter = null)
    {
        return new ShapeContext(
            Start, End,
            mode ?? Mode,
            thickness ?? Thickness,
            hBias ?? HBias,
            vBias ?? VBias,
            verticalFirst ?? VerticalFirst,
            equalDimensions ?? EqualDimensions,
            slice ?? Slice,
            connectDiameter ?? ConnectDiameter
        );
    }
}

