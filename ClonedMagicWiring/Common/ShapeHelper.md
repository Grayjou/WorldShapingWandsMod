using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MagicWiring.Common;

/// <summary>
/// Provides tile coordinate generation for each WiringShape.
/// All methods take two corner points (the drag start and end tile coords)
/// and return the set of tile positions that form the shape.
/// </summary>
public static class ShapeHelper
{
    /// <summary>
    /// Main entry point. Returns all tile positions for the given shape
    /// defined by the bounding box of start→end.
    /// </summary>
    public static List<Point> GetShapeTiles(Point start, Point end, WiringShape shape, bool verticalFirst = false)
    {
        int minX = Math.Min(start.X, end.X);
        int maxX = Math.Max(start.X, end.X);
        int minY = Math.Min(start.Y, end.Y);
        int maxY = Math.Max(start.Y, end.Y);

        // For triangle: which corner is the start point at?
        bool startIsLeft = start.X <= end.X;
        bool startIsTop = start.Y <= end.Y;

        return shape switch
        {
            WiringShape.WireKite => GetWireKiteTiles(start, end, verticalFirst),
            WiringShape.FilledRectangle => GetFilledRectangle(minX, minY, maxX, maxY),
            WiringShape.HollowRectangle => GetHollowRectangle(minX, minY, maxX, maxY),
            WiringShape.FilledDiamond => GetFilledDiamond(minX, minY, maxX, maxY),
            WiringShape.HollowDiamond => GetHollowDiamond(minX, minY, maxX, maxY),
            WiringShape.FilledTriangle => GetFilledTriangle(minX, minY, maxX, maxY, startIsLeft, startIsTop),
            WiringShape.HollowTriangle => GetHollowTriangle(minX, minY, maxX, maxY, startIsLeft, startIsTop),
            _ => new List<Point>()
        };
    }

    /// <summary>
    /// Clamps end point within maxTileDistance of start (Euclidean).
    /// Returns (original end, false) if unlimited or within range.
    /// </summary>
    public static (Point clampedEnd, bool wasClamped) ClampDistance(Point start, Point end, int maxTileDistance)
    {
        if (maxTileDistance <= 0)
            return (end, false);

        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist <= maxTileDistance)
            return (end, false);

