// Common/Geometry/OutlineHelper.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Centralized outline generation with unified thickness semantics.
/// Shapes only need to provide filled tiles — this handles mode logic.
/// </summary>
public static class OutlineHelper
{
    /// <summary>
    /// Given a set of filled tiles and a mode+thickness, produces the final tile set.
    /// </summary>
    public static ShapeTileSet Apply(HashSet<Point> filledTiles, ShapeMode mode, int thickness)
    {
        if (filledTiles.Count == 0)
            return new ShapeTileSet(filledTiles, filledTiles);

        return mode switch
        {
            ShapeMode.Filled => BuildFilled(filledTiles),
            ShapeMode.Hollow => BuildHollow(filledTiles, thickness),
            _ => BuildFilled(filledTiles)
        };
    }

    private static ShapeTileSet BuildFilled(HashSet<Point> filled)
    {
        var boundary = GeometryHelper.GetBoundaryTiles4(filled);
        return new ShapeTileSet(filled, boundary);
    }

    /// <summary>
    /// Hollow with configurable thickness:
    ///   0  = 4-neighbor boundary (slimmest)
    ///   1  = 8-neighbor boundary (fills diagonal gaps)
    ///   2+ = Chebyshev erosion, ~N tiles thick
    /// </summary>
    private static ShapeTileSet BuildHollow(HashSet<Point> filled, int thickness)
    {
        HashSet<Point> outlineTiles;

        if (thickness <= 0)
        {
            // Slim: 4-neighbor boundary only
            outlineTiles = GeometryHelper.GetBoundaryTiles4(filled);
        }
        else if (thickness == 1)
        {
            // Standard: 8-neighbor boundary (catches diagonal gaps)
            outlineTiles = GeometryHelper.GetBoundaryTiles8(filled);
        }
        else
        {
            // Thick: erode interior by thickness using Chebyshev distance,
            // then subtract what remains from the original filled set.
            var interior = ErodeChebyshev(filled, thickness);
            outlineTiles = new HashSet<Point>(filled);
            outlineTiles.ExceptWith(interior);
        }

        // Visual boundary of the outline band itself (for rendering contrast)
        var visualBoundary = GeometryHelper.GetBoundaryTiles4(outlineTiles);
        return new ShapeTileSet(outlineTiles, visualBoundary);
    }

    /// <summary>
    /// Erodes a tile set by N layers using Chebyshev distance (8-neighbor).
    /// Each iteration removes all tiles that have at least one 8-neighbor outside the set.
    /// </summary>
    private static HashSet<Point> ErodeChebyshev(HashSet<Point> tiles, int layers)
    {
        var current = new HashSet<Point>(tiles);

        for (int i = 0; i < layers; i++)
        {
            if (current.Count == 0)
                break;

            var toRemove = GeometryHelper.GetBoundaryTiles8(current);
            current.ExceptWith(toRemove);
        }

        return current;
    }
}