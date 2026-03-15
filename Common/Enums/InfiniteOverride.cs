namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Per-type override for infinite resource mode.
/// </summary>
public enum InfiniteOverride
{
    /// <summary>
    /// Follows the master Enable Infinite Resource toggle.
    /// If the master toggle is ON, this type is infinite; if OFF, it is not.
    /// </summary>
    Default,

    /// <summary>
    /// Forces this type to always be infinite, regardless of the master toggle.
    /// </summary>
    ForceOn,

    /// <summary>
    /// Forces this type to never be infinite, regardless of the master toggle.
    /// </summary>
    ForceOff
}
