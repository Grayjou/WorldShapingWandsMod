using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Protect vs Unprotect mode.
/// </summary>
public enum SafekeepingMode : byte
{
    Protect = 0,
    Unprotect = 1
}

/// <summary>
/// Settings for the Wand of Safekeeping.
/// </summary>
public class WandOfSafekeepingSettings
{
    /// <summary>The selection mode for this wand.</summary>
    public SelectionMode SelectionMode { get; set; } = SelectionMode.OneClick;

    /// <summary>The shape configuration.</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>Whether to protect or unprotect tiles.</summary>
    public SafekeepingMode Mode { get; set; } = SafekeepingMode.Protect;

    /// <summary>Whether to protect tile positions.</summary>
    public bool ProtectTiles { get; set; } = true;

    /// <summary>Whether to protect wall positions.</summary>
    public bool ProtectWalls { get; set; } = true;

    /// <summary>The starting point of the selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>The ending point of the selection.</summary>
    public Point EndPoint { get; set; }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandOfSafekeepingSettings Clone()
    {
        return new WandOfSafekeepingSettings
        {
            SelectionMode = SelectionMode,
            Shape = Shape,
            Mode = Mode,
            ProtectTiles = ProtectTiles,
            ProtectWalls = ProtectWalls,
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        SelectionMode = SelectionMode.OneClick;
        Shape = ShapeInfo.Default;
        Mode = SafekeepingMode.Protect;
        ProtectTiles = true;
        ProtectWalls = true;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        string targets = (ProtectTiles, ProtectWalls) switch
        {
            (true, true) => "Tiles & Walls",
            (true, false) => "Tiles Only",
            (false, true) => "Walls Only",
            (false, false) => "Nothing"
        };
        return $"{SelectionMode} - {Mode} {targets} - {Shape.GetDescription()}";
    }

    /// <summary>
    /// Validates all settings values.
    /// </summary>
    public void Validate()
    {
        Shape.Validate();
    }
}
