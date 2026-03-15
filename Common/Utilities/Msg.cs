using Terraria.Localization;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Shorthand helpers for accessing localized chat messages.
/// All keys live under <c>Mods.WorldShapingWandsMod.Messages.*</c>.
/// </summary>
public static class Msg
{
    private const string Prefix = "Mods.WorldShapingWandsMod.Messages";

    /// <summary>Get a localized message with no parameters.</summary>
    public static string Get(string key) =>
        Language.GetTextValue($"{Prefix}.{key}");

    /// <summary>Get a localized message with format parameters.</summary>
    public static string Get(string key, object arg0) =>
        Language.GetTextValue($"{Prefix}.{key}", arg0);

    /// <summary>Get a localized message with format parameters.</summary>
    public static string Get(string key, object arg0, object arg1) =>
        Language.GetTextValue($"{Prefix}.{key}", arg0, arg1);

    /// <summary>Get a localized message with format parameters.</summary>
    public static string Get(string key, object arg0, object arg1, object arg2) =>
        Language.GetTextValue($"{Prefix}.{key}", arg0, arg1, arg2);
}
