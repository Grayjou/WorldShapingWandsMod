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
    /// When true, after replacing each tile/wall, the wand automatically applies the
    /// first paint colour found in the player's inventory to the replaced surface.
    /// </summary>
    public bool PaintSprayer { get; set; } = false;

    /// <summary>
    /// When true, the target type tracks the source type ("Same Type" mode).
    /// This is set explicitly by the user clicking the Same Type button,
    /// NOT inferred from source == target equality.
    /// </summary>
    public bool SameTypeMode { get; set; } = false;

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
            SameTypeMode = SameTypeMode,
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
        PaintSprayer = false;
        SameTypeMode = false;
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