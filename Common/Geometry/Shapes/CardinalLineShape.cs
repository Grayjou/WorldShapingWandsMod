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
    /// Thickness uses a "circular brush" approach: for each tile along the center line,
    /// all tiles within radius = thickness/2 are included (Euclidean distance).
    /// This eliminates the checker pattern that perpendicular expansion caused on diagonals.
    /// Odd widths only; even values fall back to width-1 (except 0 → 1px line).
    /// </summary>
    public class CardinalLineShape : IShapeProvider
    {
        public ShapeType ShapeType => ShapeType.CardinalLine;

        /// <summary>
        /// Cache of circle offsets keyed by radius, to avoid recomputing per-tile.
        /// </summary>
        private static readonly Dictionary<int, List<Point>> _circleOffsetCache = new();

        public ShapeTileSet GetTiles(ShapeContext context)
        {
            var tiles = GenerateCardinalLineTiles(context);
            return OutlineHelper.Apply(tiles, context.Mode, context.Thickness);
        }

        /// <summary>
        /// Returns display dimensions that reflect the actual line shape rather than the raw
        /// cursor bounding box. Horizontal lines show Wx1, vertical lines show 1xH,
        /// diagonal lines show NxN where N = min(W,H). Thickness is accounted for.
        /// </summary>
        public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
        {
            var (dir, length) = GetDirectionAndLength(context);
            int effectiveThickness = GetEffectiveThickness(context);
            int lineLength = length + 1; // length is 0-based (distance), display is 1-based (tiles)

            bool isHorizontal = dir.Y == 0 && dir.X != 0;
            bool isVertical = dir.X == 0 && dir.Y != 0;

            if (isHorizontal)
                return (lineLength, effectiveThickness);
            if (isVertical)
                return (effectiveThickness, lineLength);

            // Diagonal: the line sweeps both axes equally, plus thickness adds a diameter
            // in each perpendicular direction
            int thickExpand = effectiveThickness - 1; // total extra tiles (radius on each side)
            return (lineLength + thickExpand, lineLength + thickExpand);
        }

        public bool ContainsPoint(Point point, ShapeContext context)
        {
            var (dir, length) = GetDirectionAndLength(context);
            int effectiveThickness = GetEffectiveThickness(context);
            int radius = effectiveThickness / 2;
            int radiusSq = radius * radius;

            // Check if the point is within circular brush distance of any center-line tile
            for (int i = 0; i <= length; i++)
            {
                int cx = context.Start.X + dir.X * i;
                int cy = context.Start.Y + dir.Y * i;
                int dx = point.X - cx;
                int dy = point.Y - cy;

                if (dx * dx + dy * dy <= radiusSq)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Generates tiles using a circular brush along the center line.
        /// For each point on the center line, stamps all tiles within a circle of
        /// radius = effectiveThickness / 2. The circle offsets are cached per radius
        /// for performance.
        /// </summary>
        private static HashSet<Point> GenerateCardinalLineTiles(ShapeContext context)
        {
            var tiles = new HashSet<Point>();
            var (dir, length) = GetDirectionAndLength(context);
            int effectiveThickness = GetEffectiveThickness(context);
            int radius = effectiveThickness / 2;

            // Get or compute cached circle offsets for this radius
            var offsets = GetCircleOffsets(radius);

            for (int i = 0; i <= length; i++)
            {
                int cx = context.Start.X + dir.X * i;
                int cy = context.Start.Y + dir.Y * i;

                foreach (var offset in offsets)
                {
                    tiles.Add(new Point(cx + offset.X, cy + offset.Y));
                }
            }

            return tiles;
        }

        /// <summary>
        /// Returns cached circle offsets for a given radius.
        /// A radius of 0 returns just the center tile (0,0).
        /// Uses Euclidean distance: dx² + dy² ≤ radius².
        /// </summary>
        private static List<Point> GetCircleOffsets(int radius)
        {
            if (_circleOffsetCache.TryGetValue(radius, out var cached))
                return cached;

            var offsets = new List<Point>();
            int radiusSq = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radiusSq)
                        offsets.Add(new Point(dx, dy));
                }
            }

            _circleOffsetCache[radius] = offsets;
            return offsets;
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
