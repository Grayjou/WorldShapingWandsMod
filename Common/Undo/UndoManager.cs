using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Undo;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.Undo;

public class UndoManager : ModPlayer
{
    private const int DefaultMaxUndoActions = 20;
    private readonly Stack<UndoAction> _undoStack = new();

    /// <summary>
    /// Returns the configured max undo stack size from <see cref="WandClientConfig"/>,
    /// falling back to <see cref="DefaultMaxUndoActions"/> if the config is unavailable.
    /// </summary>
    private static int MaxUndoActions
    {
        get
        {
            var config = ModContent.GetInstance<WandClientConfig>();
            return config?.MaxUndoStackSize ?? DefaultMaxUndoActions;
        }
    }

    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Returns the undo action at the given 1-based index (1 = most recent).
    /// Returns null if the index is out of range.
    /// </summary>
    public UndoAction PeekAt(int oneBasedIndex)
    {
        if (oneBasedIndex < 1 || oneBasedIndex > _undoStack.Count)
            return null;

        // Stack.ToArray() returns items in pop order: index 0 = newest.
        var entries = _undoStack.ToArray();
        return entries[oneBasedIndex - 1];
    }

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
        Main.NewText(Get("Undone", action.Description, action.Snapshots.Count), Color.Yellow);
        return true;
    }

    public void ClearHistory() => _undoStack.Clear();

    public override void OnEnterWorld() => ClearHistory();
}