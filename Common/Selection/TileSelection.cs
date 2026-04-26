using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// A set of selected tile positions backed by <see cref="HashSet{Point}"/>.
/// All boolean operations (Add, Remove, Intersect, XOR, Clear, Invert)
/// are constrained by the active <see cref="SelectionCanvas"/> — tiles outside
/// the canvas are never included in the selection.
/// </summary>
public class TileSelection
{
    private HashSet<Point> _selectedTiles = new();

    /// <summary>Read-only view of all selected tile positions.</summary>
    public IReadOnlySet<Point> Tiles => _selectedTiles;

    /// <summary>Number of selected tiles.</summary>
    public int Count => _selectedTiles.Count;

    /// <summary>Whether any tiles are selected.</summary>
    public bool IsActive => _selectedTiles.Count > 0;

    /// <summary>
    /// Returns <c>true</c> if the given tile position is selected.
    /// O(1) lookup via <see cref="HashSet{T}"/>.
    /// </summary>
    public bool Contains(Point p) => _selectedTiles.Contains(p);

    /// <summary>
    /// Apply a boolean operation to the selection using the given shape tiles,
    /// constrained by the canvas. Tiles outside the canvas are never added.
    /// </summary>
    /// <param name="shapeTiles">The tile positions of the shape operand.</param>
    /// <param name="op">The selection operation to perform.</param>
    /// <param name="canvas">The canvas that constrains the selection.</param>
    public void ApplyOperation(
        IEnumerable<Point> shapeTiles,
        SelectionOperation op,
        SelectionCanvas canvas)
    {
        switch (op)
        {
            case SelectionOperation.Add:
            {
                var clipped = ClipToCanvas(shapeTiles, canvas);
                _selectedTiles.UnionWith(clipped);
                break;
            }

            case SelectionOperation.Remove:
            {
                // Remove doesn't need clipping — ExceptWith only removes existing tiles
                _selectedTiles.ExceptWith(shapeTiles);
                break;
            }

            case SelectionOperation.Intersect:
            {
                // Intersect: keep only tiles in both selection and clipped shape
                var clippedSet = new HashSet<Point>(ClipToCanvas(shapeTiles, canvas));
                _selectedTiles.IntersectWith(clippedSet);
                break;
            }

            case SelectionOperation.XOR:
            {
                var clippedSet = new HashSet<Point>(ClipToCanvas(shapeTiles, canvas));
                _selectedTiles.SymmetricExceptWith(clippedSet);
                break;
            }

            case SelectionOperation.Clear:
                _selectedTiles.Clear();
                break;

            case SelectionOperation.Invert:
            {
                // Invert: select all canvas tiles NOT currently selected
                var allCanvas = canvas.GetAllPoints();
                _selectedTiles = new HashSet<Point>(allCanvas.Where(p => !_selectedTiles.Contains(p)));
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }
    }

    /// <summary>
    /// Removes any selected tiles that are no longer within the canvas.
    /// Called after the canvas is modified (shrunk) to maintain the invariant
    /// that the selection is always a subset of the canvas.
    /// </summary>
    public void ClipToCanvas(SelectionCanvas canvas)
    {
        _selectedTiles.RemoveWhere(p => !canvas.Contains(p));
    }

    /// <summary>
    /// Clears all selected tiles.
    /// </summary>
    public void Clear()
    {
        _selectedTiles.Clear();
    }

    /// <summary>
    /// Translates all selected tiles by the given offset.
    /// Used in conjunction with <see cref="SelectionCanvas.Translate"/> to
    /// move the selection along with the canvas.
    /// </summary>
    /// <param name="dx">Horizontal offset in tile units.</param>
    /// <param name="dy">Vertical offset in tile units.</param>
    public void Translate(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;

        var translated = new HashSet<Point>(_selectedTiles.Count);
        foreach (var p in _selectedTiles)
            translated.Add(new Point(p.X + dx, p.Y + dy));

        _selectedTiles = translated;
    }

    /// <summary>
    /// Filters an enumerable of tile positions to only those within the canvas.
    /// </summary>
    private static IEnumerable<Point> ClipToCanvas(IEnumerable<Point> tiles, SelectionCanvas canvas)
    {
        return tiles.Where(p => canvas.Contains(p));
    }
}
