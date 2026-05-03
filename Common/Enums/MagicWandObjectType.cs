namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0.2 + §3 +
/// §0.4 corrections.) The 12-cell SampleMode taxonomy used by
/// Magic Wand (Read) — what kind of similarity the flood-fill matches
/// against the origin tile under the cursor.
/// </summary>
/// <remarks>
/// <para><b>Two logical sub-rows in the Read SubUI's SampleMode section:</b></para>
/// <list type="bullet">
///   <item><b>Sub-row A</b> — *"Same as origin"* (2 cells):
///   <see cref="SameTile"/>, <see cref="SameWall"/>.</item>
///   <item><b>Sub-row B-1</b> — Object-type taxonomy (6 cells):
///   <see cref="Solid"/>, <see cref="Wall"/>, <see cref="Rope"/>,
///   <see cref="Platform"/>, <see cref="Rail"/>, <see cref="PlanterBox"/>.</item>
///   <item><b>Sub-row B-2</b> — Domain extras + paint channels (4 cells):
///   <see cref="Empty"/>, <see cref="Liquid"/>, <see cref="PaintTile"/>,
///   <see cref="PaintWall"/>.</item>
/// </list>
///
/// <para><b>Why 12 (not 11)</b>: the S3 design had a single
/// <c>Paint</c> cell with a child SubUI for the channel toggle.
/// S4 collapsed that into two atomic cells (<see cref="PaintTile"/>
/// /<see cref="PaintWall"/>) per GrayJou's *"phew, no nested SubUI"*
/// inline correction — the Read step uses the origin's own paint
/// colour as the match key, so there's nothing to configure ahead of
/// time besides which channel to read from.</para>
///
/// <para><b>Compositing</b>: queries like *"same tile-type AND same
/// paint"* are run as TWO Read passes through the stencil canvas Mode
/// (Intersect): first <see cref="SameTile"/>, then
/// <see cref="PaintTile"/>. We do not ship a combined cell.</para>
/// </remarks>
public enum MagicWandObjectType : byte
{
    /// <summary>Sub-row A. Predicate: <c>HasTile &amp;&amp; TileType == origin.TileType</c>.</summary>
    SameTile = 0,

    /// <summary>Sub-row A. Predicate: <c>WallType &gt; 0 &amp;&amp; WallType == origin.WallType</c>.</summary>
    SameWall = 1,

    /// <summary>Sub-row B-1. Predicate: <c>HasTile &amp;&amp; Main.tileSolid[TileType]</c>. Type-agnostic.</summary>
    Solid = 2,

    /// <summary>Sub-row B-1. Predicate: <c>WallType &gt; 0</c>. Type-agnostic.</summary>
    Wall = 3,

    /// <summary>Sub-row B-1. Predicate: <c>HasTile &amp;&amp; TileID.Sets.Ropes[TileType]</c>.</summary>
    Rope = 4,

    /// <summary>Sub-row B-1. Predicate: <c>HasTile &amp;&amp; TileID.Sets.Platforms[TileType]</c>.</summary>
    Platform = 5,

    /// <summary>Sub-row B-1. Predicate: <c>HasTile &amp;&amp; TileID.Sets.IsRail[TileType] (or equivalent)</c>.</summary>
    Rail = 6,

    /// <summary>Sub-row B-1. Predicate: <c>HasTile &amp;&amp; TileID.Sets.IsPlanterBox[TileType] (or equivalent)</c>.</summary>
    PlanterBox = 7,

    /// <summary>Sub-row B-2. Predicate: <c>!HasTile &amp;&amp; WallType == 0</c>.</summary>
    Empty = 8,

    /// <summary>Sub-row B-2. Predicate: <c>LiquidAmount &gt; 0 &amp;&amp; LiquidType == origin.LiquidType</c>.</summary>
    Liquid = 9,

    /// <summary>
    /// Sub-row B-2. Predicate: <c>HasTile &amp;&amp; TileColor == origin.TileColor</c>.
    /// Origin must be painted; otherwise Read fires the chat warning
    /// *"Magic Wand: origin tile is unpainted; PaintTile read found no matches."*
    /// and yields an empty selection.
    /// </summary>
    PaintTile = 10,

    /// <summary>
    /// Sub-row B-2. Mirror of <see cref="PaintTile"/> for the wall channel:
    /// predicate <c>WallType &gt; 0 &amp;&amp; WallColor == origin.WallColor</c>.
    /// Same unpainted-origin warning behaviour.
    /// </summary>
    PaintWall = 11,
}
