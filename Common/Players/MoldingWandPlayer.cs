using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player state for the Wand of Molding system.
/// Holds an independent canvas, tile selection, and the output <see cref="CustomShape"/>.
/// </summary>
/// <remarks>
/// <para>
/// The Molding wand's canvas/selection system is completely independent from the
/// Delimitation wand's (<see cref="DelimitationWandPlayer"/>). This means the user
/// can maintain active selections on both wands simultaneously without interference.
/// </para>
/// <para>
/// The key difference from Delimitation: the Molding wand's "execute" path
/// (<see cref="PromoteMoldToCustomShape"/>) creates a <see cref="CustomShape"/> that is
/// immediately available to all Stamp wands via <see cref="MoldedShape"/>. The user's
/// workflow is: build a shape → it becomes the custom shape → stamp it with any wand.
/// No separate "Promote" step is needed — the selection IS the mold.
/// </para>
/// </remarks>
public class MoldingWandPlayer : ModPlayer
{
    /// <summary>
    /// (Phase 3 — S9 2026-04-29; <see href="MultipleStencilsPlan.md"/> §4)
    /// Per-slot independent canvas storage. All <see cref="StencilSlotCount"/>
    /// slots are constructed eagerly with empty canvases; the active slot
    /// is exposed via the legacy <see cref="Canvas"/> passthrough getter so
    /// every existing consumer (<c>WandOfMolding.cs</c>,
    /// <c>MoldingSettingsPanel.cs</c>, <c>MoldingCanvasOverlay.cs</c>) keeps
    /// working byte-identical without any call-site rewrites.
    /// </summary>
    public StencilSlot[] StencilSlots { get; } = BuildEmptySlots();

    private static StencilSlot[] BuildEmptySlots()
    {
        var slots = new StencilSlot[StencilSlotCount];
        for (int i = 0; i < StencilSlotCount; i++) slots[i] = new StencilSlot();
        return slots;
    }

    /// <summary>(Phase 3) The currently-active slot. Always equals
    /// <c>StencilSlots[ActiveStencilSlot]</c>; never null because the array
    /// is filled eagerly at construction.</summary>
    public StencilSlot ActiveStencil => StencilSlots[ActiveStencilSlot];

    /// <summary>
    /// (Phase 3 passthrough — preserves the pre-Phase-3 single-canvas API
    /// contract) The active slot's canvas. Switching
    /// <see cref="ActiveStencilSlot"/> flips which canvas this returns —
    /// every consumer that reads <c>mwp.Canvas</c> (canvas mutation,
    /// selection clipping, overlay rendering, settings-panel readouts) now
    /// transparently routes through the active slot. The setter is gone:
    /// callers that previously assumed they could replace the canvas
    /// instance must now mutate the existing instance via
    /// <c>Canvas.Clear()</c> / <c>Canvas.ApplyOperation(...)</c>, which is
    /// the pattern every existing call site already uses.
    /// </summary>
    public SelectionCanvas Canvas => ActiveStencil.Canvas;

    /// <summary>
    /// (S11 2026-04-29 — Bug 1 fix; <see href="StencilEditVsActOn.md"/> §3)
    /// The in-flight sculpt buffer for the currently-active EDIT slot.
    /// Now a passthrough to <see cref="ActiveStencil"/>.<see cref="StencilSlot.Selection"/>;
    /// switching <see cref="ActiveStencilSlot"/> flips which slot's selection
    /// every existing call site mutates. Pre-S11 this was a single global
    /// <see cref="TileSelection"/> shared across all five slots — that
    /// caused Move/Remove/Clear/Invert in one slot to corrupt every other
    /// slot's in-flight work. Per-slot storage (on <see cref="StencilSlot"/>)
    /// is now the source of truth; this getter is API compat for the dozens
    /// of <c>mwp.Selection.X</c> call sites in the WoM codebase.
    /// </summary>
    public TileSelection Selection => ActiveStencil.Selection;

