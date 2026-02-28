using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class DiamondShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Diamond;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        var filledTiles = GetFilledTiles(bounds).ToHashSet();
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (point.X < bounds.X || point.X >= bounds.X + bounds.Width ||
            point.Y < bounds.Y || point.Y >= bounds.Y + bounds.Height)
            return false;

        double centerX = bounds.X + bounds.Width / 2.0;
        double centerY = bounds.Y + bounds.Height / 2.0;
        double halfW = Math.Max(bounds.Width / 2.0, 0.001);
        double halfH = Math.Max(bounds.Height / 2.0, 0.001);

        // Diamond formula: |x - cx|/hw + |y - cy|/hh <= 1
        return Math.Abs(point.X - centerX) / halfW + Math.Abs(point.Y - centerY) / halfH <= 1.0;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds)
    {
        double centerX = bounds.X + bounds.Width / 2.0;
        double centerY = bounds.Y + bounds.Height / 2.0;
        double halfW = Math.Max(bounds.Width / 2.0, 0.001);
        double halfH = Math.Max(bounds.Height / 2.0, 0.001);

        int minY = (int)Math.Ceiling(centerY - halfH);
        int maxY = (int)Math.Floor(centerY + halfH);

        // Clamp to bounds
        minY = Math.Max(minY, bounds.Y);
        maxY = Math.Min(maxY, bounds.Y + bounds.Height);

        for (int y = minY; y <= maxY; y++)
        {
            // Diamond formula: |x - cx|/hw + |y - cy|/hh <= 1
            // |x - cx| <= hw * (1 - |y - cy|/hh)
            double relativeY = Math.Abs(y - centerY) / halfH;
            double rowHalfWidth = halfW * (1.0 - relativeY);
            
            int startX = (int)Math.Ceiling(centerX - rowHalfWidth);
            int endX = (int)Math.Floor(centerX + rowHalfWidth);

            // Clamp to bounds
            startX = Math.Max(startX, bounds.X);
            endX = Math.Min(endX, bounds.X + bounds.Width);

            for (int x = startX; x <= endX; x++)
            {
                yield return new Point(x, y);
            }
        }
    }
}

