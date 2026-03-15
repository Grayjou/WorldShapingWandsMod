using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Provides native half-shape computation for shape providers.
/// 
/// Unlike the old post-filter approach (which clipped after rasterization),
/// this helper is designed to be called from within each shape's GetTiles
/// method so that outline/hollow computation already operates on the
/// correct half-shape geometry.
/// 
/// Workflow for a shape provider:
///   1. Compute filled tiles for the FULL shape (or just the relevant half).
///   2. Call <see cref="SliceFilledTiles"/> to clip to the correct half.
///   3. Pass the clipped filled tiles through OutlineHelper for hollow mode.
///   4. If hollow + disconnect diameter, call <see cref="RemoveDiameterEdge"/>
///      to strip tiles along the diameter.
/// </summary>
public static class SliceHelper
{
    /// <summary>
    /// Determines whether a horizontal slice keeps the top or bottom half
    /// based on drag direction.
    /// </summary>
    /// <param name="context">Shape context with Start/End.</param>
    /// <returns>True if start is above or level with end (keep top half, flat on bottom).</returns>
    public static bool IsStartAbove(ShapeContext context) => context.Start.Y <= context.End.Y;

    /// <summary>
    /// Determines whether a vertical slice keeps the left or right half
    /// based on drag direction.
    /// </summary>
    /// <param name="context">Shape context with Start/End.</param>
    /// <returns>True if start is left of or level with end (keep left half, flat on right).</returns>
    public static bool IsStartLeft(ShapeContext context) => context.Start.X <= context.End.X;

    /// <summary>
    /// Clips a set of filled tiles to the correct half based on the slice mode
    /// and drag direction. The slice boundary is the center of the bounding box.
    /// </summary>
    public static HashSet<Point> SliceFilledTiles(HashSet<Point> tiles, ShapeContext context)
    {
        if (context.Slice == SliceMode.Full || tiles.Count == 0)
            return tiles;

        var bounds = context.GetBounds();

        if (context.Slice == SliceMode.HalfHorizontal)
        {
            bool keepTop = IsStartAbove(context);
            float centerY = bounds.Top + (bounds.Height - 1) / 2f;

            return keepTop
                ? new HashSet<Point>(tiles.Where(p => p.Y <= centerY))
                : new HashSet<Point>(tiles.Where(p => p.Y >= centerY));
        }
        else // HalfVertical
        {
            bool keepLeft = IsStartLeft(context);
            float centerX = bounds.Left + (bounds.Width - 1) / 2f;

            return keepLeft
                ? new HashSet<Point>(tiles.Where(p => p.X <= centerX))
                : new HashSet<Point>(tiles.Where(p => p.X >= centerX));
        }
    }

    /// <summary>
    /// Removes tiles along the diameter edge from a hollow (outline) tile set.
    /// The diameter edge is the flat side created by the slice — the center
    /// line of the original full shape.
    /// 
    /// For HalfHorizontal: removes tiles along the horizontal center row(s).
    /// For HalfVertical: removes tiles along the vertical center column(s).
    /// </summary>
    public static HashSet<Point> RemoveDiameterEdge(HashSet<Point> outlineTiles, ShapeContext context)
    {
        if (context.Slice == SliceMode.Full || context.ConnectDiameter)
            return outlineTiles;

        var bounds = context.GetBounds();
        var result = new HashSet<Point>(outlineTiles);

        if (context.Slice == SliceMode.HalfHorizontal)
        {
            bool keepTop = IsStartAbove(context);
            float centerY = bounds.Top + (bounds.Height - 1) / 2f;

            // The diameter row is at the boundary of the slice.
            // For keepTop: diameter is the bottom-most row of the half
            // For keepBottom: diameter is the top-most row of the half
            // Remove tiles whose only reason for being in the outline is
            // that they sit on the diameter edge.
            int diameterY = keepTop
                ? (int)Math.Floor(centerY)
                : (int)Math.Ceiling(centerY);

            // Remove tiles on the diameter row that don't have neighbors
            // further into the shape interior (they're purely diameter-edge tiles)
            result.RemoveWhere(p =>
            {
                if (p.Y != diameterY) return false;
                // Keep if tile also touches the curved boundary (has non-diameter outline neighbors)
                // Remove if it's a pure diameter tile
                bool hasAboveNeighborInSet = keepTop && outlineTiles.Contains(new Point(p.X, p.Y - 1));
                bool hasBelowNeighborInSet = !keepTop && outlineTiles.Contains(new Point(p.X, p.Y + 1));
                // If it has a neighbor in the outline on the interior side, it's a corner/curve tile — keep it
                return !(hasAboveNeighborInSet || hasBelowNeighborInSet);
            });
        }
        else // HalfVertical
        {
            bool keepLeft = IsStartLeft(context);
            float centerX = bounds.Left + (bounds.Width - 1) / 2f;

            int diameterX = keepLeft
                ? (int)Math.Floor(centerX)
                : (int)Math.Ceiling(centerX);

            result.RemoveWhere(p =>
            {
                if (p.X != diameterX) return false;
                bool hasLeftNeighborInSet = keepLeft && outlineTiles.Contains(new Point(p.X - 1, p.Y));
                bool hasRightNeighborInSet = !keepLeft && outlineTiles.Contains(new Point(p.X + 1, p.Y));
                return !(hasLeftNeighborInSet || hasRightNeighborInSet);
            });
        }

        return result;
    }

    /// <summary>
    /// Full pipeline for applying slicing to a shape:
    /// 1. Clips filled tiles to the correct half
    /// 2. Applies outline/hollow mode via OutlineHelper
    /// 3. If hollow + disconnect diameter, removes the diameter edge
    /// 
    /// This is the recommended entry point for shape providers that don't
    /// need custom half-shape computation (most shapes).
    /// </summary>
    public static ShapeTileSet ApplySlicing(HashSet<Point> fullFilledTiles, ShapeContext context)
    {
        // Step 1: Clip to half
        var clippedFilled = SliceFilledTiles(fullFilledTiles, context);
        if (clippedFilled.Count == 0)
            return new ShapeTileSet(clippedFilled, clippedFilled);

        // Step 2: Apply outline/hollow mode
        var tileSet = OutlineHelper.Apply(clippedFilled, context.Mode, context.Thickness);

        // Step 3: If hollow + disconnect diameter, remove diameter edge tiles
        if (context.Mode == ShapeMode.Hollow && context.Slice != SliceMode.Full && !context.ConnectDiameter)
        {
            var outlineTiles = new HashSet<Point>(tileSet.Tiles);
            var trimmedTiles = RemoveDiameterEdge(outlineTiles, context);
            var trimmedBoundary = GeometryHelper.GetBoundaryTiles4(trimmedTiles);
            return new ShapeTileSet(trimmedTiles, trimmedBoundary);
        }

        return tileSet;
    }
}
