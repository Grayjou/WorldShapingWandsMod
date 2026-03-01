namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines what happens when the player runs out of blocks mid-placement.
/// </summary>
public enum BlockExhaustionMode : byte
{
    /// <summary>
    /// Automatically switch to the next matching block in inventory and continue.
    /// </summary>
    NextBlock = 0,

    /// <summary>
    /// Stop placement immediately at the tile that exhausted the stock.
    /// Tiles already placed remain.
    /// </summary>
    Interrupt = 1,

    /// <summary>
    /// Pre-check before starting: if there aren't enough blocks for the
    /// entire operation, abort without placing anything.
    /// </summary>
    Cancel = 2,
}
