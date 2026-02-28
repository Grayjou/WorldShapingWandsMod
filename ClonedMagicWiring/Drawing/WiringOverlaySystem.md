using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using MagicWiring.Common;
using MagicWiring.Content.Projectiles;

namespace MagicWiring.Drawing;

[Autoload(Side = ModSide.Client)]
public class WiringOverlaySystem : ModSystem
{
    /// <summary>
    /// Size of each tile in pixels.
    /// </summary>
    private const int TileSize = 16;

    /// <summary>
    /// Padding around screen bounds for culling (in pixels).
    /// </summary>
    private const int ScreenPadding = 16;

    /// <summary>
    /// Opacity multiplier for fill color (interior tiles).
    /// </summary>
    private const float FillOpacity = 0.25f;

    /// <summary>
    /// Opacity multiplier for border color (edge tiles).
    /// </summary>
    private const float BorderOpacity = 0.6f;

    public override void PostDrawTiles()
    {
        Point? start = null;
        Point? end = null;
        bool verticalFirst = false;
        bool isClamped = false;

        // Active projectile (hold mode or toggle mode preview)
        var proj = WiringWandProjectile.GetActiveProjectile();
        if (proj != null)
        {
            start = proj.StartTile;
            end = proj.EndTile;
            verticalFirst = proj.VerticalFirst;
            isClamped = proj.IsClamped;
        }
        // Toggle mode: pending start but no projectile yet (show single-tile marker)
        else if (WiringSettings.Interaction == InteractionMode.Toggle)
        {
            var wandPlayer = Main.LocalPlayer.GetModPlayer<WiringWandPlayer>();
            if (wandPlayer.PendingStartTile.HasValue)
            {
                start = wandPlayer.PendingStartTile.Value;
                end = Main.MouseWorld.ToTileCoordinates();
                verticalFirst = wandPlayer.PendingVerticalFirst;

                var config = ModContent.GetInstance<MagicWiringConfig>();
                var (clamped, wasClamped) = ShapeHelper.ClampDistance(start.Value, end.Value, 
                    config?.MaxWiringDistance ?? 200);
                end = clamped;
                isClamped = wasClamped;
            }
        }

        if (!start.HasValue || !end.HasValue) return;

        var tiles = ShapeHelper.GetShapeTiles(start.Value, end.Value, WiringSettings.Shape, verticalFirst);
        if (tiles.Count == 0) return;

        var tileSet = new HashSet<Point>(tiles);

        // Color: green=place, blue=remove, orange=clamped (pulsing)
        Color baseColor;
        if (isClamped && (int)(Main.GameUpdateCount % 30) < 15)
            baseColor = Color.Orange;
        else
            baseColor = WiringSettings.Mode == WiringMode.Place ? Color.Green : Color.Blue;

        Color fillColor = baseColor * FillOpacity;
        Color borderColor = baseColor * BorderOpacity;

        var screenBounds = new Rectangle(
            (int)Main.screenPosition.X - ScreenPadding, (int)Main.screenPosition.Y - ScreenPadding,
            Main.screenWidth + ScreenPadding * 2, Main.screenHeight + ScreenPadding * 2);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null,
            Main.GameViewMatrix.TransformationMatrix);

        foreach (var tile in tiles)
        {
            int worldX = tile.X * TileSize, worldY = tile.Y * TileSize;
            if (worldX + TileSize < screenBounds.X || worldX > screenBounds.Right ||
                worldY + TileSize < screenBounds.Y || worldY > screenBounds.Bottom) continue;

            var screenPos = new Vector2(worldX, worldY) - Main.screenPosition;
            var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, TileSize, TileSize);

            bool isEdge = !tileSet.Contains(new Point(tile.X - 1, tile.Y)) ||
                         !tileSet.Contains(new Point(tile.X + 1, tile.Y)) ||
                         !tileSet.Contains(new Point(tile.X, tile.Y - 1)) ||
                         !tileSet.Contains(new Point(tile.X, tile.Y + 1));

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, destRect,
                isEdge ? borderColor : fillColor);
        }

        Main.spriteBatch.End();
    }
}