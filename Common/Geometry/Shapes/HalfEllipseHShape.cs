using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Horizontal half-ellipse: the curved side runs along the top or bottom edge,
/// and the flat side is the opposite horizontal edge.
/// Direction is determined by the start/end point order (like triangles):
///   - If Start.Y &lt;= End.Y (start above end): flat side on top, curve on bottom.
///   - If Start.Y &gt; End.Y (start below end): flat side on bottom, curve on top.
/// </summary>
public class HalfEllipseHShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.HalfEllipseH;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        bool startIsTop = context.Start.Y <= context.End.Y;
        var filledTiles = GetFilledTiles(bounds, startIsTop);
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        bool startIsTop = context.Start.Y <= context.End.Y;
        return IsInside(point, bounds, startIsTop);
    }

    private static bool IsInside(Point point, Rectangle bounds, bool startIsTop)
    {
        int W = bounds.Width;
        int H = bounds.Height;

        // Treat as a full ellipse of width W and height 2*H, then clip to the relevant half
        double a = W / 2.0;
        double b = H; // full ellipse semi-axis Y = H (we only draw half)
        if (a < 0.5) a = 0.5;
        if (b < 0.5) b = 0.5;

        double dx = point.X + 0.5 - (bounds.X + a);

        double dy;
        if (startIsTop)
        {
            // Flat on top (Y = bounds.Y), curve on bottom
            // Ellipse center at top edge, only use bottom half
            dy = point.Y + 0.5 - bounds.Y;
            if (dy < 0) return false;
        }
        else
        {
            // Flat on bottom (Y = bounds.Y + H - 1), curve on top
            // Ellipse center at bottom edge, only use top half
            dy = (bounds.Y + H) - (point.Y + 0.5);
            if (dy < 0) return false;
        }

        return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1.0;
    }

    private static HashSet<Point> GetFilledTiles(Rectangle bounds, bool startIsTop)
    {
        var tiles = new HashSet<Point>();
        int W = bounds.Width;
        int H = bounds.Height;

        double a = W / 2.0;   // semi-axis X
        double b = H;         // semi-axis Y (full ellipse height = 2*H, half = H)

        for (int i = 0; i < W; i++)
        {
            double dx = (i + 0.5 - a) / a;
            double dxSq = dx * dx;
            if (dxSq > 1.0) continue;

            // Column height from half-ellipse equation
            double colHeight = b * Math.Sqrt(1.0 - dxSq);
            int h = (int)Math.Ceiling(colHeight - 1e-9);
            if (h <= 0) continue;
            // Clamp to bounds height
            if (h > H) h = H;

            int tileX = bounds.X + i;

            if (startIsTop)
            {
                // Flat on top, curve extends downward from top
                for (int dy = 0; dy < h; dy++)
                    tiles.Add(new Point(tileX, bounds.Y + dy));
            }
            else
            {
                // Flat on bottom, curve extends upward from bottom
                for (int dy = 0; dy < h; dy++)
                    tiles.Add(new Point(tileX, bounds.Y + H - 1 - dy));
            }
        }

        return tiles;
    }
}
