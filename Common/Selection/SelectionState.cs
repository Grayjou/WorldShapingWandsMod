using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using System;

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
    public bool IsLocked { get; }  // NEW: Prevents endpoint updates

    private SelectionState(Point start, Point end, bool isActive, bool wasClamped, bool verticalFirst, bool isLocked)
    {
        StartTile = start;
        EndTile = end;
        IsActive = isActive;
        WasClamped = wasClamped;
        VerticalFirst = verticalFirst;
        IsLocked = isLocked;
    }

    public static SelectionState Empty => new(Point.Zero, Point.Zero, false, false, false, false);

    public static SelectionState Create(Point start, Point end, bool verticalFirst = false, bool wasClamped = false)
        => new(start, end, true, wasClamped, verticalFirst, false);

    public SelectionState WithEnd(Point newEnd, bool wasClamped = false)
        => new(StartTile, newEnd, IsActive, wasClamped, VerticalFirst, IsLocked);

    public SelectionState WithLocked(bool locked)  // NEW: Lock/unlock method
        => new(StartTile, EndTile, IsActive, WasClamped, VerticalFirst, locked);

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
}