using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;

namespace WorldShapingWandsMod.Common.Geometry
{

    /// <summary>
    /// Result of a shape calculation, containing both the tiles to affect and their boundary points.
    /// </summary>
    public class ShapeTileSet
    {
        /// <summary>
        /// The tiles that should be affected by the operation (based on shape mode).
        /// </summary>
        public IEnumerable<Point> Tiles { get; }

        /// <summary>
        /// The boundary tiles of the shape, useful for outlining squares.
        /// </summary>
        public IEnumerable<Point> BoundaryTiles { get; }

        public ShapeTileSet(IEnumerable<Point> tiles, IEnumerable<Point> boundaryTiles)
        {
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            BoundaryTiles = boundaryTiles ?? throw new ArgumentNullException(nameof(boundaryTiles));
        }
    }
}