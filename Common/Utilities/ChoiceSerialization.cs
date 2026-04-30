using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Serialization helpers for InventoryView choice fields (<c>int? ChosenXItemType</c>).
///
/// <para>
/// Choices are stored on settings as raw <c>int?</c> for cheap runtime comparison,
/// but <c>int</c> item IDs are NOT stable across mod versions or load-order
/// changes. Serializing the raw int and deserializing it after a mod update or
/// reorder could silently re-choice the player to a wrong item — the worst kind
/// of bug because the choice would still appear valid.
/// </para>
///
/// <para>
/// Per <c>dev_notes/inbox/Response_2026-04-22 Letter #5.md</c> §8 (Cavendish),
/// choices are persisted as a <c>(ModName, ItemName)</c> tuple inside a nested
/// <see cref="TagCompound"/>:
/// </para>
///
/// <list type="bullet">
///   <item>Vanilla items: <c>ModName = "Terraria"</c>, <c>ItemName</c> = the
///   <see cref="ItemID"/> internal name (e.g. <c>"DirtBlock"</c>).</item>
///   <item>Modded items: <c>ModName</c> = the mod's internal name,
///   <c>ItemName</c> = <see cref="ModItem.Name"/>.</item>
/// </list>
///
/// <para>
/// On load, unresolvable tuples (mod uninstalled, item renamed, vanilla ID
/// removed) silently resolve to <c>null</c> — the wand reverts to its
/// pre-choice "first eligible item" behaviour, which is the desired graceful
/// degradation.
/// </para>
/// </summary>
public static class ChoiceSerialization
{
    private const string KeyModName = "Mod";
    private const string KeyItemName = "Name";

    /// <summary>
    /// Serializes a chosen item type to a <see cref="TagCompound"/>, or
    /// <c>null</c> if the choice is unset / invalid.
    /// </summary>
    /// <param name="itemType">The chosen item type (raw int as stored on
    /// settings), or <c>null</c> for "no choice".</param>
    /// <returns>A tag compound holding <c>(ModName, ItemName)</c>, or
    /// <c>null</c> if there is nothing to save.</returns>
    public static TagCompound SaveChoice(int? itemType)
    {
        if (itemType is not int t || t <= 0 || t >= ItemLoader.ItemCount)
            return null;

        string modName;
        string itemName;

        if (t < ItemID.Count)
        {
            // Vanilla item: ItemID.Search.GetName resolves the int → internal
            // name (e.g. ItemID.DirtBlock=2 → "DirtBlock").
            modName = "Terraria";
            itemName = ItemID.Search.GetName(t);
        }
        else
        {
            // Modded item: ItemLoader.GetItem returns the ModItem instance.
            ModItem mi = ItemLoader.GetItem(t);
            if (mi == null) return null;
            modName = mi.Mod.Name;
            itemName = mi.Name;
        }

        return new TagCompound
        {
            [KeyModName] = modName,
            [KeyItemName] = itemName,
        };
    }

    /// <summary>
    /// Deserializes a previously-saved choice tag back to an <c>int?</c> item type.
    /// Returns <c>null</c> if the tag is missing/malformed, or if the item it
    /// references can no longer be resolved (mod uninstalled, item renamed,
    /// vanilla ID removed across game updates).
    /// </summary>
    public static int? LoadChoice(TagCompound tag)
    {
        if (tag == null) return null;
        if (!tag.ContainsKey(KeyModName) || !tag.ContainsKey(KeyItemName))
            return null;

        string modName = tag.GetString(KeyModName);
        string itemName = tag.GetString(KeyItemName);
        if (string.IsNullOrEmpty(modName) || string.IsNullOrEmpty(itemName))
            return null;

        if (modName == "Terraria")
        {
            if (ItemID.Search.TryGetId(itemName, out int vid))
                return vid;
            return null;
        }

        if (ModLoader.TryGetMod(modName, out Mod mod)
            && mod.TryFind<ModItem>(itemName, out ModItem mi))
        {
            return mi.Type;
        }

        // Unresolvable → graceful degradation per design.
        return null;
    }
}
