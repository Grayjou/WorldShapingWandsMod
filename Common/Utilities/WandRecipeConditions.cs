using System.Collections.Generic;
using Terraria;

namespace WorldShapingWandsMod.Common.Utilities;

public static class WandRecipeConditions
{
    /// <summary>
    /// A recipe condition whose predicate is always <c>false</c>.
    /// Used exclusively to attach shimmer-decraft results to non-instant wand
    /// variants without the recipe ever appearing in a crafting station.
    /// </summary>
    public static readonly Condition NonCraftable =
        new("Mods.WorldShapingWandsMod.Conditions.NonCraftable", () => false);

    // ── Mode-variant registry ────────────────────────────────────────────────
    // Each non-instant wand calls Register(Type) from its AddRecipes() so that
    // BaseCyclingWand.ModifyTooltips can replace the misleading crafting tooltip
    // lines with an accurate "obtained via right-click cycling" hint.
    // We use a HashSet so lookup from ModifyTooltips is O(1).
    private static readonly HashSet<int> _nonCraftableTypes = new();

    /// <summary>Call once from <c>AddRecipes()</c> on every non-instant wand variant.</summary>
    public static void Register(int itemType) => _nonCraftableTypes.Add(itemType);

    /// <summary>Returns <c>true</c> when the item type was registered as non-craftable.</summary>
    public static bool IsNonCraftable(int itemType) => _nonCraftableTypes.Contains(itemType);
}
