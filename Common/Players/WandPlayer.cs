using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
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
    // ── Dual-selection storage ──────────────────────────────────────
    // Large shapes (Rectangle, Ellipse, Diamond, Triangle, HalfEllipse) and
    // small shapes (Elbow, CardinalLine) each get their own selection slot.
    // This prevents cross-shape-category state leaks (e.g., a 1000x1 line
    // selection becoming a 1000x1000 ellipse when switching wands) while
    // preserving selections across item switches (potions, weapons, doors).
    private SelectionState _largeSelection = SelectionState.Empty;
    private SelectionState _smallSelection = SelectionState.Empty;
    private int _largeSelectionOwner;
    private int _smallSelectionOwner;

    // ── Isolated instant selection ──────────────────────────────────
    // Instant wands operate on a completely separate selection state
    // that never interferes with the dual-slot system used by
    // Select/Confirm/Stamp wands. The instant selection is ephemeral —
    // it lives only for the duration of a single click-drag and is
    // cleared on mouse release, cancel, or item swap. This prevents
    // instant wand drag operations from overwriting stored selections
    // in the large/small slots.
    private SelectionState _instantSelection = SelectionState.Empty;
    private int _instantSelectionOwner;

    /// <summary>
    /// Returns true if the given shape type belongs to the "small" category
    /// (1D shapes: lines and elbows). All other shapes are "large" (2D area shapes).
    /// </summary>
    public static bool IsSmallShape(ShapeType shape)
        => shape == ShapeType.Elbow || shape == ShapeType.CardinalLine || shape == ShapeType.StraightLine;

    /// <summary>
    /// Gets or sets the active selection for the current wand's shape category.
    /// </summary>
    public SelectionState Selection
    {
        get
        {
            ShapeType current = GetCurrentShapeType();
            return IsSmallShape(current) ? _smallSelection : _largeSelection;
        }
        private set
        {
            ShapeType current = GetCurrentShapeType();
            if (IsSmallShape(current))
                _smallSelection = value;
            else
                _largeSelection = value;
        }
    }

    /// <summary>
    /// Tracks the Type (item ID) of the wand that started the current selection.
    /// Used to prevent cross-wand selection execution (e.g., switching from Confirm to Instant
    /// with an active selection should NOT execute that selection).
    /// </summary>
    public int SelectionOwnerItemType
    {
        get
        {
            ShapeType current = GetCurrentShapeType();
            return IsSmallShape(current) ? _smallSelectionOwner : _largeSelectionOwner;
        }
        private set
        {
            ShapeType current = GetCurrentShapeType();
            if (IsSmallShape(current))
                _smallSelectionOwner = value;
            else
                _largeSelectionOwner = value;
        }
    }

    // === Stamp mode state ===
    /// <summary>Whether the stamp template has been locked (3rd click done).</summary>
    public bool IsStampLocked { get; private set; }
    /// <summary>The stamp template delta (EndTile - StartTile) at lock time.</summary>
    public Point StampDelta { get; private set; }
    /// <summary>The anchor offset within the stamp rectangle that the mouse was on at lock time.</summary>
    public Point StampAnchorOffset { get; private set; }

    /// <summary>
    /// Tracks how many selection clicks have been completed in the current selection.
    /// 0 = no selection, 1 = start set, 2 = end locked, 3 = stamp locked.
    /// Used for cross-wand click-step compatibility: a wand with N click steps
    /// can use an existing selection only if SelectionClickStep &lt; N.
    /// </summary>
    public int SelectionClickStep { get; private set; }

    private bool _lastMouseLeft;
    private ulong _lastConsumedLeftClickTick;
    private bool _justCancelled; // Prevents immediate restart after cancellation
    private int _lastHeldItemType; // Tracks held item for cross-wand detection
    
    /// <summary>
    /// Stores the visual state of the last cancelled selection for overlay feedback.
    /// Null when no cancellation is pending display.
    /// </summary>
    public CancelledSelectionState CancelledSelection { get; private set; }

    // Per-wand settings
    public WandOfBuildingSettings BuildingSettings { get; private set; } = new();
    public WandOfDismantlingSettings DismantlingSettings { get; private set; } = new();
    public WandOfReplacementSettings ReplacementSettings { get; private set; } = new();
    public WandOfWiringSettings WiringSettings { get; private set; } = new();
    public WandOfSafekeepingSettings SafekeepingSettings { get; private set; } = new();
    public WandOfCoatingSettings CoatingSettings { get; private set; } = new();

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
        SelectionClickStep = 1;
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
    /// Uses SmallSelectionCap for Elbow/CardinalLine, HollowSelectionCap for hollow big shapes,
    /// and BigSelectionCap for filled big shapes.
    /// </summary>
    private Point ClampEndToCaps(Point start, Point end)
    {
        var config = ModContent.GetInstance<WandServerConfig>();
        if (config == null) return end;

        ShapeType currentShape = GetCurrentShapeType();
        int cap;
        if (currentShape == ShapeType.Elbow || currentShape == ShapeType.CardinalLine || currentShape == ShapeType.StraightLine)
            cap = config.SmallSelectionCap;
        else if (GetCurrentFillMode() == ShapeMode.Hollow)
            cap = config.HollowSelectionCap;
        else
            cap = config.BigSelectionCap;

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
        if (Player.HeldItem?.ModItem is WandOfDismantlingBase)
            return DismantlingSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfReplacementBase)
            return ReplacementSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfWiringBase)
            return WiringSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfSafekeepingBase)
            return SafekeepingSettings.Shape.Shape;
        if (Player.HeldItem?.ModItem is WandOfCoatingBase)
            return CoatingSettings.Shape.Shape;
        return Settings.ShapeType;
    }

    /// <summary>
    /// Determines which ShapeMode (Filled/Hollow) is currently active based on the held wand.
    /// </summary>
    private ShapeMode GetCurrentFillMode()
    {
        if (Player.HeldItem?.ModItem is WandOfBuildingBase)
            return BuildingSettings.Shape.FillMode;
        if (Player.HeldItem?.ModItem is WandOfDismantlingBase)
            return DismantlingSettings.Shape.FillMode;
        if (Player.HeldItem?.ModItem is WandOfReplacementBase)
            return ReplacementSettings.Shape.FillMode;
        if (Player.HeldItem?.ModItem is WandOfWiringBase)
            return WiringSettings.Shape.FillMode;
        if (Player.HeldItem?.ModItem is WandOfSafekeepingBase)
            return SafekeepingSettings.Shape.FillMode;
        if (Player.HeldItem?.ModItem is WandOfCoatingBase)
            return CoatingSettings.Shape.FillMode;
        return Settings.ShapeMode;
    }

    // NEW: Lock the selection to prevent endpoint updates
    public void LockSelection()
    {
        if (Selection.IsActive)
        {
            Selection = Selection.WithLocked(true);
            SelectionClickStep = 2;
        }
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
        _largeSelection = SelectionState.Empty;
        _smallSelection = SelectionState.Empty;
        _largeSelectionOwner = 0;
        _smallSelectionOwner = 0;
        _instantSelection = SelectionState.Empty;
        _instantSelectionOwner = 0;
        IsStampLocked = false;
        StampDelta = Point.Zero;
        StampAnchorOffset = Point.Zero;
        SelectionClickStep = 0;
    }

    /// <summary>
    /// Clears only the active shape-category slot (large or small), preserving the other.
    /// Used by non-instant wands that need to clear just their category.
    /// Stamp state (IsStampLocked, StampDelta, StampAnchorOffset, SelectionClickStep) is
    /// intentionally NOT cleared — it belongs to whichever stamp wand created it and
    /// may live in the opposite slot. Stamp wands manage their own lifecycle via
    /// LockStamp/UnlockStamp/CancelSelection.
    /// </summary>
    public void ClearActiveSelection()
    {
        ShapeType current = GetCurrentShapeType();
        if (IsSmallShape(current))
        {
            _smallSelection = SelectionState.Empty;
            _smallSelectionOwner = 0;
        }
        else
        {
            _largeSelection = SelectionState.Empty;
            _largeSelectionOwner = 0;
        }
    }

    // ── Instant selection methods ───────────────────────────────────
    // These operate on _instantSelection, completely bypassing the
    // dual-slot (large/small) system. Instant wands call these instead
    // of StartSelection/UpdateSelection/ClearActiveSelection.

    /// <summary>
    /// The isolated instant selection state. Read-only from outside WandPlayer.
    /// Only set by StartInstantSelection/UpdateInstantSelection/ClearInstantSelection.
    /// </summary>
    public SelectionState InstantSelection => _instantSelection;

    /// <summary>
    /// Returns true if the instant selection was started by the currently held item.
    /// Used by instant wands to avoid executing a selection started by a different wand.
    /// </summary>
    public bool IsInstantSelectionOwnedByCurrentItem()
    {
        if (!_instantSelection.IsActive) return false;
        int currentType = Player.HeldItem?.type ?? 0;
        return currentType != 0 && currentType == _instantSelectionOwner;
    }

    /// <summary>
    /// Begins a new instant selection at the given starting tile.
    /// Does NOT touch the dual-slot system (large/small) at all.
    /// </summary>
    public void StartInstantSelection(Point start, bool verticalFirst)
    {
        _instantSelection = SelectionState.Create(start, start, verticalFirst);
        _instantSelectionOwner = Player.HeldItem?.type ?? 0;
    }

    /// <summary>
    /// Updates the endpoint of the active instant selection, applying dimension capping.
    /// Does NOT touch the dual-slot system (large/small) at all.
    /// </summary>
    public void UpdateInstantSelection(Point end)
    {
        if (_instantSelection.IsActive)
        {
            var capped = ClampEndToCaps(_instantSelection.StartTile, end);
            bool wasClamped = capped != end;
            _instantSelection = _instantSelection.WithEnd(capped, wasClamped);
        }
    }

    /// <summary>
    /// Clears the instant selection. Called on mouse release or item swap.
    /// Does NOT touch the dual-slot system, stamp state, or anything else.
    /// Does NOT show a cancel animation — use <see cref="CancelInstantSelection"/> for that.
    /// </summary>
    public void ClearInstantSelection()
    {
        _instantSelection = SelectionState.Empty;
        _instantSelectionOwner = 0;
    }

    /// <summary>
    /// Cancels the instant selection with the same visual feedback (colour-changed overlay)
    /// that <see cref="CancelSelection(Color, ShapeInfo)"/> produces for two-click wands.
    /// </summary>
    public void CancelInstantSelection(Color cancelColor, ShapeInfo shape)
    {
        if (_instantSelection.IsActive)
        {
            CancelledSelection = new CancelledSelectionState(
                _instantSelection.StartTile,
                _instantSelection.EndTile,
                _instantSelection.VerticalFirst,
                shape,
                cancelColor,
                WandColors.CancelOverlayDurationTicks);
        }

        _instantSelection = SelectionState.Empty;
        _instantSelectionOwner = 0;
        _justCancelled = true;
    }

    public void CancelSelection()
    {
        CancelSelection(WandColors.CancelBuilding, ShapeInfo.Default);
    }

    /// <summary>
    /// Cancels the current selection with visual feedback.
    /// Snapshots the selection for a brief colour-changed overlay.
    /// Only clears the active shape-category slot (large or small).
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
        SelectionClickStep = 0;
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
        SelectionClickStep = 3;

        // Lock the selection so PostUpdate doesn't move the endpoint
        LockSelection();
        SelectionClickStep = 3; // Restore to 3 (LockSelection sets it to 2)
    }

    /// <summary>
    /// Unlocks a locked stamp, reverting to the "click to lock anchor" state.
    /// The selection remains locked (showing the end point), but the stamp
    /// template is cleared so the user can reposition the anchor.
    /// </summary>
    public void UnlockStamp()
    {
        IsStampLocked = false;
        StampDelta = Point.Zero;
        StampAnchorOffset = Point.Zero;
        // Keep selection locked — user still has their endpoint set.
        // They just need to re-click to set the anchor position.
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

    /// <summary>
    /// Returns true if the active selection is compatible with the given wand mode.
    /// A selection with N completed click steps is compatible with a wand that has
    /// more than N total click steps (i.e., the wand has remaining steps to handle it).
    /// Instant (OneClick=0) wands always start fresh — they're never compatible with
    /// existing multi-click selections.
    /// </summary>
    public bool IsSelectionCompatible(SelectionMode wandMode)
    {
        if (!Selection.IsActive) return true; // No selection = always compatible (will start new)
        int wandClickCount = (int)wandMode + 1; // OneClick=1, TwoClick=2, ThreeClick=3, FourClick=4
        return SelectionClickStep < wandClickCount;
    }

    /// <summary>
    /// Returns true if there is an active selection that should be visually displayed.
    /// For non-instant wands: checks dual-slot compatibility (suppresses incompatible inherited selections).
    /// For instant wands: checks the isolated instant selection (never shows dual-slot data).
    /// </summary>
    public bool IsSelectionVisuallyActive()
    {
        if (!IsHoldingWandItem()) return false;

        var wandItem = Player.HeldItem?.ModItem as BaseCyclingWand;
        if (wandItem == null) return false;

        // Instant wands use the isolated instant selection — never dual-slot
        if (wandItem.WandSelectionMode == SelectionMode.OneClick)
            return _instantSelection.IsActive && IsInstantSelectionOwnedByCurrentItem();

        // Non-instant wands use the dual-slot system
        if (!Selection.IsActive) return false;
        return IsSelectionCompatible(wandItem.WandSelectionMode);
    }

    /// <summary>
    /// Returns the selection state that the overlay should render.
    /// For instant wands: returns the isolated instant selection.
    /// For non-instant wands: returns the dual-slot selection.
    /// </summary>
    public SelectionState GetVisualSelection()
    {
        var wandItem = Player.HeldItem?.ModItem as BaseCyclingWand;
        if (wandItem != null && wandItem.WandSelectionMode == SelectionMode.OneClick)
            return _instantSelection;
        return Selection;
    }

    /// <summary>
    /// Clears the active selection if it's incompatible with the given wand mode.
    /// Called from UseItem when a non-OneClick wand actually clicks — at that point
    /// we know the player intends to use THIS wand, so the old selection is discarded.
    /// </summary>
    public void EnsureSelectionCompatibility(SelectionMode wandMode)
    {
        if (Selection.IsActive && !IsSelectionCompatible(wandMode))
        {
            // Silently clear — don't show cancel animation, just reset
            Selection = SelectionState.Empty;
            SelectionOwnerItemType = 0;
            SelectionClickStep = 0;
            IsStampLocked = false;
            StampDelta = Point.Zero;
            StampAnchorOffset = Point.Zero;
        }
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
        // The dual-selection system (large vs small shape slots) inherently prevents
        // cross-shape-category state leaks. A 1000×1 CardinalLine selection is stored
        // in the "small" slot and won't appear when viewing a Rectangle/Ellipse (large slot).
        // No need to clear selections on item switches — the player can use potions,
        // fight enemies, open doors, and switch back to their wand without losing work.

        // Clear instant selection on item swap — instant selections are ephemeral
        // and should not survive switching to a different item.
        int currentItemType = Player.HeldItem?.type ?? 0;
        if (currentItemType != _lastHeldItemType && _instantSelection.IsActive)
        {
            ClearInstantSelection();
        }
        _lastHeldItemType = currentItemType;

        // Selection compatibility is handled VISUALLY (IsSelectionVisuallyActive)
        // and DESTRUCTIVELY only on click (EnsureSelectionCompatibility in UseItem).
        // PostUpdate no longer clears incompatible selections — switching wands
        // preserves the selection so the player can switch back without losing work.

        // Update dual-slot selection preview in real-time when selection is active,
        // visually compatible, AND not locked. Incompatible selections are preserved
        // but not updated. Instant selections are updated by the wand's HoldItem.
        if (Selection.IsActive && !Selection.IsLocked && IsHoldingWandItem() && IsSelectionVisuallyActive())
        {
            var wandItem = Player.HeldItem?.ModItem as BaseCyclingWand;
            // Only update dual-slot for non-instant wands (instant wands manage their own)
            if (wandItem != null && wandItem.WandSelectionMode != SelectionMode.OneClick)
            {
                Point mouseTile = GeometryHelper.GetMouseTile();
                UpdateSelection(mouseTile);
            }
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
        return Player.HeldItem?.ModItem is WandOfDismantlingBase
            || Player.HeldItem?.ModItem is WandOfBuildingBase
            || Player.HeldItem?.ModItem is WandOfReplacementBase
            || Player.HeldItem?.ModItem is WandOfWiringBase
            || Player.HeldItem?.ModItem is WandOfSafekeepingBase
            || Player.HeldItem?.ModItem is WandOfCoatingBase
            ;
    }

    public override void OnRespawn() => ClearSelection();
    
    public override void OnEnterWorld()
    {
        ClearSelection();
        Settings.ResetToDefaults();
        BuildingSettings.ResetToDefaults();
        DismantlingSettings.ResetToDefaults();
        ReplacementSettings.ResetToDefaults();
        WiringSettings.ResetToDefaults();
        SafekeepingSettings.ResetToDefaults();
        CoatingSettings.ResetToDefaults();
    }
}