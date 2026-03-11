using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Dismantling.
/// </summary>
public class WandOfDismantlingSettings
{
    /// <summary>The selection mode for this wand.</summary>
    public SelectionMode SelectionMode { get; set; } = SelectionMode.OneClick;

    /// <summary>The shape configuration.</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>Whether to destroy tiles.</summary>
    public bool DestroyTiles { get; set; } = true;

    /// <summary>Whether to destroy walls.</summary>
    public bool DestroyWalls { get; set; } = false;

    /// <summary>The starting point of the selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>The ending point of the selection (used in ThreeClick mode).</summary>
    public Point EndPoint { get; set; }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandOfDismantlingSettings Clone()
    {
        return new WandOfDismantlingSettings
        {
            SelectionMode = SelectionMode,
            Shape = Shape,
            DestroyTiles = DestroyTiles,
            DestroyWalls = DestroyWalls,
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
        DestroyTiles = true;
        DestroyWalls = false;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        string destruction = (DestroyTiles, DestroyWalls) switch
        {
            (true, true) => "Tiles & Walls",
            (true, false) => "Tiles Only",
            (false, true) => "Walls Only",
            (false, false) => "Nothing"
        };
        return $"{SelectionMode} - {destruction} - {Shape.GetDescription()}";
    }

    /// <summary>
    /// Validates all settings values.
    /// </summary>
    public void Validate()
    {
        Shape.Validate();
    }
}