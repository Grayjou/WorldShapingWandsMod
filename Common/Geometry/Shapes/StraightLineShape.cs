using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Common.Geometry.Shapes
{
    /// <summary>
    /// Provides a straight line in one of 8 cardinal/diagonal directions.
    /// Direction is determined by the angle from Start to End using 45° sectors.
    /// Length = Max(|dx|, |dy|) in tiles.
    /// Thickness expands perpendicular to the line direction (odd widths only;
    /// even values fall back to width-1, except 0 which stays as a 1px line).
    /// </summary>
    public class StraightLineShape : IShapeProvider
    {
        public ShapeType ShapeType => ShapeType.StraightLine;

        public ShapeTileSet GetTiles(ShapeContext context)
        {
            var tiles = GenerateStraightLineTiles(context);
            return OutlineHelper.Apply(tiles, context.Mode, context.Thickness);
        }

        public bool ContainsPoint(Point point, ShapeContext context)
        {
            var (dir, length) = GetDirectionAndLength(context);
            int effectiveThickness = GetEffectiveThickness(context);
            int halfThick = effectiveThickness / 2;

            // Check if point lies on any of the thick line's tiles
            var perpDir = GetPerpendicularDirection(dir);

            for (int t = -halfThick; t <= halfThick; t++)
            {
                Point lineStart = new Point(
                    context.Start.X + perpDir.X * t,
                    context.Start.Y + perpDir.Y * t);

                for (int i = 0; i <= length; i++)
                {
                    int tx = lineStart.X + dir.X * i;
                    int ty = lineStart.Y + dir.Y * i;
                    if (point.X == tx && point.Y == ty)
                        return true;
                }
            }

            return false;
        }

        private static HashSet<Point> GenerateStraightLineTiles(ShapeContext context)
        {
            var tiles = new HashSet<Point>();
            var (dir, length) = GetDirectionAndLength(context);
            int effectiveThickness = GetEffectiveThickness(context);
            int halfThick = effectiveThickness / 2;

            // Get perpendicular direction for thickness expansion
            var perpDir = GetPerpendicularDirection(dir);

            for (int t = -halfThick; t <= halfThick; t++)
            {
                Point lineStart = new Point(
                    context.Start.X + perpDir.X * t,
                    context.Start.Y + perpDir.Y * t);

                for (int i = 0; i <= length; i++)
                {
                    tiles.Add(new Point(
                        lineStart.X + dir.X * i,
                        lineStart.Y + dir.Y * i));
                }
            }

            return tiles;
        }

        /// <summary>
        /// Determines the 8-direction unit vector and line length from the context.
        /// Uses 45° angular sectors with π/8 offset boundaries (ImproveGame style).
        /// </summary>
        private static (Point direction, int length) GetDirectionAndLength(ShapeContext context)
        {
            int dx = context.End.X - context.Start.X;
            int dy = context.End.Y - context.Start.Y;

            if (dx == 0 && dy == 0)
                return (Point.Zero, 0);

            // Determine direction using angle-based 8-sector detection
            // atan2 returns angle in radians: -π to π
            double angle = Math.Atan2(dy, dx);

            // Normalize to 0..2π
            if (angle < 0) angle += Math.PI * 2;

            // Each sector is 45° (π/4), offset by half-sector (π/8 = 22.5°)
            // Sector boundaries at 22.5°, 67.5°, 112.5°, etc.
            const double sector = Math.PI / 4.0;
            const double halfSector = Math.PI / 8.0;

            Point dir;

            if (angle < halfSector || angle >= 2 * Math.PI - halfSector)
                dir = new Point(1, 0);       // Right
            else if (angle < halfSector + sector)
                dir = new Point(1, 1);       // Down-Right
            else if (angle < halfSector + 2 * sector)
                dir = new Point(0, 1);       // Down
            else if (angle < halfSector + 3 * sector)
                dir = new Point(-1, 1);      // Down-Left
            else if (angle < halfSector + 4 * sector)
                dir = new Point(-1, 0);      // Left
            else if (angle < halfSector + 5 * sector)
                dir = new Point(-1, -1);     // Up-Left
            else if (angle < halfSector + 6 * sector)
                dir = new Point(0, -1);      // Up
            else
                dir = new Point(1, -1);      // Up-Right

            int length = Math.Max(Math.Abs(dx), Math.Abs(dy));

            return (dir, length);
        }

        /// <summary>
        /// Returns a direction perpendicular to the given line direction.
        /// For cardinal directions, the perpendicular is the other axis.
        /// For diagonals, the perpendicular is rotated 90° clockwise.
        /// </summary>
        private static Point GetPerpendicularDirection(Point dir)
        {
            // Rotate 90° clockwise: (x, y) → (y, -x)
            // This gives a consistent perpendicular for all 8 directions
            return new Point(dir.Y, -dir.X);
        }

        /// <summary>
        /// Calculates effective thickness. Odd values are used directly.
        /// Even values > 0 fall back to value - 1. Zero stays as 1.
        /// </summary>
        private static int GetEffectiveThickness(ShapeContext context)
        {
            int thickness = context.Thickness;
            if (thickness <= 0)
                return 1;

            // Force odd: even values fall back to thickness - 1
            if (thickness % 2 == 0)
                thickness -= 1;

            return Math.Max(1, thickness);
        }
    }
}
