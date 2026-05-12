using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace WorldShapingWandsMod.Common.Drawing;

internal static class StencilOverlayRenderer
{
    public static void DrawFill(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color)
    {
        if (color.A == 0 || tiles.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        foreach (var tile in tiles)
        {
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var pos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), color);
        }
    }

    public static void DrawOutsideFill(SpriteBatch spriteBatch, IReadOnlySet<Point> canvasTiles, Rectangle screenBounds, Color outsideColor)
    {
        if (outsideColor.A == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        for (int x = screenBounds.Left; x < screenBounds.Right; x++)
        for (int y = screenBounds.Top; y < screenBounds.Bottom; y++)
        {
            if (canvasTiles.Contains(new Point(x, y)))
                continue;

            var pos = new Vector2(x * 16, y * 16) - Main.screenPosition;
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 16, 16), outsideColor);
        }
    }

    public static void DrawGrid(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color)
    {
        if (color.A == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        foreach (var tile in tiles)
        {
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var basePos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            spriteBatch.Draw(pixel, basePos, new Rectangle(0, 0, 16, 1), color);
            spriteBatch.Draw(pixel, basePos, new Rectangle(0, 0, 1, 16), color);
        }
    }

    public static void DrawOutline(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color, int thickness)
    {
        if (color.A == 0 || tiles.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        foreach (var tile in tiles)
        {
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var basePos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

            if (!tiles.Contains(new Point(tile.X, tile.Y - 1)))
                spriteBatch.Draw(pixel, basePos, new Rectangle(0, 0, 16, thickness), color);
            if (!tiles.Contains(new Point(tile.X + 1, tile.Y)))
                spriteBatch.Draw(pixel, basePos + new Vector2(16 - thickness, 0), new Rectangle(0, 0, thickness, 16), color);
            if (!tiles.Contains(new Point(tile.X, tile.Y + 1)))
                spriteBatch.Draw(pixel, basePos + new Vector2(0, 16 - thickness), new Rectangle(0, 0, 16, thickness), color);
            if (!tiles.Contains(new Point(tile.X - 1, tile.Y)))
                spriteBatch.Draw(pixel, basePos, new Rectangle(0, 0, thickness, 16), color);
        }
    }
}
