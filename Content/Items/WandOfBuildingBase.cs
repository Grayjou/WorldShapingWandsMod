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
using WorldShapingWandsMod.Common.Items;
using System.Linq;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract class WandOfBuildingBase : BaseCyclingWand
    {
        public override string WandBaseName => "Wand of Building";

        protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

        public override bool? UseItem(Player player)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
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
            wandPlayer.ClearSelection();
            Main.NewText("Selection cancelled.", Color.Yellow);
        }

        protected void ExecuteBuilding(Player player, WandPlayer wandPlayer)
        {
            var settings = wandPlayer.BuildingSettings;
            var selection = wandPlayer.Selection;
            var config = ModContent.GetInstance<WandConfig>();

            // Get the condition for the selected object type
            var condition = ItemTypeHelper.GetConditions(settings.Object);

            // Find the first matching item
            Item blockItem = ItemTypeHelper.FindFirstItem(player, condition);
            if (blockItem == null)
            {
                Main.NewText($"No suitable item found for {settings.Object}.", Color.Red);
                return;
            }

            ushort tileType = (ushort)blockItem.createTile;
            if (tileType == 0)
            {
                Main.NewText($"Item {blockItem.Name} does not create a tile.", Color.Red);
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
            int required = tileSet.Tiles.Count();

            // Check availability (unless infinite resource mode allows)
            bool hasInfinite = ItemTypeHelper.CountItems(player.inventory, condition, out int total);
            if (!hasInfinite && total < required)
            {
                Main.NewText($"Need {required} {blockItem.Name}, have {total}.", Color.Red);
                return;
            }

            bool shouldConsume = true;
            if (config.EnableInfiniteResource)
            {
                if (config.InfiniteResourceAmount == 0)
                    shouldConsume = false; // always infinite
                else if (total >= config.InfiniteResourceAmount)
                    shouldConsume = false; // above threshold
            }

            var undoMgr = player.GetModPlayer<UndoManager>();
            var action = undoMgr.BeginAction("Building");

            int placed = 0;
            foreach (Point tile in tileSet.Tiles)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (Main.tile[tile.X, tile.Y].HasTile) continue; // skip occupied

                action.AddSnapshot(tile);
                if (WorldGen.PlaceTile(tile.X, tile.Y, tileType, mute: true, forced: false, plr: player.whoAmI))
                {
                    placed++;
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendTileSquare(-1, tile.X, tile.Y);
                }
            }

            if (placed > 0)
            {
                undoMgr.CommitAction(action);
                if (shouldConsume)
                {
                    blockItem.stack -= placed;
                    if (blockItem.stack <= 0) blockItem.TurnToAir();
                }
                Main.NewText($"Placed {placed} {blockItem.Name}" + (shouldConsume ? "" : " (no items consumed)"), Color.Cyan);
            }
            else
            {
                Main.NewText("No tiles could be placed.", Color.Gray);
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
                else
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
                return false;
            }
            return true;
        }
    }
}