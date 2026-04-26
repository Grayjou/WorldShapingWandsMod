using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.UI.InventoryView.Sources;

/// <summary>
/// InventoryView source for the Wand of Building's tile half.
/// Candidate set tracks the player's current <c>BuildingSettings.Object</c>
/// selection (Solid / Platform / Rope / Rail / GrassSeed / PlantPot). When
/// the user has flipped <c>Object</c> to <c>Wall</c>, this source falls back
/// to <c>PlaceType.Solid</c> so the tile choice grid still lists something
/// useful. Multi-tile items (furniture etc.) are excluded.
///
/// <para>S6 2026-04-22 (history): previously this source returned an empty
/// set whenever <c>Object == PlaceType.Wall</c>, which created an asymmetry
/// where neither half was visible during the other half's mode. The S6 fix
/// decoupled the source from Object mode so both halves were always populated.</para>
///
/// <para>S9 2026-04-22 (Cavendish patch supersedes S6 contract): the
/// in-game UI now collapses to the active mode via
/// <see cref="InventoryViewRegistry.GetProvider(Player)"/>. Under the new
/// contract this source is never invoked from in-game UI while
/// <c>Object == Wall</c>, so the wall-mode shim below is now dead code under
/// normal play. The shim is kept defensively because the mode-agnostic
/// <c>GetProviderForFamily</c> overload (used by diagnostics / tests) still
/// returns both Building sources regardless of mode.</para>
/// </summary>
public sealed class BuildingTileSource : IInventoryViewSource
{
    public string TitleKey => "UI.InventoryView.Building.TileTitle";

    public IEnumerable<int> GetCandidateItemTypes(Player player)
    {
        WandPlayer wp = player.GetModPlayer<WandPlayer>();
        PlaceType obj = wp.BuildingSettings.Object;
        // In wall mode, fall back to Solid so the tile grid still has something
        // to show — keeps the InventoryView panel coherent across mode flips.
        if (obj == PlaceType.Wall) obj = PlaceType.Solid;

        var baseCondition = ItemTypeHelper.GetConditions(obj);
        for (int i = 0; i < 50; i++)
        {
            Item it = player.inventory[i];
            if (it == null || it.IsAir) continue;
            if (!baseCondition(it)) continue;
            if (ItemTypeHelper.IsMultiTileItem(it)) continue;
            yield return it.type;
        }
    }

    public int? GetSelectedItemType(WandPlayer wp)
        => wp.BuildingSettings.ChosenTileItemType;

    public void SetSelectedItemType(WandPlayer wp, int? itemType)
        => wp.BuildingSettings.ChosenTileItemType = itemType;
}
