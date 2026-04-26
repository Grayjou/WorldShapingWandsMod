using System;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Types of selection changes that can trigger overlay invalidation.
/// </summary>
public enum SelectionChangeType
{
    /// <summary>Selection became active (start point placed).</summary>
    Started,

    /// <summary>Selection endpoints moved (dragging or adjusting).</summary>
    Moved,

    /// <summary>Selection was confirmed and cleared.</summary>
    Confirmed,

    /// <summary>Selection was cancelled (right-click or item swap).</summary>
    Cancelled,

    /// <summary>Shape settings changed (shape type, fill mode, thickness, etc.).</summary>
    SettingsChanged,
}

/// <summary>
/// Event args for selection change events raised by <see cref="OverlayManager"/>.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    /// <summary>What kind of selection change occurred.</summary>
    public SelectionChangeType ChangeType { get; }

    /// <summary>The overlay context at the time of the change.</summary>
    public OverlayContext Context { get; }

    public SelectionChangedEventArgs(SelectionChangeType changeType, OverlayContext context)
    {
        ChangeType = changeType;
        Context = context;
    }
}

/// <summary>
/// Event args for overlay registration/unregistration events.
/// </summary>
public class OverlayEventArgs : EventArgs
{
    /// <summary>The overlay that was registered or unregistered.</summary>
    public IComposableOverlay Overlay { get; }

    public OverlayEventArgs(IComposableOverlay overlay)
    {
        Overlay = overlay;
    }
}
