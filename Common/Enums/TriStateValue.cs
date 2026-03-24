namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Represents a three-state toggle: Ignore, Apply (On), or Remove (Off).
/// Used for coatings (Illuminant/Echo) and any future Inherit/ForceOn/ForceOff patterns.
/// </summary>
public enum TriStateValue : byte
{
    /// <summary>Leave the property unchanged on target tiles.</summary>
    Ignore = 0,

    /// <summary>Apply / force the property on target tiles.</summary>
    Apply = 1,

    /// <summary>Remove / force the property off target tiles.</summary>
    Remove = 2
}

/// <summary>
/// Extension methods for <see cref="TriStateValue"/>.
/// </summary>
public static class TriStateExtensions
{
    /// <summary>Cycles to the next state: Ignore → Apply → Remove → Ignore.</summary>
    public static TriStateValue Next(this TriStateValue value) => value switch
    {
        TriStateValue.Ignore => TriStateValue.Apply,
        TriStateValue.Apply => TriStateValue.Remove,
        TriStateValue.Remove => TriStateValue.Ignore,
        _ => TriStateValue.Ignore
    };

    /// <summary>Returns whether this state means "active" (Apply or Remove).</summary>
    public static bool IsActive(this TriStateValue value) =>
        value != TriStateValue.Ignore;

    /// <summary>
    /// Converts the tri-state value to the legacy two-boolean representation.
    /// Returns (applyValue, ignoreValue).
    /// </summary>
    public static (bool apply, bool ignore) ToLegacyBools(this TriStateValue value) => value switch
    {
        TriStateValue.Ignore => (false, true),
        TriStateValue.Apply => (true, false),
        TriStateValue.Remove => (false, false),
        _ => (false, true)
    };

    /// <summary>
    /// Converts the legacy two-boolean representation to a <see cref="TriStateValue"/>.
    /// </summary>
    public static TriStateValue FromLegacyBools(bool apply, bool ignore)
    {
        if (ignore) return TriStateValue.Ignore;
        if (apply) return TriStateValue.Apply;
        return TriStateValue.Remove;
    }

    /// <summary>
    /// Returns a display suffix string for UI labels: "Ignore", "On", or "Off".
    /// </summary>
    public static string GetDisplaySuffix(this TriStateValue value) => value switch
    {
        TriStateValue.Ignore => "Ignore",
        TriStateValue.Apply => "On",
        TriStateValue.Remove => "Off",
        _ => "Ignore"
    };
}
