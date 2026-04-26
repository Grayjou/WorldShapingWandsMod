using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Content.Projectiles;

/// <summary>
/// A small (8×8 px) travel projectile fired by the <see cref="Items.TorchWheelWandSolid"/>.
/// Flies toward the cursor until it hits a solid block, then spawns a
/// <see cref="TorchWheelSolidProjectile"/> with accurate collision data (impact face,
/// handedness, initial direction) and kills itself.
/// </summary>
/// <remarks>
/// <para>
/// The dual-projectile approach solves a handedness ambiguity: the original
/// 16×16 px TorchWheelSolidProjectile's center position on collision was often
/// inside the solid tile, making <see cref="HandednessCalculator.DetermineImpactFace"/>
/// unreliable. This 8×8 px projectile gives a much tighter collision point.
/// </para>
/// <para>
/// On tile collision, the spawned TorchWheelSolidProjectile receives its initialization
/// data via <c>ai[]</c> slots:
/// <list type="bullet">
///   <item><c>ai[0]</c> = 1 (initialized / tracing mode)</item>
///   <item><c>ai[1]</c> = initial <see cref="CardinalDirection"/> (cast to float)</item>
/// </list>
/// Additional data (handedness, impact face, start position) is encoded in the
/// projectile's position and velocity before the first AI tick.
/// </para>
/// </remarks>
public class FlyingTorchWheelSolid : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 8;
        Projectile.height = 8;
        Projectile.friendly = ModContent.GetInstance<Common.Configs.TorchWheelConfig>().TorchWheelFriendly;
        Projectile.tileCollide = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300; // 5 seconds max flight
        Projectile.light = 0.6f;
        Projectile.extraUpdates = 0;
    }

    public override void AI()
    {
        // Schoice while flying
        //Projectile.rotation += 0.3f * Projectile.direction;

        // Fire dust trail so the projectile is visible
        if (Main.rand.NextBool(2))
        {
            var dust = Dust.NewDustDirect(
                Projectile.position, Projectile.width, Projectile.height,
                DustID.Torch,
                -Projectile.velocity.X * 0.3f, -Projectile.velocity.Y * 0.3f,
                100, default, 1.0f);
            dust.noGravity = true;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // === Determine collision face and handedness ===
        // Handedness uses the quadrant-based algorithm: the velocity direction
        // determines a quadrant, and the impact face within that quadrant
        // determines whether to trace right-handed or left-handed.
        BlockFace face = DetermineCollisionFace(Projectile.velocity, oldVelocity);
        Handedness handedness = HandednessCalculator.ComputeHandedness(oldVelocity, face);
        CardinalDirection initialDir = HandednessCalculator.GetInitialDirection(face, handedness);

        // === Locate the solid tile and the adjacent air tile ===
        // The projectile center at collision time is in the air tile, NOT in the solid.
        // We probe forward along the collision axis to find the actual solid tile,
        // then place the tracer in the air tile adjacent to that solid face.
        Point16 centerTile = new(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));
        Point16 solidTile = FindSolidTile(centerTile, face);
        Point16 startPos = GetAirTileAdjacentToFace(solidTile, face);

        // === Spawn the tracing TorchWheelSolidProjectile ===
        if (Main.myPlayer == Projectile.owner)
        {
            Vector2 spawnPos = new(startPos.X * 16 + 8, startPos.Y * 16 + 8);

            int projIdx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                Vector2.Zero,
                ModContent.ProjectileType<TorchWheelSolidProjectile>(),
                0, 0f,
                Projectile.owner,
                ai0: 1f,                    // AiInitialized = tracing
                ai1: (float)initialDir);    // AiDirection

            if (projIdx >= 0 && projIdx < Main.maxProjectiles)
            {
                var traceProj = Main.projectile[projIdx];

                // Pass handedness via localAI[0] — read by TorchWheelSolidProjectile on first frame
                traceProj.localAI[0] = (float)handedness;

                // The projectile should not collide with tiles (it's already at the wall)
                traceProj.tileCollide = false;
                traceProj.netUpdate = true;
            }
        }

        // Kill this travel projectile
        return true;
    }

    /// <summary>
    /// Determines the collision face by comparing post-collision and pre-collision velocities.
    /// Terraria zeroes the velocity component on the axis that collided, so we detect which
    /// component changed. This is more reliable than <see cref="HandednessCalculator.DetermineImpactFace"/>
    /// at shallow angles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Shallow-angle correction:</b> At very shallow angles, Terraria's collision
    /// system can report the wrong face (e.g., hitting the side instead of the top).
    /// We cross-check using the velocity direction: given the velocity quadrant, only
    /// two faces are physically reachable. If the detected face is NOT one of those two,
    /// we check the adjacent tile on that face — if it's solid, the projectile actually
    /// hit the other reachable face.
    /// </para>
    /// <para>
    /// Example: velocity (+X, +Y) can only hit Left or Top. If we detect Right, we
    /// check the tile to the right — if solid, we actually hit Top.
    /// </para>
    /// </remarks>
    private BlockFace DetermineCollisionFace(Vector2 postVelocity, Vector2 preVelocity)
    {
        bool xCollided = Math.Abs(postVelocity.X) < Math.Abs(preVelocity.X) * 0.5f;
        bool yCollided = Math.Abs(postVelocity.Y) < Math.Abs(preVelocity.Y) * 0.5f;

        BlockFace detected;

        if (yCollided && !xCollided)
        {
            // Y was zeroed → hit top or bottom face
            detected = preVelocity.Y > 0 ? BlockFace.Top : BlockFace.Bottom;
        }
        else if (xCollided && !yCollided)
        {
            // X was zeroed → hit left or right face
            detected = preVelocity.X > 0 ? BlockFace.Left : BlockFace.Right;
        }
        else
        {
            // Both axes or neither changed — corner hit. Fall back to dominant axis.
            detected = HandednessCalculator.DetermineImpactFace(preVelocity);
        }

        // === Shallow-angle cross-check ===
        // Based on velocity quadrant, determine which two faces are physically reachable.
        // If the detected face is not one of them, correct it using adjacent tile solidity.
        return CorrectShallowAngleFace(detected, preVelocity);
    }

    /// <summary>
    /// Corrects misdetected collision faces at shallow angles by checking adjacent tile solidity.
    /// </summary>
    /// <remarks>
    /// <para>Velocity quadrant → reachable faces:</para>
    /// <list type="bullet">
    ///   <item>(+X, +Y): can hit Left or Top</item>
    ///   <item>(+X, -Y): can hit Left or Bottom</item>
    ///   <item>(-X, +Y): can hit Right or Top</item>
    ///   <item>(-X, -Y): can hit Right or Bottom</item>
    /// </list>
    /// <para>
    /// If the detected face is not in the reachable set, we check the tile adjacent to the
    /// collision point on the detected face's axis. If that tile is solid, the projectile must
    /// have actually hit the OTHER reachable face.
    /// </para>
    /// </remarks>
    private BlockFace CorrectShallowAngleFace(BlockFace detected, Vector2 velocity)
    {
        // Determine which two faces are physically reachable based on velocity direction
        BlockFace horizontalFace = velocity.X > 0 ? BlockFace.Left : BlockFace.Right;
        BlockFace verticalFace = velocity.Y > 0 ? BlockFace.Top : BlockFace.Bottom;

        // If the detected face is one of the two reachable faces, it's likely correct
        if (detected == horizontalFace || detected == verticalFace)
            return detected;

        // Detected face is NOT reachable — this is a shallow-angle misdetection.
        // Check adjacent tile to determine which reachable face was actually hit.
        Point16 centerTile = new(
            (int)(Projectile.Center.X / 16f),
            (int)(Projectile.Center.Y / 16f));

        // If we misdetected a horizontal face (Left/Right), the actual hit might be vertical.
        // Check the tile in the misdetected direction. If solid → we actually hit the vertical face.
        if (detected == BlockFace.Left || detected == BlockFace.Right)
        {
            int checkX = detected == BlockFace.Left ? centerTile.X - 1 : centerTile.X + 1;
            if (WorldGen.InWorld(checkX, centerTile.Y) && WallFollower.IsSolid(checkX, centerTile.Y))
                return verticalFace;
            return horizontalFace; // Adjacent tile is air → default to the horizontal reachable face
        }
        else
        {
            // Misdetected a vertical face (Top/Bottom), actual hit might be horizontal
            int checkY = detected == BlockFace.Top ? centerTile.Y - 1 : centerTile.Y + 1;
            if (WorldGen.InWorld(centerTile.X, checkY) && WallFollower.IsSolid(centerTile.X, checkY))
                return horizontalFace;
            return verticalFace; // Adjacent tile is air → default to the vertical reachable face
        }
    }

    /// <summary>
    /// Probes from the air tile (projectile center at collision) toward the solid block
    /// along the collision face's inward normal. Returns the first solid tile found,
    /// or the original position as fallback.
    /// </summary>
    private static Point16 FindSolidTile(Point16 airTile, BlockFace face)
    {
        // Step inward (opposite of the face's outward normal) to find the solid tile
        (int dx, int dy) = face switch
        {
            BlockFace.Top    => (0, 1),   // Face is Top → solid is below
            BlockFace.Bottom => (0, -1),  // Face is Bottom → solid is above
            BlockFace.Left   => (1, 0),   // Face is Left → solid is to the right
            BlockFace.Right  => (-1, 0),  // Face is Right → solid is to the left
            _ => (0, 0),
        };

        for (int i = 0; i <= 3; i++)
        {
            int x = airTile.X + dx * i;
            int y = airTile.Y + dy * i;

            if (WorldGen.InWorld(x, y) && WallFollower.IsSolid(x, y))
                return new Point16(x, y);
        }

        // Fallback: assume the solid is one step inward
        return new Point16(airTile.X + dx, airTile.Y + dy);
    }

    /// <summary>
    /// Given a solid tile and the face that was hit, returns the air tile adjacent
    /// to that face. This is where the tracing projectile should start.
    /// </summary>
    private static Point16 GetAirTileAdjacentToFace(Point16 solidTile, BlockFace face)
    {
        // Step outward from the solid tile along the face's normal
        (int dx, int dy) = face switch
        {
            BlockFace.Top    => (0, -1),  // Air is above the solid
            BlockFace.Bottom => (0, 1),   // Air is below the solid
            BlockFace.Left   => (-1, 0),  // Air is to the left
            BlockFace.Right  => (1, 0),   // Air is to the right
            _ => (0, 0),
        };

        int x = solidTile.X + dx;
        int y = solidTile.Y + dy;

        // Validate the air tile is actually empty
        if (WorldGen.InWorld(x, y) && WallFollower.IsEmpty(x, y))
            return new Point16(x, y);

        // Fallback: return the solid tile position (tracing will handle it)
        return solidTile;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        
        Main.EntitySpriteDraw(
            texture,
            Projectile.Center - Main.screenPosition,
            null,
            Color.White,
            Projectile.rotation,
            texture.Size() / 2f,
            Projectile.scale,
            SpriteEffects.None,
            0);

        return false;
    }


}

