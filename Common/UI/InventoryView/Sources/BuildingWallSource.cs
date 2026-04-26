using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.UI.InventoryView.Sources;

/// <summary>
/// InventoryView source for the Wand of Building's wall half.
/// Always emits the player's wall items — the candidate set is decoupled from
/// <c>BuildingSettings.Object</c>. The choice only influences execution when
/// <c>WandOfBuildingBase.WallExecution</c> runs.
///
/// <para>S6 2026-04-22 (history): previously this source gated on
/// <c>Object == PlaceType.Wall</c>, which meant `/wsw iv` reported zero
/// walls whenever the wand was in tile mode (the default). The user reported
/// that Gemspark Diamond Wall and Boreal Wood Wall were invisible to the panel
/// even though both were sitting in inventory. The S6 fix decoupled the
/// source from Object mode.</para>
///
/// <para>S9 2026-04-22 (Cavendish patch — registry-side simplification):
/// the in-game UI path collapses the Building panel to the active-mode
/// section via <see cref="InventoryViewRegistry.GetProvider(Player)"/>, so
/// in normal play this source is invoked only when
/// <c>Object == PlaceType.Wall</c>. The decoupled emission is preserved for
/// the mode-agnostic <c>GetProviderForFamily</c> overload (diagnostics / tests).</para>
/// </summary>
public sealed class BuildingWallSource : IInventoryViewSource
{
    public string TitleKey => "UI.InventoryView.Building.WallTitle";

    public IEnumerable<int> GetCandidateItemTypes(Player player)
    {
        var condition = ItemTypeHelper.GetConditions(PlaceType.Wall);
        for (int i = 0; i < 50; i++)
        {
            Item it = player.inventory[i];
            if (it == null || it.IsAir) continue;
            if (!condition(it)) continue;
            yield return it.type;
        }
    }

    public int? GetSelectedItemType(WandPlayer wp)
        => wp.BuildingSettings.ChosenWallItemType;

    public void SetSelectedItemType(WandPlayer wp, int? itemType)
        => wp.BuildingSettings.ChosenWallItemType = itemType;
}
