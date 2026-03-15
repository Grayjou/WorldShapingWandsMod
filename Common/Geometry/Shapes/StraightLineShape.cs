using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Common.Geometry.Shapes
{
    /// <summary>
    /// Provides a straight line at any arbitrary angle from Start to End.
    /// Unlike <see cref="CardinalLineShape"/> which snaps to 8 cardinal/diagonal directions,
    /// this shape draws a true point-to-point line using Bresenham's line algorithm.
    /// Thickness uses the same circular brush approach as CardinalLine.
    /// </summary>
    public class StraightLineShape : IShapeProvider
    {
        public ShapeType ShapeType => ShapeType.StraightLine;

        public ShapeTileSet GetTiles(ShapeContext context)
        {
            var tiles = GenerateStraightLineTiles(context);

            if (context.Slice != SliceMode.Full)
                return SliceHelper.ApplySlicing(tiles, context);

            return OutlineHelper.Apply(tiles, context.Mode, context.Thickness);
        }

        /// <summary>
        /// Returns display dimensions that reflect the actual line extent.
        /// Shows the bounding box of the line including thickness expansion.
        /// </summary>
        public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
        {
            int dx = Math.Abs(context.End.X - context.Start.X);
            int dy = Math.Abs(context.End.Y - context.Start.Y);
            int effectiveThickness = GetEffectiveThickness(context);
            int thickExpand = effectiveThickness - 1;

            // The line spans from Start to End; thickness adds radius in each direction
            return (dx + 1 + thickExpand, dy + 1 + thickExpand);
        }

        public bool ContainsPoint(Point point, ShapeContext context)
        {
            int effectiveThickness = GetEffectiveThickness(context);
            var offsets = CardinalLineShape.GetCircleOffsetsStatic(effectiveThickness);
            var offsetSet = new HashSet<Point>(offsets);

            // Walk the Bresenham center line and check if point is within brush radius
            foreach (var center in BresenhamLine(context.Start, context.End))
            {
                Point delta = new Point(point.X - center.X, point.Y - center.Y);
                if (offsetSet.Contains(delta))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Generates tiles using Bresenham's line algorithm for the center line,
        /// then stamps a circular brush at each center point (same approach as CardinalLine).
        /// </summary>
        private static HashSet<Point> GenerateStraightLineTiles(ShapeContext context)
        {
            var tiles = new HashSet<Point>();
            int effectiveThickness = GetEffectiveThickness(context);
            var offsets = CardinalLineShape.GetCircleOffsetsStatic(effectiveThickness);

            foreach (var center in BresenhamLine(context.Start, context.End))
            {
                foreach (var offset in offsets)
                {
                    tiles.Add(new Point(center.X + offset.X, center.Y + offset.Y));
                }
            }

            return tiles;
        }

        /// <summary>
        /// Bresenham's line algorithm. Produces all integer tile coordinates along
        /// the line from p0 to p1, visiting every tile the line passes through.
        /// Handles all octants correctly via axis swapping and sign correction.
        /// </summary>
        private static IEnumerable<Point> BresenhamLine(Point p0, Point p1)
        {
            int x0 = p0.X, y0 = p0.Y;
            int x1 = p1.X, y1 = p1.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                yield return new Point(x0, y0);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Calculates effective thickness. Zero or negative values default to 1.
        /// </summary>
        private static int GetEffectiveThickness(ShapeContext context)
        {
            return Math.Max(1, context.Thickness);
        }
    }
}
