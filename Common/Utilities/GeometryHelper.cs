using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;

namespace WorldShapingWandsMod.Common.Utilities
{

    /// <summary>
    /// Common geometry utility functions.
    /// </summary>
    public static class GeometryHelper
{
    /// <summary>
    /// Clamps the end point to be within maxDistance tiles from start.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The desired end point.</param>
    /// <param name="maxDistance">Maximum allowed distance in tiles. 0 or negative = unlimited.</param>
    /// <returns>The clamped end point and whether clamping occurred.</returns>
    public static (Point clampedEnd, bool wasClamped) ClampDistance(Point start, Point end, int maxDistance)
    {
        if (maxDistance <= 0)
            return (end, false);

        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float distSquared = dx * dx + dy * dy;
        float maxDistSquared = maxDistance * maxDistance;

        if (distSquared <= maxDistSquared)
            return (end, false);

        float dist = MathF.Sqrt(distSquared);
        float scale = maxDistance / dist;
        
        Point clamped = new Point(
            start.X + (int)(dx * scale),
            start.Y + (int)(dy * scale)
        );
        
        return (clamped, true);
    }

    /// <summary>
    /// Gets the distance between two points in tiles.
    /// </summary>
    public static float GetDistance(Point a, Point b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Gets the squared distance between two points (faster, no sqrt).
    /// </summary>
    public static float GetDistanceSquared(Point a, Point b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Determines if a point is within world bounds.
    /// </summary>
    public static bool IsInWorld(Point point, int padding = 0)
    {
        // Temporarily commented out for development - uncomment when tModLoader is installed
        // return point.X >= padding && 
        //        point.X < Terraria.Main.maxTilesX - padding &&
        //        point.Y >= padding && 
        //        point.Y < Terraria.Main.maxTilesY - padding;
        return true; // Temporary implementation
    }

    /// <summary>
    /// Converts world position to tile coordinates.
    /// </summary>
    public static Point WorldToTile(Vector2 worldPosition)
    {
        return new Point((int)(worldPosition.X / 16f), (int)(worldPosition.Y / 16f));
    }

    /// <summary>
    /// Converts tile coordinates to world position (top-left of tile).
    /// </summary>
    public static Vector2 TileToWorld(Point tilePosition)
    {
        return new Vector2(tilePosition.X * 16f, tilePosition.Y * 16f);
    }

    /// <summary>
    /// Gets the center world position of a tile.
    /// </summary>
    public static Vector2 TileCenterWorld(Point tilePosition)
    {
        return new Vector2(tilePosition.X * 16f + 8f, tilePosition.Y * 16f + 8f);
    }

    private static readonly int[][] Neighbors4 =
    {
        new[] { 1, 0 }, new[] { -1, 0 },
        new[] { 0, 1 }, new[] { 0, -1 }
    };

    private static readonly int[][] Neighbors8 =
    {
        new[] { 1, 0 }, new[] { -1, 0 },
        new[] { 0, 1 }, new[] { 0, -1 },
        new[] { 1, 1 }, new[] { 1, -1 },
        new[] { -1, 1 }, new[] { -1, -1 }
    };

    /// <summary>
    /// 4-directional (Manhattan) boundary: tiles missing at least one cardinal neighbor.
    /// Produces the thinnest possible outline; may have diagonal gaps on sloped edges.
    /// </summary>
    public static HashSet<Point> GetBoundaryTiles4(HashSet<Point> tiles)
        => GetBoundaryTilesInternal(tiles, Neighbors4);

    /// <summary>
    /// 8-directional (Chebyshev) boundary: tiles missing at least one of 8 neighbors.
    /// Slightly thicker than 4-neighbor; fills diagonal gaps.
    /// </summary>
    public static HashSet<Point> GetBoundaryTiles8(HashSet<Point> tiles)
        => GetBoundaryTilesInternal(tiles, Neighbors8);

    /// <summary>
    /// Backward-compatible alias — defaults to 4-neighbor for existing code.
    /// </summary>
    public static HashSet<Point> GetBoundaryTiles(HashSet<Point> tiles)
        => GetBoundaryTiles4(tiles);

    private static HashSet<Point> GetBoundaryTilesInternal(HashSet<Point> tiles, int[][] neighbors)
    {
        var boundary = new HashSet<Point>();

        foreach (var point in tiles)
        {
            foreach (var n in neighbors)
            {
                if (!tiles.Contains(new Point(point.X + n[0], point.Y + n[1])))
                {
                    boundary.Add(point);
                    break;
                }
            }
        }

        return boundary;
    }
}
}

