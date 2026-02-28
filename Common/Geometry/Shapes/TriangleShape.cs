using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class TriangleShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Triangle;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        
        // Determine corner orientation based on start/end positions
        bool startIsLeft = context.Start.X <= context.End.X;
        bool startIsTop = context.Start.Y <= context.End.Y;

        var filledTiles = GetFilledTiles(bounds, startIsLeft, startIsTop).ToHashSet();
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        bool startIsLeft = context.Start.X <= context.End.X;
        bool startIsTop = context.Start.Y <= context.End.Y;

        return IsInsideTriangle(point, bounds, startIsLeft, startIsTop);
    }

    private static bool IsInsideTriangle(Point point, Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        if (point.X < bounds.X || point.X >= bounds.X + bounds.Width ||
            point.Y < bounds.Y || point.Y >= bounds.Y + bounds.Height)
            return false;

        double w = bounds.Width;
        double h = bounds.Height;

        if (w < 0.001 && h < 0.001) return point.X == bounds.X && point.Y == bounds.Y;
        if (w < 0.001) return point.X == bounds.X;
        if (h < 0.001) return point.Y == bounds.Y;

        double nx = (point.X - bounds.X) / w;
        double ny = (point.Y - bounds.Y) / h;

        double fx = startIsLeft ? nx : (1.0 - nx);
        double fy = startIsTop ? ny : (1.0 - ny);

        return fx + fy <= 1.0 + 0.001;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        int minX = bounds.X;
        int minY = bounds.Y;
        int maxX = bounds.X + bounds.Width - 1;
        int maxY = bounds.Y + bounds.Height - 1;

        double w = bounds.Width;
        double h = bounds.Height;

        // Handle degenerate cases
        if (w < 0.001 && h < 0.001)
        {
            yield return new Point(minX, minY);
            yield break;
        }
        if (w < 0.001)
        {
            for (int y = minY; y <= maxY; y++)
                yield return new Point(minX, y);
            yield break;
        }
        if (h < 0.001)
        {
            for (int x = minX; x <= maxX; x++)
                yield return new Point(x, minY);
            yield break;
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                double nx = (x - minX) / w;
                double ny = (y - minY) / h;

                double fx = startIsLeft ? nx : (1.0 - nx);
                double fy = startIsTop ? ny : (1.0 - ny);

                if (fx + fy <= 1.0 + 0.001)
                    yield return new Point(x, y);
            }
        }
    }
}

