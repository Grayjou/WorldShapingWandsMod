using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

[Autoload(Side = ModSide.Client)]
public class SelectionOverlay : ModSystem
{
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
    }

    private ShapeInfo GetCurrentShapeSettings(Player player, WandPlayer wandPlayer)
    {
        if (player.HeldItem?.ModItem is WandOfDestructionBase)
        {
            return wandPlayer.DestructionSettings.Shape;
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
        else
        {
            return new ShapeInfo(wandPlayer.Settings.ShapeType, wandPlayer.Settings.ShapeMode, wandPlayer.Settings.Thickness); // fallback
        }
    }

    private bool IsHoldingWandItem(Terraria.Player player)
    {
        return player.HeldItem?.ModItem is WandOfDestructionBase
            || player.HeldItem?.ModItem is WandOfBuildingBase
            || player.HeldItem?.ModItem is WandOfReplacementBase
            || player.HeldItem?.ModItem is WandOfWiringBase;
    }

    private void DrawSelection(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        var settings = wandPlayer.Settings;
        var selection = wandPlayer.Selection;

        var context = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile);
        var tileSet = ShapeRegistry.GetShapeTiles(shapeSettings.Shape, context);

        var tiles = new HashSet<Point>(tileSet.Tiles);

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
            DrawDimensionLabel(selection, context.GetBounds());
    }

    private void DrawDimensionLabel(SelectionState selection, Rectangle bounds)
    {
        string dimensionText = $"{selection.Width} x {selection.Height}";

        const float textScale = 0.9f;
        // Position at top-left of cursor, offset so it doesn't overlap the cursor icon
        Vector2 cursorScreen = Main.MouseScreen;
        Vector2 screenPos = new Vector2(cursorScreen.X + 20f, cursorScreen.Y - 28f);

        // Clamp to screen bounds so the label doesn't go off-screen
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(dimensionText) * textScale;
        if (screenPos.X + textSize.X > Main.screenWidth - 4)
            screenPos.X = Main.screenWidth - 4 - textSize.X;
        if (screenPos.Y < 4)
            screenPos.Y = 4;

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

        var context = cancelState.Shape.ToShapeContext(cancelState.StartTile, cancelState.EndTile);
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