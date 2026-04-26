using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelPlatformProjectile
{
    // ================================================================
    //  Diagnostic Logging
    // ================================================================

    private void LogTermination(string reason)
    {
        Mod.Logger.Debug(
            $"[TorchWheelPlatform] TERMINATED: {reason} | " +
            $"pos=({_currentTilePos.X},{_currentTilePos.Y}) dir={(_direction > 0 ? "Right" : "Left")} " +
            $"pathIndex={_pathIndex} torches={_torchesPlaced} state={_state}");
    }

    // ================================================================
    //  Lighting
    // ================================================================

    private float GetCurrentLightLevel()
    {
        var config = WandConfigs.TorchWheel;
        if (config != null && !config.AnimateTorchWheel)
            return 0.8f; // Static brightness when animation is off

        float progress = Math.Min(1f, (float)_stepsSinceLastTorch / SpacingS);
        return 1.0f - progress * 0.7f;
    }

    // ================================================================
    //  Drawing
    // ================================================================

    public override bool PreDraw(ref Color lightColor)
    {
        var config = WandConfigs.TorchWheel;
        if (config != null && config.AnimateTorchWheel)
        {
            float progress = Math.Min(1f, (float)_stepsSinceLastTorch / SpacingS);
            _spritesheet.CurrentRow = (int)(progress * 3.99f);
        }
        else
        {
            _spritesheet.CurrentRow = 0; // Static frame when animation is off
        }

        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle sourceRect = _spritesheet.GetCurrentSourceRect(texture);
        Vector2 origin = _spritesheet.GetFrameOrigin(texture);

        Vector2 drawPos = _visualPosition - Main.screenPosition;

        // Flip sprite based on direction
        SpriteEffects effects = _direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        Main.EntitySpriteDraw(
            texture,
            drawPos,
            sourceRect,
            Color.White,
            0f,
            origin,
            Projectile.scale,
            effects,
            0);

        return false;
    }
}
