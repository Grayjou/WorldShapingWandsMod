namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Maps to spritesheet columns in WandActionProjectile textures.
/// Purely visual — decoupled from SelectionMode (which conflates UI interaction
/// semantics with point count). This enum represents the 4 visual states
/// displayed by the floating mode indicator.
/// </summary>
public enum WandMode : byte
{
    /// <summary>Column 0: Instant execution (OneClick).</summary>
    Instant = 0,

    /// <summary>Column 1: Selection active, awaiting endpoint (TwoClick).</summary>
    Select = 1,

    /// <summary>Column 2: Selection locked, awaiting confirmation (ThreeClick).</summary>
    Confirm = 2,

    /// <summary>Column 3: Stamp locked, repeatable execution (FourClick).</summary>
    Stamp = 3,
}
