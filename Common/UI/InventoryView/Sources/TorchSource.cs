using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.InventoryView.Sources;

/// <summary>
/// InventoryView source for the Wand of Torches.
/// Candidate set is every inventory item that places a tile in
/// <c>TileID.Sets.Torch</c>. Biome-torch / Echo coating concerns are handled
/// by the wand's own settings panel — this source only governs the
/// "which torch item to consume" chosen choice.
/// </summary>
public sealed class TorchSource : IInventoryViewSource
{
    public string TitleKey => "UI.InventoryView.Torches.Title";

    public IEnumerable<int> GetCandidateItemTypes(Player player)
    {
        for (int i = 0; i < 50; i++)
        {
            Item it = player.inventory[i];
            if (it == null || it.IsAir) continue;
            if (it.createTile <= TileID.Dirt - 1) continue;
            if (!TileID.Sets.Torch[it.createTile]) continue;
            yield return it.type;
        }
    }

    public int? GetSelectedItemType(WandPlayer wp)
        => wp.TorchSettings.ChosenTorchItemType;

    public void SetSelectedItemType(WandPlayer wp, int? itemType)
        => wp.TorchSettings.ChosenTorchItemType = itemType;

    public IEnumerable<int> GetPinnedItemTypes(WandPlayer wp)
        => wp.TorchSettings.PinnedTorchItemTypes;

    public bool IsPinned(WandPlayer wp, int itemType)
        => wp.TorchSettings.PinnedTorchItemTypes.Contains(itemType);

    public void TogglePin(WandPlayer wp, int itemType)
        => wp.TorchSettings.TogglePinnedTorchItemType(itemType);
}
