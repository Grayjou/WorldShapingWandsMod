using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
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

    // ────────────────────────────────────────────────────────────
    //  Large Shape Debounce — for shapes whose dimensions exceed
    //  LargeShapeThreshold, defer full rasterization until the
    //  mouse stops moving. Draw a simple bounding-rect outline
    //  during the debounce period instead.
    // ────────────────────────────────────────────────────────────
    private const int LargeShapeThreshold = 200;
    private const int LargeShapeDebounceFrames = 8; // ~0.13s at 60fps
    private Point _debounceLastEnd;
    private int _debounceStableFrames;
    private float _debounceFadeIn; // 0→1 fade-in alpha after rasterization completes

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
            && _cacheShape.EqualDimensions == shapeSettings.EqualDimensions
            && _cacheShape.Slice == shapeSettings.Slice
            && _cacheShape.ConnectDiameter == shapeSettings.ConnectDiameter)
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

        // Draw the active selection overlay — only when visually compatible with held wand.
        // Incompatible selections are preserved in memory but not drawn; the cursor
        // highlight below provides feedback instead (as if no selection exists).
        if (wandPlayer.IsSelectionVisuallyActive() && wandPlayer.Settings.ShouldShowPreview(isHoldingWand))
        {
            var shapeSettings = GetCurrentShapeSettings(player, wandPlayer);
            DrawSelection(wandPlayer, shapeSettings);
        }
        // Draw cursor highlight when holding a wand but no visually active selection
        else if (isHoldingWand && !wandPlayer.IsSelectionVisuallyActive())
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
        else if (player.HeldItem?.ModItem is WandOfCoatingBase)
        {
            return wandPlayer.CoatingSettings.Shape;
        }
        else
        {
            return new ShapeInfo(wandPlayer.Settings.ShapeType, wandPlayer.Settings.ShapeMode, wandPlayer.Settings.Thickness, slice: wandPlayer.Settings.Slice); // fallback
        }
    }

    private bool IsHoldingWandItem(Terraria.Player player)
    {
        return player.HeldItem?.ModItem is WandOfDismantlingBase
            || player.HeldItem?.ModItem is WandOfBuildingBase
            || player.HeldItem?.ModItem is WandOfReplacementBase
            || player.HeldItem?.ModItem is WandOfWiringBase
            || player.HeldItem?.ModItem is WandOfSafekeepingBase
            || player.HeldItem?.ModItem is WandOfCoatingBase;
    }

    private void DrawSelection(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        var settings = wandPlayer.Settings;
        var selection = wandPlayer.GetVisualSelection();

        // ── Large shape debounce ────────────────────────────────
        // For shapes whose dimensions exceed LargeShapeThreshold,
        // defer full rasterization while the endpoint is changing.
        // Show a lightweight bounding-rect outline instead.
        var context = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var bounds = context.GetBounds();
        int maxDim = Math.Max(bounds.Width, bounds.Height);

        // Apply overlay render mode from config
        var renderMode = ModContent.GetInstance<WandConfig>()?.OverlayRenderMode ?? OverlayRenderMode.Auto;

        // Track endpoint stability
        if (selection.EndTile != _debounceLastEnd)
        {
            _debounceLastEnd = selection.EndTile;
            _debounceStableFrames = 0;
            // If the shape is large AND we're in Auto mode, invalidate cache and
            // reset fade-in so we don't render a stale expensive shape while dragging.
            // In AlwaysFullShape mode, skip the fade-in reset to avoid flickering.
            if (maxDim > LargeShapeThreshold && renderMode == OverlayRenderMode.Auto)
            {
                InvalidateCache();
                _debounceFadeIn = 0f;
            }
        }
        else
        {
            _debounceStableFrames++;
        }

        bool isLargeAndDragging = maxDim > LargeShapeThreshold
                                && _debounceStableFrames < LargeShapeDebounceFrames;

        if (renderMode == OverlayRenderMode.AlwaysFullShape)
            isLargeAndDragging = false; // never use bbox fallback
        else if (renderMode == OverlayRenderMode.AlwaysBoundingBox && maxDim > 1)
            isLargeAndDragging = true; // always use bbox fallback

        // Trivially-computed shapes (O(N) with no expensive rasterization) never need
        // the debounce bounding-box fallback — they render instantly at any size.
        // Rectangle also skips because bounding box IS the rectangle — they're equivalent.
        bool isTrivialShape = shapeSettings.Shape == ShapeType.CardinalLine
            || shapeSettings.Shape == ShapeType.Elbow
            || shapeSettings.Shape == ShapeType.StraightLine
            || shapeSettings.Shape == ShapeType.Rectangle;
        if (isTrivialShape)
            isLargeAndDragging = false;

        if (isLargeAndDragging)
        {
            // Draw lightweight bounding-rect preview + dimension label
            DrawBoundingRectPreview(selection, bounds, settings.ShowDimensions, shapeSettings, context);
            return;
        }

        // ── Normal path: compute full shape ─────────────────────
        var tiles = GetOrComputeTiles(shapeSettings, selection.StartTile, selection.EndTile, selection.VerticalFirst);

        if (tiles.Count == 0) return;

        // Advance fade-in after rasterization completes on a large shape.
        // In AlwaysFullShape mode the fade-in was already skipped above, so
        // _debounceFadeIn is never reset – skip the alpha ramp entirely.
        // Trivially-computed shapes also skip — they render instantly at any size,
        // so the fade-in alpha just causes flickering.
        float alphaMultiplier = 1f;
        if (renderMode == OverlayRenderMode.Auto && !isTrivialShape && maxDim > LargeShapeThreshold && _debounceFadeIn < 1f)
        {
            _debounceFadeIn = Math.Min(1f, _debounceFadeIn + 1f / WandColors.DebounceFadeInFrames);
            alphaMultiplier = _debounceFadeIn;
        }

        Color baseColor = selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? WandColors.OverlayClamped
            : WandColors.OverlayBase;

        Color fillColor = baseColor * (WandColors.OverlayFillOpacity * alphaMultiplier);
        Color outlineColor = baseColor * (WandColors.OverlayOutlineOpacity * alphaMultiplier);

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

        // Pass 3: Draw Start/End position markers — outlined cyan squares
        // When EqualDimensions is true, the raw EndTile (mouse position) may differ
        // from the actual effective End position (which is computed by GetBounds()).
        // We derive the effective End from the bounds rectangle: it's the corner
        // diagonally opposite to Start, respecting the drag direction.
        //
        // For CardinalLine shapes, the end point snaps to one of 8 cardinal/diagonal
        // directions, so the actual endpoint differs from the mouse position.
        // We compute the effective end using the direction + length from the shape.
        Point effectiveEnd = selection.EndTile;
        if (shapeSettings.Shape == ShapeType.CardinalLine)
        {
            // Cardinal lines snap to 8 directions; compute actual endpoint
            var ctx = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
            int dx = ctx.End.X - ctx.Start.X;
            int dy = ctx.End.Y - ctx.Start.Y;
            if (dx != 0 || dy != 0)
            {
                double angle = Math.Atan2(dy, dx);
                if (angle < 0) angle += Math.PI * 2;
                const double sector = Math.PI / 4.0;
                const double halfSector = Math.PI / 8.0;

                Point dir;
                if (angle < halfSector || angle >= 2 * Math.PI - halfSector)
                    dir = new Point(1, 0);
                else if (angle < halfSector + sector)
                    dir = new Point(1, 1);
                else if (angle < halfSector + 2 * sector)
                    dir = new Point(0, 1);
                else if (angle < halfSector + 3 * sector)
                    dir = new Point(-1, 1);
                else if (angle < halfSector + 4 * sector)
                    dir = new Point(-1, 0);
                else if (angle < halfSector + 5 * sector)
                    dir = new Point(-1, -1);
                else if (angle < halfSector + 6 * sector)
                    dir = new Point(0, -1);
                else
                    dir = new Point(1, -1);

                int length = Math.Max(Math.Abs(dx), Math.Abs(dy));
                effectiveEnd = new Point(
                    selection.StartTile.X + dir.X * length,
                    selection.StartTile.Y + dir.Y * length);
            }
        }
        else if (shapeSettings.EqualDimensions)
        {
            var ctx = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
            var eqBounds = ctx.GetBounds();

            // The effective End is the corner of the EqualDimensions-adjusted bounds
            // opposite to Start. Drag direction determines which corner that is.
            effectiveEnd = new Point(
                selection.EndTile.X >= selection.StartTile.X ? eqBounds.Right - 1 : eqBounds.Left,
                selection.EndTile.Y >= selection.StartTile.Y ? eqBounds.Bottom - 1 : eqBounds.Top
            );
        }

        DrawPositionMarker(pixel, selection.StartTile, WandColors.StartMarker);
        DrawPositionMarker(pixel, effectiveEnd, WandColors.EndMarker);

        Main.spriteBatch.End();

        if (settings.ShowDimensions)
        {
            var dimContext = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
            DrawDimensionLabel(shapeSettings.Shape, dimContext, tiles);
        }
    }

    /// <summary>
    /// Draws a lightweight bounding-rectangle outline while a large shape is being
    /// resized. Much cheaper than full rasterization — just 4 axis-aligned lines —
    /// so it runs smoothly even for 500×500 selections. Once the endpoint stabilises,
    /// <see cref="DrawSelection"/> takes over with the fully rasterised shape.
    /// </summary>
    private void DrawBoundingRectPreview(
        SelectionState selection, Rectangle bounds,
        bool showDimensions, ShapeInfo shapeSettings, ShapeContext context)
    {
        Color outlineColor = (selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? WandColors.OverlayClamped
            : WandColors.OverlayBase) * WandColors.DebounceBoundingRectOpacity;

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

        // World-pixel rectangle of the bounds
        int wx = bounds.X * 16;
        int wy = bounds.Y * 16;
        int ww = bounds.Width * 16;
        int wh = bounds.Height * 16;

        // Screen coordinates
        int sx = (int)(wx - Main.screenPosition.X);
        int sy = (int)(wy - Main.screenPosition.Y);

        // Four edges
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, ww, ow), outlineColor);                // Top
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + wh - ow, ww, ow), outlineColor);      // Bottom
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, ow, wh), outlineColor);                // Left
        Main.spriteBatch.Draw(pixel, new Rectangle(sx + ww - ow, sy, ow, wh), outlineColor);      // Right

        // Start/End markers
        DrawPositionMarker(pixel, selection.StartTile, WandColors.StartMarker);
        DrawPositionMarker(pixel, selection.EndTile, WandColors.EndMarker);

        Main.spriteBatch.End();

        if (showDimensions)
        {
            DrawDimensionLabel(shapeSettings.Shape, context, null);
        }
    }

    /// <summary>
    /// Draws a single-tile outlined marker at the given world tile position.
    /// Used to highlight the Start and End points during an active selection,
    /// providing clear visual feedback of the selection anchors for precise placement.
    /// </summary>
    private void DrawPositionMarker(Texture2D pixel, Point tile, Color color)
    {
        Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;

        // Cull off-screen markers
        if (screenPos.X < -16 || screenPos.X > Main.screenWidth + 16 ||
            screenPos.Y < -16 || screenPos.Y > Main.screenHeight + 16)
            return;

        int sx = (int)screenPos.X;
        int sy = (int)screenPos.Y;
        int mw = WandColors.MarkerOutlineWidth;
        Color markerColor = color * WandColors.MarkerOutlineOpacity;

        // Draw all four edges of a single-tile outline
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, mw), markerColor);               // Top
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + 16 - mw, 16, mw), markerColor);     // Bottom
        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, mw, 16), markerColor);               // Left
        Main.spriteBatch.Draw(pixel, new Rectangle(sx + 16 - mw, sy, mw, 16), markerColor);     // Right
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

        if ((shapeSettings.Shape == ShapeType.CardinalLine || shapeSettings.Shape == ShapeType.StraightLine) && shapeSettings.Thickness > 1)
        {
            // Show circle brush preview for thick cardinal lines.
            // Uses EllipseShape's IncrementalFast algorithm for ≥ 4 diameter,
            // matching the actual brush shape used by CardinalLineShape.
            int thickness = Math.Max(1, shapeSettings.Thickness);
            var offsets = EllipseShape.GetCircleBrushOffsets(thickness);

            highlightTiles = new List<Point>(offsets.Count);
            foreach (var offset in offsets)
                highlightTiles.Add(new Point(mouseTile.X + offset.X, mouseTile.Y + offset.Y));
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

        // Use pre-computed tiles — no per-frame recomputation
        var tiles = cancelState.CachedTiles;
        if (tiles == null || tiles.Count == 0) return;

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
        DrawCancelledText(cancelState, cancelState.CachedBounds, opacity);
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