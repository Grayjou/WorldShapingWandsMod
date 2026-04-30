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

    /// <summary>
    /// (S11 2026-04-28; ColorReplacePlan.md §3 + GrayJou worried-client
    /// review of S10) Replace one paint colour with another across the
    /// selection, on the channel chosen in the Color Replace SubUI
    /// (<see cref="ColorReplaceChannel"/> Tile or Wall) using the
    /// (source, target) tuple stored in <see cref="WandOfCoatingSettings"/>.
    /// Sits in the Mode radio row alongside PaintTile/PaintWall/ScrapeMoss/
    /// HarvestMoss because semantically the player picks ONE thing the wand
    /// does at a time; the prior “separate fire-on-click action” framing
    /// from S8–S10 was per GrayJou's S11 review *“there is some
    /// misunderstanding as if these operations were perpendicular in some
    /// sense”* — they are not. Source/target/channel remain configured via
    /// the right-click SubUI on the same button.
    /// </summary>
    ColorReplace = 5,
}
