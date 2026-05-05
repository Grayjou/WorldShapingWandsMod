using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// An origin-normalized set of tile offsets captured from a <see cref="TileSelection"/>.
/// Used by Stamp wands to stamp arbitrary user-defined shapes.
/// All tile positions are relative to (0, 0) — call <see cref="GetTilesAt"/> to
/// translate to a world position for execution.
/// </summary>
public class CustomShape
{
    /// <summary>Tile positions relative to (0, 0) origin.</summary>
    public HashSet<Point> RelativeTiles { get; }

    /// <summary>Bounding box of the relative tiles (origin at 0, 0).</summary>
    public Rectangle BoundingBox { get; }

    /// <summary>Number of tiles in the custom shape.</summary>
    public int Count => RelativeTiles.Count;

    private CustomShape(HashSet<Point> relativeTiles, Rectangle bounds)
    {
        RelativeTiles = relativeTiles;
        BoundingBox = bounds;
    }

    /// <summary>
    /// Creates a <see cref="CustomShape"/> from an active <see cref="TileSelection"/>,
    /// normalizing all tile positions so the minimum corner is at (0, 0).
    /// Returns <c>null</c> if the selection is empty.
    /// </summary>
    /// <param name="selection">The tile selection to capture.</param>
    /// <returns>A new <see cref="CustomShape"/>, or <c>null</c> if the selection is empty.</returns>
    public static CustomShape FromSelection(TileSelection selection)
    {
        if (!selection.IsActive)
            return null;

        var tiles = selection.Tiles;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var p in tiles)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        var normalized = new HashSet<Point>(tiles.Count);
        foreach (var p in tiles)
            normalized.Add(new Point(p.X - minX, p.Y - minY));

        return new CustomShape(
            normalized,
            new Rectangle(0, 0, maxX - minX + 1, maxY - minY + 1));
    }

    /// <summary>
    /// Returns the absolute tile positions when the shape is placed at the given world position.
    /// The <paramref name="worldPos"/> becomes the origin (0, 0) of the shape.
    /// </summary>
    /// <param name="worldPos">The world tile position to anchor the shape at.</param>
    /// <returns>A set of absolute tile positions.</returns>
    public HashSet<Point> GetTilesAt(Point worldPos)
    {
        var result = new HashSet<Point>(RelativeTiles.Count);
        foreach (var p in RelativeTiles)
            result.Add(new Point(p.X + worldPos.X, p.Y + worldPos.Y));
        return result;
    }

    /// <summary>
    /// Returns the absolute tile positions when the shape is placed at the given
    /// world position with a specific anchor offset within the shape.
    /// </summary>
    /// <param name="worldPos">The world tile position where the anchor point should be.</param>
    /// <param name="anchorOffset">The offset within the shape that aligns to <paramref name="worldPos"/>.</param>
    /// <returns>A set of absolute tile positions.</returns>
    public HashSet<Point> GetTilesAtWithAnchor(Point worldPos, Point anchorOffset)
    {
        int offsetX = worldPos.X - anchorOffset.X;
        int offsetY = worldPos.Y - anchorOffset.Y;

        var result = new HashSet<Point>(RelativeTiles.Count);
        foreach (var p in RelativeTiles)
            result.Add(new Point(p.X + offsetX, p.Y + offsetY));
        return result;
    }

    public CustomShape FlipHorizontal()
    {
        var transformed = TileCoordTransforms.FlipHorizontal(RelativeTiles);
        var normalized = TileCoordTransforms.NormalizeToOrigin(transformed);
        var bounds = TileCoordTransforms.ComputeBounds(normalized);
        return new CustomShape(normalized, bounds);
    }

    public CustomShape FlipVertical()
    {
        var transformed = TileCoordTransforms.FlipVertical(RelativeTiles);
        var normalized = TileCoordTransforms.NormalizeToOrigin(transformed);
        var bounds = TileCoordTransforms.ComputeBounds(normalized);
        return new CustomShape(normalized, bounds);
    }

    public CustomShape Rotate90CW()
    {
        var transformed = TileCoordTransforms.Rotate90CW(RelativeTiles);
        var normalized = TileCoordTransforms.NormalizeToOrigin(transformed);
        var bounds = TileCoordTransforms.ComputeBounds(normalized);
        return new CustomShape(normalized, bounds);
    }

    public CustomShape Rotate90CCW()
    {
        var transformed = TileCoordTransforms.Rotate90CCW(RelativeTiles);
        var normalized = TileCoordTransforms.NormalizeToOrigin(transformed);
        var bounds = TileCoordTransforms.ComputeBounds(normalized);
        return new CustomShape(normalized, bounds);
    }
}
