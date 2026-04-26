using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Shorthand helpers for accessing localized chat messages.
/// All keys live under <c>Mods.WorldShapingWandsMod.Messages.*</c>.
/// </summary>
public static class Msg
{
    private const string Prefix = "Mods.WorldShapingWandsMod.Messages";

    /// <summary>
    /// When channeling over an area where the delimitation filter empties
    /// every execution, this is how many consecutive empty-filter frames
    /// must pass before the warning message is shown again (1 second).
    /// </summary>
    private const int ChannelingDelimitationWarningInterval = 60;

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

    // ── Null-result message with delimitation awareness ────────────────

    /// <summary>
    /// Shows a "zero result" chat message (e.g. "No tiles placed", "No wiring changes").
    /// <para>
    /// <b>Delimitation substitution:</b> If <see cref="DelimitationWandPlayer.LastFilterCausedEmpty"/>
    /// is <c>true</c>, the generic null message is REPLACED by the
    /// <c>DelimitationFilterActive</c> warning. This eliminates the old double-message
    /// problem where both the delimitation warning and the family-specific null message
    /// would appear simultaneously.
    /// </para>
    /// <para>
    /// <b>Stamp channeling rules:</b>
    /// <list type="bullet">
    ///   <item>First click (StampChannelTimer ≤ 1): message is ALWAYS shown.</item>
    ///   <item>Channeling repeats: generic null messages are suppressed to avoid spam.
    ///     However, if the delimitation filter has caused consecutive empty results
    ///     for ≥ 60 frames (1 second), the delimitation warning breaks through once
    ///     and the counter resets.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Error messages (missing items, bad config) should still use
    /// <c>Main.NewText</c> directly — they indicate actionable problems, not empty results.
    /// </para>
    /// </summary>
    /// <param name="wandPlayer">The player's wand state (null-safe: shows message if null).</param>
    /// <param name="key">Localization key under <c>Messages.*</c>.</param>
    /// <param name="color">Chat message color.</param>
    public static void ShowNullResult(WandPlayer wandPlayer, string key, Color color)
        => ShowNullResultCore(wandPlayer, Get(key), color);

    /// <summary>
    /// Overload that takes a pre-formatted message string instead of a localization key.
    /// Used when the null-result message is built dynamically (e.g., with context info).
    /// Same delimitation-aware and channeling-aware logic as <see cref="ShowNullResult"/>.
    /// </summary>
    public static void ShowNullResultRaw(WandPlayer wandPlayer, string message, Color color)
        => ShowNullResultCore(wandPlayer, message, color);

    /// <summary>
    /// Core implementation shared by <see cref="ShowNullResult"/> and
    /// <see cref="ShowNullResultRaw"/>. Handles delimitation substitution
    /// and stamp-channeling gating.
    /// </summary>
    private static void ShowNullResultCore(WandPlayer wandPlayer, string fallbackMessage, Color fallbackColor)
    {
        var swp = wandPlayer?.Player?.GetModPlayer<DelimitationWandPlayer>();
        bool delimitationCaused = swp?.LastFilterCausedEmpty == true;

        // ── Stamp channeling gate ────────────────────────────────
        if (wandPlayer?.IsStampChanneling == true)
        {
            bool isFirstClick = wandPlayer.StampChannelTimer <= 1;
            bool sustainedEmptyThreshold = delimitationCaused
                && swp.ConsecutiveEmptyFilterFrames >= ChannelingDelimitationWarningInterval;

            if (isFirstClick)
            {
                // First click always shows the message.
            }
            else if (sustainedEmptyThreshold)
            {
                // Reset counter so the warning fires at most once per second.
                swp.ResetConsecutiveEmptyFilterFrames();
            }
            else
            {
                // Normal channeling suppression — no message.
                return;
            }
        }

        // ── Delimitation substitution ────────────────────────────
        if (delimitationCaused)
        {
            Main.NewText(Get("DelimitationFilterActive"), WandColors.MsgWarning);
            swp.LastFilterCausedEmpty = false;
        }
        else
        {
            Main.NewText(fallbackMessage, fallbackColor);
        }
    }
}
