namespace WorldShapingWandsMod.Common.Enums;

// (S1 2026-04-29 — SubUI Architecture Phase A) The five lifecycle enums
// ratified by the `dev_notes/architecture/SubUIArchitecture.md` /
// `SubUIArchitectureStubs.md` design. These types are the formal vocabulary
// for SubPanel (popout / panel) lifecycle behaviour. They are introduced
// as a PURE ADDITION — the live `WandSubPanel` primitive (S4 2026-04-28; renamed S6 2026-04-29 Phase D) still
// drives behaviour through its existing IsLocked/OwnerVisibilityCheck
// surface, but new SubPanel subclasses (and, in Phase B, retrofitted
// existing SubPanels) declare their lifecycle via these enums so the
// behaviour table in the architecture doc is honoured by construction
// rather than by convention.
//
// See `dev_notes/planning/SubUIMigrationPlan.md` Phase A §2 for the
// rationale and Phase B for the per-consumer migration sequence.

/// <summary>
/// Chrome style for a <see cref="UI.Elements.WandSubPanel"/>.
/// </summary>
public enum SubPanelType : byte
{
    /// <summary>Originated from a <see cref="UI.Elements.UISection"/>.
    /// Implicit lock (no toggle rendered). On dismiss, returns content to
    /// the parent section (CollapseBack).</summary>
    Popout,

    /// <summary>Independent surface. Lock toggle rendered in chrome.
    /// Hides on dismiss (state retained for resurface).</summary>
    Panel,
}

/// <summary>
/// Lock behaviour for a <see cref="UI.Elements.WandSubPanel"/>.
/// </summary>
public enum LockBehaviour : byte
{
    /// <summary>Popout type. Always locked, no toggle rendered. The user can
    /// only dismiss via the explicit ✕ button (which routes through
    /// CollapseBack on Popouts).</summary>
    Implicit,

    /// <summary>Panel type. Lock toggle rendered, starts in the LOCKED state.
    /// Use for SubPanels with multiple controls where the player typically
    /// wants to set several values in one open (e.g. Color Replace's
    /// channel + source + target picker stack).</summary>
    DefaultLocked,

    /// <summary>Panel type. Lock toggle rendered, starts in the UNLOCKED
    /// state. Use for SubPanels with a single primary control where the
    /// canonical interaction is "open → pick → auto-close" (e.g. the
    /// Stencil slot picker).</summary>
    DefaultUnlocked,
}

/// <summary>
/// Behaviour when the user makes a selection inside the SubPanel.
/// </summary>
public enum ChoiceBehaviour : byte
{
    /// <summary>Selection does not dismiss the SubPanel. Use for SubPanels
    /// where the player typically wants to A/B-test multiple picks (any
    /// <see cref="LockBehaviour.DefaultLocked"/> SubPanel; all
    /// <see cref="LockBehaviour.Implicit"/> popouts).</summary>
    NeverCloses,

    /// <summary>Selection dismisses the SubPanel iff currently unlocked.
    /// Use for "open → pick → close" flows (typical
    /// <see cref="LockBehaviour.DefaultUnlocked"/> Panels). If the user
    /// flips the lock to ON, behaviour collapses to NeverCloses.</summary>
    ClosesIfUnlocked,
}

/// <summary>
/// Behaviour when the user explicitly closes the SubPanel via the ✕ button.
/// </summary>
public enum CloseBehaviour : byte
{
    /// <summary>Popout-only. Returns content to the parent
    /// <see cref="UI.Elements.UISection"/>; the SubPanel instance is
    /// destroyed.</summary>
    CollapseBack,

    /// <summary>Panel-only. Hides the SubPanel; instance + state retained
    /// for later resurface (lock state, position, picker selections all
    /// survive).</summary>
    Hide,
}

/// <summary>
/// Behaviour when the parent WandPanel closes (Esc / wand-swap to a wand
/// that doesn't host this SubPanel / X-button on the parent).
/// </summary>
public enum ParentCloseBehaviour : byte
{
    /// <summary>SubPanel stays open regardless of lock state. Used by
    /// every <see cref="SubPanelType.Popout"/>: their lock is implicit so
    /// they always survive parent-close.</summary>
    StaysUp,

    /// <summary>SubPanel stays open only if currently locked; closes if
    /// unlocked. Used by every <see cref="SubPanelType.Panel"/>. Mirrors
    /// the S14 2026-04-28 Esc-respects-lock contract: *"only the X button
    /// dismisses a locked SubPanel."*</summary>
    StaysUpIfLocked,
}
