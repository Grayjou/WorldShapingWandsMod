namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the type of object that can be placed or manipulated.
/// </summary>
public enum ObjectType : byte
{
    /// <summary>Regular tiles (blocks).</summary>
    Tile = 0,

    /// <summary>Platform tiles.</summary>
    Platform = 1,

    /// <summary>Rope tiles.</summary>
    Rope = 2,

    /// <summary>Planter box tiles.</summary>
    PlanterBox = 3,

    /// <summary>Rail tiles (minecart tracks).</summary>
    Rail = 4,

    /// <summary>Seed tiles (grass seeds, etc.).</summary>
    Seeds = 5,
}