using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player state for the Delimitation Wand system.
/// Hosts a row of <see cref="SlotCount"/> independent stencil slots.
/// At any given time, one slot (or none — <see cref="NullSlot"/>) is the active slot.
/// <para>
/// The classic <see cref="Canvas"/>, <see cref="Selection"/>, and
/// <see cref="ActiveCustomShape"/> properties are passthrough getters that route
/// to the active slot's data, so all existing callers require no changes.
/// When <see cref="IsActive"/> is <c>false</c>, those getters return empty
/// singletons (never <c>null</c>) to prevent NRE at call sites.
/// </para>
/// </summary>
public class DelimitationWandPlayer : ModPlayer
{
    // ════════════════════════════════════════════════════════════════
    //  Slot model
    // ════════════════════════════════════════════════════════════════

    /// <summary>Number of independently-addressable stencil slots (matches MoldingWandPlayer.StencilSlotCount).</summary>
    public const int SlotCount = 4;

    /// <summary>Sentinel value meaning "no active slot". Stored as <c>sbyte</c> to fit in a 1-byte tag.</summary>
    public const sbyte NullSlot = -1;

    private readonly DelimSlot[] _slots = new DelimSlot[SlotCount];

    /// <summary>
    /// The currently active slot index (0..SlotCount-1), or <see cref="NullSlot"/> when no slot is active.
    /// </summary>
    public sbyte ActiveSlot { get; private set; } = 0;


    /// <summary><c>true</c> when a slot is active (i.e., <see cref="ActiveSlot"/> ≠ <see cref="NullSlot"/>).</summary>
    public bool IsActive => ActiveSlot != NullSlot;

    // Shared empty singletons returned when no slot is active.
    // These are static so all DelimitationWandPlayer instances share one allocation.
    private static readonly SelectionCanvas _emptyCanvas    = new();
    private static readonly TileSelection   _emptySelection = new();

    // ════════════════════════════════════════════════════════════════
    //  Passthrough getters — all existing callers route through these
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// The canvas defines the boundary region that constrains all selection operations.
    /// Routes to the active slot's canvas, or an empty singleton when no slot is active.
    /// </summary>
    public SelectionCanvas Canvas
        => IsActive ? _slots[ActiveSlot].Canvas : _emptyCanvas;

    /// <summary>
    /// The tile selection — a subset of the canvas representing the active selection.
    /// Routes to the active slot's selection, or an empty singleton when no slot is active.
    /// </summary>
    public TileSelection Selection
        => IsActive ? _slots[ActiveSlot].Selection : _emptySelection;

    /// <summary>
    /// A user-defined shape captured from <see cref="Selection"/> via "Promote → Custom Shape".
    /// Routes to the active slot's custom shape.
    /// Getter returns <c>null</c> when no slot is active; setter is a no-op when no slot is active.
    /// </summary>
    public CustomShape ActiveCustomShape
    {
        get => IsActive ? _slots[ActiveSlot].CustomShape : null;
        set { if (IsActive) _slots[ActiveSlot].CustomShape = value; }
    }

    /// <summary>Per-player Delimitation Wand settings (operation, mode, visual preferences).</summary>
    public DelimitationWandSettings Settings { get; private set; } = new();

    // ════════════════════════════════════════════════════════════════
    //  Slot management
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Activates slot <paramref name="index"/> (0..SlotCount-1).
    /// No-op for out-of-range indices.
    /// </summary>
    public void SetActiveSlot(sbyte index)
    {
        if (index < 0 || index >= SlotCount) return;
        ActiveSlot = index;
    }

    /// <summary>
    /// Clears the active slot's data (canvas, selection, custom shape) without deactivating it.
    /// No-op when no slot is active.
    /// </summary>
    public void ClearActiveSlot()
    {
        if (!IsActive) return;
        _slots[ActiveSlot].ClearAll();
    }

    /// <summary>
    /// Deactivates the current slot (sets <see cref="ActiveSlot"/> to <see cref="NullSlot"/>).
    /// Data in all slots is preserved.
    /// </summary>
    public void DeactivateSlot()
    {
        ActiveSlot = NullSlot;
    }

