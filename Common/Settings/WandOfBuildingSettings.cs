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
    /// Tri-state paint-source selector for auto-painting placed surfaces.
    /// <c>Off</c> = no auto-paint (default), <c>Inventory</c> = consume paint from inventory
    /// (vanilla Paint Sprayer parity), <c>CoatingSettings</c> = use the colour stored in the
    /// player's Coating wand settings (no inventory consumption).
    /// </summary>
    public PaintSprayerSource PaintSprayer { get; set; } = PaintSprayerSource.Off;

    /// <summary>
    /// Tri-state actuation toggle:
    ///   <c>null</c> = Ignore (leave actuation state unchanged — default),
    ///   <c>true</c>  = Actuate ON (placed tiles become actuated/pass-through),
    ///   <c>false</c> = Actuate OFF (placed tiles become solid/de-actuated).
    /// </summary>
    public bool? Actuation { get; set; } = null;

    /// <summary>
    /// chosen source-item type for tile placement (InventoryView v1 framework).
    /// When non-null, the wand prefers the user's explicit choice over
    /// FindFirstItemIndex's hotbar-scan fallback. Cleared (null) by default;
    /// the panel's [✕ Clear pin] footer button or a wand-family swap may reset it.
    /// Read by <c>BuildingTileSource.GetSelectedItemType</c>; written by
    /// <c>InventoryViewPanel</c> (UI lands in a later session).
    /// </summary>
    public int? ChosenTileItemType { get; set; }

    /// <summary>
    /// chosen source-item type for wall placement. Same semantics as
    /// <see cref="ChosenTileItemType"/> but only consulted when the wand's
    /// active <see cref="Object"/> is <c>PlaceType.Wall</c>.
    /// </summary>
    public int? ChosenWallItemType { get; set; }

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
            PaintSprayer = PaintSprayer,
            Actuation = Actuation,
            ChosenTileItemType = ChosenTileItemType,
            ChosenWallItemType = ChosenWallItemType,
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
        PaintSprayer = PaintSprayerSource.Off;
        Actuation = null;
        ChosenTileItemType = null;
        ChosenWallItemType = null;
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