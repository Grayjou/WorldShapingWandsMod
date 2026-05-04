using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §5 + §3.) The
/// backend Magic Wand (Read) flood-fill implementation. Pure function:
/// takes an origin + config + domain + cap and returns the matching
/// world-coordinate tile set. No I/O, no UI, no network. Used exclusively
/// by <c>MagicWandReadShape</c>; <c>MagicWandApplyShape</c> does NOT call
/// this — Apply is a deterministic stamp from <c>StoredMagicWandShape</c>.
/// </summary>
/// <remarks>
/// <para><b>Domain</b>: the active stencil canvas membership (Read on a
/// stencil wand). Cells outside the canvas are not considered. This is
/// what makes Magic Wand Read a stencil-wand-only feature: only stencil
/// wands carry the canvas that defines the flood's world-membership.</para>
///
/// <para><b>Cap behaviour</b>: partial-accept. Once the result reaches
/// <paramref name="cap"/> the flood/iteration stops cleanly and the
/// caller is responsible for raising the chat warning *"Magic Wand
/// capped at N tiles."* The function itself never logs.</para>
///
/// <para><b>Predicates that compare against the origin tile</b>: the
/// "Same as origin" pair (<see cref="MagicWandObjectType.SameTile"/>,
/// <see cref="MagicWandObjectType.SameWall"/>) and the paint pair
/// (<see cref="MagicWandObjectType.PaintTile"/>,
/// <see cref="MagicWandObjectType.PaintWall"/>) read the origin tile
/// once and then test every candidate against that snapshot — the
/// origin's properties don't change mid-flood, so a single read is
/// both correct and faster than re-reading per candidate.</para>
///
/// <para><b>Same-type refinements (S6 2026-05-01)</b>: Rope / Platform /
/// Rail / PlanterBox now sample <i>the same specific tile type as origin</i>
/// (if origin belongs to that category) instead of category-wide matching.
/// This matches the worried-client expectation for precision workflows.
/// Rope uses a rope-like helper that includes <c>Main.tileRope</c> and
/// explicit chain fallback for better vanilla coverage.</para>
/// </remarks>
public static class MagicWandReadFn
{
    /// <summary>Outcome flags returned alongside the matching tile set.</summary>
    public enum ReadStatus
    {
        /// <summary>Read produced a non-empty result that did not hit the cap.</summary>
        Success,
        /// <summary>Read produced an empty result (no matches in the domain — not an error).</summary>
        Empty,
        /// <summary>Read hit the cap; result is partial. Caller should raise a chat warning.</summary>
        Capped,
        /// <summary>Origin tile is unpainted but the player picked PaintTile/PaintWall. Empty result; caller raises a specific warning.</summary>
        UnpaintedOrigin,
    }

