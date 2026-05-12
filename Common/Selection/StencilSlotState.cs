using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Selection;

public class StencilSlotState
{
    public HashSet<Point> CanvasTiles { get; private set; } = new();

    public HashSet<Point> SelectionTiles { get; private set; } = new();

    public CustomShape CustomShape { get; set; }

    public int CanvasCount => CanvasTiles.Count;

    public int SelectionCount => SelectionTiles.Count;

    public bool HasCanvas => CanvasTiles.Count > 0;

    public bool HasSelection => SelectionTiles.Count > 0;

    public void SetCanvas(IEnumerable<Point> tiles)
    {
        CanvasTiles = tiles == null ? new HashSet<Point>() : new HashSet<Point>(tiles);
    }

    public void SetSelection(IEnumerable<Point> tiles)
    {
        SelectionTiles = tiles == null ? new HashSet<Point>() : new HashSet<Point>(tiles);
    }

    public HashSet<Point> CloneCanvas() => new(CanvasTiles);

    public HashSet<Point> CloneSelection() => new(SelectionTiles);

    public void ClipSelectionToCanvas()
    {
        if (!HasCanvas)
        {
            SelectionTiles.Clear();
            return;
        }

        SelectionTiles.IntersectWith(CanvasTiles);
    }

    public void Translate(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return;

        CanvasTiles = TranslateSet(CanvasTiles, dx, dy);
        SelectionTiles = TranslateSet(SelectionTiles, dx, dy);
    }

    public void ClearCanvas()
    {
        CanvasTiles.Clear();
    }

    public void ClearSelection()
    {
        SelectionTiles.Clear();
    }

    public void Clear()
    {
        CanvasTiles.Clear();
        SelectionTiles.Clear();
        CustomShape = null;
    }

    private static HashSet<Point> TranslateSet(HashSet<Point> src, int dx, int dy)
    {
        var result = new HashSet<Point>(src.Count);
        foreach (var p in src)
            result.Add(new Point(p.X + dx, p.Y + dy));
        return result;
    }
}
