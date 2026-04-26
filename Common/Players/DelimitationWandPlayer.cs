using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player state for the Select Wand system.
/// Holds the canvas (boundary), tile selection (within the canvas),
/// optional custom shape (captured from selection), and settings.
/// All state is client-local — not synced in multiplayer (deferred to Phase 3).
/// </summary>
public class DelimitationWandPlayer : ModPlayer
{
    /// <summary>
    /// The canvas defines the boundary region that constrains all selection operations.
    /// Arbitrary shape via <see cref="System.Collections.Generic.HashSet{T}"/> of tile positions.
    /// </summary>
    public SelectionCanvas Canvas { get; private set; } = new();

    /// <summary>
    /// The tile selection — a subset of the canvas representing the active selection.
    /// Used as the source for <see cref="CustomShape"/> capture and
    /// as a filter for wand operations.
    /// </summary>
    public TileSelection Selection { get; private set; } = new();

    /// <summary>
    /// A user-defined shape captured from the <see cref="Selection"/> via "Promote → Custom Shape".
    /// When active, Stamp wands use this shape instead of parametric shapes.
    /// <c>null</c> when no custom shape has been captured.
    /// </summary>
    public CustomShape ActiveCustomShape { get; set; }

    /// <summary>Per-player Select Wand settings (operation, mode, visual preferences).</summary>
    public DelimitationWandSettings Settings { get; private set; } = new();

    /// <summary>
    /// Captures the current <see cref="Selection"/> as a <see cref="CustomShape"/>.
    /// Returns <c>true</c> if the capture succeeded (selection was non-empty).
    /// </summary>
    /// <param name="clearSelection">If <c>true</c>, clears the selection after capture.</param>
    /// <returns><c>true</c> if a custom shape was successfully captured.</returns>
    public bool PromoteSelectionToCustomShape(bool clearSelection = true)
    {
        var shape = CustomShape.FromSelection(Selection);
        if (shape == null)
            return false;

        ActiveCustomShape = shape;

        if (clearSelection)
            Selection.Clear();

        return true;
    }

    /// <summary>
    /// Clears the custom shape, reverting Stamp wands to parametric shape behavior.
    /// </summary>
    public void ClearCustomShape()
    {
        ActiveCustomShape = null;
    }

    /// <summary>
    /// Clears all Select Wand state: canvas, selection, and custom shape.
    /// </summary>
    public void ClearAll()
    {
        Canvas.Clear();
        Selection.Clear();
        ActiveCustomShape = null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Integration Filter — constrains wand operations to the active selection
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set to <c>true</c> by <see cref="FilterBySelection"/> when the delimitation
    /// filter removed ALL tiles from a non-empty input. Consumed and reset by
    /// <see cref="Common.Utilities.Msg.ShowNullResult"/> to replace the generic
    /// null-result message with a delimitation-specific warning.
    /// </summary>
    public bool LastFilterCausedEmpty { get; internal set; }

    /// <summary>
    /// Counts consecutive frames where <see cref="FilterBySelection"/> caused an
    /// empty result during stamp channeling. When this reaches 60 (one second),
    /// <see cref="Common.Utilities.Msg.ShowNullResult"/> allows the delimitation
    /// warning through despite the channeling suppression, then resets the counter.
    /// </summary>
    public int ConsecutiveEmptyFilterFrames { get; private set; }

    /// <summary>
    /// Filters a tile array through the active tile selection. If the selection
    /// is active, only tiles that are in the selection are kept. If the selection
    /// is inactive, all tiles pass through unmodified.
    /// <para>
    /// This is the ONLY integration point between the Select Wand system and
    /// all other wand families. Call this after generating shape tiles but
    /// before executing the wand operation.
    /// </para>
    /// <para>
    /// Does NOT fire chat messages directly. Instead, sets
    /// <see cref="LastFilterCausedEmpty"/> so that <see cref="Common.Utilities.Msg.ShowNullResult"/>
    /// can substitute the delimitation warning in place of the generic null message.
    /// </para>
    /// </summary>
    /// <param name="tiles">The tile positions to filter.</param>
    /// <returns>
    /// The filtered tile array — same reference if selection is inactive,
    /// or a new array with only selected tiles.
    /// </returns>
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
    /// Returns <c>true</c> if the delimitation selection is active, meaning
    /// <see cref="FilterBySelection"/> will actively constrain the input tile set.
    /// Callers can use this together with an empty filter result to determine
    /// whether the delimitation area caused a "no tiles" outcome.
    /// </summary>
    public bool IsFilterActive => Selection.IsActive;

    /// <summary>
    /// Resets the consecutive empty-filter frame counter. Called by
    /// <see cref="Common.Utilities.Msg.ShowNullResult"/> after the sustained-channeling
    /// threshold fires the delimitation warning, so it re-arms for the next interval.
    /// </summary>
    public void ResetConsecutiveEmptyFilterFrames() => ConsecutiveEmptyFilterFrames = 0;

    public override void OnRespawn()
    {
        // Keep canvas and custom shape on respawn — only clear selection
        Selection.Clear();
    }

    public override void OnEnterWorld()
    {
        ClearAll();
        Settings.ResetToDefaults();
    }
}
