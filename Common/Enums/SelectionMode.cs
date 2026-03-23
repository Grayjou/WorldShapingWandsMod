namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the selection mode for wand operations.
/// </summary>
/// <remarks>
/// This enum conflates the number of UI interactions with selection semantics.
/// New code should prefer <see cref="ConfirmationMode"/> (for confirmation behavior)
/// combined with <see cref="WorldShapingWandsMod.Common.Geometry.ShapeRegistry.GetRequiredPoints"/>
/// (for shape point count). Use <see cref="SelectionModeExtensions.ToConfirmationMode"/>
/// to convert.
/// </remarks>
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

/// <summary>
/// Extension methods for <see cref="SelectionMode"/> to bridge to the new
/// <see cref="ConfirmationMode"/> system.
/// </summary>
public static class SelectionModeExtensions
{
    /// <summary>
    /// Converts a legacy <see cref="SelectionMode"/> to its equivalent
    /// <see cref="ConfirmationMode"/>. OneClick and TwoClick both map to
    /// Immediate (the difference is in input handling, not confirmation).
    /// </summary>
    public static ConfirmationMode ToConfirmationMode(this SelectionMode mode) => mode switch
    {
        SelectionMode.OneClick => ConfirmationMode.Immediate,
        SelectionMode.TwoClick => ConfirmationMode.Immediate,
        SelectionMode.ThreeClick => ConfirmationMode.Confirm,
        SelectionMode.FourClick => ConfirmationMode.Stamp,
        _ => ConfirmationMode.Immediate,
    };

    /// <summary>
    /// Returns true if this selection mode uses instant execution (no preview/confirm step).
    /// </summary>
    public static bool IsImmediate(this SelectionMode mode) =>
        mode is SelectionMode.OneClick or SelectionMode.TwoClick;
}
