using Terraria;
using Terraria.DataStructures;

namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// The four cardinal grid directions used by the wall-following algorithm.
/// Values 0–3 are ordered so that <c>(dir + 1) % 4</c> gives a 90° clockwise turn.
/// </summary>
public enum CardinalDirection : byte
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3,
}

/// <summary>
/// Wall-following logic with configurable handedness for the TorchWheelSolidProjectile.
/// Given a current air-space position and facing direction, <see cref="Step"/>
/// attempts to move forward while keeping one wall adjacent.
/// </summary>
/// <remarks>
/// <para>
/// Ported from the TorchPlacement2D R&amp;D algorithm (ModReferences/TorchPlacement2D).
/// </para>
/// <para>
/// Turn priority depends on handedness:
/// <list type="bullet">
///   <item><b>Right-handed:</b> Try right, forward, left, back — wall stays on right</item>
///   <item><b>Left-handed:</b> Try left, forward, right, back — wall stays on left</item>
/// </list>
/// </para>
/// </remarks>
public class WallFollower
{
    /// <summary>
    /// Tile deltas for each <see cref="CardinalDirection"/>.
    /// </summary>
    private static readonly Point16[] Deltas =
    [
        new(0, -1),  // Up
        new(1, 0),   // Right
        new(0, 1),   // Down
        new(-1, 0),  // Left
    ];

    private readonly Handedness _handedness;

    public WallFollower(Handedness handedness)
    {
        _handedness = handedness;
    }

    /// <summary>
    /// Performs one wall-following step from the given position and direction.
    /// </summary>
    /// <param name="pos">Current air-space tile position.</param>
    /// <param name="dir">Current facing direction.</param>
    /// <returns>
    /// The new position and direction, or <c>null</c> if the projectile is stuck
    /// (all four neighbors are solid or out-of-world).
    /// </returns>
    public (Point16 newPos, CardinalDirection newDir)? Step(Point16 pos, CardinalDirection dir)
    {
        // Turn order depends on handedness:
        //   Right-handed: +1 (right), 0 (forward), +3 (left), +2 (back)
        //   Left-handed:  +3 (left),  0 (forward), +1 (right), +2 (back)
        int[] turnOffsets = _handedness == Handedness.Right
            ? [1, 0, 3, 2]
            : [3, 0, 1, 2];

        foreach (int offset in turnOffsets)
        {
            CardinalDirection newDir = (CardinalDirection)(((int)dir + offset) % 4);
            Point16 delta = Deltas[(int)newDir];
            int newX = pos.X + delta.X;
            int newY = pos.Y + delta.Y;

            if (IsEmpty(newX, newY))
                return (new Point16(newX, newY), newDir);
        }

        return null; // Stuck — all directions blocked
    }

    // ================================================================
    //  Tile Queries — static helpers shared with placement checks
    // ================================================================

    /// <summary>
    /// Returns <c>true</c> if the tile at (x, y) allows the projectile to pass through.
    /// A tile is "empty" if it has no tile, or the tile is non-solid (e.g. torches, platforms).
    /// </summary>
    public static bool IsEmpty(int x, int y)
    {
        if (!WorldGen.InWorld(x, y)) return false;

        Tile tile = Main.tile[x, y];

        if (!tile.HasTile) return true;
        if (!Main.tileSolid[tile.TileType]) return true;

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the tile at (x, y) is a solid block.
    /// </summary>
    public static bool IsSolid(int x, int y)
    {
        if (!WorldGen.InWorld(x, y)) return false;

        Tile tile = Main.tile[x, y];
        return tile.HasTile && Main.tileSolid[tile.TileType];
    }

    /// <summary>
    /// Returns <c>true</c> if the tile at (x, y) is a half-block or has a slope.
    /// Sloped tiles cannot hold torches reliably.
    /// </summary>
    public static bool IsSlope(int x, int y)
    {
        if (!WorldGen.InWorld(x, y)) return false;

        Tile tile = Main.tile[x, y];
        return tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.Solid;
    }

    /// <summary>
    /// Returns <c>true</c> if the tile at (x, y) is a solid, non-sloped block
    /// that can reliably hold a torch on one of its faces.
    /// </summary>
    public static bool CanHoldTorch(int x, int y)
    {
        return IsSolid(x, y) && !IsSlope(x, y);
    }
}
