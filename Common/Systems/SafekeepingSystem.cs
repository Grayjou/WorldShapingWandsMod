using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// World-scoped system that tracks which tile/wall positions are protected from modification.
/// Protected positions persist across saves via TagCompound serialisation.
/// </summary>
public class SafekeepingSystem : ModSystem
{
    private static HashSet<Point> _protectedTiles = new();
    private static HashSet<Point> _protectedWalls = new();

    // ── Tile protection ─────────────────────────────────────
    public static bool IsTileProtected(int x, int y) => _protectedTiles.Contains(new Point(x, y));
    public static bool IsTileProtected(Point tile) => _protectedTiles.Contains(tile);

    public static void ProtectTile(int x, int y) => _protectedTiles.Add(new Point(x, y));
    public static void ProtectTile(Point tile) => _protectedTiles.Add(tile);

    public static bool UnprotectTile(int x, int y) => _protectedTiles.Remove(new Point(x, y));
    public static bool UnprotectTile(Point tile) => _protectedTiles.Remove(tile);

    // ── Wall protection ─────────────────────────────────────
    public static bool IsWallProtected(int x, int y) => _protectedWalls.Contains(new Point(x, y));
    public static bool IsWallProtected(Point tile) => _protectedWalls.Contains(tile);

    public static void ProtectWall(int x, int y) => _protectedWalls.Add(new Point(x, y));
    public static void ProtectWall(Point tile) => _protectedWalls.Add(tile);

    public static bool UnprotectWall(int x, int y) => _protectedWalls.Remove(new Point(x, y));
    public static bool UnprotectWall(Point tile) => _protectedWalls.Remove(tile);

    // ── Read-only access ────────────────────────────────────
    public static IReadOnlyCollection<Point> ProtectedTiles => _protectedTiles;
    public static IReadOnlyCollection<Point> ProtectedWalls => _protectedWalls;
    public static int ProtectedTileCount => _protectedTiles.Count;
    public static int ProtectedWallCount => _protectedWalls.Count;

    /// <summary>
    /// Returns true if the position is protected for either tiles or walls.
    /// Convenience method used by wands that don't distinguish tile vs wall.
    /// </summary>
    public static bool IsProtected(int x, int y)
    {
        var p = new Point(x, y);
        return _protectedTiles.Contains(p) || _protectedWalls.Contains(p);
    }

    /// <summary>Removes all protection data (tiles + walls).</summary>
    public static void ClearAll()
    {
        _protectedTiles.Clear();
        _protectedWalls.Clear();
    }

    // ── Lifecycle ───────────────────────────────────────────

    public override void ClearWorld()
    {
        _protectedTiles.Clear();
        _protectedWalls.Clear();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        // Store as flat int arrays for efficiency: [x0,y0, x1,y1, ...]
        if (_protectedTiles.Count > 0)
        {
            var arr = new int[_protectedTiles.Count * 2];
            int i = 0;
            foreach (var p in _protectedTiles)
            {
                arr[i++] = p.X;
                arr[i++] = p.Y;
            }
            tag["protectedTiles"] = arr;
        }

        if (_protectedWalls.Count > 0)
        {
            var arr = new int[_protectedWalls.Count * 2];
            int i = 0;
            foreach (var p in _protectedWalls)
            {
                arr[i++] = p.X;
                arr[i++] = p.Y;
            }
            tag["protectedWalls"] = arr;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        _protectedTiles.Clear();
        _protectedWalls.Clear();

        if (tag.ContainsKey("protectedTiles"))
        {
            var arr = tag.GetIntArray("protectedTiles");
            for (int i = 0; i + 1 < arr.Length; i += 2)
                _protectedTiles.Add(new Point(arr[i], arr[i + 1]));
        }

        if (tag.ContainsKey("protectedWalls"))
        {
            var arr = tag.GetIntArray("protectedWalls");
            for (int i = 0; i + 1 < arr.Length; i += 2)
                _protectedWalls.Add(new Point(arr[i], arr[i + 1]));
        }
    }
}
