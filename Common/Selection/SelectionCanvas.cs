using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// An arbitrary-shaped canvas represented as a <see cref="HashSet{Point}"/> of tile positions.
/// The canvas defines the boundary region that constrains all <see cref="TileSelection"/> operations.
/// Non-rectangular from day one — supports any shape via per-tile storage.
/// Tracks center of mass incrementally for efficient canvas teleportation.
/// </summary>
public class SelectionCanvas
{
    /// <summary>
    /// Tile count threshold at which a warning is shown to the player.
    /// Large canvases can impact performance during edge-detection rendering.
    /// </summary>
    public const int PerformanceWarningThreshold = 50_000;

    private HashSet<Point> _canvasTiles = new();
    private Rectangle _boundingBox;

    // Center of mass — tracked incrementally as (sumX, sumY) / count
    private double _momentX;
    private double _momentY;

    /// <summary>
    /// Cached border edges for fast per-frame rendering.
    /// Each entry stores a tile position and a bitmask of exposed edges:
    /// bit 0 = Top, bit 1 = Right, bit 2 = Bottom, bit 3 = Left.
    /// Recomputed only when the canvas changes (not every frame).
    /// </summary>
    private List<(Point Tile, byte EdgeMask)> _borderEdges = new();

    /// <summary>Read-only view of all canvas tile positions.</summary>
    public IReadOnlySet<Point> Tiles => _canvasTiles;

    /// <summary>Number of tiles in the canvas.</summary>
    public int Count => _canvasTiles.Count;

    /// <summary>Whether the canvas has any tiles.</summary>
    public bool IsActive => _canvasTiles.Count > 0;

    /// <summary>
    /// Axis-aligned bounding box of all canvas tiles. Cached and recalculated on modification.
    /// Used for viewport culling during rendering.
    /// </summary>
    public Rectangle BoundingBox => _boundingBox;

    /// <summary>
    /// Center of mass of the canvas, computed as the average of all tile positions.
    /// Each tile is treated as a unit-area cell centered at (X + 0.5, Y + 0.5) in tile coords.
    /// Returns <see cref="Vector2.Zero"/> when the canvas is empty.
    /// </summary>
    public Vector2 CenterOfMass
    {
        get
        {
            if (_canvasTiles.Count == 0)
                return Vector2.Zero;
            return new Vector2(
                (float)(_momentX / _canvasTiles.Count),
                (float)(_momentY / _canvasTiles.Count));
        }
    }

    /// <summary>
    /// Pre-computed border edges for efficient rendering.
    /// Each entry contains a tile position and a bitmask indicating which edges are exposed
    /// (bit 0 = Top, bit 1 = Right, bit 2 = Bottom, bit 3 = Left).
    /// Updated only when the canvas changes, eliminating per-frame HashSet lookups.
    /// </summary>
    public IReadOnlyList<(Point Tile, byte EdgeMask)> BorderEdges => _borderEdges;

    /// <summary>
    /// Returns <c>true</c> if the given tile position is within the canvas.
    /// O(1) lookup via <see cref="HashSet{T}"/>.
    /// </summary>
    public bool Contains(Point p) => _canvasTiles.Contains(p);

