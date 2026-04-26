using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// Draws the WandOfFluidsProjectile above the player body but below the front arm.
/// This creates the illusion that the player is gripping the wand handle.
/// </summary>
public class WandProjectileDrawLayer : PlayerDrawLayer
{
    public override string Name => "WandOfFluids Held Projectile";

    // Position this layer BEFORE the front arm
    public override Position GetDefaultPosition()
        => new BeforeParent(PlayerDrawLayers.ArmOverItem);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (player.heldProj < 0 || player.heldProj >= Main.maxProjectiles)
            return false;

        Projectile proj = Main.projectile[player.heldProj];
        return proj.active && proj.type == ModContent.ProjectileType<WandOfFluidsProjectile>();
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (player.heldProj < 0 || player.heldProj >= Main.maxProjectiles)
            return;

        Projectile proj = Main.projectile[player.heldProj];
        if (!proj.active) return;

        if (proj.ModProjectile is not WandOfFluidsProjectile wandProj)
            return;

        DrawData drawData = wandProj.GetDrawData(player, drawInfo);
        drawInfo.DrawDataCache.Add(drawData);
    }
}