    /// <summary>
    /// Runs the configured Read at <paramref name="origin"/> against the
    /// given domain. Returns the matching world-coordinate tile set plus
    /// a <see cref="ReadStatus"/> that distinguishes empty-by-design from
    /// the cap-hit and unpainted-origin edge cases.
    /// </summary>
    public static (HashSet<Point> Tiles, ReadStatus Status) Read(
        Point origin,
        MagicWandReadConfig config,
        SelectionCanvas domain,
        int cap)
    {
        var result = new HashSet<Point>();
        if (domain == null || domain.Count == 0) return (result, ReadStatus.Empty);
        if (!domain.Contains(origin)) return (result, ReadStatus.Empty);

        // Resolve origin once; every predicate-vs-origin compare reuses this snapshot.
        if (!TryGetTile(origin, out var oTile)) return (result, ReadStatus.Empty);

        // Unpainted-origin gate for the two paint predicates.
        if (config.ObjectType == MagicWandObjectType.PaintTile && (!oTile.HasTile || oTile.TileColor == PaintID.None))
            return (result, ReadStatus.UnpaintedOrigin);
        if (config.ObjectType == MagicWandObjectType.PaintWall && (oTile.WallType == WallID.None || oTile.WallColor == PaintID.None))
            return (result, ReadStatus.UnpaintedOrigin);

        // Bind the predicate once.
        bool Match(Point p)
        {
            return TryGetTile(p, out var t) && MatchPredicate(t, oTile, config.ObjectType);
        }

        // (C-S3 2026-05-03) Actuation filter: gates admission based on tile.IsActuated.
        // Applied after the content-match predicate so that a non-matching tile
        // is still rejected by the content predicate (cheaper) first.
        var actuationFilter = config.ActuationFilter;
        bool AdmitByActuation(Point p)
        {
            if (actuationFilter == ActuationFilter.Both) return true;
            return TryGetTile(p, out var t) && actuationFilter.Admits(t.IsActuated);
        }

        // Origin must itself match (otherwise contiguous flood has no seed).
        // For Non-contiguous mode we also enforce this — Read is "select
        // all things LIKE the origin", which presupposes the origin is
        // itself an instance of that thing.
        if (!Match(origin)) return (result, ReadStatus.Empty);
        if (!AdmitByActuation(origin)) return (result, ReadStatus.Empty);

        if (config.Continuity == MagicWandContinuity.NonContiguous)
        {
            // Iterate the entire domain. Cap-aware.
            foreach (var p in domain.GetAllPoints())
            {
                if (result.Count >= cap) return (result, ReadStatus.Capped);
                if (Match(p) && AdmitByActuation(p)) result.Add(p);
            }
            return (result, result.Count == 0 ? ReadStatus.Empty : ReadStatus.Success);
        }

        // Contiguous flood (4- or 8-neighbour). BFS keeps the visited frontier compact.
        var queue = new Queue<Point>();
        queue.Enqueue(origin);
        result.Add(origin);

        bool eight = config.Continuity == MagicWandContinuity.EightNeighbour;
        while (queue.Count > 0 && result.Count < cap)
        {
            var p = queue.Dequeue();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!eight && dx != 0 && dy != 0) continue; // 4-neighbour skip diagonals
                    var n = new Point(p.X + dx, p.Y + dy);
                    if (result.Contains(n)) continue;
                    if (!domain.Contains(n)) continue;
                    if (!Match(n)) continue;
                    if (!AdmitByActuation(n)) continue;
                    result.Add(n);
                    if (result.Count >= cap) return (result, ReadStatus.Capped);
                    queue.Enqueue(n);
                }
            }
        }

        return (result, result.Count == 0 ? ReadStatus.Empty : ReadStatus.Success);
    }

    /// <summary>
    /// World-bounds-safe tile fetch. Returns false for OOB coordinates so
    /// the predicates don't have to repeat the bounds check. Tile is a
    /// non-nullable value type in tModLoader, hence the <c>out</c> pattern.
    /// </summary>
    private static bool TryGetTile(Point p, out Tile t)
    {
        if (p.X < 0 || p.Y < 0 || p.X >= Main.maxTilesX || p.Y >= Main.maxTilesY)
        {
            t = default;
            return false;
        }
        t = Main.tile[p.X, p.Y];
        return true;
    }

    /// <summary>The 12-cell taxonomy dispatch. One arm per <see cref="MagicWandObjectType"/> value.</summary>
    private static bool MatchPredicate(Tile t, Tile origin, MagicWandObjectType kind) => kind switch
    {
        MagicWandObjectType.SameTile   => t.HasTile && origin.HasTile && t.TileType == origin.TileType,
        MagicWandObjectType.SameWall   => t.WallType != WallID.None && origin.WallType != WallID.None && t.WallType == origin.WallType,
        MagicWandObjectType.Solid      => t.HasTile && Main.tileSolid[t.TileType],
        MagicWandObjectType.Wall       => t.WallType != WallID.None,
        MagicWandObjectType.Rope       => t.HasTile && origin.HasTile && IsRopeLike(t.TileType) && IsRopeLike(origin.TileType) && t.TileType == origin.TileType,
        MagicWandObjectType.Platform   => t.HasTile && origin.HasTile && TileID.Sets.Platforms[t.TileType] && TileID.Sets.Platforms[origin.TileType] && t.TileType == origin.TileType,
        MagicWandObjectType.Rail       => t.HasTile && origin.HasTile && IsRailLike(t.TileType) && IsRailLike(origin.TileType) && t.TileType == origin.TileType,
        MagicWandObjectType.PlanterBox => t.HasTile && origin.HasTile && IsPlanterLike(t.TileType) && IsPlanterLike(origin.TileType) && t.TileType == origin.TileType,
        MagicWandObjectType.Empty      => !t.HasTile && t.WallType == WallID.None,
        MagicWandObjectType.Liquid     => t.LiquidAmount > 0 && origin.LiquidAmount > 0 && t.LiquidType == origin.LiquidType,
        MagicWandObjectType.PaintTile  => t.HasTile && origin.HasTile && t.TileColor == origin.TileColor && origin.TileColor != PaintID.None,
        MagicWandObjectType.PaintWall  => t.WallType != WallID.None && origin.WallType != WallID.None && t.WallColor == origin.WallColor && origin.WallColor != PaintID.None,
        _ => false,
    };

    private static bool IsRopeLike(int tileType)
    {
        return Main.tileRope[tileType]
            || tileType == TileID.Chain
            || tileType == TileID.Rope
            || tileType == TileID.SilkRope
            || tileType == TileID.VineRope
            || tileType == TileID.WebRope;
    }

    private static bool IsRailLike(int tileType)
    {
        return tileType == TileID.MinecartTrack;
    }

    private static bool IsPlanterLike(int tileType)
    {
        return tileType == TileID.PlanterBox;
    }
}
