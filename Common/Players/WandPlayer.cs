using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player wand state.
/// </summary>
public class WandPlayer : ModPlayer
{
    public SelectionState Selection { get; private set; } = SelectionState.Empty;

    /// <summary>
    /// Tracks the Type (item ID) of the wand that started the current selection.
    /// Used to prevent cross-wand selection execution (e.g., switching from Confirm to Instant
    /// with an active selection should NOT execute that selection).
    /// </summary>
    public int SelectionOwnerItemType { get; private set; }

    // === Stamp mode state ===
    /// <summary>Whether the stamp template has been locked (3rd click done).</summary>
    public bool IsStampLocked { get; private set; }
    /// <summary>The stamp template delta (EndTile - StartTile) at lock time.</summary>
    public Point StampDelta { get; private set; }
    /// <summary>The anchor offset within the stamp rectangle that the mouse was on at lock time.</summary>
    public Point StampAnchorOffset { get; private set; }

    private bool _lastMouseLeft;
    private ulong _lastConsumedLeftClickTick;
    private bool _justCancelled; // Prevents immediate restart after cancellation
    
    /// <summary>
    /// Stores the visual state of the last cancelled selection for overlay feedback.
    /// Null when no cancellation is pending display.
    /// </summary>
    public CancelledSelectionState CancelledSelection { get; private set; }

    // Per-wand settings
    public WandOfBuildingSettings BuildingSettings { get; private set; } = new();
    public WandOfDestructionSettings DestructionSettings { get; private set; } = new();
    public WandOfReplacementSettings ReplacementSettings { get; private set; } = new();
    public WandOfWiringSettings WiringSettings { get; private set; } = new();

    // Keep global settings for backward compatibility with test commands
    public WandSettings Settings { get; private set; } = new WandSettings();

    public bool TryConsumeFreshLeftClick()
    {
        if (!Main.mouseLeft)
            return false;

        ulong currentTick = Main.GameUpdateCount;
        if (_lastConsumedLeftClickTick == currentTick)
            return false;

        bool isFreshPress = !_lastMouseLeft;
        if (!isFreshPress)
            return false;

        _lastConsumedLeftClickTick = currentTick;
        return true;
    }

    public void StartSelection(Point start, bool verticalFirst)
    {
        Settings.VerticalFirst = verticalFirst;
        Selection = SelectionState.Create(start, start, verticalFirst);
        SelectionOwnerItemType = Player.HeldItem?.type ?? 0;
    }

    public void UpdateSelection(Point end, bool wasClamped = false)
    {
        if (Selection.IsActive && !Selection.IsLocked)
        {
            // Apply dimension capping
            var capped = ClampEndToCaps(Selection.StartTile, end);
            bool dimensionClamped = capped != end;
            Selection = Selection.WithEnd(capped, wasClamped || dimensionClamped);
        }
    }

    /// <summary>
    /// Clamps the end point so the selection dimensions don't exceed the config cap.
    /// Uses SmallSelectionCap for Edge/StraightLine, BigSelectionCap for everything else.
    /// </summary>
    private Point ClampEndToCaps(Point start, Point end)
    {
        var config = ModContent.GetInstance<WandConfig>();
        if (config == null) return end;

        ShapeType currentShape = GetCurrentShapeType();
        int cap = (currentShape == ShapeType.Edge || currentShape == ShapeType.StraightLine)
            ? config.SmallSelectionCap
            : config.BigSelectionCap;

        int dx = end.X - start.X;
        int dy = end.Y - start.Y;

        // Clamp each axis independently: cap means max dimension = cap, so max offset = cap - 1
        int maxOffset = cap - 1;
        if (maxOffset < 0) maxOffset = 0;

        dx = System.Math.Clamp(dx, -maxOffset, maxOffset);
        dy = System.Math.Clamp(dy, -maxOffset, maxOffset);

        return new Point(start.X + dx, start.Y + dy);
    }

