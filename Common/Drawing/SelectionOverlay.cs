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
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    //  Tile Set Cache Ã¢â‚¬â€ prevents recomputing shapes every frame.
    //  Invalidated when selection endpoints or shape settings change.
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    private HashSet<Point> _cachedTiles;
    private Point _cacheStart, _cacheEnd;
    private ShapeInfo _cacheShape;
    private bool _cacheValid;

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    //  Area Calculation Cache Ã¢â‚¬â€ debounced to avoid per-frame cost.
    //  Only recomputed after dimensions stay stable for N frames.
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    private int _areaStableFrames;
    private (int W, int H) _lastAreaDimensions;
    private int _cachedAreaCount = -1;
    private const int AreaDebounceFrames = 10; // ~0.17 seconds at 60fps

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    //  Large Shape Debounce Ã¢â‚¬â€ for shapes whose dimensions exceed
    //  LargeShapeThreshold, defer full rasterization until the
    //  mouse stops moving. Draw a simple bounding-rect outline
    //  during the debounce period instead.
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    private const int LargeShapeThreshold = 200;
    private const int LargeShapeDebounceFrames = 8; // ~0.13s at 60fps
    private Point _debounceLastEnd;
    private int _debounceStableFrames;
    private float _debounceFadeIn; // 0Ã¢â€ â€™1 fade-in alpha after rasterization completes

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
            && _cacheShape.ConnectDiameter == shapeSettings.ConnectDiameter
            && _cacheShape.InvertSelection == shapeSettings.InvertSelection)
        {
            return _cachedTiles;
        }

        // Recompute
        var context = shapeSettings.ToShapeContext(start, end, verticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(shapeSettings.Shape, context);
        var tiles = new HashSet<Point>(tileSet.Tiles);

        // Apply inversion: swap selected Ã¢â€ â€ unselected within bounding rectangle
        if (shapeSettings.ShouldInvert)
        {
            var bounds = context.GetBounds();
            var invertedTiles = new HashSet<Point>();
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    var pt = new Point(x, y);
                    if (!tiles.Contains(pt))
                        invertedTiles.Add(pt);
                }
            }
            tiles = invertedTiles;
        }

        _cachedTiles = tiles;
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

        // Draw the active selection overlay Ã¢â‚¬â€ only when visually compatible with held wand.
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

        // Ã¢â€â‚¬Ã¢â€â‚¬ Large shape debounce Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        // For shapes whose dimensions exceed LargeShapeThreshold,
        // defer full rasterization while the endpoint is changing.
        // Show a lightweight bounding-rect outline instead.
        var context = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var bounds = context.GetBounds();
        int maxDim = Math.Max(bounds.Width, bounds.Height);

        // Apply overlay render mode from config
        var renderMode = ModContent.GetInstance<WandClientConfig>()?.OverlayRenderMode ?? OverlayRenderMode.Auto;

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
        // the debounce bounding-box fallback Ã¢â‚¬â€ they render instantly at any size.
        // Rectangle also skips because bounding box IS the rectangle Ã¢â‚¬â€ they're equivalent.
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

        // Ã¢â€â‚¬Ã¢â€â‚¬ Normal path: compute full shape Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        var tiles = GetOrComputeTiles(shapeSettings, selection.StartTile, selection.EndTile, selection.VerticalFirst);

        // Read alpha values from client config Ã¢â‚¬â€ allows per-player customisation.
        var clientConfig = ModContent.GetInstance<WandClientConfig>();
        float shapeAlpha = clientConfig?.ShapeOverlayAlpha ?? WandColors.OverlayFillOpacity;
        float negAlpha = clientConfig?.GetEffectiveNegativeSpaceAlpha() ?? 0f;

        // If shape is empty AND there's no negative space to draw (either disabled or
        // shape isn't inverted), nothing to render.
        if (tiles.Count == 0 && (negAlpha <= 0f || !shapeSettings.ShouldInvert))
            return;

        // Advance fade-in after rasterization completes on a large shape.
        // In AlwaysFullShape mode the fade-in was already skipped above, so
        // _debounceFadeIn is never reset Ã¢â‚¬â€œ skip the alpha ramp entirely.
        // Trivially-computed shapes also skip Ã¢â‚¬â€ they render instantly at any size,
        // so the fade-in alpha just causes flickering.
        float alphaMultiplier = 1f;
        if (renderMode == OverlayRenderMode.Auto && !isTrivialShape && maxDim > LargeShapeThreshold && _debounceFadeIn < 1f)
        {
            _debounceFadeIn = Math.Min(1f, _debounceFadeIn + 1f / WandColors.DebounceFadeInFrames);
            alphaMultiplier = _debounceFadeIn;
        }

        Color baseColor = selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? WandColors.OverlayClamped
            : WandColors.GetOverlayBase();

        Color fillColor = baseColor * (shapeAlpha * alphaMultiplier);
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

        // Pass 0: Negative space Ã¢â‚¬â€ draw bounding-rect tiles that are NOT in the shape
        // at a very low alpha, giving context for where the shape sits within its bounds.
        // Skipped when negAlpha is zero (player opted out) or for trivial shapes (Rectangle Filled)
        // where bounding rect == shape and there is no negative space.
        if (negAlpha > 0f)
        {
            Color negColor = baseColor * (negAlpha * alphaMultiplier);

            // Compute from the shape context (already available above)
            var negBounds = context.GetBounds();

            // Screen-cull: only iterate the portion of the bounds rectangle visible on screen.
            int screenMinX = (int)(Main.screenPosition.X / 16) - 1;
            int screenMinY = (int)(Main.screenPosition.Y / 16) - 1;
            int screenMaxX = (int)((Main.screenPosition.X + Main.screenWidth) / 16) + 1;
            int screenMaxY = (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + 1;

            int startX = Math.Max(negBounds.Left, screenMinX);
            int endX = Math.Min(negBounds.Right, screenMaxX);
            int startY = Math.Max(negBounds.Top, screenMinY);
            int endY = Math.Min(negBounds.Bottom, screenMaxY);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (tiles.Contains(new Point(x, y)))
                        continue; // Skip tiles that are part of the shape Ã¢â‚¬â€ they get drawn at full alpha

                    Vector2 screenPos = new Vector2(x * 16, y * 16) - Main.screenPosition;
                    Main.spriteBatch.Draw(pixel,
                        new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16),
                        negColor);
                }
            }
        }

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

            // Top edge Ã¢â‚¬â€ no neighbor above
            if (!tiles.Contains(new Point(tile.X, tile.Y - 1)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy, 16, ow),
                    outlineColor);
            }

            // Bottom edge Ã¢â‚¬â€ no neighbor below
            if (!tiles.Contains(new Point(tile.X, tile.Y + 1)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy + 16 - ow, 16, ow),
                    outlineColor);
            }

            // Left edge Ã¢â‚¬â€ no neighbor to the left
            if (!tiles.Contains(new Point(tile.X - 1, tile.Y)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx, sy, ow, 16),
                    outlineColor);
            }

            // Right edge Ã¢â‚¬â€ no neighbor to the right
            if (!tiles.Contains(new Point(tile.X + 1, tile.Y)))
            {
                Main.spriteBatch.Draw(pixel,
                    new Rectangle(sx + 16 - ow, sy, ow, 16),
                    outlineColor);
            }
        }

        // Pass 3: Draw Start/End position markers Ã¢â‚¬â€ outlined cyan squares
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
    /// resized. Much cheaper than full rasterization Ã¢â‚¬â€ just 4 axis-aligned lines Ã¢â‚¬â€
    /// so it runs smoothly even for 500Ãƒâ€”500 selections. Once the endpoint stabilises,
    /// <see cref="DrawSelection"/> takes over with the fully rasterised shape.
    /// </summary>
    private void DrawBoundingRectPreview(
        SelectionState selection, Rectangle bounds,
        bool showDimensions, ShapeInfo shapeSettings, ShapeContext context)
    {
        Color outlineColor = (selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? WandColors.OverlayClamped
            : WandColors.GetOverlayBase()) * WandColors.DebounceBoundingRectOpacity;

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

        // Use the centralised helper that reads Terraria's built-in tile target.
        // Correctly handles zoom, resolution, and UI scale Ã¢â‚¬â€ unlike WorldToTile(Main.MouseWorld)
        // which can have sub-tile rounding drift at non-native resolutions.
        Point mouseTile = GeometryHelper.GetMouseTile();

        // Determine highlight tiles
        List<Point> highlightTiles;

        if ((shapeSettings.Shape == ShapeType.CardinalLine || shapeSettings.Shape == ShapeType.StraightLine) && shapeSettings.Thickness > 1)
        {
            // Show circle brush preview for thick cardinal lines.
            // Uses EllipseShape's IncrementalFast algorithm for Ã¢â€°Â¥ 4 diameter,
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
        Color highlightFill = WandColors.GetOverlayBase() * (0.15f * pulse);
        Color highlightOutline = WandColors.GetOverlayBase() * (0.5f * pulse);

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

        // Ã¢â€â‚¬Ã¢â€â‚¬ Area calculation (debounced) Ã¢â€â‚¬Ã¢â€â‚¬
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

        // Ã¢â€â‚¬Ã¢â€â‚¬ Configurable offset Ã¢â‚¬â€ position relative to cursor Ã¢â€â‚¬Ã¢â€â‚¬
        // Offset places the label above and to the right of the cursor,
        // avoiding overlap with cursor icon and item icons.
        const float offsetX = 24f;
        const float offsetY = -32f;
        Vector2 cursorScreen = Main.MouseScreen;
        Vector2 screenPos = new Vector2(cursorScreen.X + offsetX, cursorScreen.Y + offsetY);

        // Ã¢â€â‚¬Ã¢â€â‚¬ Screen-bounds clamping with margin Ã¢â€â‚¬Ã¢â€â‚¬
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

        // Use pre-computed tiles Ã¢â‚¬â€ no per-frame recomputation
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