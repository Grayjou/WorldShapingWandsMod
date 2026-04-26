namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Selects which waterproof torch to use for auto-conversion in Ocean biome areas.
/// </summary>
public enum OceanWaterproofTorch : byte
{
    /// <summary>Coral Torch — the natural ocean torch.</summary>
    CoralTorch = 0,

    /// <summary>Automatically pick based on world evil: Ichor (Crimson) or Cursed (Corruption).</summary>
    EvilTorch = 1,

    /// <summary>Always use Cursed Torch.</summary>
    CursedTorch = 2,

    /// <summary>Always use Ichor Torch.</summary>
    IchorTorch = 3,
}
