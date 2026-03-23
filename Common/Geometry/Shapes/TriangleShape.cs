using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Right-triangle shape. The right-angle vertex is placed at the corner nearest
/// to the start point. Direction is determined by comparing Start vs End.
///
/// Orientation: the 90° corner (where both legs meet) is always at the START point.
/// The hypotenuse runs from the far end of the horizontal leg to the far end of
/// the vertical leg, tapering away from the start.
///
/// Outline optimized: uses per-column heights to compute O(perimeter) outlines
/// instead of O(area) generic boundary detection.
/// </summary>
public class TriangleShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Triangle;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();

        // Determine corner orientation from drag direction.
        bool startIsLeft = context.Start.X < context.End.X || context.Start.X == context.End.X;
        bool startIsTop  = context.Start.Y < context.End.Y || context.Start.Y == context.End.Y;

        // Slicing: fall back to full fill + generic pipeline
        if (context.Slice != SliceMode.Full)
        {
            var filledTiles = GetFilledTiles(bounds, startIsLeft, startIsTop).ToHashSet();
            return SliceHelper.ApplySlicing(filledTiles, context);
        }

        if (context.Mode == ShapeMode.Filled)
        {
            var filledTiles = GetFilledTiles(bounds, startIsLeft, startIsTop).ToHashSet();
            var boundary = ColumnHeightOutline4(bounds, startIsLeft, startIsTop);
            return new ShapeTileSet(filledTiles, boundary);
        }

        // Hollow mode: use column-height-based O(perimeter) outline
        return BuildHollow(bounds, startIsLeft, startIsTop, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        bool startIsLeft = context.Start.X < context.End.X || context.Start.X == context.End.X;
        bool startIsTop  = context.Start.Y < context.End.Y || context.Start.Y == context.End.Y;

        return IsInsideTriangle(point, bounds, startIsLeft, startIsTop);
    }

    // Per-shape hollow: column-height-based O(perimeter)

    private static ShapeTileSet BuildHollow(Rectangle bounds, bool startIsLeft, bool startIsTop, int thickness)
    {
        int W = bounds.Width;
        int H = bounds.Height;
        if (W <= 0 || H <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        if (thickness <= 0)
        {
            var outline = ColumnHeightOutline4(bounds, startIsLeft, startIsTop);
            return new ShapeTileSet(outline, outline);
        }
        else if (thickness == 1)
        {
            var outline = ColumnHeightOutline8(bounds, startIsLeft, startIsTop);
            var visualBoundary = GeometryHelper.GetBoundaryTiles4(outline);
            return new ShapeTileSet(outline, visualBoundary);
        }
        else
        {
            // Thick: fall back to generic Chebyshev erosion (triangle doesn't have
            // a clean analytical inner-shape subtraction like ellipse/diamond)
            var filledTiles = GetFilledTiles(bounds, startIsLeft, startIsTop).ToHashSet();
            return OutlineHelper.Apply(filledTiles, ShapeMode.Hollow, thickness);
        }
    }

    // Column-height-based outline computation - O(perimeter)
    // Uses the same column-height approach as the fill, but only
    // emits boundary tiles by comparing adjacent column heights.

    /// <summary>
    /// Computes the column height at column index for a triangle with the
    /// given bounds. Height grows from 1 at the hypotenuse tip to H at the
    /// right-angle column.
    /// </summary>
    private static int GetColumnHeight(int col, int W, int H, bool startIsLeft)
    {
        if (W <= 1 || H <= 1) return H;

        int effCol = startIsLeft ? (W - 1 - col) : col;
        long num = (long)effCol * (H - 1);
        return 1 + (int)((num + W - 2) / (W - 1));
    }

    /// <summary>
    /// Gets the y-range for a column given its height and orientation.
    /// </summary>
    private static (int yStart, int yEnd) GetColumnYRange(int col, Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        int W = bounds.Width;
        int H = bounds.Height;
        int height = GetColumnHeight(col, W, H, startIsLeft);

        if (startIsTop)
        {
            // Fill from top down
            return (bounds.Y, bounds.Y + height - 1);
        }
        else
        {
            // Fill from bottom up
            return (bounds.Y + H - height, bounds.Y + H - 1);
        }
    }

    /// <summary>
    /// 4-connected outline from column heights. O(perimeter).
    /// </summary>
    private static HashSet<Point> ColumnHeightOutline4(Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        var outline = new HashSet<Point>();
        int W = bounds.Width;
        int H = bounds.Height;
        if (W <= 0 || H <= 0) return outline;

        for (int col = 0; col < W; col++)
        {
            var (yStart, yEnd) = GetColumnYRange(col, bounds, startIsLeft, startIsTop);
            int height = yEnd - yStart + 1;
            if (height <= 0) continue;

            int x = bounds.X + col;

            // Get neighbor column y-ranges
            var (leftYStart, leftYEnd) = col > 0 ? GetColumnYRange(col - 1, bounds, startIsLeft, startIsTop) : (0, -1);
            var (rightYStart, rightYEnd) = col < W - 1 ? GetColumnYRange(col + 1, bounds, startIsLeft, startIsTop) : (0, -1);
            int leftH = leftYEnd - leftYStart + 1;
            int rightH = rightYEnd - rightYStart + 1;

            for (int y = yStart; y <= yEnd; y++)
            {
                bool onOutline = false;

                // Top/bottom edge
                if (y == yStart || y == yEnd) onOutline = true;
                // Left neighbor missing or doesn't cover this y
                if (!onOutline && (leftH <= 0 || y < leftYStart || y > leftYEnd)) onOutline = true;
                // Right neighbor missing or doesn't cover this y
                if (!onOutline && (rightH <= 0 || y < rightYStart || y > rightYEnd)) onOutline = true;

                if (onOutline)
                    outline.Add(new Point(x, y));
            }
        }

        return outline;
    }

    /// <summary>
    /// 8-connected outline from column heights. O(perimeter).
    /// </summary>
    private static HashSet<Point> ColumnHeightOutline8(Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        var outline = new HashSet<Point>();
        int W = bounds.Width;
        int H = bounds.Height;
        if (W <= 0 || H <= 0) return outline;

        for (int col = 0; col < W; col++)
        {
            var (yStart, yEnd) = GetColumnYRange(col, bounds, startIsLeft, startIsTop);
            int height = yEnd - yStart + 1;
            if (height <= 0) continue;

            int x = bounds.X + col;

            var (leftYStart, leftYEnd) = col > 0 ? GetColumnYRange(col - 1, bounds, startIsLeft, startIsTop) : (0, -1);
            var (rightYStart, rightYEnd) = col < W - 1 ? GetColumnYRange(col + 1, bounds, startIsLeft, startIsTop) : (0, -1);
            int leftH = leftYEnd - leftYStart + 1;
            int rightH = rightYEnd - rightYStart + 1;

            for (int y = yStart; y <= yEnd; y++)
            {
                bool onOutline = false;

                // Cardinal neighbors
                if (y == yStart || y == yEnd) onOutline = true;
                if (!onOutline && (leftH <= 0 || y < leftYStart || y > leftYEnd)) onOutline = true;
                if (!onOutline && (rightH <= 0 || y < rightYStart || y > rightYEnd)) onOutline = true;

                // Diagonal neighbors
                if (!onOutline)
                {
                    // top-left / bottom-left
                    if (leftH <= 0 || (y - 1) < leftYStart || (y + 1) > leftYEnd)
                        onOutline = true;
                    // top-right / bottom-right
                    if (!onOutline && (rightH <= 0 || (y - 1) < rightYStart || (y + 1) > rightYEnd))
                        onOutline = true;
                }

                if (onOutline)
                    outline.Add(new Point(x, y));
            }
        }

        return outline;
    }

    // Core rasterization and point-in-shape test

    private static bool IsInsideTriangle(Point point, Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        if (point.X < bounds.X || point.X >= bounds.X + bounds.Width ||
            point.Y < bounds.Y || point.Y >= bounds.Y + bounds.Height)
            return false;

        int W = bounds.Width;
        int H = bounds.Height;

        if (W <= 1 || H <= 1)
            return true;

        int col = point.X - bounds.X;
        int row = point.Y - bounds.Y;

        int effCol = startIsLeft ? (W - 1 - col) : col;

        long num = (long)effCol * (H - 1);
        int height = 1 + (int)((num + W - 2) / (W - 1));

        int effRow = startIsTop ? row : (H - 1 - row);

        return effRow < height;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        int minX = bounds.X;
        int minY = bounds.Y;
        int W = bounds.Width;
        int H = bounds.Height;

        if (W <= 1 || H <= 1)
        {
            for (int x = minX; x < minX + W; x++)
                for (int y = minY; y < minY + H; y++)
                    yield return new Point(x, y);
            yield break;
        }

        for (int col = 0; col < W; col++)
        {
            int effCol = startIsLeft ? (W - 1 - col) : col;

            long num = (long)effCol * (H - 1);
            int height = 1 + (int)((num + W - 2) / (W - 1));

            int x = minX + col;
            for (int row = 0; row < height; row++)
            {
                int y = startIsTop ? (minY + row) : (minY + H - 1 - row);
                yield return new Point(x, y);
            }
        }
    }
}