    /// <summary>
    /// (S11 2026-04-29 — Bug 2 fix; <see href="StencilEditVsActOn.md"/> §3)
    /// The output custom shape for the currently-active EDIT slot. Now a
    /// passthrough to <see cref="ActiveStencil"/>.<see cref="StencilSlot.MoldedShape"/>.
    /// Pre-S11 this was a single global field overwritten on every
    /// <see cref="PromoteMoldToCustomShape"/> call; combined with the
    /// pre-S11 global Selection that produced a "union of all stencils"
    /// symptom whenever the user stamped Mold Shape after editing
    /// multiple slots.
    /// <para><b>Important</b>: this getter returns the <em>EDIT</em> slot's
    /// committed shape (used by the WoM status text "Molded Shape: N
    /// tiles"). The <em>stamp</em> path used by every wand's Mold Shape
    /// resolves through <see cref="ActOnStencil"/> instead — see
    /// <see cref="WorldShapingWandsMod.Common.Geometry.Shapes.MoldShape"/>.</para>
    /// </summary>
    public CustomShape MoldedShape => ActiveStencil.MoldedShape;

    /// <summary>Per-player Molding Wand settings (operation, mode, visual preferences).</summary>
    public MoldingWandSettings Settings { get; private set; } = new();

    /// <summary>
    /// Active stencil slot (0..<see cref="StencilSlotCount"/>-1; UI shows 1-indexed).
    /// Phase 1 (S6 2026-04-28) wired the slot index + the always-visible 5-cell
    /// stencil row + the Mold-cell hover-icon swap. **Phase 3 (S9 2026-04-29)**
    /// added the per-slot canvas storage backing this index: changing
    /// <see cref="ActiveStencilSlot"/> flips which canvas
    /// <see cref="Canvas"/> returns (and therefore which canvas every
    /// existing consumer reads/mutates), with the previous slot's canvas
    /// preserved intact in <see cref="StencilSlots"/>. The setter clamps
    /// to <c>[0, StencilSlotCount-1]</c> defensively. In-memory only — no
    /// save/load per Cavendish C-S1 + plan §4.
    /// </summary>
    public byte ActiveStencilSlot
    {
        get => _activeStencilSlot;
        set
        {
            byte clamped = value >= StencilSlotCount ? (byte)(StencilSlotCount - 1) : value;
            _activeStencilSlot = clamped;
        }
    }
    private byte _activeStencilSlot = 0;

    /// <summary>
    /// (S11 2026-04-29 — Bug 3 fix; <see href="StencilEditVsActOn.md"/> §1)
    /// The slot index whose <see cref="StencilSlot.MoldedShape"/> is consumed
    /// by <see cref="WorldShapingWandsMod.Common.Geometry.Shapes.MoldShape"/>
    /// for stamping/preview on EVERY wand (Building, Dismantling, Coating,
    /// Wiring, Safekeeping, Fluids, Torches, Replacement, Selection, AND
    /// Wand of Molding's own Mold-shape stamp). Independent from
    /// <see cref="ActiveStencilSlot"/>: EDIT (Wand of Molding's editing
    /// cursor) is the one that mutations land in; ACT-ON (this field) is
    /// the one that stamps consume from. They can hold different values —
    /// e.g. user edits slot 3 while every wand stamps the bear from slot 1.
    /// <para><b>Lifecycle</b>: in-memory only, scoped to (Player × World)
    /// like <see cref="ActiveStencilSlot"/>. Mutated by the ACT-ON Stencil
    /// Picker SubUI (right-click on the Mold cell of any wand panel).
    /// Default 0 = first slot, preserving the pre-S11 "Mold Shape stamps
    /// the only slot's content" behaviour for users who never open the
    /// picker.</para>
    /// </summary>
    public byte ActOnStencilSlot
    {
        get => _actOnStencilSlot;
        set
        {
            byte clamped = value >= StencilSlotCount ? (byte)(StencilSlotCount - 1) : value;
            _actOnStencilSlot = clamped;
        }
    }
    private byte _actOnStencilSlot = 0;

    /// <summary>(S11) Convenience accessor — never null; mirrors
    /// <see cref="ActiveStencil"/> for the ACT-ON role.</summary>
    public StencilSlot ActOnStencil => StencilSlots[ActOnStencilSlot];

