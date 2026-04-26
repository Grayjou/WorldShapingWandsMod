#nullable enable
using System;
using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI.InventoryView.Sources;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// Maps the player's currently-held wand to the <see cref="IInventoryViewProvider"/>
/// that should populate the InventoryView panel. Returns <c>null</c> for
/// non-participating wand families (Wiring, Coating, Fluids, Dismantling,
/// Safekeeping, Delimitation, Molding) — the panel toggle in their UIs will
/// render disabled with a tooltip explanation when the panel ships.
///
/// <para><b>Participation rule</b> (per S5 2026-04-22 client direction):
/// only wands that fetch positionally from inventory participate.
/// That's exactly Building (Tile + Wall), Torches, and Replacement
/// (Source + Target). Wiring uses fixed Wire/Actuator items, Coating uses
/// the palette picker, Fluids uses bucket-as-ammo without positional logic,
/// and the remaining families don't consume inventory items at all.</para>
///
/// <para>Provider instances are constructed on demand and stateless; sources
/// hold no per-call state, so allocations are cheap and the registry can
/// be called every frame from UI render code without caching.</para>
/// </summary>
public static class InventoryViewRegistry
{
    // Singleton sources — stateless, so safe to share across providers.
    private static readonly BuildingTileSource _buildingTile = new();
    private static readonly BuildingWallSource _buildingWall = new();
    private static readonly TorchSource _torch = new();
    private static readonly ReplacementSourceSource _replacementSource = new();
    private static readonly ReplacementTargetSource _replacementTarget = new();

    /// <summary>All concrete sources, indexed by family for diagnostics.</summary>
    public static IReadOnlyDictionary<WandFamily, IInventoryViewSource[]> AllSources { get; } =
        new Dictionary<WandFamily, IInventoryViewSource[]>
        {
            [WandFamily.Building]    = new IInventoryViewSource[] { _buildingTile, _buildingWall },
            [WandFamily.Torches]     = new IInventoryViewSource[] { _torch },
            [WandFamily.Replacement] = new IInventoryViewSource[] { _replacementSource, _replacementTarget },
        };

    /// <summary>
    /// Returns the provider for the player's currently-held wand, or <c>null</c>
    /// if the held wand's family does not participate in InventoryView.
    ///
    /// <para><b>Building special-case (S9 2026-04-22, Cavendish patch):</b>
    /// the Building family collapses to a single section matching the active
    /// <see cref="WandOfBuildingSettings.Object"/> mode. Showing both Tile and
    /// Wall sections simultaneously was visual noise — the wand only places one
    /// object type at a time, and switching the wand's mode (right-click cycle)
    /// flips the panel content on the next frame because the panel calls
    /// <c>GetProvider</c> per-frame in its <c>Update()</c>. Both choice fields
    /// (<c>ChosenTileItemType</c>, <c>ChosenWallItemType</c>) are still held in
    /// settings and survive mode flips invisibly. The mode-agnostic
    /// <see cref="GetProviderForFamily"/> overload preserves the both-sources
    /// shape for diagnostics / tests.</para>
    /// </summary>
    public static IInventoryViewProvider? GetProvider(Player player)
    {
        if (player == null) return null;
        var family = BaseCyclingWand.GetCurrentFamily(player);

        // Building special-case: pick the section that matches the active mode.
        if (family == WandFamily.Building)
        {
            var wp = player.GetModPlayer<WandPlayer>();
            return wp.BuildingSettings.Object == PlaceType.Wall
                ? new SimpleProvider("UI.InventoryView.Building.PanelTitle", _buildingWall)
                : new SimpleProvider("UI.InventoryView.Building.PanelTitle", _buildingTile);
        }

        return GetProviderForFamily(family);
    }

    /// <summary>
    /// Returns the provider for a given wand family, or <c>null</c> if the
    /// family does not participate. Mode-agnostic — for the Building family
    /// this returns BOTH Tile and Wall sources; the in-game UI path uses
    /// <see cref="GetProvider(Player)"/> which collapses to the active mode.
    /// </summary>
    public static IInventoryViewProvider? GetProviderForFamily(WandFamily family) => family switch
    {
        WandFamily.Building =>
            new SimpleProvider("UI.InventoryView.Building.PanelTitle", _buildingTile, _buildingWall),
        WandFamily.Torches =>
            new SimpleProvider("UI.InventoryView.Torches.PanelTitle", _torch),
        WandFamily.Replacement =>
            new SimpleProvider("UI.InventoryView.Replacement.PanelTitle", _replacementSource, _replacementTarget),
        _ => null,
    };

    /// <summary>Returns true if the given family contributes any source.</summary>
    public static bool Participates(WandFamily family) => family switch
    {
        WandFamily.Building or WandFamily.Torches or WandFamily.Replacement => true,
        _ => false,
    };

    private sealed class SimpleProvider : IInventoryViewProvider
    {
        public string PanelTitleKey { get; }
        public IReadOnlyList<IInventoryViewSource> Sources { get; }
        public SimpleProvider(string titleKey, params IInventoryViewSource[] sources)
        {
            PanelTitleKey = titleKey;
            Sources = sources ?? Array.Empty<IInventoryViewSource>();
        }
    }
}
