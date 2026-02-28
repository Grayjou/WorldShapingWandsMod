using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using MagicWiring.Content.Projectiles;
using MagicWiring.UI;
using MagicWiring.Common;

namespace MagicWiring.Content.Items;

public class WiringWandItem : ModItem
{
    /// <summary>
    /// Item sprite dimensions (width and height in pixels).
    /// </summary>
    private const int ItemSize = 32;

    public override void SetStaticDefaults()
    {
        // Tooltip is defined in Localization/en-US.hjson
    }

    public override void SetDefaults()
    {
        Item.width = ItemSize;
        Item.height = ItemSize;
        Item.useTime = 10;
        Item.useAnimation = 10;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true;
        Item.shoot = ModContent.ProjectileType<WiringWandProjectile>();
        Item.shootSpeed = 0f;
        Item.rare = ItemRarityID.Lime;
        Item.value = Item.buyPrice(gold: 10);
        Item.mech = true;
        Item.autoReuse = false;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        // Don't mutate Item.shoot or Item.channel here!
        // Those are shared definition properties that persist across frames.
        // We handle right-click vs left-click in Shoot() and UseItem() instead.
        return true;
    }

    public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
            return false;

        if (WiringSettings.Interaction == InteractionMode.Toggle)
            return HandleToggleShoot(player);

        return true; // Hold mode: spawn projectile normally
    }

    /// <summary>
    /// Toggle mode: First click sets start and spawns preview projectile.
    /// Second click tells existing projectile to execute.
    /// </summary>
    private bool HandleToggleShoot(Player player)
    {
        var wandPlayer = player.GetModPlayer<WiringWandPlayer>();

        if (!wandPlayer.PendingStartTile.HasValue)
        {
            // First click
            wandPlayer.PendingStartTile = Main.MouseWorld.ToTileCoordinates();
            
            Vector2 toMouse = Main.MouseWorld - player.Center;
            wandPlayer.PendingVerticalFirst = Math.Abs(toMouse.Y) > Math.Abs(toMouse.X);
            
            return true; // Spawn preview projectile
        }
        else
        {
            // Second click: confirm and execute
            var proj = WiringWandProjectile.GetActiveProjectile();
            proj?.ExecuteToggleConfirm();
            return false; // Don't spawn another projectile
        }
    }

    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            if (Main.myPlayer == player.whoAmI)
                WiringWandUISystem.Instance?.ToggleUI();
            return true;
        }

        return base.UseItem(player);
    }

    public override void HoldItem(Player player)
    {
        if (Main.myPlayer == player.whoAmI)
            player.InfoAccMechShowWires = true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.WireKite, 1)
            .AddIngredient(ItemID.Wire, 50)
            .AddIngredient(ItemID.Actuator, 10)
            .AddTile(TileID.TinkerersWorkbench)
            .Register();
    }
}