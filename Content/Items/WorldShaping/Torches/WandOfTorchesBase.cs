using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// Abstract base class for all Wand of Torches variants.
/// Handles torch placement using configurable tiling algorithms (Manhattan / Grid),
/// first-torch seeding, replacement, removal, and biome conversion operations.
/// Four concrete subclasses (Instant, Select, Confirm, Stamp) provide mode behavior.
/// </summary>
public abstract class WandOfTorchesBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Torches/{Name}";
    public override string WandBaseName => "Wand of Torches";
    public override string WandLore => Get("LoreTorches");

    // ── Template Method Pattern ────────────────────────────────────────
    protected override WandFamily Family => WandFamily.Torches;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        return wandPlayer.TorchSettings.Mode switch
        {
            TorchMode.Place   => WandAction.TorchPlace,
            TorchMode.Replace => WandAction.TorchReplace,
            TorchMode.Remove  => WandAction.TorchRemove,
            TorchMode.Convert => WandAction.TorchConvert,
            _                 => WandAction.TorchPlace,
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.IronBar, 10)
            .AddCustomShimmerResult(ItemID.Torch, 50);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteTorchOperation(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.TorchSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Show a torch cursor icon from the first torch found in inventory.
        // InventoryView v1 (S6 2026-04-22): if a torch choice is set, show that one.
        var (torchItemType, _, _, _) = TorchPlacementHelper.FindTorchInInventory(
            player, wandPlayer.TorchSettings.ChosenTorchItemType);
        if (torchItemType > 0)
        {
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = torchItemType;
            player.cursorItemIconPush = 26;
        }
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(gold: 5);
    }

    public override bool? UseItem(Player player)
    {
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        TemplateHoldItem(player);
    }

    // ================================================================
    //  Core Torch Operations — Mode Dispatch
    // ================================================================

    /// <summary>
    /// Dispatches to the appropriate torch operation based on the current Mode setting.
    /// </summary>
    protected void ExecuteTorchOperation(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.TorchSettings;

        switch (settings.Mode)
        {
            case TorchMode.Place:
                ExecutePlaceMode(player, wandPlayer, settings);
                break;
            case TorchMode.Replace:
                ExecuteReplaceMode(player, wandPlayer, settings);
                break;
            case TorchMode.Remove:
                ExecuteRemoveMode(player, wandPlayer, settings);
                break;
            case TorchMode.Convert:
                ExecuteConvertMode(player, wandPlayer, settings);
                break;
        }
    }

    // ================================================================
    //  Place Mode — lay new torches at tiling positions
    // ================================================================

    private void ExecutePlaceMode(Player player, WandPlayer wandPlayer, WandTorchSettings settings)
    {
        var shapePositions = GetShapePositions(wandPlayer);
        if (shapePositions == null || shapePositions.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesInSelection", WandColors.MsgWarning);
            return;
        }

        // Find the torch to place from inventory.
        // InventoryView v1 (S6 2026-04-22): honor ChosenTorchItemType.
        var (torchItemType, torchStack, torchTileType, torchPlaceStyle) =
            TorchPlacementHelper.FindTorchInInventory(player, settings.ChosenTorchItemType);
        if (torchItemType <= 0)
        {
            Main.NewText(Get("TorchesNoTorchItem"), WandColors.MsgWarning);
            return;
        }

        var selectionBounds = ComputeBounds(shapePositions);

        // Determine seed based on reference mode
        var selection = wandPlayer.GetVisualSelection();
        Point? seed = GetTilingSeed(shapePositions, selectionBounds, settings, selection.StartTile);
        if (seed == null)
        {
            ShowNullResult(wandPlayer, "TorchesNoValidPositions", WandColors.MsgWarning);
            return;
        }

        // Compute tiling positions using BFS expansion
        var tilingPositions = TorchTilingAlgorithm.ComputePositions(
            selectionBounds, seed.Value,
            settings.SpacingX, settings.SpacingY,
            settings.TilingStyle, settings.FlipTiling);

        var shapeSet = new HashSet<Point>(shapePositions);
        var candidatePositions = tilingPositions.Where(p => shapeSet.Contains(p)).ToList();

        int placed = 0;
        int replaced = 0;
        int skipped = 0;

        foreach (var pos in candidatePositions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var (actualTileType, actualPlaceStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                player, x, y, torchTileType, torchPlaceStyle, settings.BiomeTorch);

            if (!TorchPlacementHelper.PassesUnderwaterCheck(x, y, actualTileType, actualPlaceStyle))
            {
                skipped++;
                continue;
            }

            var tile = Main.tile[x, y];
            bool isReplacement = false;
            bool existingEchoState = false;

            if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
            {
                if (!settings.OverwriteTorches)
                {
                    skipped++;
                    continue;
                }

                isReplacement = true;
                existingEchoState = tile.IsTileInvisible;
                WorldGen.KillTile(x, y, noItem: true);
                replaced++;
            }
            else if (tile.HasTile)
            {
                skipped++;
                continue;
            }

            if (!TorchPlacementHelper.IsValidTorchPosition(x, y))
            {
                skipped++;
                continue;
            }

            WorldGen.PlaceTile(x, y, actualTileType, mute: true, forced: false, style: actualPlaceStyle);

            if (Main.tile[x, y].HasTile && TileID.Sets.Torch[Main.tile[x, y].TileType])
            {
                ApplyEchoCoat(x, y, settings.EchoCoat, isReplacement, existingEchoState);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1);

                placed++;
            }
        }

        if (placed == 0 && replaced == 0)
        {
            ShowNullResult(wandPlayer, "TorchesNonePlaced", WandColors.MsgWarning);
        }
        else
            Main.NewText(Get("TorchesPlaced", placed + replaced), WandColors.MsgTorches);
    }

    // ================================================================
    //  Replace Mode — swap existing torches with inventory torch type
    // ================================================================

    private void ExecuteReplaceMode(Player player, WandPlayer wandPlayer, WandTorchSettings settings)
    {
        var shapePositions = GetShapePositions(wandPlayer);
        if (shapePositions == null || shapePositions.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesInSelection", WandColors.MsgWarning);
            return;
        }

        // InventoryView v1 (S6 2026-04-22): honor ChosenTorchItemType so replace mode
        // swaps existing torches with the user's chosen variety, not the natural-scan first one.
        var (torchItemType, torchStack, torchTileType, torchPlaceStyle) =
            TorchPlacementHelper.FindTorchInInventory(player, settings.ChosenTorchItemType);
        if (torchItemType <= 0)
        {
            Main.NewText(Get("TorchesNoTorchItem"), WandColors.MsgWarning);
            return;
        }

        int replaced = 0;
        int skipped = 0;

        foreach (var pos in shapePositions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];
            if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
            {
                // Not a torch — skip
                continue;
            }

            // Already the same torch type+style — skip
            if (tile.TileType == torchTileType && tile.TileFrameY / 22 == torchPlaceStyle)
            {
                skipped++;
                continue;
            }

            var (actualTileType, actualPlaceStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                player, x, y, torchTileType, torchPlaceStyle, settings.BiomeTorch);

            if (!TorchPlacementHelper.PassesUnderwaterCheck(x, y, actualTileType, actualPlaceStyle))
            {
                skipped++;
                continue;
            }

            bool existingEchoState = tile.IsTileInvisible;
            bool suppressDrops = WandConfigs.Sandbox.EffectiveSuppressDrops;
            WorldGen.KillTile(x, y, noItem: suppressDrops);
            WorldGen.PlaceTile(x, y, actualTileType, mute: true, forced: false, style: actualPlaceStyle);

            if (Main.tile[x, y].HasTile && TileID.Sets.Torch[Main.tile[x, y].TileType])
            {
                ApplyEchoCoat(x, y, settings.EchoCoat, true, existingEchoState);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1);

                replaced++;
            }
        }

        if (replaced == 0)
        {
            ShowNullResult(wandPlayer, "TorchesNoneReplaced", WandColors.MsgWarning);
        }
        else
        {
            // Vacuum dropped items from replaced torches (when drops are not suppressed)
            bool suppressDrops = WandConfigs.Sandbox.EffectiveSuppressDrops;
            if (!suppressDrops && WandConfigs.Sandbox.VacuumItems)
            {
                var bounds = ComputeBounds(shapePositions);
                BulkTileOperations.VacuumItemsInArea(player, bounds);
            }
            Main.NewText(Get("TorchesReplaced", replaced), WandColors.MsgTorches);
        }
    }

    // ================================================================
    //  Remove Mode — delete existing torches from the selection
    // ================================================================

    private void ExecuteRemoveMode(Player player, WandPlayer wandPlayer, WandTorchSettings settings)
    {
        var shapePositions = GetShapePositions(wandPlayer);
        if (shapePositions == null || shapePositions.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesInSelection", WandColors.MsgWarning);
            return;
        }

        var config = WandConfigs.Sandbox;
        bool suppressDrops = config.EffectiveSuppressDrops;
        int removed = 0;

        foreach (var pos in shapePositions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];
            if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
                continue;

            WorldGen.KillTile(x, y, noItem: suppressDrops);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, x, y, 1);

            removed++;
        }

        if (removed == 0)
        {
            ShowNullResult(wandPlayer, "TorchesNoneRemoved", WandColors.MsgWarning);
        }
        else
        {
            // Vacuum dropped items to the player (same pattern as Dismantling/Building)
            if (!suppressDrops && config.VacuumItems)
            {
                var bounds = ComputeBounds(shapePositions);
                BulkTileOperations.VacuumItemsInArea(player, bounds);
            }
            Main.NewText(Get("TorchesRemoved", removed), WandColors.MsgTorches);
        }
    }

    // ================================================================
    //  Convert Mode — change existing torches to biome-appropriate type
    // ================================================================

    private void ExecuteConvertMode(Player player, WandPlayer wandPlayer, WandTorchSettings settings)
    {
        var shapePositions = GetShapePositions(wandPlayer);
        if (shapePositions == null || shapePositions.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesInSelection", WandColors.MsgWarning);
            return;
        }

        int converted = 0;
        int skipped = 0;

        foreach (var pos in shapePositions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];
            if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
                continue;

            // Only convert torches that ARE biome torches (placed by the
            // biome torch system). Leave decorative torches untouched.
            // This matches Torch God's Favor behavior — never converts
            // Rainbow, Ultrabright, or other intentionally-placed torches.
            if (!TorchPlacementHelper.IsBiomeTorch(tile))
                continue;

            // Get current torch identity
            int currentTileType = tile.TileType;
            int currentPlaceStyle = tile.TileFrameY / 22;

            // Resolve what biome torch this position should have.
            // ALWAYS pass the BASE torch (TileID.Torches, style 0) so
            // ResolveBiomeTorch actually resolves the biome variant.
            // Passing the current type/style would skip biome torches
            // (style > 0 triggers the guard in ResolveBiomeTorch).
            var (targetTileType, targetPlaceStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                player, x, y,
                TileID.Torches, 0,     // Always request from base torch
                biomeTorchEnabled: true);

            // Already the correct biome torch — skip
            if (currentTileType == targetTileType && currentPlaceStyle == targetPlaceStyle)
            {
                skipped++;
                continue;
            }

            if (!TorchPlacementHelper.PassesUnderwaterCheck(x, y, targetTileType, targetPlaceStyle))
            {
                skipped++;
                continue;
            }

            bool existingEchoState = tile.IsTileInvisible;
            WorldGen.KillTile(x, y, noItem: true);
            WorldGen.PlaceTile(x, y, targetTileType, mute: true, forced: false, style: targetPlaceStyle);

            if (Main.tile[x, y].HasTile && TileID.Sets.Torch[Main.tile[x, y].TileType])
            {
                ApplyEchoCoat(x, y, settings.EchoCoat, true, existingEchoState);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1);

                converted++;
            }
        }

        if (converted == 0)
        {
            ShowNullResult(wandPlayer, "TorchesNoneConverted", WandColors.MsgWarning);
        }
        else
            Main.NewText(Get("TorchesConverted", converted), WandColors.MsgTorches);
    }

    // ================================================================
    //  Shared Helpers
    // ================================================================

    /// <summary>
    /// Determines the tiling seed based on the current ReferenceMode setting.
    /// </summary>
    /// <param name="shapePositions">All tile positions in the selection shape.</param>
    /// <param name="selectionBounds">Bounding rectangle of the selection.</param>
    /// <param name="settings">Current torch settings (includes ReferenceMode).</param>
    /// <param name="selectionStartTile">The initial click point (StartTile from SelectionState).</param>
    private Point? GetTilingSeed(List<Point> shapePositions, Rectangle selectionBounds,
        WandTorchSettings settings, Point selectionStartTile)
    {
        Point? seed = settings.ReferenceMode switch
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
                selectionStartTile,
            TorchReferenceMode.MousePosition =>
                GeometryHelper.GetMouseTile(),
            _ => // FirstValidTile (default)
                TorchPlacementHelper.FindFirstTorch(
                    shapePositions, selectionBounds,
                    settings.SpacingX, settings.SpacingY,
                    settings.OverwriteTorches,
                    settings.AlignToExistingTorches),
        };

        // For explicit reference modes, optionally snap onto the closest existing
        // torch in the selection so the new grid aligns with what's already there.
        // (FirstValidTile already handles snap inside FindFirstTorch.)
        if (seed != null
            && settings.AlignToExistingTorches
            && settings.ReferenceMode != TorchReferenceMode.FirstValidTile)
        {
            var snapped = TorchPlacementHelper.SnapToExistingTorch(seed.Value, selectionBounds);
            if (snapped != null)
                seed = snapped;
        }

        return seed;
    }

    /// <summary>
    /// Computes the bounding rectangle of a set of tile positions.
    /// </summary>
    private static Rectangle ComputeBounds(List<Point> positions)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in positions)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Applies or removes Echo Coat on a newly placed torch based on settings.
    /// </summary>
    private static void ApplyEchoCoat(int x, int y, TriStateValue echoSetting, bool isReplacement, bool previousEchoState)
    {
        bool wantEcho = echoSetting switch
        {
            TriStateValue.Apply => true,
            TriStateValue.Remove => false,
            _ => isReplacement && previousEchoState,
        };

        if (wantEcho)
            WorldGen.paintCoatTile(x, y, 2, true); // 2 = PaintCoatingID.Echo
    }

    /// <summary>
    /// Gets the set of tile positions defined by the current shape selection.
    /// </summary>
    private static List<Point> GetShapePositions(WandPlayer wandPlayer)
    {
        var settings = wandPlayer.TorchSettings;
        var selection = wandPlayer.GetVisualSelection();

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var tiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swp = wandPlayer.Player.GetModPlayer<DelimitationWandPlayer>();
        tiles = swp.FilterBySelection(tiles);

        return new List<Point>(tiles);
    }
}