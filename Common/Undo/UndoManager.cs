using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Undo;

namespace WorldShapingWandsMod.Common.Undo;

public class UndoManager : ModPlayer
{
    private const int MaxUndoActions = 20;
    private readonly Stack<UndoAction> _undoStack = new();

    public int UndoCount => _undoStack.Count;

    public UndoAction BeginAction(string description = "Shape Operation")
    {
        var action = new UndoAction { Description = description };
        return action;
    }

    public void CommitAction(UndoAction action)
    {
        if (action.Snapshots.Count == 0) return;

        _undoStack.Push(action);

        // Limit stack size
        if (_undoStack.Count > MaxUndoActions)
        {
            // Keep only the most recent MaxUndoActions entries (drop the oldest).
            // ToArray() returns items in pop (LIFO) order: index 0 = newest, last index = oldest.
            // We push from index [MaxUndoActions-1] down to 0 so that index 0 (newest) ends
            // up on top of the rebuilt stack, preserving correct pop order.
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = MaxUndoActions - 1; i >= 0; i--)
                _undoStack.Push(temp[i]);
        }
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;

        var action = _undoStack.Pop();
        action.Undo();
        Main.NewText($"Undone: {action.Description} ({action.Snapshots.Count} tiles)", Color.Yellow);
        return true;
    }

    public void ClearHistory() => _undoStack.Clear();

    public override void OnEnterWorld() => ClearHistory();
}