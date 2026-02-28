namespace MagicWiring.Common;

/// <summary>
/// Determines whether we're placing or removing wires.
/// </summary>
public enum WiringMode
{
    Place,
    Remove
}

/// <summary>
/// The available area shapes.
/// </summary>
public enum WiringShape
{
    WireKite,           // Vanilla 90-degree L-path (listed first as default)
    FilledRectangle,
    HollowRectangle,
    FilledDiamond,
    HollowDiamond,
    FilledTriangle,
    HollowTriangle
}

/// <summary>
/// Interaction mode for the wand.
/// </summary>
public enum InteractionMode
{
    Hold,   // Hold left-click and drag, release to execute
    Toggle  // Click to set start, click again to execute
}

/// <summary>
/// Client-side settings for the Wand of Wiring.
/// These mirror the concept of WiresUI.Settings.ToolMode from vanilla,
/// but are extended with shape selection.
/// </summary>
public static class WiringSettings
{
    // Wire/actuator selection — multiple can be active simultaneously
    public static bool WireRed = true;
    public static bool WireGreen = false;
    public static bool WireBlue = false;
    public static bool WireYellow = false;
    public static bool Actuator = false;

    // Operation mode
    public static WiringMode Mode = WiringMode.Place;

    // Area shape
    public static WiringShape Shape = WiringShape.WireKite;

    // Interaction mode
    public static InteractionMode Interaction = InteractionMode.Hold;

    /// <summary>
    /// Returns true if at least one wire type or actuator is selected.
    /// Prevents operations from executing with no selection.
    /// </summary>
    public static bool HasAnySelection =>
        WireRed || WireGreen || WireBlue || WireYellow || Actuator;

    /// <summary>
    /// Packs the boolean wire/actuator flags into a byte for networking.
    /// Bits: 0=Red, 1=Green, 2=Blue, 3=Yellow, 4=Actuator
    /// </summary>
    public static byte PackWireFlags()
    {
        byte flags = 0;
        if (WireRed) flags |= 1;
        if (WireGreen) flags |= 2;
        if (WireBlue) flags |= 4;
        if (WireYellow) flags |= 8;
        if (Actuator) flags |= 16;
        return flags;
    }

    /// <summary>
    /// Unpacks wire flags from a byte. Used on the receiving end of net messages.
    /// </summary>
    public static (bool red, bool green, bool blue, bool yellow, bool actuator) UnpackWireFlags(byte flags)
    {
        return (
            (flags & 1) != 0,
            (flags & 2) != 0,
            (flags & 4) != 0,
            (flags & 8) != 0,
            (flags & 16) != 0
        );
    }
}