    /// <summary>
    /// Clears data in all slots and resets <see cref="ActiveSlot"/> to <see cref="NullSlot"/>.
    /// Called by <see cref="OnEnterWorld"/> for a clean session start.
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots[i].ClearAll();
        ActiveSlot = 0;
    }

    // ════════════════════════════════════════════════════════════════
    //  Higher-level operations (all route via passthrough getters)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Captures the current <see cref="Selection"/> as a <see cref="CustomShape"/>.
    /// Returns <c>true</c> if the capture succeeded (selection was non-empty and a slot is active).
    /// </summary>
    public bool PromoteSelectionToCustomShape(bool clearSelection = true)
    {
        if (!IsActive) return false;

        var shape = CustomShape.FromSelection(Selection);
        if (shape == null)
            return false;

        _slots[ActiveSlot].CustomShape = shape;

        if (clearSelection)
            Selection.Clear();

        return true;
    }

    /// <summary>
    /// Clears the custom shape for the active slot, reverting Stamp wands to parametric shapes.
    /// No-op when no slot is active.
    /// </summary>
    public void ClearCustomShape()
    {
        if (IsActive) _slots[ActiveSlot].CustomShape = null;
    }

    /// <summary>
    /// Clears all state in the active slot: canvas, selection, and custom shape.
    /// Legacy bridge — callers of the old <c>ClearAll()</c> route here.
    /// </summary>
    public void ClearAll()
    {
        if (IsActive) _slots[ActiveSlot].ClearAll();
    }

    // ════════════════════════════════════════════════════════════════
    //  Integration Filter — constrains wand operations to the active selection
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set to <c>true</c> by <see cref="FilterBySelection"/> when the delimitation
    /// filter removed ALL tiles from a non-empty input.
    /// </summary>
    public bool LastFilterCausedEmpty { get; internal set; }

    /// <summary>
    /// Counts consecutive frames where <see cref="FilterBySelection"/> caused an
    /// empty result during stamp channeling.
    /// </summary>
    public int ConsecutiveEmptyFilterFrames { get; private set; }

    /// <summary>
    /// Filters a tile array through the active tile selection.
    /// If no slot is active or the selection is empty, all tiles pass through unmodified.
    /// </summary>
    public Point[] FilterBySelection(Point[] tiles)
    {
        if (!Selection.IsActive)
        {
            LastFilterCausedEmpty = false;
            ConsecutiveEmptyFilterFrames = 0;
            return tiles;
        }

        var filtered = tiles.Where(t => Selection.Contains(t)).ToArray();

        if (filtered.Length == 0 && tiles.Length > 0)
        {
            LastFilterCausedEmpty = true;
            ConsecutiveEmptyFilterFrames++;
        }
        else
        {
            LastFilterCausedEmpty = false;
            ConsecutiveEmptyFilterFrames = 0;
        }

        return filtered;
    }

    /// <summary>
    /// Returns <c>true</c> if the delimitation selection is active.
    /// </summary>
    public bool IsFilterActive => Selection.IsActive;

    /// <summary>
    /// Resets the consecutive empty-filter frame counter.
    /// </summary>
    public void ResetConsecutiveEmptyFilterFrames() => ConsecutiveEmptyFilterFrames = 0;

    // ════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════════════════════════════

    public DelimitationWandPlayer()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new DelimSlot();
    }

    public override void OnRespawn()
    {
        // Keep canvas and custom shape on respawn — only clear the active slot's selection.
        if (IsActive)
            _slots[ActiveSlot].Selection.Clear();
    }

    public override void OnEnterWorld()
    {
        ClearAllSlots();
        Settings.ResetToDefaults();
    }

    // ════════════════════════════════════════════════════════════════
    //  Save / Load
    // ════════════════════════════════════════════════════════════════
    // Per §8 of the MultipleStencilsPlan: canvases are in-memory only.
    // We only persist ActiveSlot (sbyte) so the panel selection is restored
    // across game restarts. Slot data (canvas/selection/customshape) is
    // session-scoped and intentionally not saved.

    private const string TagActiveSlot = "Delim_ActiveSlot";

    public override void SaveData(TagCompound tag)
    {
        tag[TagActiveSlot] = (int)ActiveSlot;
    }

    public override void LoadData(TagCompound tag)
    {
        ActiveSlot = 0;
        if (tag.ContainsKey(TagActiveSlot))
        {
            int loadedInt;
            try
            {
                loadedInt = tag.GetInt(TagActiveSlot);
            }
            catch
            {
                loadedInt = tag.GetByte(TagActiveSlot);
            }

            sbyte loaded = (sbyte)loadedInt;
            if (loaded == NullSlot || (loaded >= 0 && loaded < SlotCount))
                ActiveSlot = loaded;
        }
    }
}

