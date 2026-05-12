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
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

[Autoload(Side = ModSide.Client)]
internal sealed class PlanificationOverlay : IComposableOverlay
{
    private static readonly Color[] SlotColors =
    {
        new(255, 100, 160),
        new(255, 245, 182),
        new(182, 255, 198),
        new(182, 207, 255),
        new(187, 138, 255),
    };

    public int ZOrder => -8;

    public bool Visible { get; set; } = true;

    public bool NeedsRedraw => true;

    public void Initialize(OverlayManager manager)
    {
    }

    public void OnRegister()
    {
    }

    public void OnUnregister()
    {
    }

    public void Update(OverlayContext context)
    {
    }

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        var player = context.Player;
        if (player == null)
            return;

        bool isPlanificationHeld = player.HeldItem?.ModItem is WandOfPlanificationBase;
        if (!isPlanificationHeld)
            return;

        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        if (pwp == null)
            return;

        var screenBounds = context.ScreenTileBounds;
        bool useFirstColor = WandConfigs.Preferences?.UseFirstStencilColorForAll ?? false;

        int activeSlot = pwp.ActiveEditSlot;

        for (int slot = 0; slot < PlanificationWandPlayer.StencilSlotCount; slot++)
        {
            bool isActiveSlot = slot == activeSlot;
            if (!isActiveSlot && !pwp.IsSlotVisible(slot))
                continue;

            var canvasTiles = pwp.GetSlotCanvasWorldTiles(slot);
            var selectionTiles = pwp.GetSlotSelectionWorldTiles(slot);
            var cfg = pwp.GetRenderConfig(slot);
            var color = useFirstColor ? SlotColors[0] : SlotColors[slot];

            if (isActiveSlot)
            {
                DrawActiveSlotLikeDelimitation(spriteBatch, context, pwp, canvasTiles, selectionTiles, screenBounds, color);
                continue;
            }

            if (selectionTiles.Count == 0)
                continue;

            DrawStencilLayers(spriteBatch, selectionTiles, screenBounds, cfg, color, isActiveSlot);
        }
    }

    private static void DrawActiveSlotLikeDelimitation(
        SpriteBatch spriteBatch,
        OverlayContext context,
        PlanificationWandPlayer pwp,
        HashSet<Point> canvasTiles,
        HashSet<Point> selectionTiles,
        Rectangle screenBounds,
        Color color)
    {
        var overlayCfg = WandConfigs.CanvasOverlay;
        float outsideA = overlayCfg?.CanvasOutsideAlpha ?? 0.2f;
        float canvasA = overlayCfg?.CanvasFillAlpha ?? 0.4f;
        float tileSelA = overlayCfg?.CanvasTileSelectionAlpha ?? 0.4f;

        Color outsideColor = (overlayCfg?.CanvasOutsideColor ?? new Color(0, 0, 0, 255)) * outsideA;
        Color canvasFillColor = (overlayCfg?.CanvasFillColor ?? new Color(255, 255, 255, 255)) * canvasA;
        Color activeSelectionColor = color * tileSelA;

        var canvasPreview = new HashSet<Point>(canvasTiles);
        var selectionPreview = new HashSet<Point>(selectionTiles);
        TryBuildActiveOperationPreviewTiles(context, pwp, canvasTiles, selectionTiles, out canvasPreview, out selectionPreview);

        // Match Delimitation behavior: hide selection overlay while canvas-editing.
        if (canvasPreview.Count > 0)
        {
            DrawFill(spriteBatch, canvasPreview, screenBounds, canvasFillColor);
            DrawOutsideFill(spriteBatch, canvasPreview, screenBounds, outsideColor);
            DrawOutline(spriteBatch, canvasPreview, screenBounds, color * 0.65f, 2);
        }

        if (pwp.Settings.Mode != DelimitationWandMode.CanvasEdit && selectionPreview.Count > 0)
            DrawFill(spriteBatch, selectionPreview, screenBounds, activeSelectionColor);

        if (selectionPreview.Count > 0)
            DrawOutline(spriteBatch, selectionPreview, screenBounds, color * 0.80f, 2);
    }

    private static bool TryBuildActiveOperationPreviewTiles(
        OverlayContext context,
        PlanificationWandPlayer pwp,
        HashSet<Point> canvasTiles,
        HashSet<Point> selectionTiles,
        out HashSet<Point> canvasPreviewTiles,
        out HashSet<Point> selectionPreviewTiles)
    {
        canvasPreviewTiles = new HashSet<Point>(canvasTiles);
        selectionPreviewTiles = new HashSet<Point>(selectionTiles);

        var selection = context.Selection;
        if (!selection.IsActive)
            return false;

        var settings = pwp.Settings;
        var shapeContext = settings.Shape.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, shapeContext);
        var shapeTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), shapeContext);
        if (shapeTiles == null || shapeTiles.Length == 0)
            return false;

        var operand = new HashSet<Point>(shapeTiles);

        if (settings.Mode == DelimitationWandMode.CanvasEdit)
        {
            canvasPreviewTiles = ApplyCanvasOperationPreview(canvasTiles, operand, settings.Operation);
            selectionPreviewTiles = new HashSet<Point>(selectionTiles);
            selectionPreviewTiles.IntersectWith(canvasPreviewTiles);
            return true;
        }

        if (canvasTiles.Count == 0)
        {
            if (!settings.AutoCreateCanvas)
                return false;

            canvasPreviewTiles = new HashSet<Point>(operand);
        }
        else
        {
            canvasPreviewTiles = new HashSet<Point>(canvasTiles);
        }

        operand.IntersectWith(canvasPreviewTiles);
        selectionPreviewTiles = ApplyOperationPreview(selectionTiles, operand, settings.Operation);
        return true;
    }

    private static HashSet<Point> ApplyCanvasOperationPreview(
        HashSet<Point> current,
        HashSet<Point> operand,
        SelectionOperation operation)
    {
        var result = new HashSet<Point>(current);

        switch (operation)
        {
            case SelectionOperation.Add:
                result.UnionWith(operand);
                break;
            case SelectionOperation.Remove:
                result.ExceptWith(operand);
                break;
            case SelectionOperation.Clear:
                result.Clear();
                break;
            default:
                result.UnionWith(operand);
                break;
        }

        return result;
    }

    private static HashSet<Point> ApplyOperationPreview(
        HashSet<Point> current,
        HashSet<Point> operand,
        SelectionOperation operation)
    {
        var result = new HashSet<Point>(current);

        switch (operation)
        {
            case SelectionOperation.Add:
                result.UnionWith(operand);
                break;
            case SelectionOperation.Remove:
                result.ExceptWith(operand);
                break;
            case SelectionOperation.Intersect:
                result.IntersectWith(operand);
                break;
            case SelectionOperation.XOR:
                result.SymmetricExceptWith(operand);
                break;
            case SelectionOperation.Clear:
                result.Clear();
                break;
            default:
                result = new HashSet<Point>(operand);
                break;
        }

        return result;
    }

    private static void DrawStencilLayers(
        SpriteBatch spriteBatch,
        HashSet<Point> tiles,
        Rectangle screenBounds,
        Common.Settings.PlanificationRenderConfig cfg,
        Color color,
        bool isActiveSlot)
    {
        float fillAlpha = isActiveSlot ? 0.20f : 0.18f;
        float gridAlpha = isActiveSlot ? 0.30f : 0.25f;
        float outlineAlpha = isActiveSlot ? 0.75f : 0.65f;
        int outlineThickness = isActiveSlot ? 2 : 1;

        if (cfg.ShowFill)
            DrawFill(spriteBatch, tiles, screenBounds, color * fillAlpha);

        if (cfg.ShowGrid)
            DrawGrid(spriteBatch, tiles, screenBounds, color * gridAlpha);

        bool drawOutline = cfg.ShowOutline || (!cfg.ShowFill && !cfg.ShowGrid);
        if (drawOutline)
            DrawOutline(spriteBatch, tiles, screenBounds, color * outlineAlpha, outlineThickness);
    }

    private static void DrawOutsideFill(
        SpriteBatch spriteBatch,
        IReadOnlySet<Point> canvasTiles,
        Rectangle screenBounds,
        Color outsideColor)
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

    private static void DrawFill(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color)
    {
        if (color.A == 0)
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

    private static void DrawGrid(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color)
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

    private static void DrawOutline(SpriteBatch spriteBatch, IReadOnlySet<Point> tiles, Rectangle screenBounds, Color color, int thickness)
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
