namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// What the Wand of Coating does to each tile in the selection.
/// </summary>
public enum CoatingMode : byte
{
    /// <summary>Apply the selected paint color to the foreground tile.</summary>
    PaintTile = 0,

    /// <summary>Apply the selected paint color to the background wall.</summary>
    PaintWall = 1,

    /// <summary>
    /// [Removed from UI] Remove paint from both the foreground tile and background wall.
    /// Kept at value 2 for save-data compatibility; use PaintColor=0 (None) instead.
    /// </summary>
    [System.Obsolete("Removed from UI. Use PaintTile/PaintWall with PaintColor=0 (None) to strip paint.")]
    ScrapePaint = 2,

    /// <summary>
    /// Remove moss from foreground tiles by converting moss variants back to their
    /// base stone type, strip any paint from them, and drop the moss as an item
    /// (matches vanilla scraper behaviour).
    /// </summary>
    ScrapeMoss = 3,

    /// <summary>
    /// Harvest moss by trimming LongMoss (hanging/growing moss) down to short moss.
    /// Drops moss items with a 25% chance per tile (matches vanilla scraper behaviour).
    /// Does NOT affect short moss tiles — only targets TileID.LongMoss.
    /// </summary>
    HarvestMoss = 4,
}
