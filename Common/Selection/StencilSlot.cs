using WorldShapingWandsMod.Common.Selection;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// (Phase 3 — S9 2026-04-29; <see href="MultipleStencilsPlan.md"/> §4) One
/// independent mold canvas slot. The Wand of Molding owns
/// <see cref="MoldingWandPlayer.StencilSlotCount"/> = 5 of these; the user
/// switches between them via the slot picker (right-click on the slot
/// button → SubUI; left-click re-arms the active slot's shape).
///
/// <para>Phase 3 scope (this commit) keeps the wrapper minimal: only the
/// <see cref="Canvas"/> field is currently load-bearing. The <see cref="Label"/>
/// + <see cref="LastModifiedTick"/> fields are present so v2 (rename UI,
/// last-touched ordering) can land without a data-model migration. They
/// are NOT yet written by any code path.</para>
///
/// <para><b>Persistence</b>: none. Per Cavendish C-S1 2026-04-28 §C2 +
/// <see href="MultipleStencilsPlan.md"/> §4, stencil slots are scoped to
/// (Player × World) for the active session and live in memory only. Lost
/// on world-exit / disconnect. The only durable persistence path is the
/// planned <c>Save Current Stencil</c> / <c>Load to Current Stencil</c>
/// wand actions (plan §5.1) which write a SINGLE slot's canvas to disk
/// under the player's profile, deliberately not auto-restored.</para>
/// </summary>
public sealed class StencilSlot
{
    /// <summary>The boundary canvas for this slot. Same type as the
    /// pre-Phase-3 single <c>MoldingWandPlayer.Canvas</c>; switching the
    /// active slot transparently swaps which canvas <c>mwp.Canvas</c>
    /// returns (see the passthrough getter on
    /// <see cref="MoldingWandPlayer.Canvas"/>).</summary>
    public SelectionCanvas Canvas { get; } = new();

    /// <summary>(S11 2026-04-29 — Bug 1 fix; <c>StencilEditVsActOn.md</c> §3)
    /// The in-flight sculpt buffer for THIS slot. Was previously a single
    /// global <c>MoldingWandPlayer.Selection</c> shared across all five
    /// slots — that shared object meant every Move / Remove / Clear / Invert
    /// mutated all slots at once even though each only touched the active
    /// slot's canvas. Per-slot storage makes those mutations correctly
    /// scoped: switching slots reveals the prior slot's in-flight selection
    /// untouched.</summary>
    public TileSelection Selection { get; } = new();

    /// <summary>(S11 2026-04-29 — Bug 2 fix; <c>StencilEditVsActOn.md</c> §3)
    /// The committed mold for THIS slot — the <see cref="CustomShape"/>
    /// produced by the most recent <see cref="MoldingWandPlayer.PromoteMoldToCustomShape"/>
    /// call while this slot was active. Was previously a single global
    /// <c>MoldingWandPlayer.MoldedShape</c> overwritten on every promote;
    /// combined with the global Selection (see above) that produced a
    /// "union of all stencils" symptom on stamp. Per-slot storage means
    /// every slot remembers its own committed shape, and
    /// <see cref="WorldShapingWandsMod.Common.Geometry.Shapes.MoldShape"/>
    /// resolves which one to stamp via
    /// <see cref="MoldingWandPlayer.ActOnStencilSlot"/>.</summary>
    public CustomShape MoldedShape { get; set; }

    /// <summary>(Phase 3 placeholder for v2 rename UI) Optional user-set
    /// label. Empty string = unnamed; UI shows the 1-indexed slot number.</summary>
    public string Label { get; set; } = "";

    /// <summary>(Phase 3 placeholder for v2 last-touched ordering) Game tick
    /// at which this slot's canvas was most recently mutated. Currently
    /// unused; v2 may sort the slot picker by recency or surface a
    /// "modified Xm ago" hint.</summary>
    public int LastModifiedTick { get; set; }

    /// <summary>True iff the slot's canvas has at least one tile boundary
    /// established (i.e. the slot is "in use" rather than freshly empty).
    /// Convenience for the slot picker's grayscale-vs-coloured swatch state.</summary>
    public bool IsActive => Canvas.IsActive;
}
