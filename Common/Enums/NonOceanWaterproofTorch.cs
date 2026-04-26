namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Selects which waterproof torch to use for auto-conversion outside Ocean biome areas.
/// </summary>
public enum NonOceanWaterproofTorch : byte
{
    /// <summary>Automatically pick based on world evil: Ichor (Crimson) or Cursed (Corruption).</summary>
    EvilTorch = 0,

    /// <summary>Always use Cursed Torch.</summary>
    CursedTorch = 1,

    /// <summary>Always use Ichor Torch.</summary>
    IchorTorch = 2,

    /// <summary>Coral Torch.</summary>
    CoralTorch = 3,
}
