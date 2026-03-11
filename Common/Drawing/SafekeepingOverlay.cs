using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Draws semi-transparent colored squares over protected tiles/walls
/// when the player is holding a Wand of Safekeeping.
/// Three visual states: tile-only (cyan), wall-only (magenta), both (white/gold).
/// Inspired by how vanilla wiring mode displays actuators.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SafekeepingOverlay : ModSystem
{
    // ── Colors ──────────────────────────────────────────────
    /// <summary>Overlay for positions where only the tile is protected.</summary>
    private static readonly Color TileOnlyColor = new(80, 200, 255);   // Cyan-ish

    /// <summary>Overlay for positions where only the wall is protected.</summary>
    private static readonly Color WallOnlyColor = new(255, 100, 200);  // Magenta-ish

    /// <summary>Overlay for positions where both tile and wall are protected.</summary>
    private static readonly Color BothColor = new(255, 220, 80);       // Gold-ish

    /// <summary>Fill opacity for protection squares (0–1).</summary>
    private const float FillOpacity = 0.22f;

    /// <summary>Outline opacity for protection square edges (0–1).</summary>
    private const float OutlineOpacity = 0.45f;

    /// <summary>Outline width in pixels.</summary>
    private const int OutlineWidth = 1;

    public override void PostDrawTiles()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        // Only draw when holding a Safekeeping wand
        if (player.HeldItem?.ModItem is not WandOfSafekeepingBase)
            return;

        // Quick bail if nothing is protected
        if (SafekeepingSystem.ProtectedTileCount == 0 && SafekeepingSystem.ProtectedWallCount == 0)
            return;

        // Determine screen bounds in tile coordinates (with margin)
        int screenLeft = (int)(Main.screenPosition.X / 16f) - 1;
        int screenTop = (int)(Main.screenPosition.Y / 16f) - 1;
        int screenRight = screenLeft + (Main.screenWidth / 16) + 3;
        int screenBottom = screenTop + (Main.screenHeight / 16) + 3;

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        var pixel = TextureAssets.MagicPixel.Value;

        // Collect all unique points from both sets that are on screen
        // Use ProtectedTiles and ProtectedWalls as the iteration source
        // We iterate tiles first, then walls, tracking which points we've already drawn

        var drawnPoints = new System.Collections.Generic.HashSet<Point>();

        // Pass 1: Draw all protected tiles
        foreach (var pt in SafekeepingSystem.ProtectedTiles)
        {
            if (pt.X < screenLeft || pt.X > screenRight ||
                pt.Y < screenTop || pt.Y > screenBottom)
                continue;

            bool alsoWall = SafekeepingSystem.IsWallProtected(pt);
            Color color = alsoWall ? BothColor : TileOnlyColor;

            DrawProtectionSquare(pixel, pt, color);
            drawnPoints.Add(pt);
        }

        // Pass 2: Draw wall-only protected positions (skip those already drawn)
        foreach (var pt in SafekeepingSystem.ProtectedWalls)
        {
            if (drawnPoints.Contains(pt))
                continue;

            if (pt.X < screenLeft || pt.X > screenRight ||
                pt.Y < screenTop || pt.Y > screenBottom)
                continue;

            DrawProtectionSquare(pixel, pt, WallOnlyColor);
        }

        Main.spriteBatch.End();
    }

    private void DrawProtectionSquare(Texture2D pixel, Point tile, Color baseColor)
    {
        Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
        int sx = (int)screenPos.X;
        int sy = (int)screenPos.Y;

        // Fill
        Main.spriteBatch.Draw(pixel,
            new Rectangle(sx, sy, 16, 16),
            baseColor * FillOpacity);

        // Outline (always draw all 4 edges — these are individual protected cells, not shapes)
        Color outlineCol = baseColor * OutlineOpacity;

        // Top
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, OutlineWidth), outlineCol);
        // Bottom
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + 16 - OutlineWidth, 16, OutlineWidth), outlineCol);
        // Left
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, OutlineWidth, 16), outlineCol);
        // Right
        Main.spriteBatch.Draw(pixel, new Rectangle(sx + 16 - OutlineWidth, sy, OutlineWidth, 16), outlineCol);
    }
}
