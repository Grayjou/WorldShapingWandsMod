using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §6.0.) The
/// player-scoped, in-memory store of the most recent Magic Wand (Read)
/// capture, replayed by Magic Wand (Apply) on any wand. Strictly parallel
/// to <c>MoldShape</c>'s canvas storage — both shapes are siblings; both
/// read from a player-scoped tile set and stamp at the cursor.
/// </summary>
/// <remarks>
/// <para><b>Lifecycle</b>:</para>
/// <list type="bullet">
///   <item>Captured: at the end of every successful
///   <c>MagicWandReadShape</c> click.</item>
///   <item>Consumed by downstream stamp consumers (legacy Apply path and modern mold ingest).
///   NOT cleared on consume — Apply is repeatable across multiple click
///   positions until a new Read overwrites the slot or the player exits.</item>
///   <item>Cleared: on world exit / disconnect (in-memory only, like
///   stencil canvases per <c>MultipleStencilsPlan.md</c> §8 / Cavendish
///   C-S1 §C2 *"only configs persist; canvases and captures don't"*).</item>
///   <item>NOT synced across players in MP — each player carries their
///   own. The Apply click sends the stored payload with the request
///   packet so the server can validate + replay (one packet per Apply,
///   contains the tile set).</item>
/// </list>
///
/// <para><b>Coordinate convention</b>: <see cref="Tiles"/> are stored
/// RELATIVE to <see cref="Origin"/> — i.e. Origin itself is at the
/// relative point (0, 0). At Apply time the cursor's tile becomes the
/// new origin and every relative offset is added to it. This mirrors
/// <c>CustomShape.RelativeTiles</c> + <c>BoundingBox</c> from the Mold
/// pipeline, so future polish can lift Mold's stamping helpers
/// directly without coordinate-space reinterpretation.</para>
///
/// <para><b>Why XNA Point (not Point16)</b>: parity with the Mold
/// pipeline. <c>SelectionCanvas._canvasTiles</c> and
/// <c>CustomShape.RelativeTiles</c> are both <c>HashSet&lt;Point&gt;</c>;
/// using <c>Point16</c> here would force a coordinate-space conversion
/// at every Apply call (and at every Read write into the active stencil
/// canvas). The plan §6.0 originally suggested <c>Point16</c> for memory;
/// the Mold pipeline already chose Point and the doubled-bytes cost is
/// negligible against the much-larger HashSet bucket overhead.</para>
/// </remarks>
public sealed class StoredMagicWandShape
{
    /// <summary>The captured tile offsets, expressed relative to <see cref="Origin"/>.</summary>
    public HashSet<Point> Tiles { get; }

    /// <summary>The click point at Read time. Used purely as the (0,0) anchor for the relative offsets.</summary>
    public Point Origin { get; }

    /// <summary>
    /// The config in effect when the Read fired. Stored for diagnostic /
    /// tooltip use ("captured with SameTile + 4-neighbour"). NOT used at
    /// Apply time — Apply is a deterministic stamp, not a re-flood.
    /// </summary>
    public MagicWandReadConfig ConfigAtCapture { get; }

    /// <summary>UTC tick of capture, for tooltip / debug only.</summary>
    public long CapturedAtTicks { get; }

    /// <summary>Convenience: how many tiles the captured shape spans.</summary>
    public int Count => Tiles.Count;

    public StoredMagicWandShape(
        HashSet<Point> tiles,
        Point origin,
        MagicWandReadConfig configAtCapture,
        long capturedAtTicks)
    {
        Tiles = tiles ?? new HashSet<Point>();
        Origin = origin;
        ConfigAtCapture = configAtCapture;
        CapturedAtTicks = capturedAtTicks;
    }
}
