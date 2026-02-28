using Microsoft.Xna.Framework;

using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry
{
    /// <summary>
    /// Interface for shape calculation providers.
    /// Each shape type implements this to generate its tile points.
    /// </summary>
    public interface IShapeProvider
    {
        /// <summary>
        /// The shape type this provider handles.
        /// </summary>
        ShapeType ShapeType { get; }

        /// <summary>
        /// Calculates all tile positions that belong to this shape.
        /// </summary>
        /// <param name="context">The shape parameters.</param>
        /// <returns>Tile set containing affected tiles and boundary points.</returns>
        ShapeTileSet GetTiles(ShapeContext context);

        /// <summary>
        /// Checks if a specific point is within the filled shape.
        /// Used for hollow/outline calculations.
        /// </summary>
        /// <param name="point">The point to test (in tile coordinates).</param>
        /// <param name="context">The shape parameters.</param>
        /// <returns>True if the point is inside the filled shape.</returns>
        bool ContainsPoint(Point point, ShapeContext context);
    }
}