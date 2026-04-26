using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// The flying (travel) phase of the platform torch wheel.
/// Travels in an arc toward the cursor until it lands on a platform from above.
/// Dies if it hits a solid tile.
/// </summary>
/// <remarks>
/// <para>
/// On platform landing, spawns a <see cref="TorchWheelPlatformProjectile"/>
/// which handles the actual horizontal tracing and torch placement.
/// </para>
/// <para>
/// Unlike <see cref="FlyingTorchWheelSolid"/> (which uses tileCollide=true and
/// <c>OnTileCollide</c>), this projectile uses manual collision detection because
/// Terraria's built-in tile collision treats platforms as passable when moving
/// downward — we need to detect platform landings explicitly.
/// </para>
/// <para>
/// Direction is determined by the horizontal displacement from the player's
/// shoot position to the platform landing site: positive X → right, negative → left.
/// </para>
/// </remarks>
public class FlyingTorchWheelPlatform : ModProjectile
{
    /// <summary>
    /// Travel speed in pixels per frame, used to normalize the initial
    /// velocity on the first AI tick. Should match the wand's
    /// <c>Item.shootSpeed</c> (12f).
    /// </summary>
    private const float TravelSpeed = 12f;

    /// <summary>Gravity applied during flight.</summary>
    private const float Gravity = 0.3f;

    /// <summary>Maximum fall speed.</summary>
    private const float MaxFallSpeed = 16f;

    // Spawn position for direction calculation
    private Vector2 _spawnPosition;
    private bool _initialized;

    // Spritesheet for rotation animation
    private SpritesheetHelper _spritesheet;
    private float _rotation;

    public override void SetStaticDefaults()
    {
        // Intentionally NOT setting Main.projFrames — the texture is a single
        // 16×16 orb, not a multi-frame spritesheet. Setting projFrames=4 caused
        // a 16×4 sliver to be drawn, creating an unintended "bean shape" when
        // rotated. See FlyingTorchWheelPlatform_BeanShape_Investigation.md.
    }

    public override void SetDefaults()
    {
        Projectile.width = 8;
        Projectile.height = 8;
        Projectile.friendly = ModContent.GetInstance<Common.Configs.TorchWheelConfig>().TorchWheelFriendly;
        Projectile.tileCollide = false; // We handle collision manually
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10 * 60; // 10 second safety timeout
        Projectile.light = 0.6f;
    }

    public override void OnSpawn(IEntitySource source)
    {
        _spawnPosition = Projectile.Center;
        _initialized = false;
        _spritesheet = new SpritesheetHelper(columns: 1, rows: 1);
        _rotation = 0f;
    }

    public override void AI()
    {
        if (!_initialized)
        {
            // Normalize and scale initial velocity
            if (Projectile.velocity.Length() > 0.01f)
            {
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * TravelSpeed;
            }
            _initialized = true;
        }

        // Apply gravity
        Projectile.velocity.Y += Gravity;
        if (Projectile.velocity.Y > MaxFallSpeed)
            Projectile.velocity.Y = MaxFallSpeed;

        // Check for collisions before moving
        Vector2 nextPosition = Projectile.Center + Projectile.velocity;
        int tileX = (int)(nextPosition.X / 16f);
        int tileY = (int)(nextPosition.Y / 16f);

        // Check current tile for solid collision → die
        if (WorldGen.InWorld(tileX, tileY, 1))
        {
            Tile tile = Main.tile[tileX, tileY];

            if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
            {
                SpawnImpactDust();
                Projectile.Kill();
                return;
            }
        }

        // Check tile below for platform landing (only when moving downward)
        if (Projectile.velocity.Y > 0)
        {
            int belowY = (int)((nextPosition.Y + 8) / 16f); // Check slightly below center
            if (WorldGen.InWorld(tileX, belowY, 1))
            {
                Tile belowTile = Main.tile[tileX, belowY];

                // Platform collision from above → spawn tracer
                if (belowTile.HasTile && TileID.Sets.Platforms[belowTile.TileType])
                {
                    // Verify we're actually above the platform (not inside it)
                    float platformTop = belowY * 16f;
                    if (Projectile.Center.Y <= platformTop && nextPosition.Y >= platformTop - 8)
                    {
                        SpawnPlatformTracer(tileX, belowY);
                        Projectile.Kill();
                        return;
                    }
                }
            }
        }

        // Move
        Projectile.Center = nextPosition;

        // Rotation animation
        _rotation += Projectile.velocity.X * 0.05f;

        // Dust trail
        if (Main.rand.NextBool(3))
        {
            var dust = Dust.NewDustDirect(
                Projectile.Center - new Vector2(4f), 8, 8,
                DustID.Torch, 0f, 0f, 150, default, 0.8f);
            dust.noGravity = true;
            dust.velocity = -Projectile.velocity * 0.2f;
        }
    }

    /// <summary>
    /// Spawns the platform tracer projectile at the landing position.
    /// </summary>
    private void SpawnPlatformTracer(int tileX, int tileY)
    {
        if (Main.myPlayer != Projectile.owner) return;

        Player owner = Main.player[Projectile.owner];

        // Check for torches before spawning
        if (!TorchPlacementHelper.HasTorches(owner))
        {
            Main.NewText("No torches in inventory!", Color.Orange);
            return;
        }

        // Determine direction: if landing is to the right of spawn, go right
        Vector2 landingPos = new Vector2(tileX * 16 + 8, tileY * 16 + 8);
        int direction = (landingPos.X - _spawnPosition.X) >= 0 ? 1 : -1;

        // The tracer sits above the platform tile for torch placement
        Vector2 spawnPos = new Vector2(tileX * 16 + 8, (tileY - 1) * 16 + 8);

        int projType = ModContent.ProjectileType<TorchWheelPlatformProjectile>();
        int projIndex = Projectile.NewProjectile(
            Projectile.GetSource_FromThis(),
            spawnPos,
            Vector2.Zero,
            projType,
            0,
            0f,
            Projectile.owner);

        if (projIndex >= 0 && projIndex < Main.maxProjectiles)
        {
            Projectile tracer = Main.projectile[projIndex];
            // Pass direction via ai[0]: 1 = right, -1 = left
            tracer.ai[0] = direction;
            // Pass platform Y level via ai[1] for reference
            tracer.ai[1] = tileY;
            tracer.tileCollide = false;
            tracer.netUpdate = true;
        }

        // Landing dust burst
        for (int i = 0; i < 8; i++)
        {
            var dust = Dust.NewDustDirect(
                spawnPos - new Vector2(8f), 16, 8,
                DustID.Torch, Main.rand.NextFloat(-2f, 2f), -2f, 100, default, 1.2f);
            dust.noGravity = true;
        }

        Mod.Logger.Debug(
            $"[TorchWheelPlatform] Landed at ({tileX},{tileY}), direction={(direction > 0 ? "Right" : "Left")}");
    }

    private void SpawnImpactDust()
    {
        for (int i = 0; i < 10; i++)
        {
            var dust = Dust.NewDustDirect(
                Projectile.Center - new Vector2(8f), 16, 16,
                DustID.Smoke, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f),
                150, default, 1f);
            dust.noGravity = false;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle sourceRect = _spritesheet.GetCurrentSourceRect(texture);
        Vector2 origin = _spritesheet.GetFrameOrigin(texture);

        Main.EntitySpriteDraw(
            texture,
            Projectile.Center - Main.screenPosition,
            sourceRect,
            Color.White,
            _rotation,
            origin,
            Projectile.scale,
            SpriteEffects.None,
            0);

        return false;
    }
}
