namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Identifies the specific action a wand is currently performing.
/// Each value maps to a unique texture file for the WandActionProjectile system.
/// Texture naming convention: <c>WandAction_{Name}.png</c>
/// </summary>
/// <remarks>
/// Tier 1 (0–6): Building sub-actions, one per PlaceType.
/// Gap (7–9): Reserved for future Building sub-actions.
/// Tier 2 (10–29): One or more per wand family (Dismantling through Torches).
/// Tier 2b (30–43): Delimitation — {Canvas,Selection} × {Add,Remove,Intersect,XOR}.
/// Tier 2c (44–51): Molding — {Canvas,Selection} × {Add,Remove,Intersect,XOR}.
/// Tier 2d (52–53): Canvas creation signals (Delimitation, Molding).
/// Tier 3 (34–37): Selective drain sub-actions (per liquid type).
/// </remarks>
public enum WandAction : byte
{
    // ══ Tier 1: Building Sub-Actions (one per PlaceType) ══
    BuildingSolid      = 0,
    BuildingWalls      = 1,
    BuildingPlatforms  = 2,
    BuildingRope       = 3,
    BuildingGrassSeeds = 4,
    BuildingTracks     = 5,
    BuildingPlanterBox = 6,

    // 7–9 reserved for future Building sub-actions

    // ══ Tier 2: Family-Level Actions ══
    Dismantling         = 10,
    DismantlingVoid     = 11,
    Replacement         = 12,
    WiringAdd           = 13,
    WiringRemove        = 14,
    SafekeepingAdd      = 15,
    SafekeepingRemove   = 16,
    CoatingPaintTile    = 17,
    CoatingPaintWall    = 18,
    CoatingScrapeMoss   = 19,
    CoatingHarvestMoss  = 20,
    FluidPlace          = 21,
    FluidDrainAny       = 22,
    FluidBubble         = 23,
    FluidRainFill       = 24,  // Uses Pour* sprites until dedicated RainFill art arrives
    FluidPocketFill     = 25,  // Uses Pour* sprites until dedicated PocketFill art arrives
    TorchPlace          = 26,
    TorchReplace        = 27,
    TorchRemove         = 28,
    TorchConvert        = 29,
    // ── Delimitation: Canvas operations ──
    DelimitationCanvasAdd       = 30,
    DelimitationCanvasRemove    = 31,
    DelimitationCanvasIntersect = 38,
    DelimitationCanvasXOR       = 39,
    // ── Delimitation: Selection operations ──
    DelimitationSelectionAdd       = 40,
    DelimitationSelectionRemove    = 41,
    DelimitationSelectionIntersect = 42,
    DelimitationSelectionXOR       = 43,
    // ── Molding: Canvas operations ──
    MoldingCanvasAdd       = 44,
    MoldingCanvasRemove    = 45,
    MoldingCanvasIntersect = 46,
    MoldingCanvasXOR       = 47,
    // ── Molding: Selection operations ──
    MoldingSelectionAdd       = 48,
    MoldingSelectionRemove    = 49,
    MoldingSelectionIntersect = 50,
    MoldingSelectionXOR       = 51,

    // ══ Tier 2d: Canvas Creation Signals ══
    DelimitationNewCanvas = 52,
    MoldingNewCanvas      = 53,

    // ══ Tier 3: Selective Drain Sub-Actions ══
    FluidDrainWater     = 34,
    FluidDrainLava      = 35,
    FluidDrainHoney     = 36,
    FluidDrainShimmer   = 37,
}
