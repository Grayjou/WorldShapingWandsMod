using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class RectangleShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Rectangle;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();

        // Slicing: fall back to full fill + generic pipeline
        if (context.Slice != SliceMode.Full)
        {
            var filledTiles = GetFilledTiles(bounds).ToHashSet();
            return SliceHelper.ApplySlicing(filledTiles, context);
        }

        // Filled mode: O(area) fill, O(perimeter) boundary
        if (context.Mode == ShapeMode.Filled)
        {
            var filledTiles = GetFilledTiles(bounds).ToHashSet();
            var boundary = GetRectangleBoundary4(bounds);
            return new ShapeTileSet(filledTiles, boundary);
        }

        // Hollow mode: O(perimeter) outline generation
        return BuildHollow(bounds, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        return point.X >= bounds.X &&
               point.X < bounds.X + bounds.Width &&
               point.Y >= bounds.Y &&
               point.Y < bounds.Y + bounds.Height;
    }

    // Per-shape hollow: direct O(perimeter) for all thicknesses
    private static ShapeTileSet BuildHollow(Rectangle bounds, int thickness)
    {
        int W = bounds.Width;
        int H = bounds.Height;
        if (W <= 0 || H <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        HashSet<Point> outlineTiles;

        if (thickness <= 0)
        {
            outlineTiles = GetRectangleBoundary4(bounds);
        }
        else if (thickness == 1)
        {
            // For rectangles, 8-connected boundary is the same as 4-connected
            outlineTiles = GetRectangleBoundary4(bounds);
        }
        else
        {
            int innerW = W - thickness * 2;
            int innerH = H - thickness * 2;

            if (innerW <= 0 || innerH <= 0)
            {
                var allTiles = GetFilledTiles(bounds).ToHashSet();
                var boundary = GetRectangleBoundary4(bounds);
                return new ShapeTileSet(allTiles, boundary);
            }

            outlineTiles = new HashSet<Point>();
            int outerMinX = bounds.X;
            int outerMinY = bounds.Y;
            int outerMaxX = bounds.X + W - 1;
            int outerMaxY = bounds.Y + H - 1;
            int innerMinX = bounds.X + thickness;
            int innerMinY = bounds.Y + thickness;
            int innerMaxX = innerMinX + innerW - 1;
            int innerMaxY = innerMinY + innerH - 1;

            for (int x = outerMinX; x <= outerMaxX; x++)
            {
                for (int y = outerMinY; y <= outerMaxY; y++)
                {
                    if (x >= innerMinX && x <= innerMaxX && y >= innerMinY && y <= innerMaxY)
                        continue;
                    outlineTiles.Add(new Point(x, y));
                }
            }
        }

        // Visual boundary of the outline band
        var visualBoundary = GetRectangleOutlineBoundary4(outlineTiles, bounds, thickness);
        return new ShapeTileSet(outlineTiles, visualBoundary);
    }

    /// <summary>
    /// 4-connected boundary of a filled rectangle = the 1-tile perimeter ring.
    /// O(2W + 2H - 4).
    /// </summary>
    private static HashSet<Point> GetRectangleBoundary4(Rectangle bounds)
    {
        var boundary = new HashSet<Point>();
        int minX = bounds.X;
        int minY = bounds.Y;
        int maxX = bounds.X + bounds.Width - 1;
        int maxY = bounds.Y + bounds.Height - 1;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return boundary;

        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    boundary.Add(new Point(x, y));
            return boundary;
        }

        for (int x = minX; x <= maxX; x++)
        {
            boundary.Add(new Point(x, minY));
            boundary.Add(new Point(x, maxY));
        }

        for (int y = minY + 1; y < maxY; y++)
        {
            boundary.Add(new Point(minX, y));
            boundary.Add(new Point(maxX, y));
        }

        return boundary;
    }

    /// <summary>
    /// 4-connected boundary of a hollow rectangle outline band.
    /// For thick outlines, this is the inner and outer perimeter rings.
    /// </summary>
    private static HashSet<Point> GetRectangleOutlineBoundary4(
        HashSet<Point> outlineTiles, Rectangle bounds, int thickness)
    {
        if (thickness <= 1)
            return new HashSet<Point>(outlineTiles);

        int W = bounds.Width;
        int H = bounds.Height;
        int innerW = W - thickness * 2;
        int innerH = H - thickness * 2;

        if (innerW <= 0 || innerH <= 0)
            return GetRectangleBoundary4(bounds);

        var boundary = GetRectangleBoundary4(bounds);

        int innerMinX = bounds.X + thickness;
        int innerMinY = bounds.Y + thickness;
        int innerMaxX = innerMinX + innerW - 1;
        int innerMaxY = innerMinY + innerH - 1;

        for (int x = innerMinX; x <= innerMaxX; x++)
        {
            var above = new Point(x, innerMinY - 1);
            if (outlineTiles.Contains(above)) boundary.Add(above);
            var below = new Point(x, innerMaxY + 1);
            if (outlineTiles.Contains(below)) boundary.Add(below);
        }
        for (int y = innerMinY - 1; y <= innerMaxY + 1; y++)
        {
            var left = new Point(innerMinX - 1, y);
            if (outlineTiles.Contains(left)) boundary.Add(left);
            var right = new Point(innerMaxX + 1, y);
            if (outlineTiles.Contains(right)) boundary.Add(right);
        }

        return boundary;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds)
    {
        int maxX = bounds.X + bounds.Width - 1;
        int maxY = bounds.Y + bounds.Height - 1;

        for (int x = bounds.X; x <= maxX; x++)
            for (int y = bounds.Y; y <= maxY; y++)
                yield return new Point(x, y);
    }
}