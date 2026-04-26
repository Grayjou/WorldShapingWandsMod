using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Content.Projectiles;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// The Tile-tracing variant of the Torch Wheel Wand. Fires a
/// <see cref="FlyingTorchWheelSolid"/> that traces block outlines and places torches
/// along the path. Right-clicking in the inventory cycles to the Platform-tracing
/// variant <see cref="TorchWheelWandPlatform"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a standalone wand (not based on <see cref="Common.Items.BaseCyclingWand"/>)
/// because it has completely different mechanics: no shape selection, no UI panel,
/// fire-and-forget projectile behavior. The two-item cycling pattern (Tiles ↔ Platforms)
/// mirrors BaseCyclingWand's inventory right-click cycle for MP compatibility.
/// </para>
/// <para>
/// <b>Left-click:</b> Shoots a projectile toward the cursor. On hitting a solid
/// block, it begins tracing the outline using a wall-following algorithm,
/// placing torches at regular intervals from the player's inventory.
/// </para>
/// <para>
/// <b>Shift + Right-click (held):</b> Kills all active TorchWheelSolidProjectiles
/// owned by this player.
/// </para>
/// <para>
/// <b>Right-click in inventory:</b> Cycles to Platforms mode.
/// </para>
/// <para>
/// Biome torch behavior is controlled entirely by the player's Torch God's Favor
/// flag (<c>Player.UsingBiomeTorches</c>) — there is no per-wand setting for this.
/// </para>
/// </remarks>
public class TorchWheelWandSolid : ModItem
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/TorchWheel/{Name}";
    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.autoReuse = false;
        Item.noMelee = true;
        Item.channel = false;
        Item.shoot = ModContent.ProjectileType<FlyingTorchWheelSolid>();
        Item.shootSpeed = 12f;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(gold: 5);
    }

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse != 2)
        {
            // Left-click: require torches in inventory
            if (!TorchPlacementHelper.HasTorches(player))
                return false;
        }

        return true;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            // Shift + right-click while holding: kill all active projectiles
            if (player.controlUseTile)
            {
                KillAllTorchWheels(player);
                Main.NewText("Killed all Torch Wheel projectiles.", Color.OrangeRed);
            }
            return true;
        }

        // Left-click: spawn projectile (handled by default Shoot behavior)
        return null;
    }

    public override void ModifyShootStats(
        Player player,
        ref Vector2 position,
        ref Vector2 velocity,
        ref int type,
        ref int damage,
        ref float knockback)
    {
        // Only shoot on left-click
        if (player.altFunctionUse == 2)
            type = ProjectileID.None;
    }

    public override void HoldItem(Player player)
    {
        // Show a torch cursor icon from the first torch found in inventory
        var (torchItemType, _, _, _) = TorchPlacementHelper.FindTorchInInventory(player);
        if (torchItemType > 0)
        {
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = torchItemType;
            player.cursorItemIconPush = 26;
        }
    }

    // ── Inventory Right-Click Cycling ──────────────────────────
    public override bool CanRightClick() => true;

    public override void RightClick(Player player)
    {
        int nextType = ModContent.ItemType<TorchWheelWandPlatform>();
        int stack = Item.stack;
        bool wasFavorited = Item.favorited;

        Item.SetDefaults(nextType);
        Item.stack = stack + 1; // +1 because right-click consumes one
        Item.favorited = wasFavorited;

        Main.NewText(Get("TorchWheelSwitchToPlatforms"), Color.LightBlue);
    }

    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "ModeInfo", Get("TorchWheelModeTiles")));
        tooltips.Add(new TooltipLine(Mod, "ModeHint", Get("TorchWheelCycleHint")));
        tooltips.Add(new TooltipLine(Mod, "KillHint", Get("TorchWheelKillHint")));
    }

    private static void KillAllTorchWheels(Player player)
    {
        int solidWheelType = ModContent.ProjectileType<TorchWheelSolidProjectile>();
        int solidFlyingType = ModContent.ProjectileType<FlyingTorchWheelSolid>();
        int platformWheelType = ModContent.ProjectileType<TorchWheelPlatformProjectile>();
        int platformFlyingType = ModContent.ProjectileType<FlyingTorchWheelPlatform>();

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            if (proj.active && proj.owner == player.whoAmI
                && (proj.type == solidWheelType || proj.type == solidFlyingType
                    || proj.type == platformWheelType || proj.type == platformFlyingType))
            {
                if (proj.ModProjectile is TorchWheelSolidProjectile solidProj)
                    solidProj.SetInactive();
                else
                    proj.Kill();
            }
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddIngredient(ItemID.Torch, 5)
            .AddIngredient(ItemID.FallenStar, 1)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}
