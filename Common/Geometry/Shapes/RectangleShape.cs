using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Geometry.Shapes;

public class RectangleShape : IShapeProvider
{
    public ShapeType ShapeType => ShapeType.Rectangle;

    public ShapeTileSet GetTiles(ShapeContext context)
    {
        var bounds = context.GetBounds();
        var filledTiles = GetFilledTiles(bounds).ToHashSet();

        if (context.Slice != SliceMode.Full)
            return SliceHelper.ApplySlicing(filledTiles, context);

        return OutlineHelper.Apply(filledTiles, context.Mode, context.Thickness);
    }

    public bool ContainsPoint(Point point, ShapeContext context)
    {
        var bounds = context.GetBounds();
        return point.X >= bounds.X &&
               point.X < bounds.X + bounds.Width &&
               point.Y >= bounds.Y &&
               point.Y < bounds.Y + bounds.Height;
    }

    private static IEnumerable<Point> GetFilledTiles(Rectangle bounds)
    {
        int maxX = bounds.X + bounds.Width - 1;
        int maxY = bounds.Y + bounds.Height - 1;

        for (int x = bounds.X; x <= maxX; x++)
            for (int y = bounds.Y; y <= maxY; y++)
                yield return new Point(x, y);
    }
}