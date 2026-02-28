using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes
{

    /// <summary>
    /// Provides L-shaped line (wire kite style) calculations.
    /// Creates a path from start to end using two perpendicular segments.
/// </summary>
public class LineShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Line;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var tiles = GenerateLineTiles(context);
        return OutlineHelper.Apply(tiles, context.Mode, context.Thickness);
    }

    private static HashSet<Point> GenerateLineTiles(ShapeContext context)
    {
        var tiles = new HashSet<Point>();
        
        if (context.VerticalFirst)
        {
            // Vertical segment first, then horizontal
            AddVerticalSegment(tiles, context.Start.X, context.Start.Y, context.End.Y);
            AddHorizontalSegment(tiles, context.End.Y, context.Start.X, context.End.X);
        }
        else
        {
            // Horizontal segment first, then vertical
            AddHorizontalSegment(tiles, context.Start.Y, context.Start.X, context.End.X);
            AddVerticalSegment(tiles, context.End.X, context.Start.Y, context.End.Y);
        }

        return tiles;
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        // For line shapes, check if point is on either segment
        if (context.VerticalFirst)
        {
            bool onVertical = point.X == context.Start.X && 
                              IsInRange(point.Y, context.Start.Y, context.End.Y);
            bool onHorizontal = point.Y == context.End.Y && 
                                IsInRange(point.X, context.Start.X, context.End.X);
            return onVertical || onHorizontal;
        }
        else
        {
            bool onHorizontal = point.Y == context.Start.Y && 
                                IsInRange(point.X, context.Start.X, context.End.X);
            bool onVertical = point.X == context.End.X && 
                              IsInRange(point.Y, context.Start.Y, context.End.Y);
            return onHorizontal || onVertical;
        }
    }

    private static bool IsInRange(int value, int a, int b)
    {
        return value >= Math.Min(a, b) && value <= Math.Max(a, b);
    }

    private static void AddVerticalSegment(HashSet<Point> tiles, int x, int y1, int y2)
    {
        int step = y1 <= y2 ? 1 : -1;
        int y = y1;
        
        while (true)
        {
            tiles.Add(new Point(x, y));
            if (y == y2) break;
            y += step;
        }
    }

    private static void AddHorizontalSegment(HashSet<Point> tiles, int y, int x1, int x2)
    {
        int step = x1 <= x2 ? 1 : -1;
        int x = x1;
        
        while (true)
        {
            tiles.Add(new Point(x, y));
            if (x == x2) break;
            x += step;
        }
    }
}
}

