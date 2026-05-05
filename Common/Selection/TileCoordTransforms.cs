using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Selection;

internal static class TileCoordTransforms
{
    public static Rectangle ComputeBounds(IEnumerable<Point> tiles)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;

        foreach (var p in tiles)
        {
            any = true;
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        if (!any)
            return Rectangle.Empty;

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static HashSet<Point> FlipHorizontal(IEnumerable<Point> tiles)
    {
        var bounds = ComputeBounds(tiles);
        if (bounds.IsEmpty)
            return new HashSet<Point>();

        return FlipHorizontal(tiles, bounds, ensureNonNegative: false);
    }

    public static HashSet<Point> FlipHorizontal(
        IEnumerable<Point> tiles,
        Rectangle referenceBounds,
        bool ensureNonNegative = true)
    {
        if (referenceBounds.IsEmpty)
            return new HashSet<Point>();

        int minX = referenceBounds.Left;
        int maxX = referenceBounds.Right - 1;

        var result = new HashSet<Point>();
        int minResX = int.MaxValue;
        foreach (var p in tiles)
        {
            int nx = minX + (maxX - p.X);
            result.Add(new Point(nx, p.Y));
            if (nx < minResX) minResX = nx;
        }

        if (ensureNonNegative && minResX < 0)
        {
            int shiftX = -minResX;
            var shifted = new HashSet<Point>(result.Count);
            foreach (var p in result)
                shifted.Add(new Point(p.X + shiftX, p.Y));
            return shifted;
        }

        return result;
    }

    public static HashSet<Point> FlipVertical(IEnumerable<Point> tiles)
    {
        var bounds = ComputeBounds(tiles);
        if (bounds.IsEmpty)
            return new HashSet<Point>();

        return FlipVertical(tiles, bounds, ensureNonNegative: false);
    }

    public static HashSet<Point> FlipVertical(
        IEnumerable<Point> tiles,
        Rectangle referenceBounds,
        bool ensureNonNegative = true)
    {
        if (referenceBounds.IsEmpty)
            return new HashSet<Point>();

        int minY = referenceBounds.Top;
        int maxY = referenceBounds.Bottom - 1;

        var result = new HashSet<Point>();
        int minResY = int.MaxValue;
        foreach (var p in tiles)
        {
            int ny = minY + (maxY - p.Y);
            result.Add(new Point(p.X, ny));
            if (ny < minResY) minResY = ny;
        }

        if (ensureNonNegative && minResY < 0)
        {
            int shiftY = -minResY;
            var shifted = new HashSet<Point>(result.Count);
            foreach (var p in result)
                shifted.Add(new Point(p.X, p.Y + shiftY));
            return shifted;
        }

        return result;
    }

    public static HashSet<Point> Rotate90CW(IEnumerable<Point> tiles)
        => RotateAroundCentroid(tiles, clockwise: true, ensureNonNegative: true);

    public static HashSet<Point> Rotate90CCW(IEnumerable<Point> tiles)
        => RotateAroundCentroid(tiles, clockwise: false, ensureNonNegative: true);

    public static HashSet<Point> Rotate90CW(
        IEnumerable<Point> tiles,
        double pivotX,
        double pivotY,
        bool ensureNonNegative = true)
        => RotateAroundPivot(tiles, clockwise: true, pivotX, pivotY, ensureNonNegative);

    public static HashSet<Point> Rotate90CCW(
        IEnumerable<Point> tiles,
        double pivotX,
        double pivotY,
        bool ensureNonNegative = true)
        => RotateAroundPivot(tiles, clockwise: false, pivotX, pivotY, ensureNonNegative);

    /// <summary>
    /// Rotates the tile set 90° around its centroid (mean of all tile positions),
    /// rounds each rotated coordinate to the nearest integer, and shifts the result
    /// to keep all coordinates non-negative (Terraria world space is non-negative).
    /// Pivoting on the centroid makes asymmetric shapes spin in place instead of
    /// swinging around a corner, matching the visual centre-of-mass the player sees.
    /// </summary>
    private static HashSet<Point> RotateAroundCentroid(
        IEnumerable<Point> tiles,
        bool clockwise,
        bool ensureNonNegative)
    {
        // Materialize once so we can iterate twice (centroid + rotation).
        var list = new List<Point>(tiles);
        if (list.Count == 0)
            return new HashSet<Point>();

        // Centroid (may be fractional).
        double sx = 0, sy = 0;
        foreach (var p in list)
        {
            sx += p.X;
            sy += p.Y;
        }
        double cx = sx / list.Count;
        double cy = sy / list.Count;

        return RotateAroundPivot(list, clockwise, cx, cy, ensureNonNegative);
    }

    private static HashSet<Point> RotateAroundPivot(
        IEnumerable<Point> tiles,
        bool clockwise,
        double pivotX,
        double pivotY,
        bool ensureNonNegative)
    {
        var list = new List<Point>(tiles);
        if (list.Count == 0)
            return new HashSet<Point>();

        // Screen-space rotation (Y-down):
        //   CW : (dx, dy) -> (-dy,  dx)
        //   CCW: (dx, dy) -> ( dy, -dx)
        var result = new HashSet<Point>();
        int minResX = int.MaxValue, minResY = int.MaxValue;
        foreach (var p in list)
        {
            double dx = p.X - pivotX;
            double dy = p.Y - pivotY;
            double rdx = clockwise ? -dy : dy;
            double rdy = clockwise ?  dx : -dx;
            int nx = (int)System.Math.Round(pivotX + rdx, System.MidpointRounding.AwayFromZero);
            int ny = (int)System.Math.Round(pivotY + rdy, System.MidpointRounding.AwayFromZero);
            result.Add(new Point(nx, ny));
            if (nx < minResX) minResX = nx;
            if (ny < minResY) minResY = ny;
        }

        // Non-negative guard: Terraria world coordinates are always >= 0. If the
        // centroid sits close enough to a world edge that rotation pushed any tile
        // negative, shift the whole result back into bounds. The shape stays rigid,
        // it just slides away from the edge.
        if (ensureNonNegative && (minResX < 0 || minResY < 0))
        {
            int shiftX = minResX < 0 ? -minResX : 0;
            int shiftY = minResY < 0 ? -minResY : 0;
            var shifted = new HashSet<Point>(result.Count);
            foreach (var p in result)
                shifted.Add(new Point(p.X + shiftX, p.Y + shiftY));
            return shifted;
        }

        return result;
    }

    public static HashSet<Point> NormalizeToOrigin(IEnumerable<Point> tiles)
    {
        var bounds = ComputeBounds(tiles);
        if (bounds.IsEmpty)
            return new HashSet<Point>();

        int minX = bounds.Left;
        int minY = bounds.Top;

        var result = new HashSet<Point>();
        foreach (var p in tiles)
            result.Add(new Point(p.X - minX, p.Y - minY));

        return result;
    }
}