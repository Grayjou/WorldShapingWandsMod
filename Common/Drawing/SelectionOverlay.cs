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
using WorldShapingWandsMod.Common.Items;
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
    // (C-S3 2026-05-03) Cache the LastMagicWandShape reference so that a new Read commit
    // at the same world position (same start/end/shape key) still invalidates the cache
    // and redraws the new capture instead of reusing the previous tile set.
    private StoredMagicWandShape _cacheLastMagicWandShape;

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    //  Area Calculation Cache Ã¢â‚¬â€ debounced to avoid per-frame cost.
    //  Only recomputed after dimensions stay stable for N frames.
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    private int _areaStableFrames;
    private (int W, int H) _lastAreaDimensions;
    private int _cachedAreaCount = -1;
    private const int AreaDebounceFrames = 10; // ~0.17 seconds at 60fps

    // ── Narrow-Stock Cache ────────────────────────────────────────
    // Tracks how many of the FIRST eligible source item the held wand
    // would consume. Used by the dimension label tint matrix so the
    // colour reflects (stock vs cost) × ExhaustMode. Refreshed on a
    // short tick to follow live inventory changes without scanning
    // every frame.
    private int _cachedNarrowStock = -1;
    private bool _cachedNarrowInfinite;
    private int _narrowStockTick;
    private const int NarrowStockRefreshFrames = 15; // ~0.25s at 60fps

    // ── Stamp Smoothing v3 (W-S4-1, S4 2026-04-24) ─────────────────────────
    // No per-frame state lives here anymore. The previous _prevScreenMouse /
    // _prevScreenPosition fields and the SelectionOverlayMath.SmoothPosition
    // helper they fed have been deleted: v3 maintains the smoothed anchor as
    // world-space state on WandPlayer.SmoothAnchorWorld, updated once per
    // logic tick by WandPlayer.UpdateSmoothAnchor (called from
    // BaseCyclingWand.TemplateStampHoldItem). DrawSelection just reads it.
    //
    // Net deletion vs v1/v2: -2 fields, -1 helper method, -PositiveMod, -all
    // dual-draw scaffolding. Drift-by-camera-offset is impossible by
    // construction because no camera-offset variable is in scope of the ease.
    // ────────────────────────────────────────────────────────────────────────

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
        _cacheLastMagicWandShape = null;
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
            && _cacheShape.InvertSelection == shapeSettings.InvertSelection
            // (C-S3 2026-05-03) For Magic Wand shapes the tile set lives in
            // LastMagicWandShape, not in start/end. Two commits at the same world
            // position would hit cache and reuse the previous capture without this.
            && (shapeSettings.Shape != ShapeType.MagicWandRead
                || ReferenceEquals(_cacheLastMagicWandShape,
                    Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.LastMagicWandShape)))
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
        _cacheLastMagicWandShape = shapeSettings.Shape == ShapeType.MagicWandRead
            ? Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.LastMagicWandShape
            : null;
        return _cachedTiles;
    }

    public override void PostDrawTiles()
    {
        if (_managedByOverlaySystem) return;

        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        DrawAll();
    }

    /// <summary>
    /// Core drawing logic extracted from PostDrawTiles.
    /// Called directly by <see cref="SelectionOverlayAdapter"/> when the overlay
    /// system is active, or by PostDrawTiles when it is not.
    /// </summary>
    public void DrawAll()
    {
        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        bool isHoldingWand = IsHoldingWandItem(player);

#if DEBUG
        TickDebugSnapshot(player, wandPlayer);
#endif

        // Draw the cancelled selection overlay (fading out) if present
        if (wandPlayer.CancelledSelection != null && !wandPlayer.CancelledSelection.IsExpired)
        {
            DrawCancelledSelection(wandPlayer.CancelledSelection);
        }

        // Draw the active selection overlay -- only when visually compatible with held wand.
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
            if (!DrawStoredMagicReadPreviewIfAvailable(wandPlayer, shapeSettings))
                DrawCursorHighlight(shapeSettings);
        }
    }

