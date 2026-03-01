using Terraria;
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
using System;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;
using System.Linq;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract class WandOfBuildingBase : BaseCyclingWand
    {
        public override string WandBaseName => "Wand of Building";

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

            // Get the condition for the selected object type
            var condition = ItemTypeHelper.GetConditions(settings.Object);

            // Find the first matching placement item (block item or tile wand)
            int sourceIndex = ItemTypeHelper.FindFirstItemIndex(player, condition);
            if (sourceIndex < 0)
            {
                Main.NewText($"No suitable item found for {settings.Object}.", Color.Red);
                return;
            }

            // Build shape context
            var context = new ShapeContext(
                selection.StartTile,
                selection.EndTile,
                settings.Shape.FillMode,
                settings.Shape.Thickness,
                HorizontalBias.None,
                VerticalBias.None,
                selection.VerticalFirst
            );

            var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
            var tilesToProcess = tileSet.Tiles.ToArray();
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

            foreach (Point tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;

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

                    if (existingTile.TileType == tType) continue;
                    if (!player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y)) continue;
                    if (!WorldGen.CanKillTile(tile.X, tile.Y)) continue;

                    action.AddSnapshot(tile);

                    bool didReplace = false;
                    if (WorldGen.ReplaceTile(tile.X, tile.Y, tType, srcItem.placeStyle))
                    {
                        didReplace = true;
                    }
                    else
                    {
                        WorldGen.KillTile(tile.X, tile.Y, fail: false, effectOnly: false, noItem: true);
                        if (!Main.tile[tile.X, tile.Y].HasTile &&
                            WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI))
                        {
                            didReplace = true;
                        }
                    }

                    if (didReplace)
                    {
                        ApplySlope(tile.X, tile.Y, settings.Slope);
                        replaced++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            NetMessage.SendTileSquare(-1, tile.X, tile.Y);
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

                    action.AddSnapshot(tile);
                    if (WorldGen.PlaceTile(tile.X, tile.Y, tType, mute: true, forced: false, plr: player.whoAmI))
                    {
                        ApplySlope(tile.X, tile.Y, settings.Slope);
                        placed++;
                        if (shouldConsume) ConsumeOneItem(player, srcItem, condition);
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            NetMessage.SendTileSquare(-1, tile.X, tile.Y);
                    }
                }
            }

            int totalChanged = placed + replaced;
            if (totalChanged > 0)
            {
                undoMgr.CommitAction(action);

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
        /// Does nothing for SlopeType.Default (full block).
        /// </summary>
        private static void ApplySlope(int x, int y, SlopeType slope)
        {
            if (slope == SlopeType.Default) return;

            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;

            if (slope == SlopeType.VerticalHalf)
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
    }
}