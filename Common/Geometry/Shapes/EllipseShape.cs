using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class EllipseShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Ellipse;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        var filledTiles = GetFilledTiles(bounds);
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        int W = bounds.Width;
        int H = bounds.Height;

        double a = W / 2.0;
        double b = H / 2.0;
        if (a < 0.5) a = 0.5;
        if (b < 0.5) b = 0.5;

        // Tile center relative to ellipse center
        double dx = point.X + 0.5 - (bounds.X + a);
        double dy = point.Y + 0.5 - (bounds.Y + b);

        return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1.0;
    }

    /// <summary>
    /// Generates filled ellipse tiles using direct per-column height calculation.
    /// For each column, computes the ellipse height at the tile center using
    /// the standard equation, then fills that many tiles symmetrically about the center.
    /// </summary>
    private static HashSet<Point> GetFilledTiles(Rectangle bounds)
    {
        var tiles = new HashSet<Point>();
        int W = bounds.Width;
        int H = bounds.Height;

        double a = W / 2.0;   // semi-axis X
        double b = H / 2.0;   // semi-axis Y
        double centerY = bounds.Y + b;

        for (int i = 0; i < W; i++)
        {
            // Distance from tile-column center to ellipse center
            double dx = (i + 0.5 - a) / a;
            double dxSq = dx * dx;
            if (dxSq > 1.0) continue;

            // Column height from ellipse equation
            double colHeight = 2.0 * b * Math.Sqrt(1.0 - dxSq);
            int h = (int)Math.Ceiling(colHeight - 1e-9); // slight epsilon avoids float noise
            if (h <= 0) continue;

            int tileX = bounds.X + i;

            // Distribute tiles symmetrically around center
            int halfH = h / 2;
            int yLo, yHi;
            if (H % 2 == 1) // odd height: center row exists
            {
                int centerTile = (int)Math.Floor(centerY);
                yLo = centerTile - halfH;
                yHi = centerTile + halfH;
                if (h % 2 == 0) yLo++; // even column height in odd-height ellipse
            }
            else // even height: split evenly
            {
                int centerTile = (int)Math.Floor(centerY);
                yLo = centerTile - halfH + 1;
                yHi = centerTile + halfH;
                if (h % 2 == 1) yLo--; // odd column height in even-height ellipse
            }

            for (int y = yLo; y <= yHi; y++)
                tiles.Add(new Point(tileX, y));
        }

        return tiles;
    }
}