#if DEBUG
    /// <summary>
    /// Emits a magenta chat snapshot whenever the overlay state changes.
    /// Edge-triggered: fires at most once per state transition, never spams.
    /// Design: C-S4 2026-05-03 (DesignDoc_OverlayDebugSnapshot_OnDemand.md §3).
    /// </summary>
    private void TickDebugSnapshot(Player player, WandPlayer wp)
    {
        var snap = global::WorldShapingWandsMod.Common.Debug.OverlaySnapshot.Capture(player, wp, this);
        if (!_hasLastSnapshot || !snap.Equals(_lastSnapshot))
        {
            _hasLastSnapshot = true;
            _lastSnapshot = snap;
            Main.NewText(snap.ToChatLine(), Color.Magenta);
        }
    }
#endif

    private bool DrawStoredMagicReadPreviewIfAvailable(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        if (shapeSettings.Shape != ShapeType.MagicWandRead)
            return false;

        var stored = wandPlayer.LastMagicWandShape;
        if (stored?.Tiles == null || stored.Tiles.Count == 0)
            return false;

        var tiles = stored.Tiles;
        Color cursorBaseColor = ResolveOverlayBaseColor(Main.LocalPlayer);
        Color fillColor = cursorBaseColor * 0.14f;
        Color outlineColor = cursorBaseColor * 0.32f;

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

        foreach (var tile in tiles)
        {
            Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
            if (screenPos.X < -16 || screenPos.X > Main.screenWidth + 16 ||
                screenPos.Y < -16 || screenPos.Y > Main.screenHeight + 16)
                continue;

            int sx = (int)screenPos.X;
            int sy = (int)screenPos.Y;

            Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, 16), fillColor);

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
        return true;
    }

    /// <summary>
    /// When true, PostDrawTiles is skipped because the overlay system drives rendering.
    /// Set by <see cref="SelectionOverlayAdapter"/> during initialization.
    /// </summary>
    internal bool _managedByOverlaySystem;

#if DEBUG
    // Edge-triggered snapshot state (C-S4 2026-05-03).
    internal int DebugCachedTilesCount => _cachedTiles?.Count ?? 0;
    private global::WorldShapingWandsMod.Common.Debug.OverlaySnapshot _lastSnapshot;
    private bool _hasLastSnapshot;
