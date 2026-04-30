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

    // ── PersistentPin contract (S15 2026-04-28) ──────────────────────────────
    //
    // Pins are a SECOND, parallel layer atop the existing `Selected/Chosen`
    // layer. Per GrayJou's S15 product calls:
    //   • Pins are *stale-reference*: a pinned item type may no longer be in
    //     inventory. The IV panel still renders it as a slot (similar to
    //     ghost-chosen) so the user has a one-click handle to unpin it.
    //   • Right-click on a slot toggles its pinned state.
    //   • Pinned/Chosen are NOT linked. Terraria's "favorited" flag does NOT
    //     auto-seed pins (rejected — favorited stops items leaving inventory,
    //     pins reference items that already left).
    //   • Visual: pinned = green border. Chosen = gold border. Chosen
    //     overrides pinned visually (NOT additive).
    //   • Persistence: same `Save/LoadData` round-trip as Chosen; survives
    //     world save and is safe across mod reload (uses the same
    //     `ChoiceSerialization` mod-name+item-name stable tuple).
    //
    // Default implementations return empty / no-op so any future source
    // implementation that doesn't opt in still compiles. All 5 v1 sources
    // override these methods.

    /// <summary>
    /// Yield every item type the player has pinned for this source's current
    /// axis (e.g. for <see cref="Sources.BuildingTileSource"/> this is the set
    /// of pins for the active <c>BuildingSettings.Object</c> sub-mode). May
    /// include item types NOT currently in the player's inventory \u2014 those are
    /// stale-reference pins, rendered as ghost-style slots in the panel.
    /// </summary>
    IEnumerable<int> GetPinnedItemTypes(WandPlayer wp) => System.Array.Empty<int>();

    /// <summary>
    /// True iff <paramref name="itemType"/> is currently pinned for this
    /// source's active axis.
    /// </summary>
    bool IsPinned(WandPlayer wp, int itemType) => false;

    /// <summary>
    /// Toggle the pinned state of <paramref name="itemType"/> for this
    /// source's active axis. Right-click handler in the panel routes here.
    /// </summary>
    void TogglePin(WandPlayer wp, int itemType) { }
}
