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
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Composable overlay that previews torch tiling positions as semi-transparent
/// markers when the player is holding a <see cref="WandOfTorchesBase"/> with an
/// active selection in Place mode.
/// </summary>
/// <remarks>
/// <para>
/// This is the first native overlay in the composable pipeline (not an adapter).
/// It recomputes tiling positions whenever the selection or torch settings change,
/// using the same <see cref="TorchTilingAlgorithm.ComputePositions"/> algorithm
/// as the actual placement code in <see cref="WandOfTorchesBase"/>.
/// </para>
/// <para>
/// Markers are drawn as 8×8 pixel squares centered on each tile, using the
/// torch wand's amber color at 50% opacity. Screen-culling via
/// <see cref="OverlayContext.ScreenTileBounds"/> ensures off-screen markers
/// are skipped.
/// </para>
/// </remarks>
[Autoload(Side = ModSide.Client)]
internal sealed class TilingOverlay : IComposableOverlay
{
    /// <summary>Tiling preview draws on top of selection + safekeeping.</summary>
    public int ZOrder => 100;

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <inheritdoc />
    public bool NeedsRedraw { get; private set; }

    private OverlayManager _manager;

    /// <summary>Cached set of tiling candidate positions (world tile coords).</summary>
    private HashSet<Point> _cachedPositions;

    /// <summary>Cached anchor tile — the tile the placement grid actually
    /// expands from. After snap-to-existing-torch (if enabled and a snap target
    /// exists), this is the snapped torch; otherwise it equals the reference.
    /// Drawn in magenta, on top of all markers, regardless of whether it sits
    /// inside <see cref="_cachedPositions"/> or even inside the selection shape
    /// (the snap can land on a torch outside the shape itself).</summary>
    private Point? _cachedSeed;

    /// <summary>Cached reference tile — the raw seed implied by the active
    /// <see cref="TorchReferenceMode"/> BEFORE any snap-to-existing-torch
    /// override. Drawn in green. Equal to <see cref="_cachedSeed"/> when no
    /// snap occurred. Null only if the mode failed to produce any seed at all.</summary>
    private Point? _cachedReference;

    /// <summary>Hash of the settings/selection state that produced <see cref="_cachedPositions"/>.</summary>
    private int _lastSettingsHash;

    // ================================================================
    //  Lifecycle
    // ================================================================

    public void Initialize(OverlayManager manager)
    {
        _manager = manager;
    }

    public void OnRegister() { }
    public void OnUnregister()
    {
        _cachedPositions = null;
        _cachedSeed = null;
        _cachedReference = null;
        _lastSettingsHash = 0;
    }

    // ================================================================
    //  Update — recompute positions when settings or selection change
    // ================================================================

    public void Update(OverlayContext context)
    {
        NeedsRedraw = false;

        // Only show when holding a WandOfTorchesBase (not TorchWheelWandSolid)
        if (context.Player?.HeldItem?.ModItem is not WandOfTorchesBase)
        {
            if (_cachedPositions != null)
            {
                _cachedPositions = null;
                NeedsRedraw = true;
            }
            return;
        }

        // Only show when there's an active selection
        if (!context.Selection.IsActive)
        {
            if (_cachedPositions != null)
            {
                _cachedPositions = null;
                NeedsRedraw = true;
            }
            return;
        }

        // Only preview in Place mode
        var settings = context.WandPlayer.TorchSettings;
        if (settings.Mode != TorchMode.Place)
        {
            if (_cachedPositions != null)
            {
                _cachedPositions = null;
                NeedsRedraw = true;
            }
            return;
        }

        // Build a hash of everything that affects tiling positions
        // NOTE: when ReferenceMode == MousePosition the seed depends on the
        // live cursor tile, which is NOT part of the selection — so during
        // Confirm mode (selection locked, awaiting third click) the preview
        // would otherwise stay frozen on the cursor tile from when the selection
        // was first locked. Fold the mouse tile into the hash in that mode so
        // the cache invalidates every frame the cursor crosses a tile boundary.
        Point mouseTileForHash = settings.ReferenceMode == TorchReferenceMode.MousePosition
            ? GeometryHelper.GetMouseTile()
            : default;

        int hash = HashCode.Combine(
            context.Selection.StartTile,
            context.Selection.EndTile,
            settings.SpacingX,
            settings.SpacingY,
            (int)settings.TilingStyle,
            settings.FlipTiling,
            (int)settings.ReferenceMode,
            HashCode.Combine(settings.AlignToExistingTorches, context.ShapeInfo.GetHashCode(), mouseTileForHash));

        if (hash == _lastSettingsHash && _cachedPositions != null)
            return;

        _lastSettingsHash = hash;
        RecomputePositions(context, settings);
        NeedsRedraw = true;
    }