    /// <summary>Number of stencil slots exposed to the UI. See plan §0.1.</summary>
    public const int StencilSlotCount = 5;

    /// <summary>
    /// When <c>true</c>, the mold selection is automatically promoted to a
    /// <see cref="CustomShape"/> after every execute operation. This gives the
    /// user's desired "immediate availability" workflow.
    /// </summary>
    public bool AutoPromote { get; set; } = true;

    /// <summary>
    /// Promotes the current mold <see cref="Selection"/> to a <see cref="CustomShape"/>.
    /// The selection is NOT cleared — the user can continue adding/removing from the mold.
    /// </summary>
    /// <returns><c>true</c> if a custom shape was successfully created.</returns>
    public bool PromoteMoldToCustomShape()
    {
        // (S11 2026-04-29) Promote captures the EDIT slot's selection into
        // the EDIT slot's MoldedShape — both are per-slot now. Other slots
        // keep their own committed shapes intact, so switching ACT-ON to a
        // different slot stamps THAT slot's shape (or nothing, if empty).
        var shape = CustomShape.FromSelection(Selection);
        if (shape == null)
            return false;

        ActiveStencil.MoldedShape = shape;

        // (S11 deferred Q1 — see StencilEditVsActOn.md §6) Continue mirroring
        // the EDIT slot's shape into DelimitationWandPlayer.ActiveCustomShape
        // for legacy Stamp-wand interop. Mold Shape itself no longer reads
        // from this field — it goes through ActOnStencil — so this sync is
        // only relevant to the small number of Delimitation-flavoured
        // consumers that historically piggybacked on ActiveCustomShape.
        var delimPlayer = Player.GetModPlayer<DelimitationWandPlayer>();
        delimPlayer.ActiveCustomShape = shape;

        return true;
    }

    /// <summary>
    /// Clears the molded shape output for the currently-active EDIT slot.
    /// (S11 2026-04-29) Per-slot: only the active slot's MoldedShape is
    /// nulled; other slots' committed shapes are preserved.
    /// </summary>
    public void ClearMoldedShape()
    {
        ActiveStencil.MoldedShape = null;
    }

    /// <summary>
    /// Clears all Molding Wand state: every slot's canvas, the in-progress
    /// selection, and the molded shape. Called by <see cref="OnEnterWorld"/>
    /// so a fresh world entry starts with 5 empty slots (per plan §4:
    /// stencil canvases live in memory only and are scoped to
    /// (Player × World) for the active session).
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < StencilSlots.Length; i++)
        {
            StencilSlots[i].Canvas.Clear();
            StencilSlots[i].Selection.Clear();
            StencilSlots[i].MoldedShape = null;
        }
        ActiveStencilSlot = 0;
        ActOnStencilSlot  = 0;
    }

    /// <summary>(Phase 3) Clear only the active slot's canvas + the in-flight
    /// selection (the existing UI "Clear Canvas" action's expected scope per
    /// plan §5.1: "Clear active slot — existing Clear Canvas action operates
    /// on active slot only"). Other slots untouched.</summary>
    public void ClearActiveSlot()
    {
        ActiveStencil.Canvas.Clear();
        ActiveStencil.Selection.Clear();
        ActiveStencil.MoldedShape = null;
    }

    public override void OnRespawn()
    {
        // (S11) Keep canvases + molded shapes on respawn — only clear the
        // active EDIT slot's in-flight selection (other slots' selections
        // also persist; the user might have multiple in-progress sculpts).
        ActiveStencil.Selection.Clear();
    }

    public override void OnEnterWorld()
    {
        ClearAll();
        Settings.ResetToDefaults();
        LastHintTick = 0;
    }

    /// <summary>
    /// Game tick at which the last molding contextual hint was shown.
    /// Used by <c>WandOfMoldingBase.ShowMoldingHint</c> for rate-limiting.
    /// Reset on world entry so hints are fresh each session.
    /// </summary>
    internal int LastHintTick { get; set; }
}
