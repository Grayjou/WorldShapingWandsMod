namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Identifies the wand family currently held by the player.
/// Used for per-family overlay colors, cancel colors, and projectile animation dispatch.
/// </summary>
public enum WandFamily : byte
{
    /// <summary>No wand or unrecognized wand held.</summary>
    Unknown = 0,

    /// <summary>Wand of Building — places tiles in geometric shapes.</summary>
    Building = 1,

    /// <summary>Wand of Dismantling — removes tiles in geometric shapes.</summary>
    Dismantling = 2,

    /// <summary>Wand of Replacement — swaps tiles in geometric shapes.</summary>
    Replacement = 3,

    /// <summary>Wand of Wiring — places/removes wires in geometric shapes.</summary>
    Wiring = 4,

    /// <summary>Wand of Safekeeping — protects/unprotects tiles in geometric shapes.</summary>
    Safekeeping = 5,

    /// <summary>Wand of Coating — paints/scrapes tiles in geometric shapes.</summary>
    Coating = 6,

    /// <summary>Wand of Fluids — places/drains liquids in geometric shapes.</summary>
    Fluids = 7,

    /// <summary>Wand of Torches — places/removes torches along surfaces.</summary>
    Torches = 8,

    /// <summary>Wand of Delimitation — defines working canvas regions.</summary>
    Delimitation = 9,

    /// <summary>Wand of Molding — sculpts custom shapes via canvas editing.</summary>
    Molding = 10
}
