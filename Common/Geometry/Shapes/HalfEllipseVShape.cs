using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Vertical half-ellipse: the curved side runs along the left or right edge,
/// and the flat side is the opposite vertical edge.
/// Direction is determined by the start/end point order (like triangles):
///   - If Start.X &lt;= End.X (start left of end): flat side on left, curve on right.
///   - If Start.X &gt; End.X (start right of end): flat side on right, curve on left.
/// </summary>
public class HalfEllipseVShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.HalfEllipseV;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        bool startIsLeft = context.Start.X <= context.End.X;
        var filledTiles = GetFilledTiles(bounds, startIsLeft);
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        bool startIsLeft = context.Start.X <= context.End.X;
        return IsInside(point, bounds, startIsLeft);
    }

    private static bool IsInside(Point point, Rectangle bounds, bool startIsLeft)
    {
        int W = bounds.Width;
        int H = bounds.Height;

        // Treat as a full ellipse of width 2*W and height H, then clip to the relevant half
        double a = W;  // full ellipse semi-axis X = W (we only draw half)
        double b = H / 2.0;
        if (a < 0.5) a = 0.5;
        if (b < 0.5) b = 0.5;

        double dy = point.Y + 0.5 - (bounds.Y + b);

        double dx;
        if (startIsLeft)
        {
            // Flat on left (X = bounds.X), curve on right
            // Ellipse center at left edge, only use right half
            dx = point.X + 0.5 - bounds.X;
            if (dx < 0) return false;
        }
        else
        {
            // Flat on right (X = bounds.X + W - 1), curve on left
            // Ellipse center at right edge, only use left half
            dx = (bounds.X + W) - (point.X + 0.5);
            if (dx < 0) return false;
        }

        return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1.0;
    }

    private static HashSet<Point> GetFilledTiles(Rectangle bounds, bool startIsLeft)
    {
        var tiles = new HashSet<Point>();
        int W = bounds.Width;
        int H = bounds.Height;

        double a = W;         // semi-axis X (full ellipse width = 2*W, half = W)
        double b = H / 2.0;   // semi-axis Y
        double centerY = bounds.Y + b;

        for (int j = 0; j < H; j++)
        {
            double dy = (j + 0.5 - b) / b;
            double dySq = dy * dy;
            if (dySq > 1.0) continue;

            // Row width from half-ellipse equation
            double rowWidth = a * Math.Sqrt(1.0 - dySq);
            int w = (int)Math.Ceiling(rowWidth - 1e-9);
            if (w <= 0) continue;
            // Clamp to bounds width
            if (w > W) w = W;

            int tileY = bounds.Y + j;

            if (startIsLeft)
            {
                // Flat on left, curve extends rightward from left
                for (int dx = 0; dx < w; dx++)
                    tiles.Add(new Point(bounds.X + dx, tileY));
            }
            else
            {
                // Flat on right, curve extends leftward from right
                for (int dx = 0; dx < w; dx++)
                    tiles.Add(new Point(bounds.X + W - 1 - dx, tileY));
            }
        }

        return tiles;
    }
}
