using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Common.Systems;
using System;
using WorldShapingWandsMod.Common.Drawing;

namespace WorldShapingWandsMod.Content.Items
{
    public class WandOfBuildingInstant : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
        public override Color ModeColor => new Color(255, 80, 80); // Red — Instant (dangerous)
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingSelect>();

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.channel = true; // needed for drag detection
            Item.UseSound = null; // prevent sound spam during drag — played once on selection start
        }

        protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
        {
            // All logic handled in HoldItem for instant/drag mode
            return false;
        }

        public override void HoldItem(Player player)
        {
            base.HoldItem(player);

            if (Main.myPlayer != player.whoAmI)
                return;

            var wandPlayer = player.GetModPlayer<WandPlayer>();
            Point mouseTile = GeometryHelper.GetMouseTile();

            if (Main.mouseLeft)
            {
                // Don't start selection if mouse is over UI
                // IsMouseOverUI() checks both mouseInterface AND panel hover,
                // because HoldItem runs before UI.Update sets mouseInterface.
                if (IsMouseOverUI())
                    return;

                // Don't restart selection immediately after cancellation
                if (!wandPlayer.CanStartNewSelection())
                    return;

                if (!wandPlayer.InstantSelection.IsActive)
                {
                    bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                    Math.Abs(Main.MouseWorld.X - player.Center.X);
                    wandPlayer.StartInstantSelection(mouseTile, vertical);
                    SoundEngine.PlaySound(SoundID.Item1, player.Center);
                }
                wandPlayer.UpdateInstantSelection(mouseTile);

                // Right-click during a drag cancels the selection without executing.
                if (Main.mouseRight && wandPlayer.InstantSelection.IsActive)
                {
                    wandPlayer.CancelInstantSelection(WandColors.CancelBuilding, wandPlayer.BuildingSettings.Shape);
                    return;
                }
            }
            else if (wandPlayer.InstantSelection.IsActive)
            {
                // Don't execute if mouse released over UI (e.g. NPC shop)
                if (IsMouseOverUI())
                {
                    wandPlayer.CancelInstantSelection(WandColors.CancelBuilding, wandPlayer.BuildingSettings.Shape);
                    return;
                }

                // Mouse released - execute only if this wand started the selection
                if (wandPlayer.IsInstantSelectionOwnedByCurrentItem() && !IsOnLocalCooldown())
                {
                    ExecuteBuilding(player, wandPlayer);
                }
                wandPlayer.ClearInstantSelection();
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .AddIngredient(ItemID.GrayBrick, 10)
                .AddIngredient(ItemID.RedBrick, 10)
                .AddIngredient(ItemID.Rope, 20)
                .AddIngredient(ItemID.Gel, 10)
                .AddIngredient(ItemID.Cobweb, 10)
                .AddIngredient(ItemID.ManaCrystal, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
