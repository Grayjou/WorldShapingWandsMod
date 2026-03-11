using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes
{

    /// <summary>
    /// Provides L-shaped edge (right-angle joint) calculations.
    /// Creates a path from start to end using two perpendicular segments.
    /// </summary>
    public class ElbowShape : IShapeProvider
    {
        public ShapeType ShapeType => ShapeType.Elbow;

        public ShapeTileSet GetTiles(ShapeContext context)
        {
            var tiles = GenerateElbowTiles(context);
            return OutlineHelper.Apply(tiles, context.Mode, context.Thickness);
        }

        /// <summary>
        /// Returns display dimensions as the two segment lengths of the L-shape.
        /// For VerticalFirst: vertical segment length × horizontal segment length.
        /// For HorizontalFirst: horizontal segment length × vertical segment length.
        /// The "width" is always the first segment drawn, "height" the second.
        /// </summary>
        public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
        {
            int hLen = Math.Abs(context.End.X - context.Start.X) + 1;
            int vLen = Math.Abs(context.End.Y - context.Start.Y) + 1;

            // Display order matches drawing order: first segment = "width", second = "height"
            if (context.VerticalFirst)
                return (vLen, hLen);
            else
                return (hLen, vLen);
        }

        private static HashSet<Point> GenerateElbowTiles(ShapeContext context)
        {
            var tiles = new HashSet<Point>();

            if (context.VerticalFirst)
            {
                // Vertical segment first, then horizontal
                AddVerticalSegment(tiles, context.Start.X, context.Start.Y, context.End.Y);
                AddHorizontalSegment(tiles, context.End.Y, context.Start.X, context.End.X);
            }
            else
            {
                // Horizontal segment first, then vertical
                AddHorizontalSegment(tiles, context.Start.Y, context.Start.X, context.End.X);
                AddVerticalSegment(tiles, context.End.X, context.Start.Y, context.End.Y);
            }

            return tiles;
        }

        public bool ContainsPoint(Point point, ShapeContext context)
        {
            // For edge shapes, check if point is on either segment
            if (context.VerticalFirst)
            {
                bool onVertical = point.X == context.Start.X &&
                                  IsInRange(point.Y, context.Start.Y, context.End.Y);
                bool onHorizontal = point.Y == context.End.Y &&
                                    IsInRange(point.X, context.Start.X, context.End.X);
                return onVertical || onHorizontal;
            }
            else
            {
                bool onHorizontal = point.Y == context.Start.Y &&
                                    IsInRange(point.X, context.Start.X, context.End.X);
                bool onVertical = point.X == context.End.X &&
                                  IsInRange(point.Y, context.Start.Y, context.End.Y);
                return onHorizontal || onVertical;
            }
        }

        private static bool IsInRange(int value, int a, int b)
        {
            return value >= Math.Min(a, b) && value <= Math.Max(a, b);
        }

        private static void AddVerticalSegment(HashSet<Point> tiles, int x, int y1, int y2)
        {
            int step = y1 <= y2 ? 1 : -1;
            int y = y1;

            while (true)
            {
                tiles.Add(new Point(x, y));
                if (y == y2) break;
                y += step;
            }
        }

        private static void AddHorizontalSegment(HashSet<Point> tiles, int y, int x1, int x2)
        {
            int step = x1 <= x2 ? 1 : -1;
            int x = x1;

            while (true)
            {
                tiles.Add(new Point(x, y));
                if (x == x2) break;
                x += step;
            }
        }
    }
}
