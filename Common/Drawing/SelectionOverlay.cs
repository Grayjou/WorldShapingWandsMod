using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

[Autoload(Side = ModSide.Client)]
public class SelectionOverlay : ModSystem
{
    private const float FillOpacity = 0.20f;
    private const float BorderOpacity = 0.5f;
    private const float GridLineOpacity = 0.3f;
    private const int GridLineWidth = 1;

    public override void PostDrawTiles()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        if (!wandPlayer.Selection.IsActive) return;

        // Check if player is holding a wand item (placeholder - will be implemented when wand items are added)
        bool isHoldingWand = IsHoldingWandItem(player);
        
        if (!wandPlayer.Settings.ShouldShowPreview(isHoldingWand) && !wandPlayer.Selection.IsActive) return;

        var shapeSettings = GetCurrentShapeSettings(player, wandPlayer);
        DrawSelection(wandPlayer, shapeSettings);
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
        // As you add more wands, include them here:
        // || player.HeldItem?.ModItem is WandOfDesigner;
    }

    private void DrawSelection(WandPlayer wandPlayer, ShapeInfo shapeSettings)
    {
        var settings = wandPlayer.Settings;
        var selection = wandPlayer.Selection;

        var context = shapeSettings.ToShapeContext(selection.StartTile, selection.EndTile);
        var tileSet = ShapeRegistry.GetShapeTiles(shapeSettings.Shape, context);

        var tiles = new HashSet<Point>(tileSet.Tiles);
        var boundary = new HashSet<Point>(tileSet.BoundaryTiles);

        if (tiles.Count == 0) return;

        Color baseColor = selection.WasClamped && (Main.GameUpdateCount % 30 < 15)
            ? Color.Orange
            : Color.LimeGreen;

        Color fillColor = baseColor * FillOpacity;
        Color borderColor = baseColor * BorderOpacity;
        Color gridColor = Color.White * GridLineOpacity;

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        // Draw fill and borders
        foreach (var tile in tiles)
        {
            Vector2 worldPos = new Vector2(tile.X * 16, tile.Y * 16);
            Vector2 screenPos = worldPos - Main.screenPosition;

            if (screenPos.X < -16 || screenPos.X > Main.screenWidth + 16 ||
                screenPos.Y < -16 || screenPos.Y > Main.screenHeight + 16)
                continue;

            // Draw filled tile (slightly smaller to show grid)
            Rectangle fillRect = new Rectangle(
                (int)screenPos.X + GridLineWidth, 
                (int)screenPos.Y + GridLineWidth, 
                16 - GridLineWidth * 2, 
                16 - GridLineWidth * 2
            );
            
            Color color = boundary.Contains(tile) ? borderColor : fillColor;
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, color);

            // Draw grid lines for this tile
            DrawTileGridLines(screenPos, tiles, tile, gridColor);
        }

        Main.spriteBatch.End();

        if (settings.ShowDimensions)
            DrawDimensionLabel(selection, context.GetBounds());
    }

    private void DrawTileGridLines(Vector2 screenPos, HashSet<Point> tiles, Point tile, Color gridColor)
    {
        // Draw right edge if no tile to the right
        if (!tiles.Contains(new Point(tile.X + 1, tile.Y)))
        {
            Rectangle rightEdge = new Rectangle(
                (int)screenPos.X + 16 - GridLineWidth, 
                (int)screenPos.Y, 
                GridLineWidth, 
                16
            );
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rightEdge, gridColor);
        }

        // Draw bottom edge if no tile below
        if (!tiles.Contains(new Point(tile.X, tile.Y + 1)))
        {
            Rectangle bottomEdge = new Rectangle(
                (int)screenPos.X, 
                (int)screenPos.Y + 16 - GridLineWidth, 
                16, 
                GridLineWidth
            );
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, bottomEdge, gridColor);
        }

        // Draw left edge if no tile to the left
        if (!tiles.Contains(new Point(tile.X - 1, tile.Y)))
        {
            Rectangle leftEdge = new Rectangle(
                (int)screenPos.X, 
                (int)screenPos.Y, 
                GridLineWidth, 
                16
            );
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, leftEdge, gridColor);
        }

        // Draw top edge if no tile above
        if (!tiles.Contains(new Point(tile.X, tile.Y - 1)))
        {
            Rectangle topEdge = new Rectangle(
                (int)screenPos.X, 
                (int)screenPos.Y, 
                16, 
                GridLineWidth
            );
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, topEdge, gridColor);
        }
    }

    private void DrawDimensionLabel(SelectionState selection, Rectangle bounds)
    {
        string dimensionText = $"{selection.Width}x{selection.Height}";

        const float textScale = 0.7f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(dimensionText) * textScale;
        Vector2 worldPos = new Vector2(
            bounds.X * 16 + (bounds.Width * 16 - textSize.X) / 2,
            bounds.Y * 16 - textSize.Y - 4
        );
        Vector2 screenPos = worldPos - Main.screenPosition;

        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        Utils.DrawBorderString(Main.spriteBatch, dimensionText, screenPos, Color.White * 0.8f, textScale);

        Main.spriteBatch.End();
    }
}