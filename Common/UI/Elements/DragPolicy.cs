namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Drag-arming policy for <see cref="UIDraggablePanel"/>.
/// (S6 §1 — Draggable Panel Unification.)
/// </summary>
public enum DragPolicy : byte
{
    /// <summary>Legacy: drag arms on any bare-panel or section-title click.</summary>
    Anywhere = 0,

    /// <summary>
    /// Drag arms ONLY when the click lands on a child <see cref="UIDragHandle"/>.
    /// </summary>
    HandleOnly = 1,

    /// <summary>
    /// Hybrid: legacy bare-panel-background drag PLUS explicit handle.
    /// </summary>
    HandleOrAnywhere = 2,
}