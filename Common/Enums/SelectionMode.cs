namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the selection mode for wand operations.
/// </summary>
public enum SelectionMode : byte
{
    /// <summary>Click and drag to select area.</summary>
    OneClick = 0,

    /// <summary>Click start point, click end point.</summary>
    TwoClick = 1,

    /// <summary>Click start point, click end point, click to confirm.</summary>
    ThreeClick = 2,

    /// <summary>
    /// Click start, click end, click to lock as stamp, then click repeatedly to stamp.
    /// Right-click resets.
    /// </summary>
    FourClick = 3,
}