    /// <summary>
    /// Determines which ShapeType is currently active based on the held wand.
    /// </summary>
    private ShapeType GetCurrentShapeType()
    {
        if (Player.HeldItem?.ModItem is WandOfBuildingBase)
            return BuildingSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfDestructionBase)
            return DestructionSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfReplacementBase)
            return ReplacementSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfWiringBase)
            return WiringSettings.Shape.Shape;
        return Settings.ShapeType;
    }

    // NEW: Lock the selection to prevent endpoint updates
    public void LockSelection()
    {
        if (Selection.IsActive)
            Selection = Selection.WithLocked(true);
    }

    // NEW: Unlock if needed (e.g., user wants to adjust)
    public void UnlockSelection()
    {
        if (Selection.IsActive)
            Selection = Selection.WithLocked(false);
    }

    public SelectionState CompleteSelection()
    {
        var completed = Selection;
        Selection = SelectionState.Empty;
        return completed;
    }

    public void ClearSelection()
    {
        Selection = SelectionState.Empty;
        SelectionOwnerItemType = 0;
        IsStampLocked = false;
        StampDelta = Point.Zero;
        StampAnchorOffset = Point.Zero;
    }

    public void CancelSelection()
    {
        CancelSelection(WandColors.CancelBuilding, ShapeInfo.Default);
    }

    /// <summary>
    /// Cancels the current selection with visual feedback.
    /// Snapshots the selection for a brief colour-changed overlay.
    /// </summary>
    public void CancelSelection(Color cancelColor, ShapeInfo shape)
    {
        if (Selection.IsActive)
        {
            CancelledSelection = new CancelledSelectionState(
                Selection.StartTile,
                Selection.EndTile,
                Selection.VerticalFirst,
                shape,
                cancelColor,
                WandColors.CancelOverlayDurationTicks);
        }

        Selection = SelectionState.Empty;
        SelectionOwnerItemType = 0;
        IsStampLocked = false;
        StampDelta = Point.Zero;
        StampAnchorOffset = Point.Zero;
        _justCancelled = true; // Set flag to prevent immediate restart
    }

    /// <summary>
    /// Locks the stamp template. The stamp delta is computed from the current selection,
    /// and the anchor offset records which part of the selection the mouse was on at lock time.
    /// </summary>
    public void LockStamp(Point mouseTile)
    {
        if (!Selection.IsActive) return;

        var start = Selection.StartTile;
        var end = Selection.EndTile;
        StampDelta = new Point(end.X - start.X, end.Y - start.Y);

        // Anchor = offset of mouse from the start corner of the selection bounding box
        int minX = System.Math.Min(start.X, end.X);
        int minY = System.Math.Min(start.Y, end.Y);
        StampAnchorOffset = new Point(mouseTile.X - minX, mouseTile.Y - minY);
        IsStampLocked = true;

        // Lock the selection so PostUpdate doesn't move the endpoint
        LockSelection();
    }

    /// <summary>
    /// Repositions the stamp selection so the anchor follows the mouse.
    /// Called each frame/click while stamp is locked.
    /// </summary>
    public void MoveStampTo(Point mouseTile)
    {
        if (!IsStampLocked) return;

        // New top-left = mouse - anchor
        int newMinX = mouseTile.X - StampAnchorOffset.X;
        int newMinY = mouseTile.Y - StampAnchorOffset.Y;

        // Reconstruct start and end preserving the delta sign
        Point newStart, newEnd;
        if (StampDelta.X >= 0 && StampDelta.Y >= 0)
        {
            newStart = new Point(newMinX, newMinY);
            newEnd = new Point(newMinX + StampDelta.X, newMinY + StampDelta.Y);
        }
        else if (StampDelta.X < 0 && StampDelta.Y >= 0)
        {
            // Start was to the right of end horizontally
            newEnd = new Point(newMinX, newMinY + StampDelta.Y);
            newStart = new Point(newMinX - StampDelta.X, newMinY);
        }
        else if (StampDelta.X >= 0 && StampDelta.Y < 0)
        {
            newEnd = new Point(newMinX + StampDelta.X, newMinY);
            newStart = new Point(newMinX, newMinY - StampDelta.Y);
        }
        else
        {
            // Both negative
            newEnd = new Point(newMinX, newMinY);
            newStart = new Point(newMinX - StampDelta.X, newMinY - StampDelta.Y);
        }

        // Directly set the selection with new start/end (bypass clamping for stamp)
        Selection = SelectionState.Create(newStart, newEnd, Selection.VerticalFirst)
                        .WithLocked(true);
    }

    public bool CanStartNewSelection()
    {
        // Can't start if we just cancelled and left mouse is still held
        if (_justCancelled && Main.mouseLeft)
            return false;
        
        // Clear the flag as soon as left mouse is released (transition from held to released)
        if (_justCancelled && !Main.mouseLeft)
        {
            _justCancelled = false;
        }
        
        return true;
    }

    /// <summary>
    /// Returns true if the currently held wand item is the one that started the active selection.
    /// Used by Instant wands to avoid executing a selection that was started by a different wand
    /// (e.g., switching from a Confirm wand with a pending selection to an Instant wand).
    /// </summary>
    public bool IsSelectionOwnedByCurrentItem()
    {
        if (!Selection.IsActive) return false;
        int currentType = Player.HeldItem?.type ?? 0;
        return currentType != 0 && currentType == SelectionOwnerItemType;
    }

    public void ResetCancellationFlag()
    {
        _justCancelled = false;
    }

    /// <summary>
    /// Builds a ShapeContext from the current selection + settings.
    /// </summary>
    public ShapeContext GetCurrentShapeContext()
    {
        return Settings.ToShapeContext(Selection.StartTile, Selection.EndTile);
    }

    public override void PostUpdate()
    {
        // Update selection preview in real-time when selection is active AND not locked
        if (Selection.IsActive && !Selection.IsLocked && IsHoldingWandItem())
        {
            Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
            UpdateSelection(mouseTile);
        }

        // Expire cancelled selection overlay once its duration has elapsed
        if (CancelledSelection != null && CancelledSelection.IsExpired)
        {
            CancelledSelection = null;
        }

        // Clear cancellation flag when left mouse is released after a cancel.
        // This ensures the player can start a new selection on the next fresh click.
        if (_justCancelled && _lastMouseLeft && !Main.mouseLeft)
        {
            _justCancelled = false;
        }

        _lastMouseLeft = Main.mouseLeft;
    }

    private bool IsHoldingWandItem()
    {
        return Player.HeldItem?.ModItem is WandOfDestructionBase
            || Player.HeldItem?.ModItem is WandOfBuildingBase
            || Player.HeldItem?.ModItem is WandOfReplacementBase
            || Player.HeldItem?.ModItem is WandOfWiringBase
            // Add other wand types here as you implement them
            // || Player.HeldItem?.ModItem is WandOfDesigner
            ;
    }

    public override void OnRespawn() => ClearSelection();
    
    public override void OnEnterWorld()
    {
        ClearSelection();
        Settings.ResetToDefaults();
        BuildingSettings.ResetToDefaults();
        DestructionSettings.ResetToDefaults();
        ReplacementSettings.ResetToDefaults();
        WiringSettings.ResetToDefaults();
    }
}