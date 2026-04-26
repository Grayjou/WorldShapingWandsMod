using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// Shape provider for user-defined mold shapes captured by the Wand of Molding.
/// Unlike parametric shapes, the Mold shape is a fixed tile pattern that ignores
/// ShapeMode (Filled/Hollow), Thickness, and SliceMode. The shape is exactly what
/// the user sculpted.
/// </summary>
/// <remarks>
/// <para>
/// The mold data is read from the local player's <see cref="MoldingWandPlayer.MoldedShape"/>
/// (or equivalently, <see cref="DelimitationWandPlayer.ActiveCustomShape"/> which is
/// kept in sync by <see cref="MoldingWandPlayer.PromoteMoldToCustomShape"/>).
/// </para>
/// <para>
/// For Stamp wands, <c>Start</c> is the cursor position (anchor) and <c>End</c> is
/// the same position (single-point stamp). The mold shape is centered on the anchor
/// using the bounding box center as the offset.
/// </para>
/// <para>
/// <b>Multiplayer note</b>: Currently the mold is stored client-side only. For MP,
/// the shape data would need to be serialized in the wand operation packet. This is
/// acceptable for now since the Wand of Molding is a new feature in SP testing.
/// </para>
/// </remarks>
public class MoldShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Mold;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var mold = GetActiveMold();
        if (mold == null || mold.Count == 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        // For stamp mode: Start == End == cursor position.
        // For two-click/three-click: Start is anchor, End is ignored.
        // Center the mold on the Start position using the bounding box center as offset.
        Point anchor = context.Start;
        Point moldCenter = new Point(mold.BoundingBox.Width / 2, mold.BoundingBox.Height / 2);

        var tiles = mold.GetTilesAtWithAnchor(anchor, moldCenter);

        // Compute boundary using the standard 4-neighborhood method
        var boundary = GeometryHelper.GetBoundaryTiles4(tiles);

        return new ShapeTileSet(tiles, boundary);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var mold = GetActiveMold();
        if (mold == null || mold.Count == 0)
            return false;

        Point anchor = context.Start;
        Point moldCenter = new Point(mold.BoundingBox.Width / 2, mold.BoundingBox.Height / 2);

        // Convert world point to mold-relative coordinates
        int relX = point.X - anchor.X + moldCenter.X;
        int relY = point.Y - anchor.Y + moldCenter.Y;

        return mold.RelativeTiles.Contains(new Point(relX, relY));
    }

    public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
    {
        var mold = GetActiveMold();
        if (mold == null || mold.Count == 0)
            return (0, 0);

        return (mold.BoundingBox.Width, mold.BoundingBox.Height);
    }

    /// <summary>
    /// Retrieves the active mold shape from the local player.
    /// Checks <see cref="MoldingWandPlayer.MoldedShape"/> first (authoritative),
    /// then falls back to <see cref="DelimitationWandPlayer.ActiveCustomShape"/>.
    /// Returns <c>null</c> if no mold has been defined.
    /// </summary>
    private static CustomShape GetActiveMold()
    {
        var player = Main.LocalPlayer;
        if (player?.active != true)
            return null;

        // Primary: MoldingWandPlayer (the canonical source)
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        if (mwp?.MoldedShape != null)
            return mwp.MoldedShape;

        // Fallback: DelimitationWandPlayer.ActiveCustomShape
        // (kept in sync by PromoteMoldToCustomShape, but also set
        // by the Delimitation Wand's own promote action)
        var dwp = player.GetModPlayer<DelimitationWandPlayer>();
        return dwp?.ActiveCustomShape;
    }
}
