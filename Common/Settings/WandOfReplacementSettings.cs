using System.Collections.Generic;
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
    /// Per-<see cref="ObjectType"/> chosen source-item type for the "find" half
    /// of replacement (InventoryView v1 framework). Keyed by
    /// <see cref="ObjectType"/> so that choosing a wall item in Wall source mode
    /// does not carry over when the user switches to Tile or Platform source mode.
    ///
    /// <para>S1 2026-04-26 (bug fix): previously a single field shared across all
    /// object types, causing stale choices to persist across mode switches.</para>
    /// </summary>
    public Dictionary<ObjectType, int?> ChosenSourceItemTypeByObjectType { get; set; } = new();

    /// <summary>Helper: get the chosen source item type for the given object sub-mode.</summary>
    public int? GetChosenSourceItemType(ObjectType objectType)
        => ChosenSourceItemTypeByObjectType.TryGetValue(objectType, out int? v) ? v : null;

    /// <summary>Helper: set or clear the chosen source item type for the given object sub-mode.</summary>
    public void SetChosenSourceItemType(ObjectType objectType, int? itemType)
    {
        if (itemType.HasValue)
            ChosenSourceItemTypeByObjectType[objectType] = itemType;
        else
            ChosenSourceItemTypeByObjectType.Remove(objectType);
    }

    /// <summary>
    /// Per-<see cref="ObjectType"/> chosen target-item type for the "replace-with"
    /// half of replacement. Honored only when <see cref="SameTypeMode"/> is OFF.
    /// Keyed by <see cref="ObjectType"/> for the same isolation reason as
    /// <see cref="ChosenSourceItemTypeByObjectType"/>.
    /// </summary>
    public Dictionary<ObjectType, int?> ChosenTargetItemTypeByObjectType { get; set; } = new();

    /// <summary>Helper: get the chosen target item type for the given object sub-mode.</summary>
    public int? GetChosenTargetItemType(ObjectType objectType)
        => ChosenTargetItemTypeByObjectType.TryGetValue(objectType, out int? v) ? v : null;

    /// <summary>Helper: set or clear the chosen target item type for the given object sub-mode.</summary>
    public void SetChosenTargetItemType(ObjectType objectType, int? itemType)
    {
        if (itemType.HasValue)
            ChosenTargetItemTypeByObjectType[objectType] = itemType;
        else
            ChosenTargetItemTypeByObjectType.Remove(objectType);
    }

    // ── PersistentPin storage (S15 2026-04-28) ───────────────────────────────
    public Dictionary<ObjectType, HashSet<int>> PinnedSourceItemTypesByObjectType { get; set; } = new();
    public Dictionary<ObjectType, HashSet<int>> PinnedTargetItemTypesByObjectType { get; set; } = new();

    public HashSet<int> GetPinnedSourceItemTypes(ObjectType objectType)
    {
        if (!PinnedSourceItemTypesByObjectType.TryGetValue(objectType, out var set))
        {
            set = new HashSet<int>();
            PinnedSourceItemTypesByObjectType[objectType] = set;
        }
        return set;
    }

    public HashSet<int> GetPinnedTargetItemTypes(ObjectType objectType)
    {
        if (!PinnedTargetItemTypesByObjectType.TryGetValue(objectType, out var set))
        {
            set = new HashSet<int>();
            PinnedTargetItemTypesByObjectType[objectType] = set;
        }
        return set;
    }

    public void TogglePinnedSourceItemType(ObjectType objectType, int itemType)
    {
        var set = GetPinnedSourceItemTypes(objectType);
        if (!set.Add(itemType)) set.Remove(itemType);
    }

    public void TogglePinnedTargetItemType(ObjectType objectType, int itemType)
    {
        var set = GetPinnedTargetItemTypes(objectType);
        if (!set.Add(itemType)) set.Remove(itemType);
    }

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
            ChosenSourceItemTypeByObjectType = new Dictionary<ObjectType, int?>(ChosenSourceItemTypeByObjectType),
            ChosenTargetItemTypeByObjectType = new Dictionary<ObjectType, int?>(ChosenTargetItemTypeByObjectType),
            PinnedSourceItemTypesByObjectType = ClonePinDict(PinnedSourceItemTypesByObjectType),
            PinnedTargetItemTypesByObjectType = ClonePinDict(PinnedTargetItemTypesByObjectType),
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    private static Dictionary<ObjectType, HashSet<int>> ClonePinDict(Dictionary<ObjectType, HashSet<int>> src)
    {
        var dst = new Dictionary<ObjectType, HashSet<int>>(src.Count);
        foreach (var kv in src) dst[kv.Key] = new HashSet<int>(kv.Value);
        return dst;
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
        ChosenSourceItemTypeByObjectType = new Dictionary<ObjectType, int?>();
        ChosenTargetItemTypeByObjectType = new Dictionary<ObjectType, int?>();
        PinnedSourceItemTypesByObjectType = new Dictionary<ObjectType, HashSet<int>>();
        PinnedTargetItemTypesByObjectType = new Dictionary<ObjectType, HashSet<int>>();
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