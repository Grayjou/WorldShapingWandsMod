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
        while (_undoStack.Count > MaxUndoActions)
        {
            // Remove oldest (would need a different data structure for efficiency)
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < temp.Length - 1; i++)
                _undoStack.Push(temp[temp.Length - 1 - i]);
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