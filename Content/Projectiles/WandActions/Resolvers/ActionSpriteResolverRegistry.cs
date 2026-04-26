using System.Collections.Generic;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Projectiles.WandActions.Resolvers;

/// <summary>
/// Central registry mapping <see cref="WandFamily"/> to its
/// <see cref="IActionSpriteResolver"/>. Also provides the reverse mapping from
/// <see cref="WandAction"/> → <see cref="WandFamily"/> so the projectile can
/// look up the correct resolver at runtime.
/// </summary>
/// <remarks>
/// All resolvers are stateless singletons created once at static init.
/// The family→resolver dictionary is keyed by <see cref="WandFamily"/>,
/// and the action→family mapping uses an explicit switch for clarity and compile-time safety.
/// </remarks>
internal static class ActionSpriteResolverRegistry
{
    // ══════════════════════════════════════════════════════════════════
    //  Resolver Instances (stateless singletons)
    // ══════════════════════════════════════════════════════════════════

    // ── Building ── enum names match file names, no overrides needed
    private static readonly StaticSpriteResolver BuildingResolver = new("Building");

    // ── Dismantling ──
    private static readonly StaticSpriteResolver DismantlingResolver = new("Dismantling",
        new Dictionary<WandAction, string>
        {
            [WandAction.Dismantling]     = "Dismantle",
            [WandAction.DismantlingVoid] = "Void",
        });

    // ── Replacement ──
    private static readonly StaticSpriteResolver ReplacementResolver = new("Replacement",
        new Dictionary<WandAction, string>
        {
            [WandAction.Replacement] = "Replace",
        });

    // ── Wiring ──
    private static readonly StaticSpriteResolver WiringResolver = new("Wiring",
        new Dictionary<WandAction, string>
        {
            [WandAction.WiringAdd]    = "WireUp",
            [WandAction.WiringRemove] = "CutWire",
        });

    // ── Safekeeping ──
    private static readonly StaticSpriteResolver SafekeepingResolver = new("Safekeeping",
        new Dictionary<WandAction, string>
        {
            [WandAction.SafekeepingAdd]    = "SafekeepAdd",
            [WandAction.SafekeepingRemove] = "SafekeepRemove",
        });

    // ── Coating ──
    private static readonly StaticSpriteResolver CoatingResolver = new("Coating",
        new Dictionary<WandAction, string>
        {
            [WandAction.CoatingPaintTile]   = "PaintSolid",
            [WandAction.CoatingPaintWall]   = "PaintWalls",
            [WandAction.CoatingScrapeMoss]  = "ScrapeMoss",
            [WandAction.CoatingHarvestMoss] = "HarvestMoss",
        });

    // ── Fluids ── dynamic resolution (liquid type × fill mode)
    private static readonly FluidsSpriteResolver FluidsResolver = new();

    // ── Torches ──
    private static readonly StaticSpriteResolver TorchesResolver = new("Torches",
        new Dictionary<WandAction, string>
        {
            [WandAction.TorchPlace]   = "PlaceTorches",
            [WandAction.TorchReplace] = "ReplaceTorches",
            [WandAction.TorchRemove]  = "RemoveTorches",
            [WandAction.TorchConvert] = "ConvertTorches",
        });

    // ── Delimitation ──
    private static readonly StaticSpriteResolver DelimitationResolver = new("Delimitation",
        new Dictionary<WandAction, string>
        {
            [WandAction.DelimitationCanvasAdd]          = "DelimitationCanvasAdd",
            [WandAction.DelimitationCanvasRemove]       = "DelimitationCanvasRemove",
            [WandAction.DelimitationCanvasIntersect]    = "DelimitationCanvasIntersect",
            [WandAction.DelimitationCanvasXOR]          = "DelimitationCanvasXOR",
            [WandAction.DelimitationSelectionAdd]       = "DelimitationSelectionAdd",
            [WandAction.DelimitationSelectionRemove]    = "DelimitationSelectionRemove",
            [WandAction.DelimitationSelectionIntersect] = "DelimitationSelectionIntersect",
            [WandAction.DelimitationSelectionXOR]       = "DelimitationSelectionXOR",
            [WandAction.DelimitationNewCanvas]           = "DelimitationNewCanvas",
        });

