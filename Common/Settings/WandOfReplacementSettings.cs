using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Replacement.
/// </summary>
public class WandOfReplacementSettings
{
    /// <summary>The selection mode for this wand.</summary>
    public SelectionMode SelectionMode { get; set; } = SelectionMode.OneClick;

    /// <summary>The type of object to place (replacement target).</summary>
    public ObjectType NewObject { get; set; } = ObjectType.Tile;

    /// <summary>The type of object to replace (source).</summary>
    public ObjectType OldObject { get; set; } = ObjectType.Tile;

    /// <summary>The shape configuration.</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>
    /// Tri-state paint-source selector for auto-painting replaced surfaces.
    /// <c>Off</c> = no auto-paint (default), <c>Inventory</c> = consume paint from inventory,
    /// <c>CoatingSettings</c> = use the colour stored in the Coating wand settings.
    /// When both PaintSprayer is active and PreservePaint is ON, PaintSprayer only paints
    /// tiles/walls that were previously unpainted (PreservePaint wins).
    /// </summary>
    public PaintSprayerSource PaintSprayer { get; set; } = PaintSprayerSource.Off;

    /// <summary>
    /// When true, the original paint color of each replaced tile/wall is preserved
    /// on the new tile/wall. PreservePaint takes precedence over PaintSprayer:
    /// if both are ON, PaintSprayer only applies to tiles that had no paint.
    /// </summary>
    public bool PreservePaint { get; set; } = true;

    /// <summary>
    /// When true, the target type tracks the source type ("Same Type" mode).
    /// This is set explicitly by the user clicking the Same Type button,
    /// NOT inferred from source == target equality.
    /// </summary>
    public bool SameTypeMode { get; set; } = false;

    /// <summary>
    /// chosen source-item type for the "find" half of replacement (InventoryView v1).
    /// When non-null, the wand searches inventory for this exact item type as the
    /// match-source instead of falling back to the broad <see cref="OldObject"/>
    /// category. Cleared (null) by default.
    /// </summary>
    public int? ChosenSourceItemType { get; set; }

    /// <summary>
    /// chosen target-item type for the "replace-with" half of replacement.
    /// Honored only when <see cref="SameTypeMode"/> is OFF (otherwise the source
    /// item type drives both sides). Cleared (null) by default.
    /// </summary>
    public int? ChosenTargetItemType { get; set; }

    /// <summary>The starting point of the selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>The ending point of the selection (used in ThreeClick mode).</summary>
    public Point EndPoint { get; set; }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandOfReplacementSettings Clone()
    {
        return new WandOfReplacementSettings
        {
            SelectionMode = SelectionMode,
            NewObject = NewObject,
            OldObject = OldObject,
            Shape = Shape,
            PaintSprayer = PaintSprayer,
            PreservePaint = PreservePaint,
            SameTypeMode = SameTypeMode,
            ChosenSourceItemType = ChosenSourceItemType,
            ChosenTargetItemType = ChosenTargetItemType,
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
        NewObject = ObjectType.Tile;
        OldObject = ObjectType.Tile;
        Shape = ShapeInfo.Default;
        PaintSprayer = PaintSprayerSource.Off;
        PreservePaint = true;
        SameTypeMode = false;
        ChosenSourceItemType = null;
        ChosenTargetItemType = null;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        return $"{SelectionMode} - {OldObject} â†’ {NewObject} - {Shape.GetDescription()}";
    }

    /// <summary>
    /// Validates all settings values.
    /// </summary>
    public void Validate()
    {
        Shape.Validate();
    }
}