        float scale = maxTileDistance / dist;
        Point clamped = new Point(
            start.X + (int)(dx * scale),
            start.Y + (int)(dy * scale));
        return (clamped, true);
    }

    /// <summary>
    /// Every tile inside the rectangle, inclusive.
    /// </summary>
    private static List<Point> GetFilledRectangle(int minX, int minY, int maxX, int maxY)
    {
        var tiles = new List<Point>((maxX - minX + 1) * (maxY - minY + 1));
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                tiles.Add(new Point(x, y));
            }
        }
        return tiles;
    }

    /// <summary>
    /// Only the border tiles of the rectangle.
    /// For a 1-wide or 1-tall rectangle, this degenerates to a filled line.
    /// </summary>
    private static List<Point> GetHollowRectangle(int minX, int minY, int maxX, int maxY)
    {
        var tiles = new List<Point>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Include if on any edge
                if (x == minX || x == maxX || y == minY || y == maxY)
                {
                    tiles.Add(new Point(x, y));
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// A filled diamond (rhombus) inscribed in the bounding box.
    /// Uses the Manhattan-distance formula:
    ///   |x - cx| / halfW + |y - cy| / halfH &lt;= 1
    /// 
    /// For discrete tiles, we need to be careful with the boundary.
    /// The diamond's tips touch the midpoints of each edge of the bounding box.
    /// </summary>
    private static List<Point> GetFilledDiamond(int minX, int minY, int maxX, int maxY)
    {
        var tiles = new List<Point>();

        // Center of the bounding box (in tile coordinates, using doubles for precision)
        double cx = (minX + maxX) / 2.0;
        double cy = (minY + maxY) / 2.0;

        // Half-dimensions (from center to edge)
        double halfW = (maxX - minX) / 2.0;
        double halfH = (maxY - minY) / 2.0;

        // Avoid division by zero for degenerate cases
        if (halfW < 0.001) halfW = 0.001;
        if (halfH < 0.001) halfH = 0.001;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                double normalizedDist = Math.Abs(x - cx) / halfW + Math.Abs(y - cy) / halfH;

                // Use a small epsilon to include boundary tiles cleanly
                if (normalizedDist <= 1.0 + 0.001)
                {
                    tiles.Add(new Point(x, y));
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// Only the border of the diamond. A tile is on the border if it's inside
    /// the diamond but at least one of its 4-connected neighbors is outside.
    /// </summary>
    private static List<Point> GetHollowDiamond(int minX, int minY, int maxX, int maxY)
    {
        var tiles = new List<Point>();

        double cx = (minX + maxX) / 2.0;
        double cy = (minY + maxY) / 2.0;
        double halfW = (maxX - minX) / 2.0;
        double halfH = (maxY - minY) / 2.0;

        if (halfW < 0.001) halfW = 0.001;
        if (halfH < 0.001) halfH = 0.001;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                double dist = Math.Abs(x - cx) / halfW + Math.Abs(y - cy) / halfH;

                if (dist > 1.0 + 0.001)
                    continue; // Outside the diamond entirely

                // Check if any neighbor is outside the diamond → this tile is border
                bool isBorder = false;
                int[][] neighbors = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
                foreach (var n in neighbors)
                {
                    int nx = x + n[0];
                    int ny = y + n[1];
                    double nDist = Math.Abs(nx - cx) / halfW + Math.Abs(ny - cy) / halfH;
                    if (nDist > 1.0 + 0.001)
                    {
                        isBorder = true;
                        break;
                    }
                }

                if (isBorder)
                    tiles.Add(new Point(x, y));
            }
        }

        return tiles;
    }

    // =====================================================================
    // WireKite: Vanilla 90-degree L-shaped path
    // =====================================================================

    /// <summary>
    /// Creates L-shaped path from start to end. verticalFirst determines
    /// whether to go vertical-then-horizontal or horizontal-then-vertical.
    /// 
    /// BUGFIX: Uses while(true)/break instead of Math.Sign() to avoid
    /// infinite loop when start == end on an axis (Math.Sign(0) = 0).
    /// </summary>
    private static List<Point> GetWireKiteTiles(Point start, Point end, bool verticalFirst)
    {
        var tileSet = new HashSet<Point>();

        if (verticalFirst)
        {
            AddLineSegment(tileSet, start.X, start.X, start.Y, end.Y, true);
            AddLineSegment(tileSet, start.X, end.X, end.Y, end.Y, false);
        }
        else
        {
            AddLineSegment(tileSet, start.X, end.X, start.Y, start.Y, false);
            AddLineSegment(tileSet, end.X, end.X, start.Y, end.Y, true);
        }

        return new List<Point>(tileSet);
    }

    private static void AddLineSegment(HashSet<Point> tiles, int x1, int x2, int y1, int y2, bool iterateY)
    {
        if (iterateY)
        {
            int step = y1 <= y2 ? 1 : -1;
            int y = y1;
            while (true)
            {
                tiles.Add(new Point(x1, y));
                if (y == y2) break;
                y += step;
            }
        }
        else
        {
            int step = x1 <= x2 ? 1 : -1;
            int x = x1;
            while (true)
            {
                tiles.Add(new Point(x, y1));
                if (x == x2) break;
                x += step;
            }
        }
    }

    // =====================================================================
    // Triangle shapes - 4 orientations based on drag direction
    // =====================================================================

    /// <summary>
    /// Right triangle with right-angle at the START point of the drag.
    /// This gives 4 natural orientations:
    /// - Drag right+down: right angle at top-left
    /// - Drag left+down: right angle at top-right  
    /// - Drag right+up: right angle at bottom-left
    /// - Drag left+up: right angle at bottom-right
    /// </summary>
    private static List<Point> GetFilledTriangle(int minX, int minY, int maxX, int maxY,
        bool startIsLeft, bool startIsTop)
    {
        var tiles = new List<Point>();
        double w = maxX - minX;
        double h = maxY - minY;

        // Degenerate cases
        if (w < 0.001 && h < 0.001)
        {
            tiles.Add(new Point(minX, minY));
            return tiles;
        }
        if (w < 0.001)
        {
            for (int y = minY; y <= maxY; y++) tiles.Add(new Point(minX, y));
            return tiles;
        }
        if (h < 0.001)
        {
            for (int x = minX; x <= maxX; x++) tiles.Add(new Point(x, minY));
            return tiles;
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                double nx = (x - minX) / w;  // 0 at left, 1 at right
                double ny = (y - minY) / h;  // 0 at top, 1 at bottom

                // Flip so right angle is at start point
                double fx = startIsLeft ? nx : (1.0 - nx);
                double fy = startIsTop ? ny : (1.0 - ny);

                // Standard triangle: fx + fy <= 1
                if (fx + fy <= 1.0 + 0.001)
                    tiles.Add(new Point(x, y));
            }
        }
        return tiles;
    }

    private static List<Point> GetHollowTriangle(int minX, int minY, int maxX, int maxY,
        bool startIsLeft, bool startIsTop)
    {
        var filled = GetFilledTriangle(minX, minY, maxX, maxY, startIsLeft, startIsTop);
        var set = new HashSet<Point>(filled);
        var border = new List<Point>();

        foreach (var p in filled)
        {
            if (!set.Contains(new Point(p.X + 1, p.Y)) ||
                !set.Contains(new Point(p.X - 1, p.Y)) ||
                !set.Contains(new Point(p.X, p.Y + 1)) ||
                !set.Contains(new Point(p.X, p.Y - 1)))
            {
                border.Add(p);
            }
        }
        return border;
    }
}