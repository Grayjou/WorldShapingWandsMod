using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0/§7.)
/// Magic Wand — Apply variant. Available on every wand. Bare shape: no
/// parameter row, no SubUI, no configuration. At click time it stamps
/// <c>WandPlayer.LastMagicWandShape</c> at the cursor (Mold-style
/// translation by stored origin) and the wand's per-tile action runs
/// over the stamped set.
/// </summary>
/// <remarks>
/// <para><b>Strict sibling of <see cref="MoldShape"/></b>: both shapes
/// read from a player-scoped tile set and stamp at the cursor. The only
/// real differences are <i>which</i> player slot they read from
/// (<c>MoldedShape</c> vs <c>LastMagicWandShape</c>) and how the slot
/// got there in the first place (mold sculpting vs Magic Wand Read).
/// The stamping math is identical: translate every relative offset by
/// (cursorX − originX, cursorY − originY) and emit the world-coord
/// tile set.</para>
///
/// <para><b>Empty-storage handling</b>: the chat warning *"Magic Wand:
/// no captured shape. Use Magic Wand Read on a stencil wand first."* is
/// scheduled to fire at the cast site (S11+ once locale keys ship); the
/// shape provider itself returns an empty <see cref="ShapeTileSet"/> so
/// the wand operation is a clean no-op and no exception bubbles.</para>
///
/// <para><b>Why no Read fallback</b>: per Cavendish C-S1 §B4 + GrayJou
/// inline override (plan §0.A), Apply is *strictly* a deterministic
/// stamp, NOT a re-flood. The previous *"Apply runs Read internally with
/// a default config"* idea was rejected — it would (a) require every
/// non-stencil wand to access a domain it doesn't have (no canvas), and
/// (b) hide *which* shape the player thought they were stamping behind
/// an invisible default. The current design is honest: no capture →
/// nothing to stamp → tell the player.</para>
/// </remarks>
public class MagicWandApplyShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.MagicWandApply;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var emptySet = new HashSet<Point>();
        var emptyResult = new ShapeTileSet(emptySet, emptySet);

        var player = Main.LocalPlayer;
        if (player?.active != true) return emptyResult;
        var wp = player.GetModPlayer<WandPlayer>();
        var stored = wp?.LastMagicWandShape;
        if (stored == null || stored.Tiles.Count == 0) return emptyResult;

        // Mold-style translation: stored.Tiles are world coords from
        // the original Read, anchored at stored.Origin. New cursor =
        // context.Start; delta = cursor - origin. We do NOT re-anchor
        // by bounding-box centre (the way MoldShape does) — Magic Wand
        // captures preserve the click point as the natural anchor, so
        // the player's "where I clicked Read = where I'll click Apply"
        // mental model lines up perfectly.
        int dx = context.Start.X - stored.Origin.X;
        int dy = context.Start.Y - stored.Origin.Y;

        var stamped = new HashSet<Point>(stored.Tiles.Count);
        foreach (var p in stored.Tiles)
            stamped.Add(new Point(p.X + dx, p.Y + dy));

        var boundary = GeometryHelper.GetBoundaryTiles4(stamped);
        return new ShapeTileSet(stamped, boundary);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var player = Main.LocalPlayer;
        if (player?.active != true) return false;
        var wp = player.GetModPlayer<WandPlayer>();
        var stored = wp?.LastMagicWandShape;
        if (stored == null || stored.Tiles.Count == 0) return false;

        // Inverse-translate the query point into stored-shape space.
        int relX = point.X - context.Start.X + stored.Origin.X;
        int relY = point.Y - context.Start.Y + stored.Origin.Y;
        return stored.Tiles.Contains(new Point(relX, relY));
    }

    public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
    {
        var player = Main.LocalPlayer;
        if (player?.active != true) return (0, 0);
        var stored = player.GetModPlayer<WandPlayer>()?.LastMagicWandShape;
        if (stored == null || stored.Tiles.Count == 0) return (0, 0);

        // Bounding box from the stored relative tiles — translation
        // doesn't change dimensions, so this is cursor-position-agnostic.
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in stored.Tiles)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return (maxX - minX + 1, maxY - minY + 1);
    }
}
