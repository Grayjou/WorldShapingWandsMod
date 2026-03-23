namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines how the user commits a wand operation after the shape is defined.
/// Separated from shape input (point count) to support multi-point shapes.
/// </summary>
/// <remarks>
/// Previously, <see cref="SelectionMode"/> conflated the number of UI
/// interactions with the confirmation semantics. This enum captures only
/// the confirmation behavior, while the number of points required is
/// determined by <see cref="WorldShapingWandsMod.Common.Geometry.ShapeRegistry.GetRequiredPoints"/>.
/// 
/// Total clicks = ShapeRegistry.GetRequiredPoints(shape) + ConfirmationMode clicks:
///   Immediate = 0 extra clicks (execute on last shape point)
///   Confirm   = 1 extra click  (preview then confirm)
///   Stamp     = 1 extra click  (preview then stamp repeatedly)
/// </remarks>
public enum ConfirmationMode : byte
{
    /// <summary>
    /// Execute immediately when the shape is fully defined.
    /// Maps to legacy OneClick (Instant) and TwoClick (Select) modes.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Show a preview after shape definition, require a confirm click to execute.
    /// Maps to legacy ThreeClick (Confirm) mode.
    /// </summary>
    Confirm = 1,

    /// <summary>
    /// Show a preview after shape definition, allow stamping the shape repeatedly.
    /// Maps to legacy FourClick (Stamp) mode. Right-click cancels.
    /// </summary>
    Stamp = 2,
}
