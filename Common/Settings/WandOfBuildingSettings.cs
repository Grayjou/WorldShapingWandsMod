using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Building.
/// </summary>
public class WandOfBuildingSettings
{
    /// <summary>The selection mode for this wand.</summary>
    public SelectionMode SelectionMode { get; set; } = SelectionMode.OneClick;

    /// <summary>The type of object to place.</summary>
    public PlaceType Object { get; set; } = PlaceType.Solid;

    /// <summary>The slope configuration for placed tiles.</summary>
    public SlopeType Slope { get; set; } = SlopeType.Default;

    /// <summary>
    /// When true, the selected slope is always applied to placed/replaced tiles.
    /// When false, slopes are left as-is (existing tiles keep their slope, new tiles get default).
    /// </summary>
    public bool OverwriteSlope { get; set; } = true;

    /// <summary>The shape configuration.</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>
    /// What happens when the player runs out of blocks mid-placement.
    /// </summary>
    public BlockExhaustionMode ExhaustionMode { get; set; } = BlockExhaustionMode.NextBlock;

    /// <summary>
    /// When true, after placing each tile/wall, the wand automatically applies the
    /// first paint colour found in the player's inventory to the placed surface.
    /// </summary>
    public bool PaintSprayer { get; set; } = false;

    /// <summary>
    /// Tri-state actuation toggle:
    ///   <c>null</c> = Ignore (leave actuation state unchanged — default),
    ///   <c>true</c>  = Actuate ON (placed tiles become actuated/pass-through),
    ///   <c>false</c> = Actuate OFF (placed tiles become solid/de-actuated).
    /// </summary>
    public bool? Actuation { get; set; } = null;

    /// <summary>The starting point of the selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>The ending point of the selection (used in ThreeClick mode).</summary>
    public Point EndPoint { get; set; }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandOfBuildingSettings Clone()
    {
        return new WandOfBuildingSettings
        {
            SelectionMode = SelectionMode,
            Object = Object,
            Slope = Slope,
            OverwriteSlope = OverwriteSlope,
            Shape = Shape,
            ExhaustionMode = ExhaustionMode,
            PaintSprayer = PaintSprayer,
            Actuation = Actuation,
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
        Object = PlaceType.Solid;
        Slope = SlopeType.Default;
        OverwriteSlope = true;
        Shape = ShapeInfo.Default;
        ExhaustionMode = BlockExhaustionMode.NextBlock;
        PaintSprayer = false;
        Actuation = null;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        return $"{SelectionMode} - {Object} ({Slope}) - {Shape.GetDescription()}";
    }

    /// <summary>
    /// Validates all settings values.
    /// </summary>
    public void Validate()
    {
        Shape.Validate();
    }
}