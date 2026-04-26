using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.UI.InventoryView.Sources;

/// <summary>
/// InventoryView source for the "find" half of Wand of Replacement.
/// Candidate set tracks <c>ReplacementSettings.OldObject</c>; selection
/// chooses which exact item type the wand should match in the world. When
/// <c>SameTypeMode</c> is ON, this source's choice also drives the target side.
///
/// <para>S9 (GrayJou Letter #9): the currently-chosen <em>target</em> item
/// is excluded from this source's candidate set. The user observed that
/// choosing the same item on both sides produced confusing behaviour (the
/// cursor icon updated but the wand kept the previous target, and the
/// operation was a no-op even though it visually appeared otherwise).
/// Filtering at the panel layer makes the bad state unreachable through
/// the UI — cleaner than handling source==target as a runtime no-op.
/// SameTypeMode is a separate gesture (the SameType button) and is unaffected.</para>
/// </summary>
public sealed class ReplacementSourceSource : IInventoryViewSource
{
    public string TitleKey => "UI.InventoryView.Replacement.SourceTitle";

    public IEnumerable<int> GetCandidateItemTypes(Player player)
    {
        WandPlayer wp = player.GetModPlayer<WandPlayer>();
        ObjectType obj = wp.ReplacementSettings.OldObject;
        if (obj == ObjectType.Air) yield break; // Air has no inventory representative.

        // (S1 2026-04-26 fix): target exclusion is now per-ObjectType aware.
        int? excludeTarget = wp.ReplacementSettings.GetChosenTargetItemType(wp.ReplacementSettings.NewObject);
        var condition = ItemTypeHelper.GetConditions(obj);
        for (int i = 0; i < 50; i++)
        {
            Item it = player.inventory[i];
            if (it == null || it.IsAir) continue;
            if (!condition(it)) continue;
            if (obj != ObjectType.Wall && ItemTypeHelper.IsMultiTileItem(it)) continue;
            // S9: exclude the target choice so source↔target collision is unreachable via UI.
            if (excludeTarget.HasValue && it.type == excludeTarget.Value) continue;
            yield return it.type;
        }
    }

    public int? GetSelectedItemType(WandPlayer wp)
    {
        // (S1 2026-04-26 fix): independent slot per OldObject sub-mode.
        return wp.ReplacementSettings.GetChosenSourceItemType(wp.ReplacementSettings.OldObject);
    }

    public void SetSelectedItemType(WandPlayer wp, int? itemType)
        => wp.ReplacementSettings.SetChosenSourceItemType(wp.ReplacementSettings.OldObject, itemType);
}
