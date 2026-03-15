using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Right-triangle shape. The right-angle vertex is placed at the corner nearest
/// to the start point. Direction is determined by comparing Start vs End.
///
/// Orientation: the 90° corner (where both legs meet) is always at the START point.
/// The hypotenuse runs from the far end of the horizontal leg to the far end of
/// the vertical leg, tapering away from the start.
///
/// Example — start=TopLeft (startIsLeft=true, startIsTop=true):
///   col=0 (left/start) gets full height H, tapering to height=1 at col=W-1 (right).
///   Rows fill from top down, so the 90° corner sits at (minX, minY) = the start tile.
///
/// Orientation uses &lt; comparison on Start vs End coordinates (not bounding-box center)
/// to keep the flip logic simple: equal positions default left/top.
/// </summary>
public class TriangleShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Triangle;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();

        // Determine corner orientation from drag direction.
        // Use strict less-than: when equal, default to left/top (stable default).
        bool startIsLeft = context.Start.X < context.End.X || context.Start.X == context.End.X;
        bool startIsTop  = context.Start.Y < context.End.Y || context.Start.Y == context.End.Y;

        var filledTiles = GetFilledTiles(bounds, startIsLeft, startIsTop).ToHashSet();

        if (context.Slice != SliceMode.Full)
            return SliceHelper.ApplySlicing(filledTiles, context);

        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        bool startIsLeft = context.Start.X < context.End.X || context.Start.X == context.End.X;
        bool startIsTop  = context.Start.Y < context.End.Y || context.Start.Y == context.End.Y;

        return IsInsideTriangle(point, bounds, startIsLeft, startIsTop);
    }

    private static bool IsInsideTriangle(Point point, Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        if (point.X < bounds.X || point.X >= bounds.X + bounds.Width ||
            point.Y < bounds.Y || point.Y >= bounds.Y + bounds.Height)
            return false;

        int W = bounds.Width;
        int H = bounds.Height;

        // Degenerate: 1-tile dimension → all tiles inside (triangle = full rect)
        if (W <= 1 || H <= 1)
            return true;

        // Mirror the column-height logic from GetFilledTiles for pixel-perfect matching.
        int col = point.X - bounds.X;
        int row = point.Y - bounds.Y;

        // Apply horizontal orientation flip
        int effCol = startIsLeft ? (W - 1 - col) : col;

        // height = 1 + ceil(effCol * (H-1) / (W-1))
        long num = (long)effCol * (H - 1);
        int height = 1 + (int)((num + W - 2) / (W - 1));

        // Apply vertical orientation: startIsTop → fill from top (row is already top-relative)
        // startIsTop=false → fill from bottom, so invert row to bottom-relative
        int effRow = startIsTop ? row : (H - 1 - row);

        return effRow < height;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds, bool startIsLeft, bool startIsTop)
    {
        int minX = bounds.X;
        int minY = bounds.Y;
        int W = bounds.Width;
        int H = bounds.Height;

        // Degenerate: 1-tile or 1-row/column — fill everything.
        if (W <= 1 || H <= 1)
        {
            for (int x = minX; x < minX + W; x++)
                for (int y = minY; y < minY + H; y++)
                    yield return new Point(x, y);
            yield break;
        }

        // Column-height lerp: for each column index col (0..W-1), compute how many
        // rows that column spans in the solid triangle. The right-angle tip always
        // occupies the full height (H rows) and the opposite edge tapers to 1 row.
        //
        // Using integer ceiling ensures no column is ever empty and the hypotenuse
        // is drawn as a staircase that touches every column, rather than the float
        // formula  fx + fy ≤ 1  which can produce zero-height columns when the
        // hypotenuse passes between tile centers.
        //
        //   height[col] = 1 + ceil( col × (H − 1) / (W − 1) )
        //
        // col=0      → height = 1
        // col=W−1    → height = 1 + (H−1) = H   (full column, right-angle corner)

        for (int col = 0; col < W; col++)
        {
            // Flip column index according to horizontal orientation
            int effCol = startIsLeft ? (W - 1 - col) : col;

            // Integer ceiling: (effCol * (H-1) + (W-2)) / (W-1)
            long num = (long)effCol * (H - 1);
            int height = 1 + (int)((num + W - 2) / (W - 1));

            int x = minX + col;
            for (int row = 0; row < height; row++)
            {
                // startIsTop=true  → fill from the top down (right angle at top)
                // startIsTop=false → fill from the bottom up (right angle at bottom)
                int y = startIsTop ? (minY + row) : (minY + H - 1 - row);
                yield return new Point(x, y);
            }
        }
    }
}