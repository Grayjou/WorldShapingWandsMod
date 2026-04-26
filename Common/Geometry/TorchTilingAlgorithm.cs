using Microsoft.Xna.Framework;
using System.Collections.Generic;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// BFS-based expansion engine that computes torch candidate positions
/// for Manhattan (diamond lattice) and Grid (rectangular) tiling styles.
/// </summary>
public static class TorchTilingAlgorithm
{
    /// <summary>
    /// Computes all candidate torch positions using a BFS expansion from
    /// a seed point, using the specified tiling style and spacing.
    /// </summary>
    /// <param name="selectionBounds">Bounding rectangle of the selection area in world tile coordinates.</param>
    /// <param name="seed">The origin torch position (world tile coordinates).</param>
    /// <param name="spacingX">Horizontal spacing between torches (>=1).</param>
    /// <param name="spacingY">Vertical spacing between torches (>=1).</param>
    /// <param name="style">Manhattan (diamond) or Grid (rectangular) tiling pattern.</param>
    /// <param name="flipTiling">When true, uses the flipped Manhattan offsets (diagonally upwards).</param>
    /// <returns>Set of world tile positions where torches should be placed.</returns>
    public static HashSet<Point> ComputePositions(
        Rectangle selectionBounds,
        Point seed,
        int spacingX,
        int spacingY,
        TilingStyle style,
        bool flipTiling = false)
    {
        var result = new HashSet<Point>();
        var queue = new Queue<Point>();

        // Only expand if the seed is within bounds
        if (!selectionBounds.Contains(seed))
            return result;

        result.Add(seed);
        queue.Enqueue(seed);

        var offsets = GetOffsets(spacingX, spacingY, style, flipTiling);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var offset in offsets)
            {
                var candidate = new Point(current.X + offset.X, current.Y + offset.Y);

                if (!selectionBounds.Contains(candidate))
                    continue;

                if (result.Contains(candidate))
                    continue;

                result.Add(candidate);
                queue.Enqueue(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the BFS neighbor offsets for the specified tiling style, spacing, and flip mode.
    /// </summary>
    private static Point[] GetOffsets(int spacingX, int spacingY, TilingStyle style, bool flipTiling)
    {
        return style switch
        {
            TilingStyle.Manhattan => flipTiling
                ? GetFlippedManhattanOffsets(spacingX, spacingY)
                : GetManhattanOffsets(spacingX, spacingY),
            TilingStyle.Grid => GetGridOffsets(spacingX, spacingY),
            _ => GetGridOffsets(spacingX, spacingY),
        };
    }

    /// <summary>
    /// Manhattan (diamond lattice) offsets — normal pattern (diagonally downwards).
    /// Torches propagate in four diagonal directions so the Manhattan distance
    /// between any two adjacent torches is <c>spacingX + spacingY - 1</c>.
    /// </summary>
    private static Point[] GetManhattanOffsets(int dx, int dy)
    {
        return new[]
        {
            new Point(dx, -(dy - 1)),    // Up-Right
            new Point(dx - 1, dy),       // Down-Right
            new Point(-dx, dy - 1),      // Down-Left
            new Point(-(dx - 1), -dy),   // Up-Left
        };
    }

    /// <summary>
    /// Manhattan (diamond lattice) offsets — flipped pattern (diagonally upwards).
    /// Produces a mirror-image tiling where the diagonal runs upward instead of downward.
    /// </summary>
    private static Point[] GetFlippedManhattanOffsets(int dx, int dy)
    {
        return new[]
        {
            new Point(dx - 1, -dy),      // Up-Right
            new Point(dx, dy - 1),       // Down-Right
            new Point(-(dx - 1), dy),    // Down-Left
            new Point(-dx, -(dy - 1)),   // Up-Left
        };
    }

    /// <summary>
    /// Grid (rectangular) offsets.
    /// Torches propagate in four cardinal directions at fixed axis-aligned spacing.
    /// </summary>
    private static Point[] GetGridOffsets(int dx, int dy)
    {
        return new[]
        {
            new Point(0, -(dy + 1)),     // Up
            new Point(0, dy + 1),        // Down
            new Point(dx + 1, 0),        // Right
            new Point(-(dx + 1), 0),     // Left
        };
    }
}