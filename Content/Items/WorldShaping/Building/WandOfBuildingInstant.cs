using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Items
{
    /// <summary>
    /// Instant (OneClick) mode for the Wand of Building.
    /// All mode-specific input logic lives in BaseCyclingWand's template methods.
    /// </summary>
    public class WandOfBuildingInstant : WandOfBuildingBase
    {
        public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
        public override Color ModeColor => new Color(255, 80, 80); // Red — Instant
        public override int GetNextModeItemType() => ModContent.ItemType<WandOfBuildingSelect>();

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.channel = true; // needed for drag detection
            Item.UseSound = null; // prevent sound spam during drag
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
