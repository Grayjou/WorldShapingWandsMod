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
    /// 
    /// Thickness-aware: when the outline has thickness &gt; 1 (Chebyshev band),
    /// the flat-side band is up to <c>thickness</c> tiles deep. All tiles in
    /// that band are removed so the diameter stays visually open regardless of
    /// outline thickness. A band width of at least 1 is always enforced so
    /// thin outlines (thickness 0 or 1) behave identically to before.
    /// </summary>
    public static HashSet<Point> RemoveDiameterEdge(HashSet<Point> outlineTiles, ShapeContext context)
    {
        if (context.Slice == SliceMode.Full || context.ConnectDiameter)
            return outlineTiles;

        var bounds = context.GetBounds();
        var result = new HashSet<Point>(outlineTiles);

        // Band width = number of tile rows/columns adjacent to the diameter
        // that belong to the outline. For thin outlines (thickness 0 or 1) this
        // is always 1. For thick outlines it matches the configured thickness.
        int bandWidth = Math.Max(1, context.Thickness);

        if (context.Slice == SliceMode.HalfHorizontal)
        {
            bool keepTop = IsStartAbove(context);
            float centerY = bounds.Top + (bounds.Height - 1) / 2f;

            int diameterY = keepTop
                ? (int)Math.Floor(centerY)
                : (int)Math.Ceiling(centerY);

            // Remove the full diameter band on the flat side of the half-shape.
            // keepTop  → band runs from (diameterY - bandWidth + 1) up to diameterY  (bottom rows)
            // keepBot  → band runs from diameterY up to (diameterY + bandWidth - 1)  (top rows)
            result.RemoveWhere(p =>
                keepTop
                    ? p.Y >= diameterY - (bandWidth - 1) && p.Y <= diameterY
                    : p.Y >= diameterY && p.Y <= diameterY + (bandWidth - 1));
        }
        else // HalfVertical
        {
            bool keepLeft = IsStartLeft(context);
            float centerX = bounds.Left + (bounds.Width - 1) / 2f;

            int diameterX = keepLeft
                ? (int)Math.Floor(centerX)
                : (int)Math.Ceiling(centerX);

            result.RemoveWhere(p =>
                keepLeft
                    ? p.X >= diameterX - (bandWidth - 1) && p.X <= diameterX
                    : p.X >= diameterX && p.X <= diameterX + (bandWidth - 1));
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
