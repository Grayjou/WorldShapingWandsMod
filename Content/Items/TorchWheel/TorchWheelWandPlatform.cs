using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Content.Projectiles;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

/// <summary>
/// The Platform-tracing variant of the Torch Wheel Wand. Right-clicking in the
/// inventory cycles back to the Tile-tracing <see cref="TorchWheelWandSolid"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a separate item (not a mode flag) for multiplayer compatibility:
/// <c>Item</c> instances sync automatically between clients via Terraria's net
/// code, whereas custom mode fields on <c>ModItem</c> do not. Two distinct items
/// that cycle via <c>RightClick</c> is the same proven pattern used by
/// <see cref="Common.Items.BaseCyclingWand"/>.
/// </para>
/// <para>
/// <b>Status:</b> Platform tracing projectile is not yet implemented. Holding this
/// wand and left-clicking shows an informational message and does not fire.
/// </para>
/// </remarks>
public class TorchWheelWandPlatform : ModItem
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
        Item.shoot = ModContent.ProjectileType<FlyingTorchWheelPlatform>();
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
        int nextType = ModContent.ItemType<TorchWheelWandSolid>();
        int stack = Item.stack;
        bool wasFavorited = Item.favorited;

        Item.SetDefaults(nextType);
        Item.stack = stack + 1; // +1 because right-click consumes one
        Item.favorited = wasFavorited;

        Main.NewText(Get("TorchWheelSwitchToTiles"), Color.Yellow);
    }

    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "ModeInfo", Get("TorchWheelModePlatforms")));
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
        // Non-craftable — obtained by right-clicking TorchWheelWandSolid in inventory
    }
}
