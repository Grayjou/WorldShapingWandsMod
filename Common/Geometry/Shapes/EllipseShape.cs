using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class EllipseShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Ellipse;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return new ShapeTileSet(new HashSet<Point>(), new HashSet<Point>());

        var filledTiles = GetFilledTiles(bounds).ToHashSet();
        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        // Keep existing ContainsPoint logic
        var bounds = context.GetBounds();
        var parameters = CalculateEllipseParameters(bounds);
        
        float dx = point.X + 0.5f - parameters.CenterX;
        float dy = point.Y + 0.5f - parameters.CenterY;
        float normalizedDist = (dx * dx) / (parameters.RadiusX * parameters.RadiusX) +
                              (dy * dy) / (parameters.RadiusY * parameters.RadiusY);
        
        return normalizedDist <= 1.0f;
    }

    private IEnumerable<Point> GetFilledTiles(Rectangle bounds)
    {
        var parameters = CalculateEllipseParameters(bounds);
        
        for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
        {
            for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
            {
                float dx = x + 0.5f - parameters.CenterX;
                float dy = y + 0.5f - parameters.CenterY;
                float normalizedDist = (dx * dx) / (parameters.RadiusX * parameters.RadiusX) +
                                      (dy * dy) / (parameters.RadiusY * parameters.RadiusY);
                
                if (normalizedDist <= 1.0f)
                    yield return new Point(x, y);
            }
        }
    }

    // Keep CalculateEllipseParameters and EllipseParameters struct
    // DELETE: GetOutlineTiles, GetOutlineTileSet, GetHollowTiles - OutlineHelper handles these now
    
    private EllipseParameters CalculateEllipseParameters(Rectangle bounds)
    {
        return new EllipseParameters
        {
            CenterX = bounds.X + bounds.Width / 2f,
            CenterY = bounds.Y + bounds.Height / 2f,
            RadiusX = bounds.Width / 2f,
            RadiusY = bounds.Height / 2f
        };
    }

    private struct EllipseParameters
    {
        public float CenterX;
        public float CenterY;
        public float RadiusX;
        public float RadiusY;
    }
}