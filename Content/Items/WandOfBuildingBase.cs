using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract class WandOfBuildingBase : BaseCyclingWand
    {
        public override string WandBaseName => "Wand of Building";
        public override string WandLore => "The Deity of Creation lets you manifest order and existence at will.";

        protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

        public override bool? UseItem(Player player)
        {
            // Don't do anything if the mouse is over UI
            if (Main.LocalPlayer.mouseInterface)
                return false;

            var wandPlayer = player.GetModPlayer<WandPlayer>();

            if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
                return false;

            Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
            return HandleUseItem(player, wandPlayer, mouseTile);
        }

        public override void HoldItem(Player player)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
            {
                CancelSelection(wandPlayer);
                Main.mouseRightRelease = false;
            }

            // Display cursor item icon for the block that will be placed
            var condition = ItemTypeHelper.GetConditions(wandPlayer.BuildingSettings.Object);
            Item blockItem = ItemTypeHelper.FindFirstItem(player, condition);
            if (blockItem != null)
            {
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = blockItem.type;
                player.cursorItemIconPush = 26;
            }
        }

        protected virtual void CancelSelection(WandPlayer wandPlayer)
        {
            wandPlayer.CancelSelection(WandColors.CancelBuilding, wandPlayer.BuildingSettings.Shape);
        }

        protected void ExecuteBuilding(Player player, WandPlayer wandPlayer)
        {
            var settings = wandPlayer.BuildingSettings;
            var selection = wandPlayer.Selection;
            var config = ModContent.GetInstance<WandConfig>();

            // Wall mode uses a completely separate placement path
            if (settings.Object == PlaceType.Wall)
            {
                ExecuteWallBuilding(player, wandPlayer, settings, selection, config);
                return;
            }

            // Get the condition for the selected object type, filtering out multi-tile objects
            // so they are silently skipped (e.g., Furnace is ignored, Wood is used instead).
            var baseCondition = ItemTypeHelper.GetConditions(settings.Object);
            Func<Item, bool> condition = item => baseCondition(item) && !ItemTypeHelper.IsMultiTileItem(item);

            // Find the first matching placement item (block item or tile wand)
            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition);
            if (sourceIndex < 0)
            {
                Main.NewText($"No suitable item found for {settings.Object}.", Color.Red);
                return;
            }

            Item initialSourceItem = player.inventory[sourceIndex];

            // Build shape context
            var context = new ShapeContext(
                selection.StartTile,
                selection.EndTile,
                settings.Shape.FillMode,
                settings.Shape.Thickness,
                HorizontalBias.None,
                VerticalBias.None,
                selection.VerticalFirst,
                settings.Shape.EqualDimensions
            );

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = tileSet.Tiles.ToArray();

            // Sort gravity-affected blocks bottom-to-top so they settle correctly
            // instead of cascading downward as each tile is placed from the top.
            if (initialSourceItem.createTile >= TileID.Dirt && Main.tileSand[initialSourceItem.createTile])
            {
                Array.Sort(tilesToProcess, (a, b) => b.Y.CompareTo(a.Y));
            }

            int required = tilesToProcess.Length;

            // --- Cancel mode: pre-check total stock before touching anything ---
            if (settings.ExhaustionMode == BlockExhaustionMode.Cancel)
            {
                Item firstSourceItem = player.inventory[sourceIndex];
                bool usingTileWandForCheck = firstSourceItem.tileWand >= 0;
                Func<Item, bool> checkCond = usingTileWandForCheck
                    ? i => !i.IsAir && i.type == firstSourceItem.tileWand
                    : i => !i.IsAir && condition(i);

                bool hasInfinite = ItemTypeHelper.CountItems(player.inventory, checkCond, out int totalAvailable);
                if (!hasInfinite && totalAvailable < required)
                {
                    string itemName = usingTileWandForCheck
                        ? Lang.GetItemNameValue(firstSourceItem.tileWand)
                        : firstSourceItem.Name;
                    Main.NewText($"Need {required} {itemName}, have {totalAvailable}. Operation cancelled.", Color.Red);
                    return;
                }
            }

            // Determine if consumption is needed
            bool shouldConsume = true;
            if (config.EnableInfiniteResource)
            {
                // Check total across all matching items
                ItemTypeHelper.CountItems(player.inventory, i => !i.IsAir && condition(i), out int grandTotal);
                if (config.InfiniteResourceAmount == 0)
                    shouldConsume = false;
                else if (grandTotal >= config.InfiniteResourceAmount)
                    shouldConsume = false;
            }

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building");

            int placed = 0;
            int replaced = 0;
            bool replaceMode = player.TileReplacementEnabled;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            // WorldGen.gen suppresses per-tile sounds, dust, gore, AND tile drops.
            // For placements (empty → tile) this is always fine (no drops expected).
            // For replacements (tile → tile), we only suppress when config says so,
            // otherwise the old tile should drop its items naturally.
            bool wasGen = WorldGen.gen;
            bool suppressDrops = config.SuppressDrops;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

                // Skip protected positions
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                var existingTile = Main.tile[tile.X, tile.Y];

                if (existingTile.HasTile)
                {
                    if (!replaceMode) continue;

                    // Re-lookup source item for this tile (supports NextBlock)
                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                        continue; // NextBlock: no more items, skip remaining
                    }

                    Item srcItem = player.inventory[idx];
                    ushort tType = (ushort)srcItem.createTile;

                    // Skip if the tile is already the exact same type AND style
                    // (platforms share TileType but differ by placeStyle/frame)
                    if (existingTile.TileType == tType)
                    {
                        // For platforms and similar multi-style tiles, also compare style
                        int existingStyle = existingTile.TileFrameX / 18;
                        if (!TileID.Sets.Platforms[tType] || existingStyle == srcItem.placeStyle)
                            continue;
                    }
                    if (!config.BypassPickaxePower && !player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
                    if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);

                    // Replace path: only suppress effects when drops are suppressed
                    WorldGen.gen = suppressDrops;

                    bool didReplace = false;
                    if (WorldGen.ReplaceTile(tile.X, tile.Y, tType, srcItem.placeStyle))
                    {
                        didReplace = true;
                    }
                    else
                    {
                        WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: suppressDrops);
                        if (!Main.tile[tile.X, tile.Y].HasTile &&
                            WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI, style: srcItem.placeStyle))
                        {
                            didReplace = true;
                        }
                    }

                    // Restore gen for next iteration
                    WorldGen.gen = wasGen;

                    if (didReplace)
                    {
                        if (settings.OverwriteSlope)
                            ApplySlope(tile.X, tile.Y, settings.Slope);
                        replaced++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        affectedPositions.Add(tile);
                    }
                }
                else
                {
                    // Re-lookup source item for this tile (supports NextBlock)
                    int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                    if (idx < 0)
                    {
                        if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                        continue; // NextBlock: no more items, skip remaining
                    }

                    Item srcItem = player.inventory[idx];
                    ushort tType = (ushort)srcItem.createTile;

                    // Placement path: always suppress effects (no drops from empty space)
                    WorldGen.gen = true;

                    action.AddSnapshot(tile);
                    if (WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI, style: srcItem.placeStyle))
                    {
                        if (settings.OverwriteSlope)
                            ApplySlope(tile.X, tile.Y, settings.Slope);
                        placed++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        affectedPositions.Add(tile);
                    }

                    WorldGen.gen = wasGen;
                }
            }

            // Restore WorldGen.gen state (safety — already restored per-iteration)
            WorldGen.gen = wasGen;

            // Batch network sync: one packet instead of N per-tile messages
            if (affectedPositions.Count > 0)
                BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(affectedPositions));

            int totalChanged = placed + replaced;
            if (totalChanged > 0)
            {
                undoMgr.CommitAction(action);

                // No custom sound — Terraria's PlaceTile API already handles
                // per-tile placement sounds naturally.

                string detail;
                if (placed > 0 && replaced > 0)
                    detail = $"Placed {placed}, replaced {replaced}";
                else if (replaced > 0)
                    detail = $"Replaced {replaced}";
                else
                    detail = $"Placed {placed}";

                if (!shouldConsume) detail += " (no items consumed)";
                if (interrupted) detail += " — ran out of blocks";
                Main.NewText(detail, Color.Cyan);
            }
            else
            {
                Main.NewText("No tiles could be placed.", Color.Gray);
            }
        }

        /// <summary>
        /// Wall placement path. Walls use createWall, PlaceWall, KillWall — 
        /// a fundamentally different API from tile placement.
        /// Supports replace mode (replacing existing walls) and standard placement.
        /// </summary>
        private void ExecuteWallBuilding(Player player, WandPlayer wandPlayer,
            WandOfBuildingSettings settings, SelectionState selection, WandConfig config)
        {
            var condition = ItemTypeHelper.GetConditions(PlaceType.Wall);

            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition);
            if (sourceIndex < 0)
            {
                Main.NewText("No wall item found in inventory.", Color.Red);
                return;
            }

            var context = new ShapeContext(
                selection.StartTile, selection.EndTile,
                settings.Shape.FillMode, settings.Shape.Thickness,
                HorizontalBias.None, VerticalBias.None,
                selection.VerticalFirst, settings.Shape.EqualDimensions
            );

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = tileSet.Tiles.ToArray();

            bool shouldConsume = true;
            if (config.EnableInfiniteResource)
            {
                ItemTypeHelper.CountItems(player.inventory, i => !i.IsAir && condition(i), out int grandTotal);
                if (config.InfiniteResourceAmount == 0)
                    shouldConsume = false;
                else if (grandTotal >= config.InfiniteResourceAmount)
                    shouldConsume = false;
            }

            if (settings.ExhaustionMode == BlockExhaustionMode.Cancel)
            {
                Item firstItem = player.inventory[sourceIndex];
                Func<Item, bool> checkCond = i => !i.IsAir && i.type == firstItem.type;
                bool hasInfinite = ItemTypeHelper.CountItems(player.inventory, checkCond, out int totalAvailable);
                if (!hasInfinite && totalAvailable < tilesToProcess.Length)
                {
                    Main.NewText($"Need {tilesToProcess.Length} {firstItem.Name}, have {totalAvailable}. Operation cancelled.", Color.Red);
                    return;
                }
            }

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building (Walls)");

            int placed = 0;
            int replaced = 0;
            bool replaceMode = player.TileReplacementEnabled;
            bool interrupted = false;
            var affectedPositions = new List<Point>();

            bool wasGen = WorldGen.gen;

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                var t = Main.tile[tile.X, tile.Y];

                int idx = ItemTypeHelper.FindFirstItemIndex(player, condition);
                if (idx < 0)
                {
                    if (settings.ExhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                    continue;
                }

                Item srcItem = player.inventory[idx];
                ushort wallType = (ushort)srcItem.createWall;

                if (t.WallType != WallID.None)
                {
                    // Wall already exists
                    if (!replaceMode) continue;
                    if (t.WallType == wallType) continue; // same wall, skip

                    action.AddSnapshot(tile);
                    WorldGen.gen = config.SuppressDrops;
                    WorldGen.KillWall(tile.X, tile.Y, fail: false);
                    if (t.WallType == WallID.None)
                    {
                        WorldGen.PlaceWall(tile.X, tile.Y, wallType, mute: true);
                        if (t.WallType == wallType)
                        {
                            replaced++;
                            if (shouldConsume) ItemTypeHelper.ConsumeItems(player.inventory,
                                i => !i.IsAir && i.type == srcItem.type, 1);
                            affectedPositions.Add(tile);
                        }
                    }
                    WorldGen.gen = wasGen;
                }
                else
                {
                    // Empty wall slot — place
                    action.AddSnapshot(tile);
                    WorldGen.gen = true;
                    WorldGen.PlaceWall(tile.X, tile.Y, wallType, mute: true);
                    if (t.WallType == wallType)
                    {
                        placed++;
                        if (shouldConsume) ItemTypeHelper.ConsumeItems(player.inventory,
                            i => !i.IsAir && i.type == srcItem.type, 1);
                        affectedPositions.Add(tile);
                    }
                    WorldGen.gen = wasGen;
                }
            }

            WorldGen.gen = wasGen;

            if (affectedPositions.Count > 0)
            {
                // Wall framing for proper visual merging
                foreach (var pos in affectedPositions)
                    Framing.WallFrame(pos.X, pos.Y);
                BulkTileOperations.BatchNetworkSync(BulkTileOperations.ComputeBounds(affectedPositions));
            }

            int total = placed + replaced;
            if (total > 0)
            {
                undoMgr.CommitAction(action);

                // No custom sound — Terraria's PlaceWall API already handles
                // per-tile placement sounds naturally.

                string detail = placed > 0 && replaced > 0 ? $"Placed {placed}, replaced {replaced} walls"
                    : replaced > 0 ? $"Replaced {replaced} walls" : $"Placed {placed} walls";
                if (!shouldConsume) detail += " (no items consumed)";
                if (interrupted) detail += " — ran out of walls";
                Main.NewText(detail, Color.Cyan);
            }
            else
            {
                Main.NewText("No walls could be placed.", Color.Gray);
            }
        }

        /// <summary>
        /// Consumes one item from the player's inventory for the given source item.
        /// Handles tile wand ammo vs. direct consumption.
        /// </summary>
        private static void ConsumeOneItem(Player player, Item sourceItem, Func<Item, bool> placeCondition)
        {
            bool usingTileWand = sourceItem.tileWand >= 0;
            Func<Item, bool> consumeCond = usingTileWand
                ? i => !i.IsAir && i.type == sourceItem.tileWand
                : i => !i.IsAir && i.type == sourceItem.type;

            ItemTypeHelper.ConsumeItems(player.inventory, consumeCond, 1);
        }

        /// <summary>
        /// Applies the selected slope/half-block setting to a placed tile.
        /// Always applies the selected slope type, including forcing full-block for SlopeType.Default.
        /// </summary>
        private static void ApplySlope(int x, int y, SlopeType slope)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;

            if (slope == SlopeType.Default)
            {
                // Force full block (clear any slope/half-block)
                tile.IsHalfBlock = false;
                tile.Slope = Terraria.ID.SlopeType.Solid;
            }
            else if (slope == SlopeType.VerticalHalf)
            {
                tile.IsHalfBlock = true;
                tile.Slope = Terraria.ID.SlopeType.Solid; // clear any slope when setting half-block
            }
            else
            {
                tile.IsHalfBlock = false;

                tile.Slope = slope switch
                {
                    SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                    SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                    SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpRight,
                    SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpLeft,
                    _ => Terraria.ID.SlopeType.Solid
                };
            }

            // Update tile frame so collision and visuals are immediately correct
            // (without this, sloped platforms act as solid blocks until an adjacent tile update)
            WorldGen.SquareTileFrame(x, y);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Right-click
            {
                var wandPlayer = player.GetModPlayer<WandPlayer>();
                if (wandPlayer.Selection.IsActive)
                {
                    CancelSelection(wandPlayer);
                }
                else if (Main.myPlayer == player.whoAmI)
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            // Only the Instant variant has a craftable recipe.
            // Other modes are obtained via right-click cycling in inventory.
        }
    }
}