using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

public class WandOfWiringSettings
{
    // Wire toggles
    public bool WireRed { get; set; } = true;
    public bool WireGreen { get; set; } = false;
    public bool WireBlue { get; set; } = false;
    public bool WireYellow { get; set; } = false;
    public bool Actuator { get; set; } = false;

    // Mode
    public WiringMode Mode { get; set; } = WiringMode.Place;

    // Shape (uses your existing ShapeInfo)
    public ShapeInfo Shape { get; set; } = new ShapeInfo(ShapeType.Line, ShapeMode.Filled, 1);

    // Selection mode (for cycling wand)
    public SelectionMode SelectionMode { get; set; } = SelectionMode.OneClick;

    public bool HasAnySelection =>
        WireRed || WireGreen || WireBlue || WireYellow || Actuator;

    public byte PackWireFlags()
    {
        byte flags = 0;
        if (WireRed) flags |= 1;
        if (WireGreen) flags |= 2;
        if (WireBlue) flags |= 4;
        if (WireYellow) flags |= 8;
        if (Actuator) flags |= 16;
        return flags;
    }

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

    public WandOfWiringSettings Clone()
    {
        return new WandOfWiringSettings
        {
            WireRed = WireRed,
            WireGreen = WireGreen,
            WireBlue = WireBlue,
            WireYellow = WireYellow,
            Actuator = Actuator,
            Mode = Mode,
            Shape = Shape,
            SelectionMode = SelectionMode
        };
    }

    public void ResetToDefaults()
    {
        WireRed = true;
        WireGreen = false;
        WireBlue = false;
        WireYellow = false;
        Actuator = false;
        Mode = WiringMode.Place;
        Shape = new ShapeInfo(ShapeType.Line, ShapeMode.Filled, 1);
        SelectionMode = SelectionMode.OneClick;
    }

    public string GetDescription()
    {
        string wires = "";
        if (WireRed) wires += "R";
        if (WireGreen) wires += "G";
        if (WireBlue) wires += "B";
        if (WireYellow) wires += "Y";
        if (Actuator) wires += "A";
        if (string.IsNullOrEmpty(wires)) wires = "None";

        return $"{Mode} [{wires}] - {Shape.GetDescription()}";
    }
}

public enum WiringMode
{
    Place,
    Remove
}