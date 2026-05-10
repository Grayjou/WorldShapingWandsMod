using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Utilities;

internal static class TransformPivotSnapHelper
{
    public const float TileSize = 16f;

    /// <summary>
    /// Snap world position to nearest tile center (world-space: 8, 24, 40, ...).
    /// </summary>
    public static Point SnapWorldToNearestTile(Vector2 worldPosition)
    {
        int x = (int)System.Math.Floor(worldPosition.X / TileSize + 0.5f);
        int y = (int)System.Math.Floor(worldPosition.Y / TileSize + 0.5f);
        return new Point(x, y);
    }

    /// <summary>
    /// Snap tile-center coordinate to nearest integer tile center (1.5 → 1.5, 1.6 → 1.5, 1.4 → 1.5).
    /// </summary>
    public static Vector2 SnapTileCenter(Vector2 tileCenter)
    {
        float x = (float)System.Math.Floor(tileCenter.X + 0.5f) - 0.5f;
        float y = (float)System.Math.Floor(tileCenter.Y + 0.5f) - 0.5f;
        return new Vector2(x, y);
    }

    public static Vector2 TileCenterFromTile(Point tile)
        => new(tile.X + 0.5f, tile.Y + 0.5f);
}