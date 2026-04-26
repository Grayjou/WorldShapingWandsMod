namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Reference point mode for torch tiling seed selection.
/// Determines where the tiling grid origin is anchored.
/// </summary>
public enum TorchReferenceMode : byte
{
    /// <summary>
    /// Default: Scan top-down, left-right for the first valid torch position
    /// (or nearby existing torch) and use that as the tiling seed.
    /// </summary>
    FirstValidTile = 0,

    /// <summary>
    /// Use the top-left corner of the selection bounding box as the tiling seed.
    /// The grid is anchored to the bbox origin regardless of validity.
    /// </summary>
    BboxTopLeft = 1,

    /// <summary>
    /// Use the top-right corner of the selection bounding box as the tiling seed.
    /// </summary>
    BboxTopRight = 2,

    /// <summary>
    /// Use the bottom-left corner of the selection bounding box as the tiling seed.
    /// </summary>
    BboxBottomLeft = 3,

    /// <summary>
    /// Use the bottom-right corner of the selection bounding box as the tiling seed.
    /// </summary>
    BboxBottomRight = 4,

    /// <summary>
    /// Use the initial click point (selection start tile) as the tiling seed.
    /// The grid origin is anchored to where the player first clicked to begin
    /// the selection, regardless of how the bounding box was dragged.
    /// </summary>
    FirstBboxClick = 5,

    /// <summary>
    /// Use the mouse position at execution time as the tiling seed.
    /// The grid origin is anchored to wherever the cursor was when the
    /// player confirmed the operation.
    /// </summary>
    MousePosition = 6,
}