#endif

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
        else if (player.HeldItem?.ModItem is WandOfFluidsBase)
        {
            return wandPlayer.FluidsSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfTorchesBase)
        {
            return wandPlayer.TorchSettings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfDelimitationBase)
        {
            var swp = player.GetModPlayer<DelimitationWandPlayer>();
            return swp.Settings.Shape;
        }
        else if (player.HeldItem?.ModItem is WandOfMoldingBase)
        {
            var mwp = player.GetModPlayer<MoldingWandPlayer>();
            return mwp.Settings.Shape;
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
            || player.HeldItem?.ModItem is WandOfCoatingBase
            || player.HeldItem?.ModItem is WandOfFluidsBase
            || player.HeldItem?.ModItem is WandOfTorchesBase
            || player.HeldItem?.ModItem is WandOfDelimitationBase
            || player.HeldItem?.ModItem is WandOfMoldingBase;
    }

    /// <summary>
    /// Resolves the overlay base color for the current player's held wand.
    /// For Fluids wands, returns a per-liquid color from WandColors.GetOverlayBaseForFluids.
    /// For all other families, delegates to WandColors.GetOverlayBaseForFamily.
    /// </summary>
    private static Color ResolveOverlayBaseColor(Player player)
    {
        WandFamily family = BaseCyclingWand.GetCurrentFamily(player);

        if (family == WandFamily.Fluids)
        {
            var fluidsSettings = player.GetModPlayer<WandPlayer>().FluidsSettings;
            return WandColors.GetOverlayBaseForFluids(fluidsSettings);
        }

        return WandColors.GetOverlayBaseForFamily(family);
    }

    private void DrawSelection(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        var settings = wandPlayer.Settings;
        var selection = wandPlayer.GetVisualSelection();

        // ── Stamp anchor sub-pixel offset (W-S4-1, S4 2026-04-24) ────────────
        // Per Cavendish DesignDoc_StampSmoothingV3.md §3.3. Replaces the v1/v2
        // SelectionOverlayMath.SmoothPosition path entirely (those eased in
        // screen space against MouseScreen and produced the GrayJou-reported
        // Δ up to 700 px during the W-S3-1 dual-draw playtest verdict).
        //
        // v3 model: WandPlayer.UpdateSmoothAnchor maintains a world-space
        // exponential ease toward the precise anchor on every logic tick (called
        // from BaseCyclingWand.TemplateStampHoldItem). At draw time we just pick
        // between the smoothed and precise anchor and do a single named
        // world-to-screen subtraction. No camera-offset variable is in scope of
        // the ease, so coordinate-system drift is impossible by construction.
        //
        // Net deletion of v1/v2 scaffold: SelectionOverlayMath.SmoothPosition,
        // PositiveMod, _prevScreenMouse/_prevScreenPosition tracking,
        // GetOverlayPositionOffset call site, and all W-S3-1 dual-draw
        // diagnostic blocks.
        var stampRenderMode = WandConfigs.Overlay?.StampRenderMode ?? StampRenderMode.Precise;

        Vector2 anchorTileWorld;
        if (wandPlayer.IsStampLocked)
        {
            int bboxMinX = Math.Min(selection.StartTile.X, selection.EndTile.X);
            int bboxMinY = Math.Min(selection.StartTile.Y, selection.EndTile.Y);
            anchorTileWorld = new Vector2(
                (bboxMinX + wandPlayer.StampAnchorOffset.X) * 16f,
                (bboxMinY + wandPlayer.StampAnchorOffset.Y) * 16f);
        }
        else
        {
            anchorTileWorld = Vector2.Zero;
        }

        // §3.3 draw-step. When Smooth + locked + initialised, draw at the
        // smoothed world anchor; otherwise draw at the precise tile-snapped
        // anchor (= grid-snap behaviour). The translation from "anchor world"
        // to "subPixelOffset added to tile*16 - cameraPos" is just the gap
        // between the smoothed anchor and the tile-snapped anchor.
        Vector2 subPixelOffset;
        if (stampRenderMode == StampRenderMode.Smooth
            && wandPlayer.IsStampLocked
            && wandPlayer.SmoothAnchorInitialised)
        {
            subPixelOffset = wandPlayer.SmoothAnchorWorld - anchorTileWorld;
        }
        else
        {
            subPixelOffset = Vector2.Zero;
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ Large shape debounce Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        // For shapes whose dimensions exceed LargeShapeThreshold,
        // defer full rasterization while the endpoint is changing.
        // Show a lightweight bounding-rect outline instead.
        var context = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var bounds = context.GetBounds();
        int maxDim = Math.Max(bounds.Width, bounds.Height);

        // Apply overlay render mode from config
        var renderMode = WandConfigs.Overlay?.OverlayRenderMode ?? OverlayRenderMode.Auto;

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
            || shapeSettings.Shape == ShapeType.StraightLine;
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
        var clientConfig = WandConfigs.Overlay;
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
            : ResolveOverlayBaseColor(Main.LocalPlayer);

        // Apply step-based brightness (dimmer at first step, brighter near execution)
        float stepBrightness = WandColors.GetStepBrightness(
            wandPlayer.SelectionClickStep,
            wandPlayer.Player.HeldItem?.ModItem switch
            {
                BaseCyclingWand bcw => (int)bcw.WandSelectionMode,
                _ => 1
            });
        baseColor = new Color(
            (int)(baseColor.R * stepBrightness),
            (int)(baseColor.G * stepBrightness),
            (int)(baseColor.B * stepBrightness));

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

                    Vector2 screenPos = new Vector2(x * 16, y * 16) - Main.screenPosition + subPixelOffset;
                    Main.spriteBatch.Draw(pixel,
                        new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16),
                        negColor);
                }
            }
        }

        // Pass 1 + 2 draw strategy:
        // For very large shapes (especially Stamp/rectangle-like domains), iterating
        // every tile each frame causes spikes even with per-tile screen culls.
        // Prefer a viewport-window walk when visible area is smaller than tile count.
        int screenCullMinX = (int)(Main.screenPosition.X / 16) - 2;
        int screenCullMinY = (int)(Main.screenPosition.Y / 16) - 2;
        int screenCullMaxX = (int)((Main.screenPosition.X + Main.screenWidth) / 16) + 2;
        int screenCullMaxY = (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + 2;

        int visStartX = Math.Max(bounds.Left, screenCullMinX);
        int visStartY = Math.Max(bounds.Top, screenCullMinY);
        int visEndX = Math.Min(bounds.Right, screenCullMaxX);
        int visEndY = Math.Min(bounds.Bottom, screenCullMaxY);

        int visibleArea = Math.Max(0, visEndX - visStartX) * Math.Max(0, visEndY - visStartY);
        // (C-S1.c 2026-05-03) Disable viewport-window optimisation for Magic Wand
        // shapes. For MagicWandApply and MagicWandRead, context.GetBounds() is the
        // cursor click bbox (1×1), not the bounding box of the translated/captured
        // tile set. This means visibleArea ≈ 1 << tiles.Count, so useViewportWindow
        // is always true, the walk covers only the 5×5 area around the cursor, and
        // the entire shape is invisible unless the cursor is placed exactly on a
        // captured tile. The screen-cull inside the fallback foreach passes handles
        // performance adequately for these shapes.
        bool isMagicWandShape = shapeSettings.Shape == ShapeType.MagicWandApply
                             || shapeSettings.Shape == ShapeType.MagicWandRead;
        bool useViewportWindow = !isMagicWandShape && visibleArea > 0 && visibleArea < tiles.Count;

        if (useViewportWindow)
        {
            for (int y = visStartY; y < visEndY; y++)
            {
                for (int x = visStartX; x < visEndX; x++)
                {
                    var tile = new Point(x, y);
                    if (!tiles.Contains(tile))
                        continue;

                    Vector2 screenPos = new Vector2(x * 16, y * 16) - Main.screenPosition + subPixelOffset;
                    int sx = (int)screenPos.X;
                    int sy = (int)screenPos.Y;

                    Main.spriteBatch.Draw(pixel,
                        new Rectangle(sx, sy, 16, 16),
                        fillColor);

                    if (!tiles.Contains(new Point(x, y - 1)))
                        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, 16, ow), outlineColor);
                    if (!tiles.Contains(new Point(x, y + 1)))
                        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy + 16 - ow, 16, ow), outlineColor);
                    if (!tiles.Contains(new Point(x - 1, y)))
                        Main.spriteBatch.Draw(pixel, new Rectangle(sx, sy, ow, 16), outlineColor);
                    if (!tiles.Contains(new Point(x + 1, y)))
                        Main.spriteBatch.Draw(pixel, new Rectangle(sx + 16 - ow, sy, ow, 16), outlineColor);
                }
            }
        }
        else
        {
            // Pass 1: Fill all tiles
            foreach (var tile in tiles)
            {
                Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition + subPixelOffset;

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
                Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition + subPixelOffset;

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

        DrawPositionMarker(pixel, selection.StartTile, WandColors.StartMarker, subPixelOffset);
        DrawPositionMarker(pixel, effectiveEnd, WandColors.EndMarker, subPixelOffset);

        Main.spriteBatch.End();

#if DEBUG
        // ── W-S4-1 (S4 2026-04-24) v3 smoothing debug HUD ─────────────
        // Always-on in DEBUG builds. Prints the v3 smoothing state directly
        // from WandPlayer (no helper-class snapshot anymore — that piece of
        // v1/v2 scaffolding was deleted with SelectionOverlayMath). Cheap
        // (single string per frame); never compiled into Release.
        DrawSmoothOverlayDebugHud(wandPlayer, stampRenderMode, anchorTileWorld, subPixelOffset);
#endif

        if (settings.ShowDimensions)
        {
            var dimContext = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
            DrawDimensionLabel(shapeSettings.Shape, dimContext, tiles);
        }
    }

#if DEBUG
    private void DrawSmoothOverlayDebugHud(
        WandPlayer wp, StampRenderMode mode, Vector2 anchorTileWorld, Vector2 subPixelOffset)
    {
        Vector2 mouseTileF = Main.MouseWorld / 16f;
        string text =
            $"WSW Overlay v3  mode={mode}  locked={wp.IsStampLocked}  init={wp.SmoothAnchorInitialised}\n" +
            $"  mouseTile  = ({(int)Math.Floor(mouseTileF.X)}, {(int)Math.Floor(mouseTileF.Y)})\n" +
            $"  anchorTW   = ({anchorTileWorld.X:F1}, {anchorTileWorld.Y:F1})\n" +
            $"  smoothAW   = ({wp.SmoothAnchorWorld.X:F1}, {wp.SmoothAnchorWorld.Y:F1})\n" +
            $"  subPixel   = ({subPixelOffset.X:F2}, {subPixelOffset.Y:F2})  |Δ|={subPixelOffset.Length():F2}";
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        Terraria.Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(8, 96), Color.Lime);
        Main.spriteBatch.End();
    }