    /// <summary>
    /// Recomputes tiling positions using the same algorithm as
    /// <see cref="WandOfTorchesBase.ExecutePlaceMode"/>.
    /// </summary>
    private void RecomputePositions(OverlayContext context, WandTorchSettings settings)
    {
        // Get shape positions via the same flow as WandOfTorchesBase.GetShapePositions
        var shapeContext = settings.Shape.ToShapeContext(
            context.Selection.StartTile, context.Selection.EndTile, context.Selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, shapeContext);
        var tiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), shapeContext);

        if (tiles.Length == 0)
        {
            _cachedPositions = null;
            _cachedSeed = null;
            _cachedReference = null;
            return;
        }

        var shapePositions = new List<Point>(tiles);

        // Compute bounding rectangle
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in shapePositions)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        var selectionBounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);

        // Determine the RAW reference seed first (BEFORE any snap-to-torch).
        // For FirstValidTile we ask the helper with alignToExisting=false so
        // we get the unsnapped first-valid position; everything else is the
        // mode's intrinsic point.
        Point? referenceSeed = settings.ReferenceMode switch
        {
            TorchReferenceMode.BboxTopLeft =>
                TorchPlacementHelper.GetBboxTopLeftSeed(selectionBounds),
            TorchReferenceMode.BboxTopRight =>
                TorchPlacementHelper.GetBboxTopRightSeed(selectionBounds),
            TorchReferenceMode.BboxBottomLeft =>
                TorchPlacementHelper.GetBboxBottomLeftSeed(selectionBounds),
            TorchReferenceMode.BboxBottomRight =>
                TorchPlacementHelper.GetBboxBottomRightSeed(selectionBounds),
            TorchReferenceMode.FirstBboxClick =>
                context.Selection.StartTile,
            TorchReferenceMode.MousePosition =>
                GeometryHelper.GetMouseTile(),
            _ =>
                TorchPlacementHelper.FindFirstTorch(
                    shapePositions, selectionBounds,
                    settings.SpacingX, settings.SpacingY,
                    settings.OverwriteTorches,
                    alignToExisting: false),
        };

        // Compute the ANCHOR by optionally snapping the reference onto the
        // closest existing torch within the selection bounds. The snap result
        // is allowed to sit outside the shape (the user explicitly asked us
        // to align to that existing torch) — the BFS expansion below will
        // still emit a coherent grid, and the magenta cell will draw wherever
        // the snap landed (no longer gated on shape-set membership).
        Point? anchorSeed = referenceSeed;
        if (referenceSeed != null && settings.AlignToExistingTorches)
        {
            var snapped = TorchPlacementHelper.SnapToExistingTorch(referenceSeed.Value, selectionBounds);
            if (snapped != null)
                anchorSeed = snapped;
        }

        if (anchorSeed == null)
        {
            _cachedPositions = null;
            _cachedSeed = null;
            _cachedReference = referenceSeed; // may also be null
            return;
        }

        _cachedSeed = anchorSeed;
        _cachedReference = referenceSeed;

        // Compute tiling positions using BFS expansion
        var tilingPositions = TorchTilingAlgorithm.ComputePositions(
            selectionBounds, anchorSeed.Value,
            settings.SpacingX, settings.SpacingY,
            settings.TilingStyle, settings.FlipTiling);

        // Intersect with the shape set
        var shapeSet = new HashSet<Point>(shapePositions);
        _cachedPositions = new HashSet<Point>(tilingPositions.Where(p => shapeSet.Contains(p)));
    }

    // ================================================================
    //  Draw — render 8×8 markers at tiling positions
    // ================================================================

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        if (_cachedPositions == null || _cachedPositions.Count == 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;

        // All colours sourced from WandColors so they're easily tweakable
        // (the user explicitly asked for "no magic numbers for the colors").
        var markerColor    = WandColors.TilingMarker     * WandColors.TilingMarkerOpacity;
        var anchorFill     = WandColors.TilingAnchor     * WandColors.TilingAnchorOpacity;
        var referenceFill  = WandColors.TilingReference  * WandColors.TilingReferenceOpacity;
        var cellOutline    = WandColors.TilingCellOutline * WandColors.TilingCellOutlineOpacity;

        var screenBounds = context.ScreenTileBounds;

        foreach (var pos in _cachedPositions)
        {
            // Screen-cull: skip tiles outside the visible area
            if (!screenBounds.Contains(pos))
                continue;

            // Skip both the anchor AND the reference here — they're drawn
            // separately on top with their own coloured cells.
            if (_cachedSeed.HasValue && pos == _cachedSeed.Value)
                continue;
            if (_cachedReference.HasValue && pos == _cachedReference.Value)
                continue;

            var screenPos = new Vector2(pos.X * 16, pos.Y * 16) - Main.screenPosition;

            // Draw a centered 8×8 marker within the 16×16 tile
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)screenPos.X + 4,
                    (int)screenPos.Y + 4,
                    8, 8),
                markerColor);
        }

        // ── Reference cell (green) ───────────────────────────────────────
        // Draw FIRST so the anchor (magenta) covers it on coincident tiles.
        // Note we deliberately do NOT gate on `_cachedPositions.Contains(...)` —
        // for modes like BboxTopLeft the reference can sit on a tile outside
        // the shape, and the user explicitly asked to see that point.
        if (_cachedReference.HasValue
            && screenBounds.Contains(_cachedReference.Value)
            && (!_cachedSeed.HasValue || _cachedReference.Value != _cachedSeed.Value))
        {
            DrawCornerCell(spriteBatch, pixel, _cachedReference.Value, referenceFill, cellOutline);
        }

        // ── Anchor cell (magenta) ────────────────────────────────────────
        // Drawn LAST so it always wins on coincident tiles. Also no
        // _cachedPositions gate — the snap-to-existing-torch result is allowed
        // to land outside the shape, and we still want to show where the grid
        // is anchored.
        if (_cachedSeed.HasValue && screenBounds.Contains(_cachedSeed.Value))
        {
            DrawCornerCell(spriteBatch, pixel, _cachedSeed.Value, anchorFill, cellOutline);
        }
    }

    /// <summary>Helper: draw a 12×12 filled cell with a 1-px outline frame
    /// in the 14×14 area inside a tile, used for both the anchor and
    /// reference indicators.</summary>
    private static void DrawCornerCell(SpriteBatch spriteBatch, Texture2D pixel,
        Point tile, Color fill, Color outline)
    {
        var screen = new Vector2(tile.X * 16, tile.Y * 16) - Main.screenPosition;
        int sx = (int)screen.X;
        int sy = (int)screen.Y;

        // 1-px outline frame around a 14×14 region
        spriteBatch.Draw(pixel, new Rectangle(sx + 1, sy + 1, 14, 1), outline);  // top
        spriteBatch.Draw(pixel, new Rectangle(sx + 1, sy + 14, 14, 1), outline); // bottom
        spriteBatch.Draw(pixel, new Rectangle(sx + 1, sy + 1, 1, 14), outline);  // left
        spriteBatch.Draw(pixel, new Rectangle(sx + 14, sy + 1, 1, 14), outline); // right

        // 12×12 filled centre
        spriteBatch.Draw(pixel, new Rectangle(sx + 2, sy + 2, 12, 12), fill);
    }
}
