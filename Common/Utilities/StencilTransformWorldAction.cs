using System;
using Microsoft.Xna.Framework;
using Terraria;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Utilities;

public static class StencilTransformWorldAction
{
    public readonly struct TransformState
    {
        public TransformActionMode ActiveAction { get; init; }

        public Point? PendingMoveStart { get; init; }

        public Point? PersistentPivot { get; init; }

        public Point? TemporaryPivot { get; init; }
    }

    public static bool IsArmed(bool transformModeEnabled, TransformActionMode activeAction, bool isMouseOverUi)
    {
        if (!transformModeEnabled || activeAction == TransformActionMode.None)
            return false;

        if (isMouseOverUi)
            return false;

        return true;
    }

    public static bool ShouldInterceptTransformClick(Player player, bool isArmed, Func<bool> handleTransform)
    {
        if (!isArmed)
            return false;

        if (Main.myPlayer != player.whoAmI)
            return true;

        if (!Main.mouseLeft || Main.LocalPlayer.mouseInterface)
            return true;

        if (!Main.mouseLeftRelease)
            return true;

        Main.mouseLeftRelease = false;
        return handleTransform();
    }

    public static bool Handle(
        TransformState state,
        Point mouseTile,
        bool hasAnyTiles,
        Action<int, int> applyTranslate,
        Action<string> showHint,
        out TransformState updated)
    {
        updated = state;

        switch (state.ActiveAction)
        {
            case TransformActionMode.SetPivotPersistent:
                updated = new TransformState
                {
                    ActiveAction = state.ActiveAction,
                    PendingMoveStart = null,
                    PersistentPivot = mouseTile,
                    TemporaryPivot = state.TemporaryPivot,
                };
                showHint("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotPersistentHint");
                return true;

            case TransformActionMode.SetPivotTemporary:
                updated = new TransformState
                {
                    ActiveAction = state.ActiveAction,
                    PendingMoveStart = null,
                    PersistentPivot = state.PersistentPivot,
                    TemporaryPivot = mouseTile,
                };
                showHint("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotTemporaryHint");
                return true;

            case TransformActionMode.Move:
                if (!hasAnyTiles)
                    return true;

                if (!state.PendingMoveStart.HasValue)
                {
                    updated = new TransformState
                    {
                        ActiveAction = state.ActiveAction,
                        PendingMoveStart = mouseTile,
                        PersistentPivot = state.PersistentPivot,
                        TemporaryPivot = state.TemporaryPivot,
                    };
                    showHint("Mods.WorldShapingWandsMod.UI.TransformMode.MoveStartHint");
                    return true;
                }

                Point start = state.PendingMoveStart.Value;
                int dx = mouseTile.X - start.X;
                int dy = mouseTile.Y - start.Y;

                updated = new TransformState
                {
                    ActiveAction = state.ActiveAction,
                    PendingMoveStart = null,
                    PersistentPivot = state.PersistentPivot.HasValue
                        ? new Point(state.PersistentPivot.Value.X + dx, state.PersistentPivot.Value.Y + dy)
                        : null,
                    TemporaryPivot = state.TemporaryPivot.HasValue
                        ? new Point(state.TemporaryPivot.Value.X + dx, state.TemporaryPivot.Value.Y + dy)
                        : null,
                };

                if (dx == 0 && dy == 0)
                    return true;

                applyTranslate(dx, dy);
                showHint("Mods.WorldShapingWandsMod.UI.TransformMode.MoveHint");
                return true;
        }

        return false;
    }
}