#endif

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
            : ResolveOverlayBaseColor(Main.LocalPlayer))
            * WandColors.DebounceBoundingRectOpacity;

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
    private void DrawPositionMarker(Texture2D pixel, Point tile, Color color, Vector2 offset = default)
    {
        Vector2 screenPos = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition + offset;

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
        Color cursorBaseColor = ResolveOverlayBaseColor(Main.LocalPlayer);
        Color highlightFill = cursorBaseColor * (0.15f * pulse);
        Color highlightOutline = cursorBaseColor * (0.5f * pulse);

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

    /// <summary>
    /// Resolves the held wand's "intended source item" stock vs cost for the dimension label tint.
    /// Currently scoped to <see cref="WandOfBuildingBase"/> (the only family with BlockExhaustion semantics).
    /// Cached and refreshed every <see cref="NarrowStockRefreshFrames"/> frames; the cache is also
    /// invalidated whenever the area dimensions change. Returns false if no intent is applicable
    /// (e.g. wand isn't a Building wand, or no eligible source item is in inventory).
    /// </summary>
    private bool TryGetNarrowStockForHeldWand(Player player, out int stock, out bool infinite)
    {
        stock = 0;
        infinite = false;
        if (player == null) return false;

        // Only Building wands currently honor BlockExhaustion. Other families have no intended-block concept.
        if (player.HeldItem?.ModItem is not WandOfBuildingBase) return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.BuildingSettings;
        var resourcesCfg = WandConfigs.Resources;

        // Refresh the narrow-stock cache periodically OR after dimensions have changed
        // (the latter is signalled by _cachedAreaCount being -1 / area unstable).
        _narrowStockTick++;
        bool needRefresh = _cachedNarrowStock < 0
            || _narrowStockTick >= NarrowStockRefreshFrames;

        if (needRefresh)
        {
            _narrowStockTick = 0;

            var baseCondition = ItemTypeHelper.GetConditions(settings.Object);
            Func<Item, bool> condition = item => baseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);
            // 2026-04-23 Session 1 (Letter #10 §8 bug): pass the chosen so the tint reflects
            // the chosen item's stock, not the scan-order-first item's stock. Without the
            // chosen, a user who chosen "Adamantite Ore" but also had "Dirt" earlier in the
            // inventory saw the tint track Dirt's 999-stack and go white even when the
            // actual placement item (Adamantite) was short. The chosen feed here mirrors the
            // execute path in TileExecution.cs so preview tint ≡ actual operation behaviour.
            int? chosen = settings.GetChosenTileItemType(settings.Object);
            int sourceIdx = ItemTypeHelper.FindFirstItemIndex(player, condition, chosen);
            if (sourceIdx < 0)
            {
                _cachedNarrowStock = 0;
                _cachedNarrowInfinite = false;
                stock = 0;
                infinite = false;
                return true; // intent applies, but no items in stock → "insufficient" path
            }

            Item firstItem = player.inventory[sourceIdx];
            int chosenType = firstItem.tileWand >= 0 ? firstItem.tileWand : firstItem.type;
            Func<Item, bool> narrow = i => !i.IsAir && i.type == chosenType;

            _cachedNarrowInfinite = ItemTypeHelper.CountItems(player.inventory, narrow, out int total);

            // Resource config "infinite threshold" also counts as effectively infinite.
            if (!_cachedNarrowInfinite && resourcesCfg != null
                && resourcesCfg.IsInfiniteForPlaceType(settings.Object))
            {
                int threshold = resourcesCfg.GetThresholdForPlaceType(settings.Object);
                if (threshold == 0 || total >= threshold)
                    _cachedNarrowInfinite = true;
            }

            _cachedNarrowStock = total;
        }

        stock = _cachedNarrowStock;
        infinite = _cachedNarrowInfinite;
        return true;
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
            _cachedNarrowStock = -1; // re-query stock when cost changes too
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
        // Main.MouseScreen is in physical screen pixels but we draw with Main.UIScaleMatrix
        // (which scales every coordinate by Main.UIScale). Feeding raw pixels in causes
        // the label to drift away from the cursor by (1 - 1/UIScale) * mouse — invisible
        // at the top-left, growing toward the bottom-right. Convert to UI-space first.
        float uiScale = Main.UIScale <= 0f ? 1f : Main.UIScale;
        Vector2 cursorScreen = Main.MouseScreen / uiScale;
        Vector2 screenPos = new Vector2(cursorScreen.X + offsetX, cursorScreen.Y + offsetY);

        // Screen-bounds clamping with margin (also in UI-space).
        const float margin = 4f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(dimensionText) * textScale;
        float uiWidth  = Main.screenWidth  / uiScale;
        float uiHeight = Main.screenHeight / uiScale;

        screenPos.X = Math.Clamp(screenPos.X, margin, uiWidth  - margin - textSize.X);
        screenPos.Y = Math.Clamp(screenPos.Y, margin, uiHeight - margin - textSize.Y);

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.UIScaleMatrix  // Use UI scale so it stays crisp at any zoom
        );

        // Tint by (stock-vs-cost) × BlockExhaustion mode — a 2D matrix:
        //   stock ≥ cost (or infinite)             → White
        //   stock <  cost & mode == NextBlock      → Yellow  ("will substitute next block")
        //   stock <  cost & mode == Cancel         → Red     ("won't even start — short on intended block")
        //   stock <  cost & mode == Interrupt      → Orange  ("will stop partway when intended block runs out")
        // Stock is measured against the FIRST eligible source item (the user's intended block),
        // matching the narrowing applied in TileExecution/WallExecution for Cancel/Interrupt.
        var exhaust = WandConfigs.Preferences?.BlockExhaustion ?? Common.Enums.BlockExhaustionMode.NextBlock;
        bool insufficient = TryGetNarrowStockForHeldWand(Main.LocalPlayer, out int stock, out bool infinite)
            && !infinite
            && _cachedAreaCount > 0
            && stock < _cachedAreaCount;

        Color labelTint = !insufficient
            ? Color.White
            : exhaust switch
            {
                Common.Enums.BlockExhaustionMode.Cancel    => new Color(255,  90,  90), // Red
                Common.Enums.BlockExhaustionMode.Interrupt => new Color(255, 160,  60), // Orange
                _                                          => new Color(255, 220,  80), // Yellow (NextBlock)
            };

        Utils.DrawBorderString(Main.spriteBatch, dimensionText, screenPos,
            labelTint * WandColors.DimensionLabelOpacity, textScale);

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