    // ── Molding ──
    private static readonly StaticSpriteResolver MoldingResolver = new("Molding",
        new Dictionary<WandAction, string>
        {
            [WandAction.MoldingCanvasAdd]          = "MoldingCanvasAdd",
            [WandAction.MoldingCanvasRemove]       = "MoldingCanvasRemove",
            [WandAction.MoldingCanvasIntersect]    = "MoldingCanvasIntersect",
            [WandAction.MoldingCanvasXOR]          = "MoldingCanvasXOR",
            [WandAction.MoldingSelectionAdd]       = "MoldingSelectionAdd",
            [WandAction.MoldingSelectionRemove]    = "MoldingSelectionRemove",
            [WandAction.MoldingSelectionIntersect] = "MoldingSelectionIntersect",
            [WandAction.MoldingSelectionXOR]       = "MoldingSelectionXOR",
            [WandAction.MoldingNewCanvas]           = "MoldingNewCanvas",
        });

    // ══════════════════════════════════════════════════════════════════
    //  Family → Resolver Registry
    // ══════════════════════════════════════════════════════════════════

    private static readonly Dictionary<WandFamily, IActionSpriteResolver> Resolvers = new()
    {
        [WandFamily.Building]     = BuildingResolver,
        [WandFamily.Dismantling]  = DismantlingResolver,
        [WandFamily.Replacement]  = ReplacementResolver,
        [WandFamily.Wiring]       = WiringResolver,
        [WandFamily.Safekeeping]  = SafekeepingResolver,
        [WandFamily.Coating]      = CoatingResolver,
        [WandFamily.Fluids]       = FluidsResolver,
        [WandFamily.Torches]      = TorchesResolver,
        [WandFamily.Delimitation] = DelimitationResolver,
        [WandFamily.Molding]      = MoldingResolver,
    };

    // ══════════════════════════════════════════════════════════════════
    //  Fallback Resolver
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Used when a WandAction maps to an unknown family. Resolves to
    /// <c>Building/WandAction_{EnumName}</c> as a safe fallback.
    /// </summary>
    private static readonly StaticSpriteResolver FallbackResolver = new("Building");

    // ══════════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the appropriate <see cref="IActionSpriteResolver"/> for the
    /// given action. Performs a two-step lookup:
    /// <c>WandAction → WandFamily → IActionSpriteResolver</c>.
    /// </summary>
    public static IActionSpriteResolver GetResolver(WandAction action)
    {
        WandFamily family = GetFamily(action);
        return Resolvers.TryGetValue(family, out var resolver)
            ? resolver
            : FallbackResolver;
    }

    /// <summary>
    /// Maps a <see cref="WandAction"/> to its owning <see cref="WandFamily"/>.
    /// This is the canonical reverse mapping from action → family, used by the
    /// projectile to select the correct sprite resolver at runtime.
    /// </summary>
    public static WandFamily GetFamily(WandAction action) => action switch
    {
        >= WandAction.BuildingSolid and <= WandAction.BuildingPlanterBox
            => WandFamily.Building,

        WandAction.Dismantling or WandAction.DismantlingVoid
            => WandFamily.Dismantling,

        WandAction.Replacement
            => WandFamily.Replacement,

        WandAction.WiringAdd or WandAction.WiringRemove
            => WandFamily.Wiring,

        WandAction.SafekeepingAdd or WandAction.SafekeepingRemove
            => WandFamily.Safekeeping,

        >= WandAction.CoatingPaintTile and <= WandAction.CoatingHarvestMoss
            => WandFamily.Coating,

        >= WandAction.FluidPlace and <= WandAction.FluidPocketFill
            => WandFamily.Fluids,

        >= WandAction.FluidDrainWater and <= WandAction.FluidDrainShimmer
            => WandFamily.Fluids,

        >= WandAction.TorchPlace and <= WandAction.TorchConvert
            => WandFamily.Torches,

        >= WandAction.DelimitationCanvasAdd and <= WandAction.DelimitationSelectionXOR
            => WandFamily.Delimitation,

        WandAction.DelimitationNewCanvas
            => WandFamily.Delimitation,

        >= WandAction.MoldingCanvasAdd and <= WandAction.MoldingSelectionXOR
            => WandFamily.Molding,

        WandAction.MoldingNewCanvas
            => WandFamily.Molding,

        _ => WandFamily.Unknown,
    };
}
