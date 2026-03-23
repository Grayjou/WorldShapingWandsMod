using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using System;
using System.Collections.Generic;

#nullable enable

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// Immutable selection state - tracks current wand selection.
/// </summary>
public sealed class SelectionState
{
    public Point StartTile { get; }
    public Point EndTile { get; }
    public bool IsActive { get; }
    public bool WasClamped { get; }
    public bool VerticalFirst { get; }
    public bool IsLocked { get; }  // Prevents endpoint updates

    // ?? Multi-point shape support ??
    /// <summary>
    /// Additional defining points for multi-point shapes (3+ points).
    /// Null for standard 2-point shapes. Points are stored in click order.
    /// </summary>
    public IReadOnlyList<Point>? IntermediatePoints { get; }

    /// <summary>
    /// How many shape-defining points have been placed so far.
    /// For 2-point shapes: 0 (empty), 1 (start placed), 2 (both placed).
    /// For multi-point shapes: tracks progress toward PointsRequired.
    /// </summary>
    public int PointsPlaced { get; }

    /// <summary>
    /// How many points are required to fully define the current shape.
    /// 2 for all current shapes. Higher for future multi-point shapes.
    /// -1 for variable-point shapes (e.g. Polygon).
    /// </summary>
    public int PointsRequired { get; }

    /// <summary>
    /// True when all shape-defining points have been placed.
    /// For 2-point shapes, equivalent to IsActive (both Start and End are set).
    /// For variable-point shapes (PointsRequired == -1), must be set explicitly.
    /// </summary>
    public bool IsShapeComplete => PointsRequired >= 0
        ? PointsPlaced >= PointsRequired
        : _explicitlyComplete;

    private readonly bool _explicitlyComplete;

    private SelectionState(Point start, Point end, bool isActive, bool wasClamped,
        bool verticalFirst, bool isLocked, IReadOnlyList<Point>? intermediatePoints = null,
        int pointsPlaced = 0, int pointsRequired = 2, bool explicitlyComplete = false)
    {
        StartTile = start;
        EndTile = end;
        IsActive = isActive;
        WasClamped = wasClamped;
        VerticalFirst = verticalFirst;
        IsLocked = isLocked;
        IntermediatePoints = intermediatePoints;
        PointsPlaced = pointsPlaced;
        PointsRequired = pointsRequired;
        _explicitlyComplete = explicitlyComplete;
    }

    public static SelectionState Empty => new(Point.Zero, Point.Zero, false, false, false, false, pointsRequired: 2);

    public static SelectionState Create(Point start, Point end, bool verticalFirst = false,
        bool wasClamped = false, int pointsRequired = 2)
        => new(start, end, true, wasClamped, verticalFirst, false, pointsRequired: pointsRequired, pointsPlaced: 2);

    public SelectionState WithEnd(Point newEnd, bool wasClamped = false)
        => new(StartTile, newEnd, IsActive, wasClamped, VerticalFirst, IsLocked,
            IntermediatePoints, PointsPlaced, PointsRequired, _explicitlyComplete);

    public SelectionState WithLocked(bool locked)
        => new(StartTile, EndTile, IsActive, WasClamped, VerticalFirst, locked,
            IntermediatePoints, PointsPlaced, PointsRequired, _explicitlyComplete);

    public ShapeContext ToShapeContext(ShapeMode mode = ShapeMode.Filled, int thickness = 1)
    {
        return new ShapeContext(StartTile, EndTile)
        {
            Mode = mode,
            Thickness = thickness,
            VerticalFirst = VerticalFirst
        };
    }

    public int Width => Math.Abs(EndTile.X - StartTile.X) + 1;
    public int Height => Math.Abs(EndTile.Y - StartTile.Y) + 1;

    /// <summary>
    /// Returns the Chebyshev distance (max of dx, dy) from a point to the closest
    /// edge of the selection's bounding box, in tiles.
    /// Returns 0 if the point is inside or on the boundary.
    /// </summary>
    public int DistanceFromBBox(Point tile)
    {
        int minX = Math.Min(StartTile.X, EndTile.X);
        int maxX = Math.Max(StartTile.X, EndTile.X);
        int minY = Math.Min(StartTile.Y, EndTile.Y);
        int maxY = Math.Max(StartTile.Y, EndTile.Y);

        int dx = tile.X < minX ? minX - tile.X
               : tile.X > maxX ? tile.X - maxX
               : 0;
        int dy = tile.Y < minY ? minY - tile.Y
               : tile.Y > maxY ? tile.Y - maxY
               : 0;

        return Math.Max(dx, dy);
    }
}