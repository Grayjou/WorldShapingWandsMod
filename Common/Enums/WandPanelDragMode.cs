namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Client preference for how wand settings panels arm dragging.
/// </summary>
public enum WandPanelDragMode : byte
{
    /// <summary>
    /// Drag only starts from the explicit drag handle.
    /// </summary>
    HandleOnly = 0,

    /// <summary>
    /// Drag starts from the explicit handle or any bare-panel area.
    /// </summary>
    HandleOrAnywhere = 1,
}
