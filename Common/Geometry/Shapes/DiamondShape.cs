using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

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
/// </summary>
public class DiamondShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Diamond;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        var filledTiles = GetFilledTiles(bounds).ToHashSet();

        if (context.Slice != SliceMode.Full)
            return SliceHelper.ApplySlicing(filledTiles, context);

        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
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
    /// Returns (0, -1) when the row is empty.
    /// Pure integer arithmetic.
    /// </summary>
    private static (int startX, int endX) RowRange(int y, Rectangle b)
    {
        // ×2 center coordinates (avoids floating point)
        int cx2 = 2 * b.X + b.Width;
        int cy2 = 2 * b.Y + b.Height;
        int W   = b.Width;
        int H   = b.Height;

        int y2  = 2 * y + 1;
        int dy2 = Math.Abs(y2 - cy2);

        // Diamond equation in ×2 units: |dx2|*H + |dy2|*W <= W*H
        // => max |dx2| = W * (H - dy2) / H
        //
        // Use ceiling division so that rows near the tips of an even-dimension diamond
        // are never left empty. Integer truncation gives maxDx2=0 when the numerator
        // is a small positive value less than H, which (for even W) yields an empty row
        // because 2*x+1 can never equal an even cx2. Ceiling gives maxDx2≥1 whenever
        // the row is geometrically inside the diamond, producing a proper 1-tile tip.
        long numer = (long)W * ((long)H - dy2);
        if (numer < 0)
            return (0, -1);          // row outside diamond

        int maxDx2 = (int)((numer + H - 1) / H);  // ceiling division (long-safe)

        // Solve  cx2 - maxDx2 <= 2*x + 1 <= cx2 + maxDx2
        int startX = CeilDiv(cx2 - maxDx2 - 1, 2);
        int endX   = FloorDiv(cx2 + maxDx2 - 1, 2);

        // Clamp to bounding rectangle
        int xMin = b.X;
        int xMax = b.X + b.Width - 1;
        if (startX < xMin) startX = xMin;
        if (endX   > xMax) endX   = xMax;

        return (startX, endX);
    }

    /// <summary>
    /// Point-in-diamond test using the ×2 coordinate equation.
    /// </summary>
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

    /// <summary>Floor division (rounds towards −∞). Assumes b &gt; 0.</summary>
    private static int FloorDiv(int a, int b)
    {
        int q = Math.DivRem(a, b, out int r);
        return (r < 0) ? q - 1 : q;
    }

    /// <summary>Ceiling division (rounds towards +∞). Assumes b &gt; 0.</summary>
    private static int CeilDiv(int a, int b)
    {
        int q = Math.DivRem(a, b, out int r);
        return (r > 0) ? q + 1 : q;
    }
}
