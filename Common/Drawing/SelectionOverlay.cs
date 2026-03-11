using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Geometry.Shapes;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

[Autoload(Side = ModSide.Client)]
public class SelectionOverlay : ModSystem
{
    // ────────────────────────────────────────────────────────────
    //  Tile Set Cache — prevents recomputing shapes every frame.
    //  Invalidated when selection endpoints or shape settings change.
    // ────────────────────────────────────────────────────────────
    private HashSet<Point> _cachedTiles;
    private Point _cacheStart, _cacheEnd;
    private ShapeInfo _cacheShape;
    private bool _cacheValid;

    // ────────────────────────────────────────────────────────────
    //  Area Calculation Cache — debounced to avoid per-frame cost.
    //  Only recomputed after dimensions stay stable for N frames.
    // ────────────────────────────────────────────────────────────
    private int _areaStableFrames;
    private (int W, int H) _lastAreaDimensions;
    private int _cachedAreaCount = -1;
    private const int AreaDebounceFrames = 10; // ~0.17 seconds at 60fps

    private void InvalidateCache()
    {
        _cacheValid = false;
        _cachedTiles = null;
    }

    private bool _cacheVerticalFirst;

    private HashSet<Point> GetOrComputeTiles(ShapeInfo shapeSettings, Point start, Point end, bool verticalFirst)
    {
        // Check if cache is still valid
        if (_cacheValid && _cacheStart == start && _cacheEnd == end
            && _cacheVerticalFirst == verticalFirst
            && _cacheShape.Shape == shapeSettings.Shape
            && _cacheShape.FillMode == shapeSettings.FillMode
            && _cacheShape.Thickness == shapeSettings.Thickness
            && _cacheShape.EqualDimensions == shapeSettings.EqualDimensions)
        {
            return _cachedTiles;
        }

        // Recompute
        var context = shapeSettings.ToShapeContext(start, end, verticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(shapeSettings.Shape, context);
        _cachedTiles = new HashSet<Point>(tileSet.Tiles);
        _cacheStart = start;
        _cacheEnd = end;
        _cacheVerticalFirst = verticalFirst;
        _cacheShape = shapeSettings;
        _cacheValid = true;
        return _cachedTiles;
    }

    public override void PostDrawTiles()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        bool isHoldingWand = IsHoldingWandItem(player);

        // Draw the cancelled selection overlay (fading out) if present
        if (wandPlayer.CancelledSelection != null && !wandPlayer.CancelledSelection.IsExpired)
        {
            DrawCancelledSelection(wandPlayer.CancelledSelection);
        }

        // Draw the active selection overlay
        if (wandPlayer.Selection.IsActive && wandPlayer.Settings.ShouldShowPreview(isHoldingWand))
        {
            var shapeSettings = GetCurrentShapeSettings(player, wandPlayer);
            DrawSelection(wandPlayer, shapeSettings);
        }
        // Draw cursor highlight when holding a wand but no selection is active
        else if (isHoldingWand && !wandPlayer.Selection.IsActive)
        {
            var shapeSettings = GetCurrentShapeSettings(player, wandPlayer);
            DrawCursorHighlight(shapeSettings);
        }
    }

    private ShapeInfo GetCurrentShapeSettings(Player player, WandPlayer wandPlayer)
    {
        if (player.HeldItem?.ModItem is WandOfDismantlingBase)
        {
            return wandPlayer.DismantlingSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfBuildingBase)
        {
            return wandPlayer.BuildingSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfReplacementBase)
        {
            return wandPlayer.ReplacementSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfWiringBase)
        {
            return wandPlayer.WiringSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfSafekeepingBase)
        {
            return wandPlayer.SafekeepingSettings.Shape;
        }
        else
        {
            return new ShapeInfo(wandPlayer.Settings.ShapeType, wandPlayer.Settings.ShapeMode, wandPlayer.Settings.Thickness); // fallback
        }
    }

    private bool IsHoldingWandItem(Terraria.Player player)
    {
        return player.HeldItem?.ModItem is WandOfDismantlingBase
            || player.HeldItem?.ModItem is WandOfBuildingBase
            || player.HeldItem?.ModItem is WandOfReplacementBase
            || player.HeldItem?.ModItem is WandOfWiringBase
            || player.HeldItem?.ModItem is WandOfSafekeepingBase;
    }

    private void DrawSelection(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        var settings = wandPlayer.Settings;
        var selection = wandPlayer.Selection;

        var tiles = GetOrComputeTiles(shapeSettings, selection.StartTile, selection.EndTile, selection.VerticalFirst);

        if (tiles.Count == 0) return;

        Color baseColor = selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? WandColors.OverlayClamped
            : WandColors.OverlayBase;

        Color fillColor = baseColor * WandColors.OverlayFillOpacity;
        Color outlineColor = baseColor * WandColors.OverlayOutlineOpacity;

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
        int ow = WandColors.OverlayOutlineWidth;

        // Pass 1: Fill all tiles
        foreach (var tile in tiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

            if (screenPos.X < -16 || screenPos.X > Main.screenWidth + 16 ||
                screenPos.Y < -16 || screenPos.Y > Main.screenHeight + 16)
                continue;

            Main.spriteBatch.Draw(pixel,
                new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16),
                fillColor);
        }

