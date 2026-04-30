using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0/§5/§7.)
/// Magic Wand — Read variant. Stencil-wands only. Runs the configured
/// flood-fill match (12-cell SampleMode taxonomy + 3-mode contiguity)
/// at <see cref="ShapeContext.Start"/> against the active stencil
/// canvas, returns the matching tile set, and captures the result on
/// <c>WandPlayer.LastMagicWandShape</c> for later replay by
/// <see cref="MagicWandApplyShape"/>.
/// </summary>
/// <remarks>
/// <para><b>Pure-shape contract</b>: <see cref="GetTiles"/> is the
/// canonical entry point and is deterministic given (origin + active
/// canvas snapshot + player config). The capture-into-<c>WandPlayer</c>
/// side effect runs on the LOCAL player only and only when invoked from
/// a real world click (not from preview/overlay polling) — the gate is
/// <c>Main.LocalPlayer.whoAmI &gt;= 0 &amp;&amp; Start == End</c>, the same
/// "single-point stamp" signature MoldShape uses to recognise a
/// committed cast vs a hover preview.</para>
///
/// <para><b>Domain</b>: the active <c>MoldingWandPlayer.Canvas</c>
/// (the Phase 3 passthrough getter — flips with the active stencil
/// slot). Empty canvas = empty result; the player gets the standard
/// "use the molding wand to set up a canvas first" workflow because the
/// Read shape inherently needs a domain to flood across.</para>
///
/// <para><b>S11+ scheduled</b>: chat-warning emission for the four
/// non-Success <see cref="MagicWandReadFn.ReadStatus"/> values is
/// stubbed with code-comments here. The actual <c>Msg.LocalChat</c>
/// calls will land alongside the Read SubUI in S11+ once locale keys
/// for the warnings ship — keeping the shape provider deterministic
/// (no I/O during a hover-preview pass) is more important than
/// emitting the warning at the source. The capture path already runs
/// only on real clicks, so the warning will fire from the correct
/// site when wired.</para>
///
/// <para><b>UI gating</b>: the shape's *availability* (i.e. whether it
/// shows up in the Shape Selector grid) is enforced by the panel-side
/// builder (<c>MoldingSettingsPanel</c> + sibling stencil panels), NOT
/// by the registry — the registry registers everything; consumer
/// panels decide what to render. Today the shape provider compiles +
/// runs but is not yet rendered anywhere; S11+ adds the cell.</para>
/// </remarks>
public class MagicWandReadShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.MagicWandRead;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var emptySet = new HashSet<Point>();
        var emptyResult = new ShapeTileSet(emptySet, emptySet);

        // Resolve the local player's stencil-wand state. Magic Wand Read
        // is stencil-only, so we always look at MoldingWandPlayer's
        // active canvas (Phase 3 passthrough — the active slot's canvas).
        var player = Main.LocalPlayer;
        if (player?.active != true) return emptyResult;
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        var wp = player.GetModPlayer<WandPlayer>();
        if (mwp == null || wp == null) return emptyResult;
        var canvas = mwp.Canvas;
        if (canvas == null || canvas.Count == 0) return emptyResult;

        // Cap: pick the stencil-wand cap from the server-authoritative
        // LimitsConfig. The stencil-wand cap (MoldingSelectionCap)
        // governs every Read since Read is stencil-only.
        int cap = ResolveCap();

        var origin = context.Start;
        var (tiles, status) = MagicWandReadFn.Read(origin, wp.MagicWandReadConfig, canvas, cap);

        // Capture into player-scoped slot — only on a "real cast"
        // signature (single-point stamp; Start == End). Hover previews
        // pass distinct Start/End or repeat-Start frames; we don't want
        // every preview frame to overwrite the last successful capture.
        // S11+ TODO: emit chat warnings for status ∈ {Empty,
        // UnpaintedOrigin, Capped} from the cast site (here once a
        // distinct "is this a commit vs a preview" gate lands; for now
        // the gate is Start == End which is correct for stamp semantics).
        if (context.Start == context.End && status != MagicWandReadFn.ReadStatus.Empty)
        {
            wp.LastMagicWandShape = new StoredMagicWandShape(
                tiles: new HashSet<Point>(tiles),
                origin: origin,
                configAtCapture: wp.MagicWandReadConfig,
                capturedAtTicks: DateTime.UtcNow.Ticks);
        }

        // Boundary: 4-neighbour edge mask, same convention as every
        // other shape in the registry. Re-uses the canvas helper that
        // already proved itself for stencil rendering.
        var boundary = GeometryHelper.GetBoundaryTiles4(tiles);
        return new ShapeTileSet(tiles, boundary);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        // Shape membership is the live Read result; for an
        // already-captured store we'd defer to LastMagicWandShape, but
        // the more useful contract here is "is this point in the most
        // recent freshly-computed Read?" — which is what GetTiles
        // provides. ShapeTileSet returns IEnumerable; HashSet-cast (or
        // materialise) for an O(1) Contains.
        var set = GetTiles(context).Tiles as HashSet<Point> ?? new HashSet<Point>(GetTiles(context).Tiles);
        return set.Contains(point);
    }

    public (int Width, int Height) GetDisplayDimensions(ShapeContext context)
    {
        // Display dims for HUD / cursor — bounding-box of the matching
        // set. Cheap because the canvas is already capped.
        var set = GetTiles(context).Tiles as HashSet<Point> ?? new HashSet<Point>(GetTiles(context).Tiles);
        if (set.Count == 0) return (0, 0);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in set)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return (maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Resolves the cap from server-authoritative <c>LimitsConfig</c>.
    /// Magic Wand Read is stencil-only so we use the molding cap.
    /// Defensive default of 1000 if config is unavailable (matches the
    /// shipped default value in <c>LimitsConfig</c>).
    /// </summary>
    private static int ResolveCap()
    {
        try
        {
            var cfg = ModContent.GetInstance<LimitsConfig>();
            return cfg?.MoldingSelectionCap ?? 1000;
        }
        catch
        {
            return 1000;
        }
    }
}
