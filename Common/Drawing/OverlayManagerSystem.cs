using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Client-side ModSystem that owns the <see cref="OverlayManager"/> lifecycle
/// and drives the overlay pipeline from <see cref="PostDrawTiles"/>.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class OverlayManagerSystem : ModSystem
{
    public override void Load()
    {
        OverlayManager.Create();

        // Register the canvas/selection overlay — renders the three-layer model
        // (outside dimming, canvas fill, selection fill) + canvas border.
        // ZOrder -10: draws behind all other overlays.
        var canvasOverlay = new SelectionCanvasOverlay();
        OverlayManager.Instance?.Register(canvasOverlay);

        // Register the Molding wand's canvas/selection overlay — independent from
        // the Delimitation wand's overlay, uses teal/cyan color palette.
        // ZOrder -9: draws right after SelectionCanvasOverlay.
        var moldingCanvasOverlay = new MoldingCanvasOverlay();
        OverlayManager.Instance?.Register(moldingCanvasOverlay);

        // Register the selection overlay adapter — wraps the existing SelectionOverlay
        var selectionAdapter = new SelectionOverlayAdapter();
        OverlayManager.Instance?.Register(selectionAdapter);

        // Register the safekeeping overlay adapter — wraps the existing SafekeepingOverlay
        var safekeepingAdapter = new SafekeepingOverlayAdapter();
        OverlayManager.Instance?.Register(safekeepingAdapter);

        // Register the tiling preview overlay — native composable overlay (not an adapter)
        var tilingOverlay = new TilingOverlay();
        OverlayManager.Instance?.Register(tilingOverlay);
    }

    public override void Unload()
    {
        OverlayManager.Destroy();
    }

    public override void PostDrawTiles()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var manager = OverlayManager.Instance;
        if (manager == null || manager.Count == 0) return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        var context = CreateContext(player, wandPlayer);

        // Wire the ShowTilingPreview config toggle to TilingOverlay visibility
        var clientConfig = Configs.WandConfigs.Overlay;
        if (manager.TryGet<TilingOverlay>(out var tilingOverlay))
            tilingOverlay.Visible = clientConfig?.ShowTilingPreview ?? true;

        manager.UpdateAll(context);
        manager.DrawAll(Main.spriteBatch);
    }

    /// <summary>
    /// Builds an <see cref="OverlayContext"/> snapshot from the current game state.
    /// Uses the same shape-settings dispatch as <see cref="SelectionOverlay.GetCurrentShapeSettings"/>.
    /// </summary>
    private static OverlayContext CreateContext(Player player, WandPlayer wandPlayer)
    {
        var selection = wandPlayer.GetVisualSelection();
        bool isHoldingWand = IsHoldingWandItem(player);
        var shapeInfo = GetCurrentShapeInfo(player, wandPlayer);

        var shapeContext = selection.IsActive
            ? shapeInfo.ToShapeContext(selection.StartTile, selection.EndTile, selection.VerticalFirst)
            : default;

        return new OverlayContext
        {
            Selection = selection,
            ShapeInfo = shapeInfo,
            ShapeContext = shapeContext,
            Player = player,
            WandPlayer = wandPlayer,
            GameTime = Main._drawInterfaceGameTime,
            ScreenTileBounds = ComputeScreenTileBounds(),
            HeldItemType = player.HeldItem?.type ?? 0,
            IsHoldingWand = isHoldingWand,
            CancelledSelection = wandPlayer.CancelledSelection,
        };
    }

    /// <summary>
    /// Dispatches to the correct per-wand shape settings based on the held item type.
    /// Mirrors <c>SelectionOverlay.GetCurrentShapeSettings</c>.
    /// </summary>
    private static ShapeInfo GetCurrentShapeInfo(Player player, WandPlayer wandPlayer)
    {
        if (player.HeldItem?.ModItem is WandOfDismantlingBase)
            return wandPlayer.DismantlingSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfBuildingBase)
            return wandPlayer.BuildingSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfReplacementBase)
            return wandPlayer.ReplacementSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfWiringBase)
            return wandPlayer.WiringSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfSafekeepingBase)
            return wandPlayer.SafekeepingSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfCoatingBase)
            return wandPlayer.CoatingSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfFluidsBase)
            return wandPlayer.FluidsSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfTorchesBase)
            return wandPlayer.TorchSettings.Shape;
        if (player.HeldItem?.ModItem is WandOfDelimitationBase)
        {
            var swp = player.GetModPlayer<Players.DelimitationWandPlayer>();
            return swp.Settings.Shape;
        }
        if (player.HeldItem?.ModItem is WandOfMoldingBase)
        {
            var mwp = player.GetModPlayer<Players.MoldingWandPlayer>();
            return mwp.Settings.Shape;
        }

        return new ShapeInfo(
            wandPlayer.Settings.ShapeType,
            wandPlayer.Settings.ShapeMode,
            wandPlayer.Settings.Thickness,
            slice: wandPlayer.Settings.Slice);
    }

    /// <summary>
    /// Checks if the player is holding any recognized wand item.
    /// Mirrors <c>SelectionOverlay.IsHoldingWandItem</c>.
    /// </summary>
    private static bool IsHoldingWandItem(Player player)
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
    /// Computes the tile-coordinate bounds of the visible screen area,
    /// with a 1-tile margin for partially visible tiles.
    /// </summary>
    private static Rectangle ComputeScreenTileBounds()
    {
        int minX = (int)(Main.screenPosition.X / 16) - 1;
        int minY = (int)(Main.screenPosition.Y / 16) - 1;
        int maxX = (int)((Main.screenPosition.X + Main.screenWidth) / 16) + 2;
        int maxY = (int)((Main.screenPosition.Y + Main.screenHeight) / 16) + 2;
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }
}
