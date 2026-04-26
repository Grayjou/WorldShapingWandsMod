using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// Rate-limited chat-toast helper for the "ghost-choice" case: the user chose a
/// specific item via the InventoryView panel, but at execute-time that item
/// wasn't in inventory and a different item was substituted instead.
///
/// <para>This is the second-priority Phase 2 polish item per Cavendish's
/// re-ranked list (Note_WillowAsks_Steering.md, S7 inbox). Goal: prevent the
/// silent "wait, why did Adamantite Ore not place?" confusion when a choice
/// goes stale mid-build, without spamming chat on every wand swing.</para>
///
/// <para><b>Throttling</b>: keyed on the (chosenType, fallbackType) pair so that
/// switching wands or running out of multiple distinct choices each gets a chance
/// to surface, but a continuous stream of clicks with the same stale choice only
/// fires once every <see cref="ThrottleTicks"/> ticks (~2 s at 60 Hz).</para>
///
/// <para><b>Scope (S10)</b>: Wand of Building tile + wall execution paths.
/// Wand of Replacement and Wand of Torches use the same choice infrastructure
/// and the same hookpoint pattern but are intentionally left as a deferred
/// item for the follow-up session — they need their own per-side semantics
/// reviewed (target-choice vs source-choice in WoR; torch-pair pins in WoT).</para>
/// </summary>
public static class GhostChoiceToast
{
    /// <summary>~2 seconds at 60 Hz. Tuned to be long enough that a ten-tile
    /// click-burst against a stale choice prints exactly once, but short enough
    /// that the user still sees the toast on the next deliberate operation.</summary>
    private const int ThrottleTicks = 120;

    private static int _lastChoiceType;
    private static int _lastFallbackType;
    private static int _lastFireTick;

    /// <summary>
    /// Emit the toast if (a) a choice was set, (b) the resolved fallback item
    /// type differs, and (c) the throttle has elapsed for this exact
    /// (chosen, fallback) pair. Safe to call every frame; cheap on the fast path.
    /// </summary>
    /// <param name="chosenType">The user's chosen item type (nullable; no-op when null).</param>
    /// <param name="actualType">The item type actually selected by the inventory scan.</param>
    public static void TryEmit(int? chosenType, int actualType)
    {
        if (!chosenType.HasValue || chosenType.Value <= 0) return;
        if (actualType == chosenType.Value) return; // choice honored — no toast

        int now = (int)Main.GameUpdateCount;
        bool sameSituation = _lastChoiceType == chosenType.Value
                             && _lastFallbackType == actualType;
        if (sameSituation && now - _lastFireTick < ThrottleTicks)
            return;

        _lastChoiceType = chosenType.Value;
        _lastFallbackType = actualType;
        _lastFireTick = now;

        string chosenName = Lang.GetItemNameValue(chosenType.Value);
        string fallbackName = Lang.GetItemNameValue(actualType);
        string msg = Language.GetTextValue(
            "Mods.WorldShapingWandsMod.UI.Common.ChosenItemFallback",
            chosenName, fallbackName);
        Main.NewText(msg, WandPanelTheme.Colors.GhostChoiceToast);
    }
}
