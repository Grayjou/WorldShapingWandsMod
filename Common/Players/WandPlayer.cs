using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

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

    // ── W-S4-1 (S4 2026-04-24, Cavendish DesignDoc_StampSmoothingV3.md §3.1) ──
    // Stamp Smoothing v3 state. World-space exponential ease toward the
    // precise stamp anchor. Replaces the v1/v2 sub-pixel-offset model entirely
    // (those eased in screen space against MouseScreen and produced Δ up to
    // 700 px per the W-S3-1 dual-draw verdict).
    //
    // Updated once per logic tick from BaseCyclingWand.TemplateStampHoldItem
    // (which itself runs at HoldItem/Update rate). Read every Draw frame from
    // SelectionOverlay.DrawSelection; no draw-rate accumulation here means
    // FPS-rate independence by construction.
    /// <summary>Smoothed (eased) anchor position in world pixels. v3 model:
    /// updated once per logic tick toward the precise anchor; consumed at draw
    /// time as <c>screenPos = SmoothAnchorWorld - Main.screenPosition</c>.</summary>
    public Vector2 SmoothAnchorWorld { get; private set; }
    /// <summary>False after lock/unlock/clear/cancel. The first
    /// <see cref="UpdateSmoothAnchor"/> call after that snaps the anchor to
    /// the target (no easing on first appearance — §3.5 of the v3 design doc).</summary>
    public bool SmoothAnchorInitialised { get; private set; }

    // === Stamp Channeling state (client-local, not synced in MP) ===
    /// <summary>Whether the player is currently holding left-click in stamp-locked state to channel.</summary>
    public bool IsStampChanneling { get; private set; }
    /// <summary>Frames the player has been holding left-click since channeling began. Resets on release.</summary>
    public int StampChannelTimer { get; private set; }
    /// <summary>Frames since last channeling execution. Used for repeat interval timing.</summary>
    public int StampRepeatTimer { get; private set; }
    /// <summary>Whether the channel threshold has been reached (first execution done).</summary>
    public bool StampChannelCharged { get; private set; }
    /// <summary>Counter for throttling channeling sounds. Increments each repeat execution.</summary>
    public int StampChannelSoundCounter { get; private set; }

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
    public WandOfFluidsSettings FluidsSettings { get; private set; } = new();
    public WandTorchSettings TorchSettings { get; private set; } = new();

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
        var config = WandConfigs.Limits;
        if (config == null) return end;

        ShapeType currentShape = GetCurrentShapeType();
        int cap;

        // Coating wand uses its own generous cap — no batching, no lag, instant application
        if (Player.HeldItem?.ModItem is WandOfCoatingBase)
        {
            cap = config.CoatingSelectionCap;
        }
        // Molding and Delimitation are cheap, instant canvas/filter operations
        // — no per-tile mutation. They use generous caps too (separate from Coating
        // so users can tune each independently).
        else if (Player.HeldItem?.ModItem is WandOfMoldingBase)
        {
            cap = config.MoldingSelectionCap;
        }
        else if (Player.HeldItem?.ModItem is WandOfDelimitationBase)
        {
            cap = config.DelimitationSelectionCap;
        }
        else if (currentShape == ShapeType.Elbow || currentShape == ShapeType.CardinalLine || currentShape == ShapeType.StraightLine)
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
        if (Player.HeldItem?.ModItem is WandOfMoldingBase)
        {
            var mwp = Player.GetModPlayer<MoldingWandPlayer>();
            return mwp.Settings.Shape.Shape;
        }
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
        if (Player.HeldItem?.ModItem is WandOfMoldingBase)
        {
            var mwp = Player.GetModPlayer<MoldingWandPlayer>();
            return mwp.Settings.Shape.FillMode;
        }
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
        SmoothAnchorInitialised = false; // W-S4-1: v3 smoothing reset on clear
        SelectionClickStep = 0;
        ResetStampChanneling();
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
        SmoothAnchorInitialised = false; // W-S4-1: v3 smoothing reset on cancel
        ResetStampChanneling();
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
        SmoothAnchorInitialised = false; // W-S4-1: v3 smoothing reset on unlock
        // Keep selection locked — user still has their endpoint set.
        // They just need to re-click to set the anchor position.
    }

    // ── W-S4-1 (S4 2026-04-24, Cavendish DesignDoc_StampSmoothingV3.md §3.2) ──
    /// <summary>
    /// Stamp Smoothing v3: world-space exponential ease toward the precise stamp
    /// anchor. Called once per logic tick from
    /// <c>BaseCyclingWand.TemplateStampHoldItem</c> with the current
    /// <c>(bboxMinTile + StampAnchorOffset) * 16</c> world-pixel position.
    /// <para>Self-gates against pause (§3.4) and snaps on first appearance after
    /// any reset (§3.5). A teleport guard (§3.6) snaps when the gap exceeds
    /// <c>SmoothTeleportThreshold</c> world pixels so cross-screen jumps don't
    /// drag a long lerp tail behind the cursor.</para>
    /// <para>Read at draw time via <see cref="SmoothAnchorWorld"/> /
    /// <see cref="SmoothAnchorInitialised"/>; the consumer never accumulates
    /// state of its own, so FPS-rate independence holds by construction.</para>
    /// </summary>
    /// <param name="targetAnchorWorld">Precise anchor position in world pixels
    /// (= <c>(bboxMinTile + StampAnchorOffset) * 16</c>).</param>
    public void UpdateSmoothAnchor(Vector2 targetAnchorWorld)
    {
        // §3.4 Pause gate. A frozen world should not have a moving overlay;
        // the smoothed anchor must sit still while paused (closes S3 R-1).
        if (Main.gamePaused) return;

        // §3.5 First-frame init: snap to target so the stamp appears exactly
        // where the cursor is, without a visible easing-in animation.
        if (!SmoothAnchorInitialised)
        {
            SmoothAnchorWorld = targetAnchorWorld;
            SmoothAnchorInitialised = true;
            return;
        }

        // §3.6 Teleport guard. 512 world-px = 32 tiles ≈ half a screen-width.
        // If the precise anchor jumps more than this in a single tick (camera
        // teleport, magic-mirror, recall, etc.) snap rather than ease.
        const float teleportThresholdSq = 512f * 512f;
        if (Vector2.DistanceSquared(SmoothAnchorWorld, targetAnchorWorld) > teleportThresholdSq)
        {
            SmoothAnchorWorld = targetAnchorWorld;
            return;
        }

        // §3.2 Exponential ease. easeT chosen for ~4-frame visible settle at
        // 60 Hz: aggressive enough that the smoothing isn't perceived as lag,
        // gentle enough to absorb single-frame coordinate spikes.
        const float easeT = 0.25f;
        SmoothAnchorWorld = Vector2.Lerp(SmoothAnchorWorld, targetAnchorWorld, easeT);
    }

    // ── Stamp Channeling methods ────────────────────────────────────

    /// <summary>
    /// Begins stamp channeling. Called from HandleUseItem on the 4th+ click
    /// when channelFrames &gt; 0 (hold-to-channel mode).
    /// </summary>
    public void BeginStampChanneling()
    {
        IsStampChanneling = true;
        StampChannelTimer = 0;
        StampRepeatTimer = 0;
        StampChannelCharged = false;
        StampChannelSoundCounter = 0;
    }

    /// <summary>
    /// Resets all channeling state. Called when the player releases left-click,
    /// cancels the selection, or switches items.
    /// </summary>
    public void ResetStampChanneling()
    {
        IsStampChanneling = false;
        StampChannelTimer = 0;
        StampRepeatTimer = 0;
        StampChannelCharged = false;
        StampChannelSoundCounter = 0;
    }

    /// <summary>Increments the channel charge timer by one frame.</summary>
    public void IncrementChannelTimer() => StampChannelTimer++;

    /// <summary>Marks the channel as fully charged (first execution threshold reached).</summary>
    public void SetStampChannelCharged()
    {
        StampChannelCharged = true;
        StampRepeatTimer = 0;
        StampChannelSoundCounter = 0;
    }

    /// <summary>Increments the repeat interval timer by one frame.</summary>
    public void IncrementRepeatTimer() => StampRepeatTimer++;

    /// <summary>Resets the repeat timer to zero (after a repeat execution fires).</summary>
    public void ResetRepeatTimer() => StampRepeatTimer = 0;

    /// <summary>Increments the sound throttle counter (called on each repeat execution).</summary>
    public void IncrementChannelSoundCounter() => StampChannelSoundCounter++;

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
            ResetStampChanneling();
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
            || Player.HeldItem?.ModItem is WandOfFluidsBase
            || Player.HeldItem?.ModItem is WandOfTorchesBase
            || Player.HeldItem?.ModItem is WandOfDelimitationBase
            || Player.HeldItem?.ModItem is WandOfMoldingBase
            ;
    }

    public override void OnRespawn() => ClearSelection();

    // ──────────────────────────────────────────────────────────────────────
    //  Persistence — InventoryView choice fields (per-character, per Cavendish
    //  Response_2026-04-22 Letter #5 §8). Choices serialize as (ModName, ItemName)
    //  tuples via ChoiceSerialization so cross-mod-version item-ID renumbering
    //  doesn't silently re-choice players to the wrong items. Unresolvable choices
    //  on Load silently fall back to null (legacy behaviour).
    //
    //  We deliberately persist ONLY the choices right now, NOT the full settings
    //  surface (Object/Shape/SelectionMode/etc. already round-trip via the
    //  per-instance UI state and per-session ResetToDefaults() in OnEnterWorld).
    //  Expanding the persistence scope is a separate decision; choices are the
    //  one setting whose semantic value is "I committed to this choice"
    //  (Response #5 §8 ¶3).
    // ──────────────────────────────────────────────────────────────────────

    private const string TagChoiceBuildingTile = "ChoiceBuildingTile"; // Legacy key for backward-compat load (mapped to Solid)
    private const string TagChoiceBuildingTileKeys = "ChoiceBuildingTileKeys";   // S1 2026-04-26 per-PlaceType dict
    private const string TagChoiceBuildingTileVals = "ChoiceBuildingTileVals";   // parallel tag-compound list
    private const string TagChoiceBuildingWall = "ChoiceBuildingWall";
    private const string TagChoiceTorch = "ChoiceTorch";
    private const string TagChoiceReplacementSource = "ChoiceReplacementSource"; // Legacy key (single-slot, pre-S1-2026-04-26)
    private const string TagChoiceReplacementTarget = "ChoiceReplacementTarget"; // Legacy key (single-slot, pre-S1-2026-04-26)
    private const string TagChoiceReplacementSourceKeys = "ChoiceReplacementSourceKeys"; // S1 2026-04-26 per-ObjectType dict
    private const string TagChoiceReplacementSourceVals = "ChoiceReplacementSourceVals";
    private const string TagChoiceReplacementTargetKeys = "ChoiceReplacementTargetKeys"; // S1 2026-04-26 per-ObjectType dict
    private const string TagChoiceReplacementTargetVals = "ChoiceReplacementTargetVals";

    // PersistentPin tags (S15 2026-04-28). Each pin set is serialized as a
    // parallel (key-list, vals-list) pair where each "val" is a List<TagCompound>
    // of ChoiceSerialization payloads (mod-name + item-name stable across reload).
    // Flat sets (Wall, Torch) write a single List<TagCompound>.
    private const string TagPinBuildingTileKeys = "PinBuildingTileKeys";
    private const string TagPinBuildingTileVals = "PinBuildingTileVals"; // List<List<TagCompound>>
    private const string TagPinBuildingWall    = "PinBuildingWall";    // List<TagCompound>
    private const string TagPinTorch           = "PinTorch";           // List<TagCompound>
    private const string TagPinReplacementSourceKeys = "PinReplacementSourceKeys";
    private const string TagPinReplacementSourceVals = "PinReplacementSourceVals";
    private const string TagPinReplacementTargetKeys = "PinReplacementTargetKeys";
    private const string TagPinReplacementTargetVals = "PinReplacementTargetVals";

    // ── Collapsible-section persistence (2026-04-23 S4 framework) ───
    // Cavendish DesignDoc_CollapsablePanelSystem.md §2.4 named a SettingsPlayer
    // class for these dicts; no such class exists in WSW. Adapted onto WandPlayer
    // — the existing SaveData/LoadData round-trip is the natural home. Keys are
    // stable strings in the form "PanelName.SectionName" (e.g. "CoatingPanel.PaintColor").
    // Empty dicts are treated as "all sections expanded / no popout positions saved".
    private const string TagCollapsedSections = "CollapsedSections";
    private const string TagPopoutPositions   = "PopoutPositions";

    /// <summary>Per-section collapsed bit, keyed by <c>CollapsibleSection.PreferenceKey</c>.</summary>
    public Dictionary<string, bool> CollapsedSections { get; set; } = new();

    /// <summary>Per-section popout-host position (screen-pixel top-left), keyed identically.</summary>
    public Dictionary<string, Vector2> PopoutPositions { get; set; } = new();

    // ── Magic Wand state (S10 2026-04-29; StencilMagicWandSelectionPlan.md §6.0 + §7) ──
    /// <summary>
    /// Per-player Magic Wand (Read) configuration — one shared config
    /// across every stencil wand the player owns. Persists with the
    /// player save through <see cref="SaveData"/> /
    /// <see cref="LoadData"/> as a 2-byte payload
    /// (<c>"MagicWand_Object"</c> + <c>"MagicWand_Cont"</c>).
    /// Defaults to (SameTile, FourNeighbour) — the canonical, safest,
    /// most-recognisable Magic-Wand behaviour.
    /// </summary>
    public MagicWandReadConfig MagicWandReadConfig { get; set; }
        = MagicWandReadConfig.Default;

    /// <summary>
    /// The most recent Magic Wand (Read) capture, replayed by
    /// <c>MagicWandApplyShape</c> on any wand. <c>null</c> means
    /// *“nothing has been Read yet this session”* — Apply on null
    /// shows the chat warning *“Magic Wand: no captured shape. Use
    /// Magic Wand Read on a stencil wand first.”* and is a no-op.
    /// In-memory only per <c>MultipleStencilsPlan.md</c> §8 / Cavendish
    /// C-S1 §C2 (only configs persist; canvases and captures don't);
    /// cleared on world-exit / disconnect via <c>OnEnterWorld</c>'s
    /// reset-to-null below.
    /// </summary>
    public StoredMagicWandShape LastMagicWandShape { get; set; }

    public override void SaveData(TagCompound tag)
    {
        // Per-PlaceType tile choices (S1 2026-04-26 — each sub-mode is independent).
        // Serialized as a parallel (byte-key, TagCompound-value) list so each slot
        // round-trips via ChoiceSerialization's stable (ModName, ItemName) tuple.
        var tileChoiceDict = BuildingSettings.ChosenTileItemTypeByObjectType;
        if (tileChoiceDict != null && tileChoiceDict.Count > 0)
        {
            var tileKeys = new List<byte>();
            var tileVals = new List<TagCompound>();
            foreach (var kv in tileChoiceDict)
            {
                var payload = ChoiceSerialization.SaveChoice(kv.Value);
                if (payload != null)
                {
                    tileKeys.Add((byte)kv.Key);
                    tileVals.Add(payload);
                }
            }
            if (tileKeys.Count > 0)
            {
                tag[TagChoiceBuildingTileKeys] = tileKeys;
                tag[TagChoiceBuildingTileVals] = tileVals;
            }
        }
        TrySetTag(tag, TagChoiceBuildingWall, BuildingSettings.ChosenWallItemType);
        TrySetTag(tag, TagChoiceTorch, TorchSettings.ChosenTorchItemType);
        // Per-ObjectType replacement source/target choices (S1 2026-04-26).
        var replaceSrcDict = ReplacementSettings.ChosenSourceItemTypeByObjectType;
        if (replaceSrcDict != null && replaceSrcDict.Count > 0)
        {
            var srcKeys = new List<byte>(); var srcVals = new List<TagCompound>();
            foreach (var kv in replaceSrcDict) { var p = ChoiceSerialization.SaveChoice(kv.Value); if (p != null) { srcKeys.Add((byte)kv.Key); srcVals.Add(p); } }
            if (srcKeys.Count > 0) { tag[TagChoiceReplacementSourceKeys] = srcKeys; tag[TagChoiceReplacementSourceVals] = srcVals; }
        }
        var replaceTgtDict = ReplacementSettings.ChosenTargetItemTypeByObjectType;
        if (replaceTgtDict != null && replaceTgtDict.Count > 0)
        {
            var tgtKeys = new List<byte>(); var tgtVals = new List<TagCompound>();
            foreach (var kv in replaceTgtDict) { var p = ChoiceSerialization.SaveChoice(kv.Value); if (p != null) { tgtKeys.Add((byte)kv.Key); tgtVals.Add(p); } }
            if (tgtKeys.Count > 0) { tag[TagChoiceReplacementTargetKeys] = tgtKeys; tag[TagChoiceReplacementTargetVals] = tgtVals; }
        }

        // PersistentPin save (S15 2026-04-28). Per-axis sets serialized as
        // List<TagCompound> of ChoiceSerialization payloads. Empty sets emit
        // no tag (LoadData treats absent tags as empty).
        SavePinDictByPlaceType(tag, TagPinBuildingTileKeys, TagPinBuildingTileVals,
            BuildingSettings.PinnedTileItemTypesByObjectType);
        SavePinFlatSet(tag, TagPinBuildingWall, BuildingSettings.PinnedWallItemTypes);
        SavePinFlatSet(tag, TagPinTorch, TorchSettings.PinnedTorchItemTypes);
        SavePinDictByObjectType(tag, TagPinReplacementSourceKeys, TagPinReplacementSourceVals,
            ReplacementSettings.PinnedSourceItemTypesByObjectType);
        SavePinDictByObjectType(tag, TagPinReplacementTargetKeys, TagPinReplacementTargetVals,
            ReplacementSettings.PinnedTargetItemTypesByObjectType);

        // Collapsible-section state: serialized as parallel string-list payloads
        // (TagCompound's Dictionary support is lossy across versions; parallel
        // lists round-trip cleanly). Empty dict → no tag written.
        if (CollapsedSections != null && CollapsedSections.Count > 0)
        {
            var keys = new List<string>(CollapsedSections.Count);
            var vals = new List<bool>(CollapsedSections.Count);
            foreach (var kv in CollapsedSections) { keys.Add(kv.Key); vals.Add(kv.Value); }
            tag[TagCollapsedSections + "_K"] = keys;
            tag[TagCollapsedSections + "_V"] = vals;
        }
        if (PopoutPositions != null && PopoutPositions.Count > 0)
        {
            var keys = new List<string>(PopoutPositions.Count);
            var xs = new List<float>(PopoutPositions.Count);
            var ys = new List<float>(PopoutPositions.Count);
            foreach (var kv in PopoutPositions) { keys.Add(kv.Key); xs.Add(kv.Value.X); ys.Add(kv.Value.Y); }
            tag[TagPopoutPositions + "_K"] = keys;
            tag[TagPopoutPositions + "_X"] = xs;
            tag[TagPopoutPositions + "_Y"] = ys;
        }

        // (S10 2026-04-29) Magic Wand Read config — 2-byte tag pair.
        // The captured shape (LastMagicWandShape) is in-memory only and
        // intentionally NOT persisted per the plan §6.0 lifecycle.
        MagicWandReadConfig.Save(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        BuildingSettings.ChosenTileItemTypeByObjectType = new System.Collections.Generic.Dictionary<PlaceType, int?>();
        // S1 2026-04-26: new per-PlaceType format.
        if (tag.ContainsKey(TagChoiceBuildingTileKeys) && tag.ContainsKey(TagChoiceBuildingTileVals))
        {
            var tileKeys = tag.GetList<byte>(TagChoiceBuildingTileKeys);
            var tileVals = tag.GetList<TagCompound>(TagChoiceBuildingTileVals);
            int n = System.Math.Min(tileKeys.Count, tileVals.Count);
            for (int i = 0; i < n; i++)
            {
                int? loaded = ChoiceSerialization.LoadChoice(tileVals[i]);
                if (loaded.HasValue)
                    BuildingSettings.ChosenTileItemTypeByObjectType[(PlaceType)tileKeys[i]] = loaded;
            }
        }
        else if (tag.ContainsKey(TagChoiceBuildingTile))
        {
            // Legacy: old single-slot format mapped to Solid sub-mode.
            int? legacy = LoadChoiceTag(tag, TagChoiceBuildingTile);
            if (legacy.HasValue)
                BuildingSettings.ChosenTileItemTypeByObjectType[PlaceType.Solid] = legacy;
        }
        BuildingSettings.ChosenWallItemType = LoadChoiceTag(tag, TagChoiceBuildingWall);
        TorchSettings.ChosenTorchItemType = LoadChoiceTag(tag, TagChoiceTorch);
        ReplacementSettings.ChosenSourceItemTypeByObjectType = new System.Collections.Generic.Dictionary<ObjectType, int?>();
        if (tag.ContainsKey(TagChoiceReplacementSourceKeys) && tag.ContainsKey(TagChoiceReplacementSourceVals))
        {
            var srcKeys = tag.GetList<byte>(TagChoiceReplacementSourceKeys);
            var srcVals = tag.GetList<TagCompound>(TagChoiceReplacementSourceVals);
            int n = System.Math.Min(srcKeys.Count, srcVals.Count);
            for (int i = 0; i < n; i++) { int? loaded = ChoiceSerialization.LoadChoice(srcVals[i]); if (loaded.HasValue) ReplacementSettings.ChosenSourceItemTypeByObjectType[(ObjectType)srcKeys[i]] = loaded; }
        }
        else if (tag.ContainsKey(TagChoiceReplacementSource))
        {
            int? legacy = LoadChoiceTag(tag, TagChoiceReplacementSource);
            if (legacy.HasValue) ReplacementSettings.ChosenSourceItemTypeByObjectType[ObjectType.Tile] = legacy;
        }
        ReplacementSettings.ChosenTargetItemTypeByObjectType = new System.Collections.Generic.Dictionary<ObjectType, int?>();
        if (tag.ContainsKey(TagChoiceReplacementTargetKeys) && tag.ContainsKey(TagChoiceReplacementTargetVals))
        {
            var tgtKeys = tag.GetList<byte>(TagChoiceReplacementTargetKeys);
            var tgtVals = tag.GetList<TagCompound>(TagChoiceReplacementTargetVals);
            int n = System.Math.Min(tgtKeys.Count, tgtVals.Count);
            for (int i = 0; i < n; i++) { int? loaded = ChoiceSerialization.LoadChoice(tgtVals[i]); if (loaded.HasValue) ReplacementSettings.ChosenTargetItemTypeByObjectType[(ObjectType)tgtKeys[i]] = loaded; }
        }
        else if (tag.ContainsKey(TagChoiceReplacementTarget))
        {
            int? legacy = LoadChoiceTag(tag, TagChoiceReplacementTarget);
            if (legacy.HasValue) ReplacementSettings.ChosenTargetItemTypeByObjectType[ObjectType.Tile] = legacy;
        }

        // PersistentPin load (S15 2026-04-28).
        BuildingSettings.PinnedTileItemTypesByObjectType =
            LoadPinDictByPlaceType(tag, TagPinBuildingTileKeys, TagPinBuildingTileVals);
        BuildingSettings.PinnedWallItemTypes = LoadPinFlatSet(tag, TagPinBuildingWall);
        TorchSettings.PinnedTorchItemTypes = LoadPinFlatSet(tag, TagPinTorch);
        ReplacementSettings.PinnedSourceItemTypesByObjectType =
            LoadPinDictByObjectType(tag, TagPinReplacementSourceKeys, TagPinReplacementSourceVals);
        ReplacementSettings.PinnedTargetItemTypesByObjectType =
            LoadPinDictByObjectType(tag, TagPinReplacementTargetKeys, TagPinReplacementTargetVals);

        CollapsedSections = new Dictionary<string, bool>();
        if (tag.ContainsKey(TagCollapsedSections + "_K") && tag.ContainsKey(TagCollapsedSections + "_V"))
        {
            var keys = tag.GetList<string>(TagCollapsedSections + "_K");
            var vals = tag.GetList<bool>(TagCollapsedSections + "_V");
            int n = System.Math.Min(keys.Count, vals.Count);
            for (int i = 0; i < n; i++) CollapsedSections[keys[i]] = vals[i];
        }

        PopoutPositions = new Dictionary<string, Vector2>();
        if (tag.ContainsKey(TagPopoutPositions + "_K") && tag.ContainsKey(TagPopoutPositions + "_X") && tag.ContainsKey(TagPopoutPositions + "_Y"))
        {
            var keys = tag.GetList<string>(TagPopoutPositions + "_K");
            var xs = tag.GetList<float>(TagPopoutPositions + "_X");
            var ys = tag.GetList<float>(TagPopoutPositions + "_Y");
            int n = System.Math.Min(keys.Count, System.Math.Min(xs.Count, ys.Count));
            for (int i = 0; i < n; i++) PopoutPositions[keys[i]] = new Vector2(xs[i], ys[i]);
        }

        // (S10 2026-04-29) Magic Wand Read config — absent tags read as
        // (SameTile, FourNeighbour) defaults; out-of-range bytes also
        // fall back to defaults to keep loads non-fatal across enum drift.
        MagicWandReadConfig = global::WorldShapingWandsMod.Common.Settings.MagicWandReadConfig.Load(tag);
        // LastMagicWandShape is in-memory only — never loaded from save.
        LastMagicWandShape = null;
    }

    private static void TrySetTag(TagCompound tag, string key, int? itemType)
    {
        TagCompound payload = ChoiceSerialization.SaveChoice(itemType);
        if (payload != null)
            tag[key] = payload;
    }

    private static int? LoadChoiceTag(TagCompound tag, string key)
    {
        if (tag == null || !tag.ContainsKey(key))
            return null;
        return ChoiceSerialization.LoadChoice(tag.GetCompound(key));
    }

    // ── PersistentPin (S15 2026-04-28) serialization helpers ─────────────────────────
    // Pins are stale-reference: a pinned item type may be from an unloaded mod
    // when LoadData runs. ChoiceSerialization handles this gracefully by
    // returning null for unresolvable tuples; we silently drop those entries.

    private static void SavePinFlatSet(TagCompound tag, string key, System.Collections.Generic.HashSet<int> set)
    {
        if (set == null || set.Count == 0) return;
        var payloads = new List<TagCompound>(set.Count);
        foreach (int t in set)
        {
            var p = ChoiceSerialization.SaveChoice(t);
            if (p != null) payloads.Add(p);
        }
        if (payloads.Count > 0) tag[key] = payloads;
    }

    private static System.Collections.Generic.HashSet<int> LoadPinFlatSet(TagCompound tag, string key)
    {
        var set = new System.Collections.Generic.HashSet<int>();
        if (tag == null || !tag.ContainsKey(key)) return set;
        var payloads = tag.GetList<TagCompound>(key);
        foreach (var p in payloads)
        {
            int? loaded = ChoiceSerialization.LoadChoice(p);
            if (loaded.HasValue) set.Add(loaded.Value);
        }
        return set;
    }

    private static void SavePinDictByPlaceType(TagCompound tag, string keysKey, string valsKey,
        System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>> dict)
    {
        if (dict == null || dict.Count == 0) return;
        var keys = new List<byte>();
        var vals = new List<List<TagCompound>>();
        foreach (var kv in dict)
        {
            if (kv.Value == null || kv.Value.Count == 0) continue;
            var inner = new List<TagCompound>(kv.Value.Count);
            foreach (int t in kv.Value)
            {
                var p = ChoiceSerialization.SaveChoice(t);
                if (p != null) inner.Add(p);
            }
            if (inner.Count > 0)
            {
                keys.Add((byte)kv.Key);
                vals.Add(inner);
            }
        }
        if (keys.Count > 0)
        {
            tag[keysKey] = keys;
            // tModLoader's TagCompound serializer handles nested list-of-list-of-tag, but it's
            // safer (round-trips reliably across versions) to flatten each inner list into a
            // single TagCompound carrying its payloads under the indexed key "i".
            var wrapped = new List<TagCompound>(vals.Count);
            foreach (var inner in vals)
            {
                var w = new TagCompound { ["items"] = inner };
                wrapped.Add(w);
            }
            tag[valsKey] = wrapped;
        }
    }

    private static System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>>
        LoadPinDictByPlaceType(TagCompound tag, string keysKey, string valsKey)
    {
        var dict = new System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>>();
        if (tag == null || !tag.ContainsKey(keysKey) || !tag.ContainsKey(valsKey)) return dict;
        var keys = tag.GetList<byte>(keysKey);
        var wrapped = tag.GetList<TagCompound>(valsKey);
        int n = System.Math.Min(keys.Count, wrapped.Count);
        for (int i = 0; i < n; i++)
        {
            var inner = wrapped[i].GetList<TagCompound>("items");
            var set = new System.Collections.Generic.HashSet<int>();
            foreach (var p in inner)
            {
                int? loaded = ChoiceSerialization.LoadChoice(p);
                if (loaded.HasValue) set.Add(loaded.Value);
            }
            if (set.Count > 0) dict[(PlaceType)keys[i]] = set;
        }
        return dict;
    }

    private static void SavePinDictByObjectType(TagCompound tag, string keysKey, string valsKey,
        System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>> dict)
    {
        if (dict == null || dict.Count == 0) return;
        var keys = new List<byte>();
        var wrapped = new List<TagCompound>();
        foreach (var kv in dict)
        {
            if (kv.Value == null || kv.Value.Count == 0) continue;
            var inner = new List<TagCompound>(kv.Value.Count);
            foreach (int t in kv.Value)
            {
                var p = ChoiceSerialization.SaveChoice(t);
                if (p != null) inner.Add(p);
            }
            if (inner.Count > 0)
            {
                keys.Add((byte)kv.Key);
                wrapped.Add(new TagCompound { ["items"] = inner });
            }
        }
        if (keys.Count > 0)
        {
            tag[keysKey] = keys;
            tag[valsKey] = wrapped;
        }
    }

    private static System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>>
        LoadPinDictByObjectType(TagCompound tag, string keysKey, string valsKey)
    {
        var dict = new System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>>();
        if (tag == null || !tag.ContainsKey(keysKey) || !tag.ContainsKey(valsKey)) return dict;
        var keys = tag.GetList<byte>(keysKey);
        var wrapped = tag.GetList<TagCompound>(valsKey);
        int n = System.Math.Min(keys.Count, wrapped.Count);
        for (int i = 0; i < n; i++)
        {
            var inner = wrapped[i].GetList<TagCompound>("items");
            var set = new System.Collections.Generic.HashSet<int>();
            foreach (var p in inner)
            {
                int? loaded = ChoiceSerialization.LoadChoice(p);
                if (loaded.HasValue) set.Add(loaded.Value);
            }
            if (set.Count > 0) dict[(ObjectType)keys[i]] = set;
        }
        return dict;
    }

    // OnEnterWorld snapshot helpers (S15 PersistentPin) — deep-copy pin dicts so the
    // post-ResetToDefaults restore doesn't share refs with the now-cleared settings.
    private static System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>>
        ClonePinDictPlace(System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>> src)
    {
        var dst = new System.Collections.Generic.Dictionary<PlaceType, System.Collections.Generic.HashSet<int>>(src.Count);
        foreach (var kv in src) dst[kv.Key] = new System.Collections.Generic.HashSet<int>(kv.Value);
        return dst;
    }

    private static System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>>
        ClonePinDictObject(System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>> src)
    {
        var dst = new System.Collections.Generic.Dictionary<ObjectType, System.Collections.Generic.HashSet<int>>(src.Count);
        foreach (var kv in src) dst[kv.Key] = new System.Collections.Generic.HashSet<int>(kv.Value);
        return dst;
    }

    /// <summary>
    /// Reconciles the Wand of Replacement's per-side <see cref="ObjectType"/> field
    /// (<see cref="WandOfReplacementSettings.OldObject"/> when <paramref name="isSource"/>
    /// is true, <see cref="WandOfReplacementSettings.NewObject"/> otherwise) to match
    /// the actual nature of the restored choice item type. Used by <see cref="OnEnterWorld"/>
    /// to fix the post-relog visual mismatch where a wall choice would persist under a
    /// freshly-defaulted "Tile" object section. See block-comment in OnEnterWorld for
    /// full rationale (Letter #11 fix, 2026-04-23 S2).
    /// </summary>
    private void ReconcileReplacementObjectTypeToChoice(int? choiceItemType, bool isSource)
    {
        if (!choiceItemType.HasValue)
            return;
        Item probe = new();
        probe.SetDefaults(choiceItemType.Value);
        if (probe.IsAir)
            return;
        ObjectType inferred;
        if (probe.createWall > 0)
            inferred = ObjectType.Wall;
        #pragma warning disable ChangeMagicNumberToID
        else if (probe.createTile >= 0)
            // Tile category covers Platforms / Ropes / Rails / Seeds / PlanterBox /
            // regular Tiles. Rather than try to deduce the precise sub-category here
            // (the IV source predicate already filters by the broader OldObject
            // bucket), we only differentiate Wall vs non-Wall — the only mismatch
            // the user can reach via the IV today is "Tile-bucket section vs Wall
            // choice" because Source/Target sections are wired off the Tile/Wall
            // dimension. Sub-categories within Tile (Platform / Rope / etc.) are
            // a future concern if a choice is ever attachable across them.
            inferred = ObjectType.Tile;
        #pragma warning restore ChangeMagicNumberToID
        else
            return;

        if (isSource)
        {
            if (ReplacementSettings.OldObject != inferred)
                ReplacementSettings.OldObject = inferred;
        }
        else
        {
            if (ReplacementSettings.NewObject != inferred)
                ReplacementSettings.NewObject = inferred;
        }
    }

    public override void OnEnterWorld()
    {
        ClearSelection();

        // Snapshot choices BEFORE the per-world reset wipes them. ResetToDefaults
        // intentionally clears every setting (Object/Shape/SelectionMode/etc.)
        // because we want every other setting to start fresh per world entry,
        // but choices are explicitly per-character-persistent (S8 2026-04-22 per
        // Cavendish Response #5 §8) so we restore them after the reset.
        // (S1 2026-04-26) tile choices are now a per-PlaceType dictionary; copy it.
        var savedTileChoices = new System.Collections.Generic.Dictionary<PlaceType, int?>(
            BuildingSettings.ChosenTileItemTypeByObjectType);
        int? choiceWall = BuildingSettings.ChosenWallItemType;
        int? choiceTorch = TorchSettings.ChosenTorchItemType;
        var savedReplaceSrc = new System.Collections.Generic.Dictionary<ObjectType, int?>(ReplacementSettings.ChosenSourceItemTypeByObjectType);
        var savedReplaceTgt = new System.Collections.Generic.Dictionary<ObjectType, int?>(ReplacementSettings.ChosenTargetItemTypeByObjectType);
        // (S15 PersistentPin) Snapshot pin collections too — same per-character-persistent
        // contract as Chosen, must survive the per-world ResetToDefaults wipe.
        var savedPinTile = ClonePinDictPlace(BuildingSettings.PinnedTileItemTypesByObjectType);
        var savedPinWall = new System.Collections.Generic.HashSet<int>(BuildingSettings.PinnedWallItemTypes);
        var savedPinTorch = new System.Collections.Generic.HashSet<int>(TorchSettings.PinnedTorchItemTypes);
        var savedPinReplaceSrc = ClonePinDictObject(ReplacementSettings.PinnedSourceItemTypesByObjectType);
        var savedPinReplaceTgt = ClonePinDictObject(ReplacementSettings.PinnedTargetItemTypesByObjectType);

        Settings.ResetToDefaults();
        BuildingSettings.ResetToDefaults();
        DismantlingSettings.ResetToDefaults();
        ReplacementSettings.ResetToDefaults();
        WiringSettings.ResetToDefaults();
        SafekeepingSettings.ResetToDefaults();
        CoatingSettings.ResetToDefaults();

        // Restore choices after the reset.
        BuildingSettings.ChosenTileItemTypeByObjectType = savedTileChoices;
        BuildingSettings.ChosenWallItemType = choiceWall;
        TorchSettings.ChosenTorchItemType = choiceTorch;
        ReplacementSettings.ChosenSourceItemTypeByObjectType = savedReplaceSrc;
        ReplacementSettings.ChosenTargetItemTypeByObjectType = savedReplaceTgt;
        BuildingSettings.PinnedTileItemTypesByObjectType = savedPinTile;
        BuildingSettings.PinnedWallItemTypes = savedPinWall;
        TorchSettings.PinnedTorchItemTypes = savedPinTorch;
        ReplacementSettings.PinnedSourceItemTypesByObjectType = savedPinReplaceSrc;
        ReplacementSettings.PinnedTargetItemTypesByObjectType = savedPinReplaceTgt;

        // 2026-04-23 Session 2 (Letter #11 — WoR choice/object-type save-load mismatch).
        // Bug GrayJou reported: "I was using the Inventory View to replace walls
        // and I had choices, when I logged back in, it still had the wall choices
        // although the object type reset back to solid blocks." Root cause: choices
        // are persistent (per Cavendish Response #5 §8) but ObjectType (OldObject /
        // NewObject) is wiped by ResetToDefaults() above. After restore the choice
        // resolves to a wall item but the panel still says Tile, so the IV shows
        // a wall slot under a "Tile" section header — visual mismatch. Behaviour
        // was correct because the choice overrides the broad ObjectType category at
        // execute time, but the UX read as broken.
        //
        // Fix: choice = authoritative intent; reconcile the per-side ObjectType to
        // match the choice's actual createWall/createTile/torch nature on world
        // entry. This costs nothing when choices are null (default branch), and is
        // a single-shot reconciliation that doesn't fight further user changes.
        // S1 2026-04-26: Replacement choices are now per-ObjectType dicts, so each
        // choice IS already keyed to its correct ObjectType. The former
        // ReconcileReplacementObjectTypeToChoice(choiceReplaceSrc/Tgt) calls that patched
        // the wall-vs-tile mismatch (Letter #11 fix, 2026-04-23 S2) are no longer
        // needed — the dict key IS the authoritative ObjectType. No reconciliation required.

        // Same reconciliation for WoB (tile vs wall choice vs OperationType — though
        // WoB's tile/wall switch is not in scope of the bug report, the same
        // principle applies trivially: building's two choices live on independent
        // settings fields tied to the WoB:Tile vs WoB:Wall mode that the user
        // explicitly toggled with the wand-mode button, so no panel/choice mismatch
        // arises there. Left as a comment so future audits remember.)

        // Reset Molding wand state (managed by separate ModPlayer, but coordinated here)
        var moldingPlayer = Player.GetModPlayer<MoldingWandPlayer>();
        moldingPlayer.ClearAll();
        moldingPlayer.Settings.ResetToDefaults();

        // Photosensitivity warning for low Torch Wheel spacing values
        ShowPhotosensitivityWarningIfNeeded();
    }

    /// <summary>
    /// If the Outline Spacing (S) is below 6 and Animate Torch Wheel is on,
    /// show a one-time chat warning about potential photosensitivity issues.
    /// The warning is suppressed when animation is disabled or when the config
    /// option PhotosensitivityWarning is off.
    /// </summary>
    /// <remarks>
    /// At 30 tiles/second, low S values produce flicker frequencies in the
    /// photosensitive range (16–25 Hz) while the wheel remains on screen
    /// for multiple cycles. Values ≥ 6 keep the effective frequency below
    /// safe thresholds. See TorchWheelUnderwaterPlan_Verification.md.
    /// </remarks>
    private void ShowPhotosensitivityWarningIfNeeded()
    {
        var config = WandConfigs.TorchWheel;
        if (config == null) return;

        // All three conditions must be true to show the warning
        if (!config.PhotosensitivityWarning) return;
        if (!config.AnimateTorchWheel) return;
        if (config.TorchWheelSpacingS >= 6) return;

        Main.NewText(
            "[World Shaping Wands] ⚠ Photosensitivity Notice: " +
            $"Torch Wheel Outline Spacing is set to {config.TorchWheelSpacingS}. " +
            "Low spacing values (below 6) can produce rapid flicker. " +
            "If you are sensitive to flashing lights, increase spacing or " +
            "disable 'Animate Torch Wheel' in the Torch Wheel config. " +
            "This warning can be disabled in config.",
            new Color(255, 200, 100));
    }
}