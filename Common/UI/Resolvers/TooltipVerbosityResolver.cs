using Terraria;
using Terraria.Localization;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.Resolvers;

/// <summary>
/// (Session 4, 2026-05-02 G-42) Helper to resolve tooltip localization keys
/// between long-form and short-form variants based on per-player verbosity preference.
///
/// <para>
/// The resolver supports a "tooltip verbosity" toggle that allows players to choose
/// between full-text descriptions (long keys) and concise summaries (short keys) for
/// wand tooltips displayed via the Help chrome button or other UI surfaces.
/// </para>
///
/// <para>
/// **Convention**: Short-key variants use the `_Short` suffix appended to the long key.
/// For example, given long key `"UI.Chrome.Help"`, the short variant is `"UI.Chrome.Help_Short"`.
/// If the short variant is not defined in the locale file, the resolver falls back to
/// the long key and logs a trace message (never crashes).
/// </para>
///
/// <para>
/// **Client-only**: This resolver is safe to use in any context (MP-safe; the player
/// preference is stored locally on WandPlayer and never broadcast). The resolved text
/// is typically dispatched to chat for display.
/// </para>
/// </summary>
public static class TooltipVerbosityResolver
{
    /// <summary>
    /// Resolves a tooltip key to either the long or short variant based on the player's
    /// verbosity preference.
    /// </summary>
    /// <param name="longKey">The full localization key (e.g., "UI.Chrome.Help").</param>
    /// <param name="player">The player whose verbosity preference is used. If null, defaults to long form.</param>
    /// <returns>
    /// The localized text for either the long key or the short variant (longKey + "_Short").
    /// If the short key is not defined, returns the long key text (no error).
    /// </returns>
    public static string ResolveTooltip(string longKey, Player player)
    {
        if (longKey == null)
            return "";

        // Null player → default to long form (safety fallback).
        if (player == null)
            return Language.GetTextValue(longKey);

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        if (wandPlayer == null)
            return Language.GetTextValue(longKey);

        // If verbosity is enabled (true = verbose = long form), use the long key.
        // If verbosity is disabled (false = concise = short form), try the short key.
        if (wandPlayer.TooltipVerbosityEnabled)
            return Language.GetTextValue(longKey);

        // Try the short variant. If it doesn't exist, fall back to long.
        string shortKey = longKey + "_Short";
        string shortText = Language.GetTextValue(shortKey);

        // If the short key resolved to itself (i.e., it wasn't found), Language.GetTextValue
        // returns the key as-is. We can check if it matches to detect this, but for safety,
        // we simply return the long text if we detect a likely key-not-found condition.
        // The Language API returns "[<localization key>]" for missing keys in debug mode;
        // in release mode it returns the key itself. We check for both patterns.
        if (shortText == shortKey || shortText.StartsWith("[") && shortText.EndsWith("]"))
        {
            // Short key not found — fall back to long.
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"TooltipVerbosityResolver: short key '{shortKey}' not found; falling back to long key '{longKey}'.");
            #endif
            return Language.GetTextValue(longKey);
        }

        return shortText;
    }

    /// <summary>
    /// Toggles the player's tooltip verbosity preference. Safe to call repeatedly.
    /// </summary>
    /// <param name="player">The player to toggle. If null, does nothing.</param>
    public static void ToggleVerbosity(Player player)
    {
        if (player == null)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        if (wandPlayer != null)
            wandPlayer.TooltipVerbosityEnabled = !wandPlayer.TooltipVerbosityEnabled;
    }
}
