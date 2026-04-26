namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the tiling pattern used by the Wand of Torches
/// for spacing torch placements across a selection.
/// </summary>
public enum TilingStyle : byte
{
    /// <summary>
    /// Diamond lattice pattern. Torches propagate diagonally, creating a
    /// pattern where the Manhattan distance between adjacent torches is
    /// <c>SpacingX + SpacingY - 1</c>.
    /// </summary>
    Manhattan = 0,

    /// <summary>
    /// Rectangular grid pattern. Torches propagate in cardinal directions,
    /// creating axis-aligned rows and columns.
    /// </summary>
    Grid = 1,
}
