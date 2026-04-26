using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Renders the border of an arbitrary-shaped <see cref="Selection.SelectionCanvas"/>
/// using per-tile edge detection. For each canvas tile, checks its 4 cardinal neighbours —
/// if a neighbour is NOT in the canvas, a border segment is drawn on that edge.
/// </summary>
/// <remarks>
/// <para>
/// Only iterates tiles within <paramref name="screenBounds"/> (viewport culling).
/// Edge checks are O(1) per neighbour via <see cref="HashSet{T}"/> lookup.
/// </para>
/// <para>
/// For ~1000 visible canvas tiles this produces ~4000 edge checks and ~2000 draw calls
/// (only border-adjacent edges), comparable to the existing per-tile overlay rendering.
/// </para>
/// </remarks>
public static class CanvasBorderRenderer
{
    /// <summary>Cardinal neighbour offsets: Top, Right, Bottom, Left.</summary>
    private static readonly Point[] Neighbors =
    {
        new(0, -1),  // 0 = Top
        new(1, 0),   // 1 = Right
        new(0, 1),   // 2 = Bottom
        new(-1, 0),  // 3 = Left
    };

    /// <summary>
    /// Draws gold edge segments around the boundary of the canvas using pre-computed border edges.
    /// This overload avoids per-frame HashSet lookups — the edge mask for each border tile
    /// was already computed when the canvas was modified.
    /// Only processes tiles within the visible screen bounds (viewport culling).
    /// </summary>
    /// <param name="sb">The active <see cref="SpriteBatch"/>.</param>
    /// <param name="borderEdges">Pre-computed border tiles with their exposed-edge bitmasks.</param>
    /// <param name="screenBounds">Visible tile-coordinate bounds for viewport culling.</param>
    /// <param name="borderColor">Color for the border segments.</param>
    public static void DrawBorder(
        SpriteBatch sb,
        IReadOnlyList<(Point Tile, byte EdgeMask)> borderEdges,
        Rectangle screenBounds,
        Color borderColor)
    {
        if (borderEdges.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        const int thickness = DelimitationWandSettings.CanvasBorderThickness;

        foreach (var (tile, edgeMask) in borderEdges)
        {
            // Viewport culling — skip tiles outside the visible area
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var worldPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

            // Draw each exposed edge from the pre-computed mask
            if ((edgeMask & 0b0001) != 0) // Top
                DrawEdgeSegment(sb, pixel, worldPos, 0, thickness, borderColor);
            if ((edgeMask & 0b0010) != 0) // Right
                DrawEdgeSegment(sb, pixel, worldPos, 1, thickness, borderColor);
            if ((edgeMask & 0b0100) != 0) // Bottom
                DrawEdgeSegment(sb, pixel, worldPos, 2, thickness, borderColor);
            if ((edgeMask & 0b1000) != 0) // Left
                DrawEdgeSegment(sb, pixel, worldPos, 3, thickness, borderColor);
        }
    }

    /// <summary>
    /// Draws gold edge segments around the boundary of the canvas.
    /// Only processes tiles within the visible screen bounds.
    /// </summary>
    /// <remarks>
    /// Legacy overload that computes edges on-the-fly via per-tile neighbour checks.
    /// Prefer the <see cref="DrawBorder(SpriteBatch, IReadOnlyList{ValueTuple{Point, byte}}, Rectangle, Color)"/>
    /// overload with pre-computed border edges for better per-frame performance.
    /// </remarks>
    /// <param name="sb">The active <see cref="SpriteBatch"/>.</param>
    /// <param name="canvasTiles">All tile positions in the canvas.</param>
    /// <param name="screenBounds">Visible tile-coordinate bounds for viewport culling.</param>
    /// <param name="borderColor">Color for the border segments.</param>
    public static void DrawBorder(
        SpriteBatch sb,
        IReadOnlySet<Point> canvasTiles,
        Rectangle screenBounds,
        Color borderColor)
    {
        if (canvasTiles.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        const int thickness = DelimitationWandSettings.CanvasBorderThickness;

        foreach (var tile in canvasTiles)
        {
            // Viewport culling — skip tiles outside the visible area
            if (tile.X < screenBounds.Left || tile.X >= screenBounds.Right ||
                tile.Y < screenBounds.Top || tile.Y >= screenBounds.Bottom)
                continue;

            var worldPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

            for (int d = 0; d < 4; d++)
            {
                var neighbor = new Point(tile.X + Neighbors[d].X, tile.Y + Neighbors[d].Y);
                if (!canvasTiles.Contains(neighbor))
                {
                    DrawEdgeSegment(sb, pixel, worldPos, d, thickness, borderColor);
                }
            }
        }
    }

    /// <summary>
    /// Draws a single edge segment (2px thick line) on one side of a tile.
    /// </summary>
    /// <param name="sb">The active sprite batch.</param>
    /// <param name="pixel">A 1×1 white pixel texture (<see cref="TextureAssets.MagicPixel"/>).</param>
    /// <param name="worldPos">Screen-space position of the tile's top-left corner.</param>
    /// <param name="direction">Edge direction: 0=Top, 1=Right, 2=Bottom, 3=Left.</param>
    /// <param name="thickness">Border line thickness in pixels.</param>
    /// <param name="color">Border color.</param>
    private static void DrawEdgeSegment(
        SpriteBatch sb,
        Texture2D pixel,
        Vector2 worldPos,
        int direction,
        int thickness,
        Color color)
    {
        Rectangle dest = direction switch
        {
            0 => new Rectangle((int)worldPos.X, (int)worldPos.Y, 16, thickness),                    // Top
            1 => new Rectangle((int)worldPos.X + 16 - thickness, (int)worldPos.Y, thickness, 16),   // Right
            2 => new Rectangle((int)worldPos.X, (int)worldPos.Y + 16 - thickness, 16, thickness),   // Bottom
            3 => new Rectangle((int)worldPos.X, (int)worldPos.Y, thickness, 16),                    // Left
            _ => Rectangle.Empty,
        };

        sb.Draw(pixel, dest, color);
    }
}
