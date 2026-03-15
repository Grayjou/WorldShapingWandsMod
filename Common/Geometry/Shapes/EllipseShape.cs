using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class EllipseShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Ellipse;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        int twoA = bounds.Width;
        int twoB = bounds.Height;

        // Step 1: Generate heights (shared by all modes)
        var halfHeights = GenerateHalfHeights(twoA, twoB);
        var fullHeights = BuildFullHeights(halfHeights, twoA, twoB);

        // If slicing is active, use SliceHelper's full pipeline (clip → outline → disconnect)
        if (context.Slice != SliceMode.Full)
        {
            var filledTiles = HeightsToWorldTiles(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            return SliceHelper.ApplySlicing(filledTiles, context);
        }

        if (context.Mode == ShapeMode.Filled)
        {
            var filledTiles = HeightsToWorldTiles(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            // Compute visual boundary using the heights-based 4-connected outline
            var boundary = HeightsToOutline4(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            return new ShapeTileSet(filledTiles, boundary);
        }

        // Hollow mode (full shape): use heights-based outline for thickness 0 and 1 (O(perimeter))
        if (context.Thickness <= 0)
        {
            // Slim: 4-connected boundary from heights
            var outline = HeightsToOutline4(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            // For a 1-pixel-wide outline, every tile IS a boundary tile
            return new ShapeTileSet(outline, outline);
        }
        else if (context.Thickness == 1)
        {
            // Standard: 8-connected boundary from heights
            var outline = HeightsToOutline8(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            // Visual boundary of the outline band itself
            var visualBoundary = GeometryHelper.GetBoundaryTiles4(outline);
            return new ShapeTileSet(outline, visualBoundary);
        }
        else
        {
            // Thick outlines (thickness >= 2): fall back to OutlineHelper's
            // Chebyshev erosion which handles arbitrary thickness
            var filledTiles = HeightsToWorldTiles(fullHeights, twoA, twoB, bounds.X, bounds.Y);
            return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
        }
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        int W = bounds.Width;
        int H = bounds.Height;

        // Use the same point-in-ellipse test as the rasterization reference:
        // Account for even/odd parity with reposition factors.
        double a = W / 2.0;
        double b = H / 2.0;
        if (a < 0.5) a = 0.5;
        if (b < 0.5) b = 0.5;

        double repositionX = (W % 2 == 1) ? 0.5 : 0.0;
        double repositionY = (H % 2 == 1) ? 0.5 : 0.0;

        // Convert world-space point to normalized coordinates
        int normX = point.X - bounds.X - (W / 2) + 1;
        int normY = point.Y - bounds.Y - (H / 2) + 1;

        double x = normX - 0.5 - repositionX;
        double y = normY - 0.5 - repositionY;

        return (x * x) / (a * a) + (y * y) / (b * b) <= 1.0;
    }

    // ──────────────────────────────────────────────────────────────
    //  Heights-based outline computation — O(perimeter)
    //  Ported from ClonedEllipseRasterization (owned by developer).
    //  Determines outline tiles directly from per-column heights without
    //  building the full O(area) filled HashSet. This is the key insight
    //  from the C++ study: outlines are a property of adjacent column
    //  height differences, not of set membership queries.
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 4-connected (cardinal) outline: a filled pixel is on the outline if at least
    /// one cardinal neighbor (up/down/left/right) is outside the ellipse.
    /// Produces the thinnest outline. Computed in O(perimeter) from heights.
    /// </summary>
    private static HashSet<Point> HeightsToOutline4(
        List<int> heights, int twoA, int twoB, int worldX, int worldY)
    {
        var outline = new HashSet<Point>();
        int W = heights.Count;
        if (W == 0) return outline;

        int yCenter = twoB / 2;

        for (int i = 0; i < W; i++)
        {
            int H = heights[i];
            if (H <= 0) continue;

            int tileX = worldX + i;

            int halfH = H / 2;
            int yLo = -halfH;
            int yHi = (H % 2 == 1) ? halfH : halfH - 1;

            int hLeft  = (i > 0)     ? heights[i - 1] : 0;
            int hRight = (i < W - 1) ? heights[i + 1] : 0;

            // Compute y ranges for left and right columns
            int halfHL = hLeft / 2;
            int yLoL = -halfHL;
            int yHiL = (hLeft % 2 == 1) ? halfHL : halfHL - 1;

            int halfHR = hRight / 2;
            int yLoR = -halfHR;
            int yHiR = (hRight % 2 == 1) ? halfHR : halfHR - 1;

            if (hLeft == 0 && hRight == 0)
            {
                // Isolated column — everything is outline
                for (int y = yLo; y <= yHi; y++)
                    outline.Add(new Point(tileX, worldY + yCenter + y));
                continue;
            }

            for (int y = yLo; y <= yHi; y++)
            {
                bool onOutline = false;

                if (y == yHi) onOutline = true;                              // top: y+1 outside
                if (y == yLo) onOutline = true;                              // bottom: y-1 outside
                if (hLeft  == 0 || y < yLoL || y > yHiL) onOutline = true;  // left neighbor missing
                if (hRight == 0 || y < yLoR || y > yHiR) onOutline = true;  // right neighbor missing

                if (onOutline)
                    outline.Add(new Point(tileX, worldY + yCenter + y));
            }
        }

        return outline;
    }

    /// <summary>
    /// 8-connected (Chebyshev) outline: a filled pixel is on the outline if at least
    /// one of its 8 neighbors (cardinal + diagonal) is outside the ellipse.
    /// Slightly thicker than 4-connected; fills diagonal gaps. O(perimeter) from heights.
    /// </summary>
    private static HashSet<Point> HeightsToOutline8(
        List<int> heights, int twoA, int twoB, int worldX, int worldY)
    {
        var outline = new HashSet<Point>();
        int W = heights.Count;
        if (W == 0) return outline;

        int yCenter = twoB / 2;

        for (int i = 0; i < W; i++)
        {
            int H = heights[i];
            if (H <= 0) continue;

            int tileX = worldX + i;

            int halfH = H / 2;
            int yLo = -halfH;
            int yHi = (H % 2 == 1) ? halfH : halfH - 1;

            int hLeft  = (i > 0)     ? heights[i - 1] : 0;
            int hRight = (i < W - 1) ? heights[i + 1] : 0;

            int halfHL = hLeft / 2;
            int yLoL = -halfHL;
            int yHiL = (hLeft % 2 == 1) ? halfHL : halfHL - 1;

            int halfHR = hRight / 2;
            int yLoR = -halfHR;
            int yHiR = (hRight % 2 == 1) ? halfHR : halfHR - 1;

            if (hLeft == 0 && hRight == 0)
            {
                for (int y = yLo; y <= yHi; y++)
                    outline.Add(new Point(tileX, worldY + yCenter + y));
                continue;
            }

            for (int y = yLo; y <= yHi; y++)
            {
                bool onOutline = false;

                // Cardinal neighbors
                if (y == yHi) onOutline = true;
                if (y == yLo) onOutline = true;
                if (hLeft  == 0 || y < yLoL || y > yHiL) onOutline = true;
                if (hRight == 0 || y < yLoR || y > yHiR) onOutline = true;

                // Diagonal neighbors (only check if cardinal didn't trigger)
                if (!onOutline)
                {
                    // top-left (x-1, y+1)
                    if (hLeft == 0 || (y + 1) < yLoL || (y + 1) > yHiL) onOutline = true;
                    // bottom-left (x-1, y-1)
                    if (!onOutline && (hLeft == 0 || (y - 1) < yLoL || (y - 1) > yHiL)) onOutline = true;
                    // top-right (x+1, y+1)
                    if (!onOutline && (hRight == 0 || (y + 1) < yLoR || (y + 1) > yHiR)) onOutline = true;
                    // bottom-right (x+1, y-1)
                    if (!onOutline && (hRight == 0 || (y - 1) < yLoR || (y - 1) > yHiR)) onOutline = true;
                }

                if (onOutline)
                    outline.Add(new Point(tileX, worldY + yCenter + y));
            }
        }

        return outline;
    }

    // ──────────────────────────────────────────────────────────────
    //  IncrementalFast ellipse rasterization — O(W + H)
    //  Ported from ClonedEllipseRasterization (owned by developer).
    //  Pure integer arithmetic, no floating-point. Generates per-column
    //  half-heights from the outermost column inward, then mirrors them
    //  to produce symmetrical full-heights. Guarantees Y-axis symmetry
    //  for both even and odd dimensions.
    //
    //  Algorithm choice: Benchmarked against Direct, IncrementalFastAxisFlip,
    //  and other approaches. IncrementalFast scored 0.87µs mean — only 0.04µs
    //  slower than AxisFlip (0.83µs) which requires more complex code for
    //  negligible gain at tile-scale dimensions. Direct was 1.03µs.
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a filled circle/ellipse as a set of (dx, dy) offsets centered at (0, 0).
    /// Uses the IncrementalFast algorithm for diameter ≥ 4; falls back to Euclidean
    /// distance check for smaller sizes where the incremental approach has too few
    /// pixels to produce good results.
    /// </summary>
    /// <param name="diameter">The diameter of the circle in tiles.</param>
    /// <returns>List of (dx, dy) offsets relative to center.</returns>
    public static List<Point> GetCircleBrushOffsets(int diameter)
    {
        if (diameter <= 0)
            return new List<Point> { Point.Zero };

        if (diameter < 4)
        {
            // For tiny circles (1–3), use the same IncrementalFast ellipse path as
            // larger sizes to ensure consistent diameter semantics.  The original
            // Euclidean-distance fallback computed radius = diameter/2 via integer
            // truncation, making diameter=2 and diameter=3 both produce a 5-tile cross
            // (radius=1 in both cases). Now every diameter goes through the same path.
            //
            // Special-case diameter=1: return just the center tile.
            if (diameter == 1)
                return new List<Point> { Point.Zero };

            // Fall through to the IncrementalFast path below.
        }

        // Use IncrementalFast algorithm for diameter ≥ 4
        var halfHeights = GenerateHalfHeights(diameter, diameter);
        var fullHeights = BuildFullHeights(halfHeights, diameter, diameter);

        var result = new List<Point>();
        int W = fullHeights.Count;
        if (W == 0) return result;

        int xCenter = diameter / 2;
        int yCenter = diameter / 2;

        for (int i = 0; i < W; i++)
        {
            int H = fullHeights[i];
            if (H <= 0) continue;

            int dx = i - xCenter;

            int halfH = H / 2;
            int yLo = -halfH;
            int yHi = (H % 2 == 1) ? halfH : halfH - 1;

            for (int y = yLo; y <= yHi; y++)
                result.Add(new Point(dx, y));
        }

        return result;
    }

    /// <summary>
    /// O(W + H) pure-integer incremental computation of half-heights.
    /// For each column from the outer edge inward, computes how many rows
    /// above the center line are inside the ellipse.
    /// </summary>
    private static List<int> GenerateHalfHeights(int twoA, int twoB)
    {
        var halfHeights = new List<int>();
        int halfW = (twoA + 1) / 2;  // Include center column for odd widths
        int halfH = (twoB + 1) / 2;

        if (halfW == 0)
            return halfHeights;

        // Integer arithmetic: (sx/a)² + (sy/b)² <= 1
        // Becomes: sx² * b² + sy² * a² <= a² * b²
        long aSquared = (long)twoA * twoA;
        long bSquared = (long)twoB * twoB;
        long threshold = aSquared * bSquared;
        long eightASquared = aSquared << 3;
        long eightBSquared = bSquared << 3;

        int xOffset = twoA & 1;
        int yOffset = twoB & 1;

        // Initialize y tracking
        int currentHeight = 1;
        long syNext = 3 - yOffset;
        long yTermNext = syNext * syNext * aSquared;
        long yDelta = (aSquared << 2) * (syNext + 1);

        // Initialize x tracking
        long sx = 2 * halfW - 1 - xOffset;
        long xTerm = sx * sx * bSquared;
        long xDelta = (bSquared << 2) * (sx - 1);

        for (int col = halfW; col > 0; col--)
        {
            while (currentHeight < halfH && xTerm + yTermNext <= threshold)
            {
                currentHeight++;
                yTermNext += yDelta;
                yDelta += eightASquared;
            }
            halfHeights.Add(currentHeight);

            // Update x term for next column
            xTerm -= xDelta;
            xDelta -= eightBSquared;
        }

        return halfHeights;
    }

    /// <summary>
    /// Mirrors half-heights to produce full column heights, handling
    /// even/odd width and height parity correctly.
    /// </summary>
    private static List<int> BuildFullHeights(List<int> halfHeights, int twoA, int twoB)
    {
        // Build the left half (head)
        List<int>  head;
        if (twoA % 2 == 0)
        {
            head = new List<int>(halfHeights);
        }
        else
        {
            // Odd width: skip the last element (center column) in head
            // so the center column appears only once in the tail's reverse
            head = halfHeights.Count > 0
                ? halfHeights.GetRange(0, halfHeights.Count - 1)
                : new List<int>();
        }

        // Build the right half (tail) — reversed copy of all half-heights
        var tail = new List<int>(halfHeights);
        tail.Reverse();

        // Combine: head + tail
        var fullHalfHeights = new List<int>(head.Count + tail.Count);
        fullHalfHeights.AddRange(head);
        fullHalfHeights.AddRange(tail);

        // Convert half-heights to full column heights
        var fullHeights = new List<int>(fullHalfHeights.Count);
        if (twoB % 2 == 0)
        {
            foreach (int h in fullHalfHeights)
                fullHeights.Add(h * 2);
        }
        else
        {
            foreach (int h in fullHalfHeights)
                fullHeights.Add(h * 2 - 1);
        }

        return fullHeights;
    }

    /// <summary>
    /// Converts per-column heights to world-space tile coordinates.
    /// Centered (0,0) maps to the center of the bounding box:
    ///   tileX = worldX + i  (column index directly maps to x)
    ///   tileY = worldY + twoB/2 + y  (centered y offset by half-height)
    /// </summary>
    private static HashSet<Point> HeightsToWorldTiles(
        List<int> heights, int twoA, int twoB, int worldX, int worldY)
    {
        var tiles = new HashSet<Point>();
        int W = heights.Count;
        if (W == 0) return tiles;

        int yCenter = twoB / 2;

        for (int i = 0; i < W; i++)
        {
            int H = heights[i];
            if (H <= 0) continue;

            int tileX = worldX + i;

            // YRange: odd H → (-halfH, halfH), even H → (-halfH, halfH - 1)
            int halfH = H / 2;
            int yLo = -halfH;
            int yHi = (H % 2 == 1) ? halfH : halfH - 1;

            for (int y = yLo; y <= yHi; y++)
                tiles.Add(new Point(tileX, worldY + yCenter + y));
        }

        return tiles;
    }
}