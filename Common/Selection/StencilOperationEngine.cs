using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Selection;

public static class StencilOperationEngine
{
    public static HashSet<Point> BuildOperandTiles(WandPlayer wandPlayer, ShapeInfo shape)
    {
        var selection = wandPlayer.GetVisualSelection();
        if (!selection.IsActive)
            return new HashSet<Point>();

        var context = shape.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(shape.Shape, context);
        var applied = shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        return applied == null ? new HashSet<Point>() : new HashSet<Point>(applied);
    }

    public static bool EnsureCanvasForSelection(
        StencilSlotState slot,
        HashSet<Point> operandTiles,
        bool autoCreateCanvas)
    {
        if (slot.HasCanvas)
            return true;

        if (!autoCreateCanvas)
            return false;

        slot.SetCanvas(operandTiles);
        return slot.HasCanvas;
    }

    public static void ExecuteCanvasOperation(
        StencilSlotState slot,
        HashSet<Point> operandTiles,
        SelectionOperation operation)
    {
        operandTiles ??= new HashSet<Point>();
        ApplyCanvasOperation(slot.CanvasTiles, operandTiles, operation);
        slot.ClipSelectionToCanvas();
    }

    public static bool ExecuteSelectionOperation(
        StencilSlotState slot,
        HashSet<Point> operandTiles,
        SelectionOperation operation,
        bool autoCreateCanvas)
    {
        operandTiles ??= new HashSet<Point>();
        if (!EnsureCanvasForSelection(slot, operandTiles, autoCreateCanvas))
            return false;

        var clippedOperand = new HashSet<Point>(operandTiles);
        clippedOperand.IntersectWith(slot.CanvasTiles);

        ApplySelectionOperation(slot.SelectionTiles, clippedOperand, operation);
        return true;
    }

    public static CanvasOperation ToCanvasOperation(SelectionOperation operation)
        => operation switch
        {
            SelectionOperation.Add => CanvasOperation.Add,
            SelectionOperation.Remove => CanvasOperation.Remove,
            SelectionOperation.Clear => CanvasOperation.Clear,
            _ => CanvasOperation.Add,
        };

    public static void ApplyCanvasOperation(
        HashSet<Point> canvas,
        HashSet<Point> operand,
        SelectionOperation operation)
    {
        switch (ToCanvasOperation(operation))
        {
            case CanvasOperation.Add:
                canvas.UnionWith(operand);
                break;
            case CanvasOperation.Remove:
                canvas.ExceptWith(operand);
                break;
            case CanvasOperation.Clear:
                canvas.Clear();
                break;
        }
    }

    public static void ApplySelectionOperation(
        HashSet<Point> selection,
        HashSet<Point> operand,
        SelectionOperation operation)
    {
        switch (operation)
        {
            case SelectionOperation.Add:
                selection.UnionWith(operand);
                break;
            case SelectionOperation.Remove:
                selection.ExceptWith(operand);
                break;
            case SelectionOperation.Intersect:
                selection.IntersectWith(operand);
                break;
            case SelectionOperation.XOR:
                selection.SymmetricExceptWith(operand);
                break;
            case SelectionOperation.Clear:
                selection.Clear();
                break;
            default:
                selection.UnionWith(operand);
                break;
        }
    }
}
