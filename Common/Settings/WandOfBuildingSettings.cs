using System.Collections.Generic;
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
    /// Per-<see cref="PlaceType"/> chosen source-item types for tile-mode placement
    /// (InventoryView v1 framework). Keyed by <see cref="PlaceType"/> so that
    /// a chosen Platform item does not persist when the user switches to Solid
    /// mode, Rope mode, etc. Each object type has an independent choice slot.
    ///
    /// <para>S1 2026-04-26 (bug fix): previously this was a single
    /// <c>int? ChosenTileItemType</c> shared across all tile sub-modes, which
    /// caused stale choices (e.g., Wood chosen in Solid mode showing as active
    /// in Platform mode, then failing to find any platform in inventory and
    /// blocking placement).</para>
    ///
    /// <para>Wall is excluded from this dictionary; wall choices live in
    /// <see cref="ChosenWallItemType"/> (separate because Wall mode uses a
    /// distinct execution path).</para>
    /// </summary>
    public Dictionary<PlaceType, int?> ChosenTileItemTypeByObjectType { get; set; } = new();

    /// <summary>
    /// Helper: get the chosen tile item type for the currently-active
    /// <see cref="Object"/> sub-mode. Returns <c>null</c> when no choice is
    /// set for this sub-mode (falls back to inventory hotbar scan).
    /// </summary>
    public int? GetChosenTileItemType(PlaceType objectType)
    {
        if (objectType == PlaceType.Wall) return null; // Wall has its own field.
        return ChosenTileItemTypeByObjectType.TryGetValue(objectType, out int? v) ? v : null;
    }

    /// <summary>
    /// Helper: set (or clear) the chosen tile item type for the given
    /// <see cref="PlaceType"/> sub-mode.
    /// </summary>
    public void SetChosenTileItemType(PlaceType objectType, int? itemType)
    {
        if (objectType == PlaceType.Wall) return; // Wall has its own field.
        if (itemType.HasValue)
            ChosenTileItemTypeByObjectType[objectType] = itemType;
        else
            ChosenTileItemTypeByObjectType.Remove(objectType);
    }

    /// <summary>
    /// chosen source-item type for wall placement. Same semantics as the tile
    /// dictionary but only consulted when the wand's active <see cref="Object"/>
    /// is <c>PlaceType.Wall</c>.
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
            ChosenTileItemTypeByObjectType = new Dictionary<PlaceType, int?>(ChosenTileItemTypeByObjectType),
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
        ChosenTileItemTypeByObjectType = new Dictionary<PlaceType, int?>();
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