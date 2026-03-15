using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Selection;

/// <summary>
/// Stores the visual state of a cancelled selection for overlay feedback.
/// The overlay persists briefly after cancellation, changing color, then fades out.
/// Tiles are computed once at creation time and reused every frame during the fade.
/// </summary>
public class CancelledSelectionState
{
    /// <summary>The frozen selection start tile.</summary>
    public Point StartTile { get; }

    /// <summary>The frozen selection end tile.</summary>
    public Point EndTile { get; }

    /// <summary>Whether the selection was vertical-first.</summary>
    public bool VerticalFirst { get; }

    /// <summary>The shape settings at time of cancellation.</summary>
    public ShapeInfo Shape { get; }

    /// <summary>The overlay color for the cancelled state.</summary>
    public Color CancelColor { get; }

    /// <summary>The tick (GameUpdateCount) when the cancellation occurred.</summary>
    public ulong CancelTick { get; }

    /// <summary>Duration in ticks the overlay stays visible.</summary>
    public int DurationTicks { get; }

    /// <summary>Pre-computed tile set, calculated once at creation to avoid per-frame recomputation.</summary>
    public HashSet<Point> CachedTiles { get; }

    /// <summary>Pre-computed bounding rectangle for the cancelled selection.</summary>
    public Rectangle CachedBounds { get; }

    public CancelledSelectionState(
        Point startTile,
        Point endTile,
        bool verticalFirst,
        ShapeInfo shape,
        Color cancelColor,
        int durationTicks)
    {
        StartTile = startTile;
        EndTile = endTile;
        VerticalFirst = verticalFirst;
        Shape = shape;
        CancelColor = cancelColor;
        CancelTick = Terraria.Main.GameUpdateCount;
        DurationTicks = durationTicks;

        // Pre-compute tiles ONCE at creation — avoids O(area) recomputation every frame
        var context = shape.ToShapeContext(startTile, endTile, verticalFirst);
        CachedBounds = context.GetBounds();
        var tileSet = ShapeRegistry.GetShapeTiles(shape.Shape, context);
        CachedTiles = new HashSet<Point>(tileSet.Tiles);
    }

    /// <summary>How many ticks have elapsed since cancellation.</summary>
    public int ElapsedTicks => (int)(Terraria.Main.GameUpdateCount - CancelTick);

    /// <summary>Whether the cancelled overlay has expired.</summary>
    public bool IsExpired => ElapsedTicks >= DurationTicks;

    /// <summary>
    /// Returns the opacity multiplier (1.0 → 0.0) over the duration.
    /// </summary>
    public float Opacity
    {
        get
        {
            if (IsExpired) return 0f;
            return 1f - (float)ElapsedTicks / DurationTicks;
        }
    }
}