        // Pass 2: Draw outline edges
        foreach (var tile in tiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

            if (screenPos.X < -32 || screenPos.X > Main.screenWidth + 32 ||
                screenPos.Y < -32 || screenPos.Y > Main.screenHeight + 32)
                continue;

            int sx = (int)screenPos.X;
            int sy = (int)screenPos.Y;

            // Top edge — no neighbor above
            if (!tiles.Contains(new Point(tile.X, tile.Y - 1)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy, 16, ow),
                    outlineColor);
            }

            // Bottom edge — no neighbor below
            if (!tiles.Contains(new Point(tile.X, tile.Y + 1)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy + 16 - ow, 16, ow),
                    outlineColor);
            }

            // Left edge — no neighbor to the left
            if (!tiles.Contains(new Point(tile.X - 1, tile.Y)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy, ow, 16),
                    outlineColor);
            }

            // Right edge — no neighbor to the right
            if (!tiles.Contains(new Point(tile.X + 1, tile.Y)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx + 16 - ow, sy, ow, 16),
                    outlineColor);
            }
        }

        Main.spriteBatch.End();

        if (settings.ShowDimensions)
        {
            var dimContext = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
            DrawDimensionLabel(shapeSettings.Shape, dimContext, tiles);
        }
    }

    /// <summary>
    /// Draws a cursor highlight at the mouse position when holding a wand.
    /// For cardinal lines with thickness > 1, shows a circle of diameter = thickness.
    /// For all other shapes, shows a single tile highlight.
    /// This gives the player immediate visual feedback of where their selection will start.
    /// </summary>
    private void DrawCursorHighlight(ShapeInfo shapeSettings)
    {
        // Don't draw if mouse is over UI
        if (Main.LocalPlayer.mouseInterface) return;

        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);

        // Determine highlight tiles
        List<Point> highlightTiles;

        if (shapeSettings.Shape == ShapeType.CardinalLine && shapeSettings.Thickness > 1)
        {
            // Show circle brush preview for thick cardinal lines
            int thickness = shapeSettings.Thickness;
            if (thickness % 2 == 0) thickness -= 1;
            thickness = Math.Max(1, thickness);
            int radius = thickness / 2;

            // Reuse the cached circle offsets from CardinalLineShape
            highlightTiles = new List<Point>();
            int radiusSq = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radiusSq)
                        highlightTiles.Add(new Point(mouseTile.X + dx, mouseTile.Y + dy));
                }
            }
        }
        else
        {
            // Single tile highlight for all other shapes
            highlightTiles = new List<Point> { mouseTile };
        }

        // Draw with a subtle pulse effect
        float pulse = 0.5f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.08);
        Color highlightFill = WandColors.OverlayBase * (0.15f * pulse);
        Color highlightOutline = WandColors.OverlayBase * (0.5f * pulse);

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
        int ow = WandColors.OverlayOutlineWidth;
        var tileSet = new HashSet<Point>(highlightTiles);

        // Fill pass
        foreach (var tile in highlightTiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            Main.spriteBatch.Draw(pixel,
                new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16),
                highlightFill);
        }

        // Outline pass (only outer edges)
        foreach (var tile in highlightTiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            int sx = (int)screenPos.X;
            int sy = (int)screenPos.Y;

            if (!tileSet.Contains(new Point(tile.X, tile.Y - 1)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, ow), highlightOutline);
            if (!tileSet.Contains(new Point(tile.X, tile.Y + 1)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + 16 - ow, 16, ow), highlightOutline);
            if (!tileSet.Contains(new Point(tile.X - 1, tile.Y)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, ow, 16), highlightOutline);
            if (!tileSet.Contains(new Point(tile.X + 1, tile.Y)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx + 16 - ow, sy, ow, 16), highlightOutline);
        }

        Main.spriteBatch.End();
    }

    private void DrawDimensionLabel(ShapeType shapeType, ShapeContext context, HashSet<Point> tiles)
    {
        var (displayWidth, displayHeight) = ShapeRegistry.GetDisplayDimensions(shapeType, context);

        // ── Area calculation (debounced) ──
        var currentDims = (displayWidth, displayHeight);
        if (currentDims != _lastAreaDimensions)
        {
            _lastAreaDimensions = currentDims;
            _areaStableFrames = 0;
            _cachedAreaCount = -1; // invalidate while dimensions are changing
        }
        else
        {
            _areaStableFrames++;
        }

        if (_cachedAreaCount < 0 && _areaStableFrames >= AreaDebounceFrames && tiles != null)
        {
            _cachedAreaCount = tiles.Count;
        }

        string dimensionText = $"{displayWidth} x {displayHeight}";
        if (_cachedAreaCount >= 0)
            dimensionText += $"  ({_cachedAreaCount})";

        const float textScale = 0.9f;

        // ── Configurable offset — position relative to cursor ──
        // Offset places the label above and to the right of the cursor,
        // avoiding overlap with cursor icon and item icons.
        const float offsetX = 24f;
        const float offsetY = -32f;
        Vector2 cursorScreen = Main.MouseScreen;
        Vector2 screenPos = new Vector2(cursorScreen.X + offsetX, cursorScreen.Y + offsetY);

        // ── Screen-bounds clamping with margin ──
        const float margin = 4f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(dimensionText) * textScale;

        screenPos.X = Math.Clamp(screenPos.X, margin, Main.screenWidth - margin - textSize.X);
        screenPos.Y = Math.Clamp(screenPos.Y, margin, Main.screenHeight - margin - textSize.Y);

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.UIScaleMatrix  // Use UI scale so it stays crisp at any zoom
        );

        Utils.DrawBorderString(Main.spriteBatch, dimensionText, screenPos,
            Color.White * WandColors.DimensionLabelOpacity, textScale);

        Main.spriteBatch.End();
    }

    /// <summary>
    /// Draws the cancelled selection overlay with fading opacity and a "Cancelled" text label.
    /// </summary>
    private void DrawCancelledSelection(CancelledSelectionState cancelState)
    {
        float opacity = cancelState.Opacity;
        if (opacity <= 0f) return;

        var context = cancelState.Shape.ToShapeContext(cancelState.StartTile, cancelState.EndTile, cancelState.VerticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(cancelState.Shape.Shape, context);
        var tiles = new HashSet<Point>(tileSet.Tiles);

        if (tiles.Count == 0) return;

        Color baseColor = cancelState.CancelColor;
        Color fillColor = baseColor * (WandColors.OverlayFillOpacity * opacity);
        Color outlineColor = baseColor * (WandColors.OverlayOutlineOpacity * opacity);

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
        int ow = WandColors.OverlayOutlineWidth;

        // Fill pass
        foreach (var tile in tiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            if (screenPos.X < -16 || screenPos.X > Main.screenWidth + 16 ||
                screenPos.Y < -16 || screenPos.Y > Main.screenHeight + 16)
                continue;

            Main.spriteBatch.Draw(pixel,
                new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16),
                fillColor);
        }

        // Outline pass
        foreach (var tile in tiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            if (screenPos.X < -32 || screenPos.X > Main.screenWidth + 32 ||
                screenPos.Y < -32 || screenPos.Y > Main.screenHeight + 32)
                continue;

            int sx = (int)screenPos.X;
            int sy = (int)screenPos.Y;

            if (!tiles.Contains(new Point(tile.X, tile.Y - 1)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, ow), outlineColor);
            if (!tiles.Contains(new Point(tile.X, tile.Y + 1)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + 16 - ow, 16, ow), outlineColor);
            if (!tiles.Contains(new Point(tile.X - 1, tile.Y)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, ow, 16), outlineColor);
            if (!tiles.Contains(new Point(tile.X + 1, tile.Y)))
                Main.spriteBatch.Draw(pixel, new Rectangle(sx + 16 - ow, sy, ow, 16), outlineColor);
        }

        Main.spriteBatch.End();

        // Draw fading "Cancelled" text at the center of the selection
        DrawCancelledText(cancelState, context.GetBounds(), opacity);
    }

    /// <summary>
    /// Draws a fading "Cancelled" text centered on the cancelled selection.
    /// </summary>
    private void DrawCancelledText(CancelledSelectionState cancelState, Rectangle bounds, float opacity)
    {
        // Use a separate timer for text fade (slightly shorter so text vanishes first)
        float textOpacity = cancelState.ElapsedTicks < WandColors.CancelTextDurationTicks
            ? 1f - (float)cancelState.ElapsedTicks / WandColors.CancelTextDurationTicks
            : 0f;

        if (textOpacity <= 0f) return;

        const string cancelText = "Cancelled";
        const float textScale = 0.9f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(cancelText) * textScale;

        // Center of the selection bounds in world coordinates
        Vector2 worldCenter = new Vector2(
            bounds.X * 16 + bounds.Width * 16 * 0.5f,
            bounds.Y * 16 + bounds.Height * 16 * 0.5f
        );

        // Rise slightly as it fades
        float rise = cancelState.ElapsedTicks * 0.3f;
        Vector2 screenPos = worldCenter - Main.screenPosition - textSize * 0.5f;
        screenPos.Y -= rise;

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        Utils.DrawBorderString(Main.spriteBatch, cancelText, screenPos,
            cancelState.CancelColor * textOpacity, textScale);

        Main.spriteBatch.End();
    }
}