    /// <summary>
    /// Apply a shape-based modification to the canvas.
    /// Center of mass and bounding box are updated incrementally.
    /// </summary>
    /// <param name="shapeTiles">The tile positions of the shape to apply.</param>
    /// <param name="op">The canvas operation to perform.</param>
    public void ApplyOperation(IEnumerable<Point> shapeTiles, CanvasOperation op)
    {
        switch (op)
        {
            case CanvasOperation.Add:
            {
                foreach (var p in shapeTiles)
                {
                    if (_canvasTiles.Add(p))
                    {
                        _momentX += p.X + 0.5;
                        _momentY += p.Y + 0.5;
                    }
                }
                break;
            }

            case CanvasOperation.Remove:
            {
                foreach (var p in shapeTiles)
                {
                    if (_canvasTiles.Remove(p))
                    {
                        _momentX -= p.X + 0.5;
                        _momentY -= p.Y + 0.5;
                    }
                }
                break;
            }

            case CanvasOperation.Clear:
                _canvasTiles.Clear();
                _momentX = 0;
                _momentY = 0;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }

        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    /// <summary>
    /// Translates all canvas tiles by the given offset.
    /// Used by "Teleport to Player" to move the canvas so its center of mass
    /// lands on the player's position.
    /// </summary>
    /// <param name="dx">Horizontal offset in tile units.</param>
    /// <param name="dy">Vertical offset in tile units.</param>
    public void Translate(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;

        var translated = new HashSet<Point>(_canvasTiles.Count);
        foreach (var p in _canvasTiles)
            translated.Add(new Point(p.X + dx, p.Y + dy));

        _canvasTiles = translated;
        _momentX += (double)dx * _canvasTiles.Count;
        _momentY += (double)dy * _canvasTiles.Count;
        RecalculateBoundingBox();

        // Border edges just need their Point offset — topology is unchanged.
        for (int i = 0; i < _borderEdges.Count; i++)
        {
            var (pt, mask) = _borderEdges[i];
            _borderEdges[i] = (new Point(pt.X + dx, pt.Y + dy), mask);
        }
    }

    public void FlipHorizontal()
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.FlipHorizontal(_canvasTiles);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void FlipHorizontal(Rectangle referenceBounds, bool ensureNonNegative = true)
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.FlipHorizontal(_canvasTiles, referenceBounds, ensureNonNegative);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void FlipVertical()
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.FlipVertical(_canvasTiles);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void FlipVertical(Rectangle referenceBounds, bool ensureNonNegative = true)
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.FlipVertical(_canvasTiles, referenceBounds, ensureNonNegative);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void Rotate90CW()
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.Rotate90CW(_canvasTiles);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void Rotate90CW(double pivotX, double pivotY, bool ensureNonNegative = true)
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.Rotate90CW(_canvasTiles, pivotX, pivotY, ensureNonNegative);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void Rotate90CCW()
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.Rotate90CCW(_canvasTiles);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    public void Rotate90CCW(double pivotX, double pivotY, bool ensureNonNegative = true)
    {
        if (_canvasTiles.Count == 0) return;
        _canvasTiles = TileCoordTransforms.Rotate90CCW(_canvasTiles, pivotX, pivotY, ensureNonNegative);
        RecalculateMoments();
        RecalculateBoundingBox();
        RecomputeBorderEdges();
    }

    /// <summary>
    /// Read-only access to all canvas tile positions.
    /// Used by <see cref="TileSelection.ApplyOperation"/> for Invert operations.
    /// </summary>
    public IReadOnlySet<Point> GetAllPoints() => _canvasTiles;

    /// <summary>
    /// Clears all canvas tiles and resets the bounding box and center of mass.
    /// </summary>
    public void Clear()
    {
        _canvasTiles.Clear();
        _borderEdges.Clear();
        _boundingBox = Rectangle.Empty;
        _momentX = 0;
        _momentY = 0;
    }

    /// <summary>
    /// Recomputes the cached border edges from scratch.
    /// For each canvas tile, checks 4 cardinal neighbours — any edge facing a non-canvas
    /// tile is recorded in the bitmask (bit 0 = Top, 1 = Right, 2 = Bottom, 3 = Left).
    /// Only tiles with at least one exposed edge are included.
    /// Called after every modification except <see cref="Translate"/>,
    /// which offsets existing edges instead.
    /// </summary>
    private void RecomputeBorderEdges()
    {
        _borderEdges.Clear();

        foreach (var tile in _canvasTiles)
        {
            byte mask = 0;
            if (!_canvasTiles.Contains(new Point(tile.X, tile.Y - 1))) mask |= 0b0001; // Top
            if (!_canvasTiles.Contains(new Point(tile.X + 1, tile.Y))) mask |= 0b0010; // Right
            if (!_canvasTiles.Contains(new Point(tile.X, tile.Y + 1))) mask |= 0b0100; // Bottom
            if (!_canvasTiles.Contains(new Point(tile.X - 1, tile.Y))) mask |= 0b1000; // Left

            if (mask != 0)
                _borderEdges.Add((tile, mask));
        }
    }

    /// <summary>
    /// Recalculates the axis-aligned bounding box from all tile positions.
    /// Called after every modification.
    /// </summary>
    private void RecalculateBoundingBox()
    {
        if (_canvasTiles.Count == 0)
        {
            _boundingBox = Rectangle.Empty;
            return;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var p in _canvasTiles)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        _boundingBox = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void RecalculateMoments()
    {
        _momentX = 0;
        _momentY = 0;
        foreach (var p in _canvasTiles)
        {
            _momentX += p.X + 0.5;
            _momentY += p.Y + 0.5;
        }
    }
}
