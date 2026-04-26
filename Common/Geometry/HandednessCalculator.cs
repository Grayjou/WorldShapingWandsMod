using Microsoft.Xna.Framework;
using System;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// Wall-following handedness — determines which side of a surface
/// the projectile keeps adjacent as it traces outlines.
/// </summary>
public enum Handedness : byte
{
    Right = 0,
    Left = 1,
}

/// <summary>
/// The face of a solid block that was hit by the projectile.
/// </summary>
public enum BlockFace : byte
{
    Top = 0,
    Bottom = 1,
    Left = 2,
    Right = 3,
}

/// <summary>
/// Determines wall-following handedness from a projectile's velocity and
/// impact face. Used by the TorchWheelSolidProjectile to decide which direction
/// to trace after hitting its first solid block.
/// </summary>
/// <remarks>
/// Ported from the TorchPlacement2D R&amp;D algorithm (ModReferences/TorchPlacement2D).
/// The handedness is computed via a 2D cross product (v × n):
/// <list type="bullet">
///   <item>Positive cross → Right-handed (wall on the right)</item>
///   <item>Negative cross → Left-handed (wall on the left)</item>
///   <item>Zero → defaults to right-handed</item>
/// </list>
/// </remarks>
public static class HandednessCalculator
{
    /// <summary>
    /// Face normals pointing OUTWARD from the block.
    /// Index matches <see cref="BlockFace"/> ordinal values.
    /// </summary>
    private static readonly Vector2[] FaceNormals =
    [
        new(0, -1),  // Top    — normal points up
        new(0, 1),   // Bottom — normal points down
        new(-1, 0),  // Left   — normal points left
        new(1, 0),   // Right  — normal points right
    ];

    /// <summary>
    /// Computes handedness from velocity and impact face using quadrant-based logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The velocity direction determines a quadrant (1–4), and the impact face
    /// within that quadrant determines whether the projectile should trace
    /// right-handed (wall on the right) or left-handed (wall on the left).
    /// </para>
    /// <para>
    /// Quadrant mapping (screen coords: +Y = down):
    /// <list type="bullet">
    ///   <item>Q1 (+X, −Y): hit left → right, hit bottom → left</item>
    ///   <item>Q2 (−X, −Y): hit right → left, hit bottom → right</item>
    ///   <item>Q3 (−X, +Y): hit right → right, hit top → left</item>
    ///   <item>Q4 (+X, +Y): hit left → left, hit top → right</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="velocity">Projectile velocity at impact.</param>
    /// <param name="impactFace">Which face of the block was struck.</param>
    /// <param name="defaultHand">Fallback when the quadrant/face combination is ambiguous.</param>
    /// <returns>Whether the projectile should trace right-handed or left-handed.</returns>
    public static Handedness ComputeHandedness(
        Vector2 velocity,
        BlockFace impactFace,
        Handedness defaultHand = Handedness.Right)
    {
        bool positiveX = velocity.X >= 0;
        bool positiveY = velocity.Y >= 0;

        // Quadrant 1: +X, -Y (moving right and up in screen coords)
        if (positiveX && !positiveY)
        {
            if (impactFace == BlockFace.Left)   return Handedness.Right;
            if (impactFace == BlockFace.Bottom)  return Handedness.Left;
            return defaultHand;
        }

        // Quadrant 2: -X, -Y (moving left and up)
        if (!positiveX && !positiveY)
        {
            if (impactFace == BlockFace.Right)  return Handedness.Left;
            if (impactFace == BlockFace.Bottom)  return Handedness.Right;
            return defaultHand;
        }

        // Quadrant 3: -X, +Y (moving left and down)
        if (!positiveX && positiveY)
        {
            if (impactFace == BlockFace.Right)  return Handedness.Right;
            if (impactFace == BlockFace.Top)     return Handedness.Left;
            return defaultHand;
        }

        // Quadrant 4: +X, +Y (moving right and down)
        if (positiveX && positiveY)
        {
            if (impactFace == BlockFace.Left)   return Handedness.Left;
            if (impactFace == BlockFace.Top)     return Handedness.Right;
            return defaultHand;
        }

        return defaultHand;
    }

    /// <summary>
    /// Determines which face of a block was hit based on approach direction.
    /// Uses the dominant velocity axis.
    /// </summary>
    public static BlockFace DetermineImpactFace(Vector2 velocity)
    {
        if (Math.Abs(velocity.X) > Math.Abs(velocity.Y))
            return velocity.X > 0 ? BlockFace.Left : BlockFace.Right;
        else
            return velocity.Y > 0 ? BlockFace.Top : BlockFace.Bottom;
    }

    /// <summary>
    /// Gets the initial walking direction after impact, given handedness.
    /// Rotates the face normal 90° in the appropriate direction:
    /// right-handed → counter-clockwise, left-handed → clockwise.
    /// </summary>
    public static CardinalDirection GetInitialDirection(BlockFace impactFace, Handedness handedness)
    {
        Vector2 normal = FaceNormals[(int)impactFace];
        Vector2 walkDir;

        if (handedness == Handedness.Right)
        {
            // Rotate normal 90° clockwise (wall stays on right)
            // CW rotation: (x, y) → (y, -x)  — but in screen coords Y is down,
            // so CW on screen is: (x, y) → (-y, x)
            walkDir = new Vector2(-normal.Y, normal.X);
        }
        else
        {
            // Rotate normal 90° counter-clockwise (wall stays on left)
            // CCW rotation: (x, y) → (y, -x)
            walkDir = new Vector2(normal.Y, -normal.X);
        }

        return DirectionFromVector(walkDir);
    }

    private static CardinalDirection DirectionFromVector(Vector2 v)
    {
        if (Math.Abs(v.X) > Math.Abs(v.Y))
            return v.X > 0 ? CardinalDirection.Right : CardinalDirection.Left;
        else
            return v.Y > 0 ? CardinalDirection.Down : CardinalDirection.Up;
    }
}
