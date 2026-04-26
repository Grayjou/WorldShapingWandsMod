using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.UI.InventoryView.Sources;

/// <summary>
/// InventoryView source for the "replace-with" half of Wand of Replacement.
/// Candidate set tracks <c>ReplacementSettings.NewObject</c>. Honored only
/// when <c>SameTypeMode</c> is OFF — when ON, the source choice drives both sides
/// and the panel renders this grid greyed-out (UI-side concern, later session).
///
/// <para>S9 (GrayJou Letter #9): mirror of the source-side exclusion — the
/// currently-chosen <em>source</em> item is hidden from this candidate set so
/// the user cannot choose the same item on both sides through the UI. See
/// <see cref="ReplacementSourceSource"/> for the rationale.</para>
/// </summary>
public sealed class ReplacementTargetSource : IInventoryViewSource
{
    public string TitleKey => "UI.InventoryView.Replacement.TargetTitle";

    public IEnumerable<int> GetCandidateItemTypes(Player player)
    {
        WandPlayer wp = player.GetModPlayer<WandPlayer>();
        ObjectType obj = wp.ReplacementSettings.NewObject;
        if (obj == ObjectType.Air) yield break; // Air-target = removal; no item choice needed.

        // (S1 2026-04-26 fix): source exclusion is now per-ObjectType aware.
        int? excludeSource = wp.ReplacementSettings.GetChosenSourceItemType(wp.ReplacementSettings.OldObject);
        var condition = ItemTypeHelper.GetConditions(obj);
        for (int i = 0; i < 50; i++)
        {
            Item it = player.inventory[i];
            if (it == null || it.IsAir) continue;
            if (!condition(it)) continue;
            if (obj != ObjectType.Wall && ItemTypeHelper.IsMultiTileItem(it)) continue;
            // S9: exclude the source choice (mirror of ReplacementSourceSource).
            if (excludeSource.HasValue && it.type == excludeSource.Value) continue;
            yield return it.type;
        }
    }

    public int? GetSelectedItemType(WandPlayer wp)
    {
        // (S1 2026-04-26 fix): independent slot per NewObject sub-mode.
        return wp.ReplacementSettings.GetChosenTargetItemType(wp.ReplacementSettings.NewObject);
    }

    public void SetSelectedItemType(WandPlayer wp, int? itemType)
        => wp.ReplacementSettings.SetChosenTargetItemType(wp.ReplacementSettings.NewObject, itemType);
}
