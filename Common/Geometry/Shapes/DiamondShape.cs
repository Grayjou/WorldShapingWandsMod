using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Diamond (rhombus) shape inscribed in the bounding rectangle.
///
/// Uses the "×2 coordinate" trick (tile-center coordinates doubled to stay
/// in integer arithmetic) for precise, symmetric rasterisation.
///
/// Equation (in ×2 units):  |dx2| * H + |dy2| * W &lt;= W * H
/// where dx2/dy2 are signed distances from the diamond center and W, H are
/// the bounding rectangle's width and height.
///
/// Outline optimized: uses per-row RowRange to compute O(perimeter) outlines
/// instead of O(area) generic boundary detection.
/// </summary>
public class DiamondShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Diamond;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();

        // Slicing: fall back to full fill + generic pipeline
        if (context.Slice != SliceMode.Full)
        {
            var filledTiles = GetFilledTiles(bounds).ToHashSet();
            return SliceHelper.ApplySlicing(filledTiles, context);
        }

        if (context.Mode == ShapeMode.Filled)
        {
            var filledTiles = GetFilledTiles(bounds).ToHashSet();
            var boundary = RowRangeOutline4(bounds);
            return new ShapeTileSet(filledTiles, boundary);
        }

        // Hollow mode: use row-range-based O(perimeter) outline
        return BuildHollow(bounds, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var b = context.GetBounds();

        if (point.X < b.X || point.X >= b.X + b.Width ||
            point.Y < b.Y || point.Y >= b.Y + b.Height)
            return false;

        if (b.Width <= 0 || b.Height <= 0)
            return false;

        return IsInsideDiamond(point.X, point.Y, b);
    }

    // Per-shape hollow: row-range-based O(perimeter)

    private static ShapeTileSet BuildHollow(Rectangle bounds, int thickness)
    {
        int W = bounds.Width;
        int H = bounds.Height;
        if (W <= 0 || H <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        if (thickness <= 0)
        {
            // Slim: 4-connected boundary from row ranges
            var outline = RowRangeOutline4(bounds);
            return new ShapeTileSet(outline, outline);
        }
        else if (thickness == 1)
        {
            // Standard: 8-connected boundary from row ranges
            var outline = RowRangeOutline8(bounds);
            var visualBoundary = GeometryHelper.GetBoundaryTiles4(outline);
            return new ShapeTileSet(outline, visualBoundary);
        }
        else
        {
            // Thick: outer diamond minus inner diamond (shrunk by thickness)
            var outerTiles = GetFilledTiles(bounds).ToHashSet();

            int innerW = W - thickness * 2;
            int innerH = H - thickness * 2;

            if (innerW <= 0 || innerH <= 0)
            {
                var boundary = RowRangeOutline4(bounds);
                return new ShapeTileSet(outerTiles, boundary);
            }

            var innerBounds = new Rectangle(
                bounds.X + thickness, bounds.Y + thickness, innerW, innerH);
            var innerTiles = GetFilledTiles(innerBounds).ToHashSet();

            outerTiles.ExceptWith(innerTiles);
            var visualBoundary = GeometryHelper.GetBoundaryTiles4(outerTiles);
            return new ShapeTileSet(outerTiles, visualBoundary);
        }
    }

    // Row-range-based outline computation - O(perimeter)
    // Uses RowRange() to get per-row x-spans and detects boundary
    // tiles by comparing adjacent row widths.

    /// <summary>
    /// 4-connected outline: a filled pixel is on the outline if at least
    /// one cardinal neighbor is outside the diamond. O(perimeter).
    /// </summary>
    private static HashSet<Point> RowRangeOutline4(Rectangle bounds)
    {
        var outline = new HashSet<Point>();
        int H = bounds.Height;
        if (H <= 0 || bounds.Width <= 0) return outline;

        for (int y = bounds.Y; y < bounds.Y + H; y++)
        {
            var (startX, endX) = RowRange(y, bounds);
            if (startX > endX) continue;

            var (aboveStart, aboveEnd) = (y > bounds.Y) ? RowRange(y - 1, bounds) : (0, -1);
            var (belowStart, belowEnd) = (y < bounds.Y + H - 1) ? RowRange(y + 1, bounds) : (0, -1);

            for (int x = startX; x <= endX; x++)
            {
                bool onOutline = false;

                // Left/right edge of this row
                if (x == startX || x == endX) onOutline = true;
                // No row above or point outside above row
                if (!onOutline && (aboveStart > aboveEnd || x < aboveStart || x > aboveEnd)) onOutline = true;
                // No row below or point outside below row
                if (!onOutline && (belowStart > belowEnd || x < belowStart || x > belowEnd)) onOutline = true;

                if (onOutline)
                    outline.Add(new Point(x, y));
            }
        }

        return outline;
    }

    /// <summary>
    /// 8-connected outline: a filled pixel is on the outline if at least
    /// one of its 8 neighbors is outside the diamond. O(perimeter).
    /// </summary>
    private static HashSet<Point> RowRangeOutline8(Rectangle bounds)
    {
        var outline = new HashSet<Point>();
        int H = bounds.Height;
        if (H <= 0 || bounds.Width <= 0) return outline;

        for (int y = bounds.Y; y < bounds.Y + H; y++)
        {
            var (startX, endX) = RowRange(y, bounds);
            if (startX > endX) continue;

            var (aboveStart, aboveEnd) = (y > bounds.Y) ? RowRange(y - 1, bounds) : (0, -1);
            var (belowStart, belowEnd) = (y < bounds.Y + H - 1) ? RowRange(y + 1, bounds) : (0, -1);

            for (int x = startX; x <= endX; x++)
            {
                bool onOutline = false;

                // Cardinal neighbors
                if (x == startX || x == endX) onOutline = true;
                if (!onOutline && (aboveStart > aboveEnd || x < aboveStart || x > aboveEnd)) onOutline = true;
                if (!onOutline && (belowStart > belowEnd || x < belowStart || x > belowEnd)) onOutline = true;

                // Diagonal neighbors (only check if cardinal didn't trigger)
                if (!onOutline)
                {
                    // top-left / top-right diagonal
                    if (aboveStart > aboveEnd || (x - 1) < aboveStart || (x + 1) > aboveEnd)
                        onOutline = true;
                    // bottom-left / bottom-right diagonal
                    if (!onOutline && (belowStart > belowEnd || (x - 1) < belowStart || (x + 1) > belowEnd))
                        onOutline = true;
                }

                if (onOutline)
                    outline.Add(new Point(x, y));
            }
        }

        return outline;
    }

    // Core rasterization

    /// <summary>
    /// Enumerate every tile inside the diamond inscribed in <paramref name="b"/>.
    /// Pure integer arithmetic, O(area).
    /// </summary>
    public static IEnumerable<Point> GetFilledTiles(Rectangle b)
    {
        if (b.Width <= 0 || b.Height <= 0)
            yield break;

        int yMin = b.Y;
        int yMax = b.Y + b.Height - 1;

        for (int y = yMin; y <= yMax; y++)
        {
            var (startX, endX) = RowRange(y, b);
            for (int x = startX; x <= endX; x++)
                yield return new Point(x, y);
        }
    }

    /// <summary>
    /// Compute the inclusive x-range [startX, endX] of filled tiles at row
    /// <paramref name="y"/> inside the diamond inscribed in <paramref name="b"/>.
    /// Returns (0, -1) when the row is empty. Pure integer arithmetic.
    /// </summary>
    private static (int startX, int endX) RowRange(int y, Rectangle b)
    {
        int cx2 = 2 * b.X + b.Width;
        int cy2 = 2 * b.Y + b.Height;
        int W   = b.Width;
        int H   = b.Height;

        int y2  = 2 * y + 1;
        int dy2 = Math.Abs(y2 - cy2);

        long numer = (long)W * ((long)H - dy2);
        if (numer < 0)
            return (0, -1);

        int maxDx2 = (int)((numer + H - 1) / H);

        int startX = CeilDiv(cx2 - maxDx2 - 1, 2);
        int endX   = FloorDiv(cx2 + maxDx2 - 1, 2);

        int xMin = b.X;
        int xMax = b.X + b.Width - 1;
        if (startX < xMin) startX = xMin;
        if (endX   > xMax) endX   = xMax;

        return (startX, endX);
    }

    private static bool IsInsideDiamond(int x, int y, Rectangle b)
    {
        int x2 = 2 * x + 1;
        int y2 = 2 * y + 1;

        int cx2 = 2 * b.X + b.Width;
        int cy2 = 2 * b.Y + b.Height;

        int W = b.Width;
        int H = b.Height;

        long dx2 = Math.Abs((long)x2 - cx2);
        long dy2 = Math.Abs((long)y2 - cy2);

        return dx2 * H + dy2 * W <= (long)W * H;
    }

    private static int FloorDiv(int a, int b)
    {
        int q = Math.DivRem(a, b, out int r);
        return (r < 0) ? q - 1 : q;
    }

    private static int CeilDiv(int a, int b)
    {
        int q = Math.DivRem(a, b, out int r);
        return (r > 0) ? q + 1 : q;
    }
}
