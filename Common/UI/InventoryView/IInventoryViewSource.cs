using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// Single-grid candidate-and-selection contract for the InventoryView panel.
/// Each participating wand family contributes 1+ source instances; sources are
/// grouped into one panel via <see cref="IInventoryViewProvider"/>.
///
/// <para>Phase 1 (this commit): backend only — interfaces, sources, registry,
/// debug command. The visible <c>InventoryViewPanel</c> UIState lands in a
/// later session and consumes this contract unchanged.</para>
///
/// <para>Phase 2 wire-up: each wand's execution path (e.g.
/// <c>WandOfBuildingBase.TileExecution.ExecuteBuilding</c>) reads the source's
/// <see cref="GetSelectedItemType"/> and, when non-null, narrows the
/// FindFirstItemIndex condition to that exact item type. This is what turns
/// the InventoryView choice into a real source-of-truth — not just decoration.</para>
///
/// <para><b>Terminology note:</b> this interface intentionally uses
/// <c>Selected</c>/<c>Chosen</c> semantics. Future true persistence pinning
/// should use a distinct name such as <c>PersistentPin</c> to stay grep-safe
/// and avoid semantic collision.</para>
/// </summary>
public interface IInventoryViewSource
{
    /// <summary>
    /// Localization key (under <c>Mods.WorldShapingWandsMod.</c>) for the
    /// per-source title shown above its grid in the panel.
    /// </summary>
    string TitleKey { get; }

    /// <summary>
    /// Yield every inventory item type that this wand could currently use as
    /// a source item. Recomputed on every panel-frame; deterministic.
    /// </summary>
    /// <param name="player">The local player whose inventory is being scanned.</param>
    IEnumerable<int> GetCandidateItemTypes(Player player);

    /// <summary>
    /// Returns the player's chosen item type for this source, or <c>null</c>
    /// if no choice is set (default state — execution falls back to hotbar scan).
    /// </summary>
    int? GetSelectedItemType(WandPlayer wp);

    /// <summary>
    /// Sets the chosen item type. Pass <c>null</c> to clear the choice (matches
    /// clicking an already-chosen slot in the panel).
    /// </summary>
    void SetSelectedItemType(WandPlayer wp, int? itemType);
}
