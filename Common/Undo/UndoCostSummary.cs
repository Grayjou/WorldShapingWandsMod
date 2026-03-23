using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Undo;

/// <summary>
/// Tracks the resource cost of undoing an operation.
/// "Lost" tiles/walls are those that currently exist but would be reverted.
/// "Restored" tiles/walls are those the snapshot would bring back.
/// </summary>
public class UndoCostSummary
{
    /// <summary>Tile types that currently exist and would be removed by undo (keyed by TileType → count).</summary>
    public Dictionary<int, int> LostTiles { get; } = new();

    /// <summary>Wall types that currently exist and would be removed by undo (keyed by WallType → count).</summary>
    public Dictionary<ushort, int> LostWalls { get; } = new();

    /// <summary>Tile types that the snapshot would restore (keyed by TileType → count).</summary>
    public Dictionary<int, int> RestoredTiles { get; } = new();

    /// <summary>Wall types that the snapshot would restore (keyed by WallType → count).</summary>
    public Dictionary<ushort, int> RestoredWalls { get; } = new();

    public void AddTile(int tileType)
    {
        if (!LostTiles.ContainsKey(tileType)) LostTiles[tileType] = 0;
        LostTiles[tileType]++;
    }

    public void AddWall(ushort wallType)
    {
        if (!LostWalls.ContainsKey(wallType)) LostWalls[wallType] = 0;
        LostWalls[wallType]++;
    }

    public void AddRestoredTile(int tileType)
    {
        if (!RestoredTiles.ContainsKey(tileType)) RestoredTiles[tileType] = 0;
        RestoredTiles[tileType]++;
    }

    public void AddRestoredWall(ushort wallType)
    {
        if (!RestoredWalls.ContainsKey(wallType)) RestoredWalls[wallType] = 0;
        RestoredWalls[wallType]++;
    }

    /// <summary>
    /// Returns true if the undo has zero resource impact (no tiles or walls change type/existence).
    /// </summary>
    public bool IsZeroCost => LostTiles.Count == 0 && LostWalls.Count == 0
        && RestoredTiles.Count == 0 && RestoredWalls.Count == 0;

    /// <summary>
    /// Formats the cost for chat display.
    /// </summary>
    public string FormatForChat()
    {
        if (IsZeroCost) return "  No tile/wall changes to reverse.";

        var sb = new StringBuilder();

        if (RestoredTiles.Count > 0 || RestoredWalls.Count > 0)
        {
            sb.AppendLine("  [c/90EE90:Restored by undo:]");
            foreach (var kvp in RestoredTiles)
            {
                string name = GetTileName(kvp.Key);
                sb.AppendLine($"    + {kvp.Value}x {name}");
            }
            foreach (var kvp in RestoredWalls)
            {
                string name = GetWallName(kvp.Key);
                sb.AppendLine($"    + {kvp.Value}x {name} (wall)");
            }
        }

        if (LostTiles.Count > 0 || LostWalls.Count > 0)
        {
            sb.AppendLine("  [c/FF6B6B:Removed by undo:]");
            foreach (var kvp in LostTiles)
            {
                string name = GetTileName(kvp.Key);
                sb.AppendLine($"    - {kvp.Value}x {name}");
            }
            foreach (var kvp in LostWalls)
            {
                string name = GetWallName(kvp.Key);
                sb.AppendLine($"    - {kvp.Value}x {name} (wall)");
            }
        }

        return sb.ToString().TrimEnd();
    }

    // ── Tile/Wall Name Resolution Cache ──────────────────────────────────

    private static Dictionary<int, string> _tileToItemName;
    private static Dictionary<int, bool> _tileIsWandPlaced;
    private static Dictionary<ushort, string> _wallToItemName;

    /// <summary>
    /// Builds the tile-type → item-name and wall-type → item-name caches on first use.
    /// Prefers consumable basic materials over non-consumable tile wands (HiveWand, etc.)
    /// when multiple items map to the same createTile.
    /// </summary>
    private static void EnsureCacheBuilt()
    {
        if (_tileToItemName != null) return;

        _tileToItemName = new Dictionary<int, string>();
        _tileIsWandPlaced = new Dictionary<int, bool>();
        _wallToItemName = new Dictionary<ushort, string>();

        for (int i = 0; i < ItemLoader.ItemCount; i++)
        {
            var item = ContentSamples.ItemsByType.GetValueOrDefault(i);
            if (item == null) continue;

            if (item.createTile > -1)
            {
                bool isWand = item.tileWand >= 0;

                // If we already have an entry, only overwrite if the existing entry
                // is a wand and the new item is a basic (consumable) material.
                // This ensures basic materials are preferred over tile wands.
                if (_tileToItemName.ContainsKey(item.createTile))
                {
                    if (_tileIsWandPlaced[item.createTile] && !isWand)
                    {
                        // Replace wand entry with basic material
                        _tileToItemName[item.createTile] = item.Name;
                        _tileIsWandPlaced[item.createTile] = false;
                    }
                }
                else
                {
                    _tileToItemName[item.createTile] = item.Name;
                    _tileIsWandPlaced[item.createTile] = isWand;
                }
            }

            if (item.createWall > 0 && !_wallToItemName.ContainsKey((ushort)item.createWall))
            {
                _wallToItemName[(ushort)item.createWall] = item.Name;
            }
        }
    }

    /// <summary>
    /// Multi-strategy tile name resolution:
    /// 1. Placeable item lookup (cached) — returns item name + wand-placed tag
    /// 2. Map entry name (Terraria's map hover text)
    /// 3. Modded tile class name (PascalCase split)
    /// 4. Fallback: Tile#N (natural)
    /// </summary>
    private static string GetTileName(int tileType)
    {
        EnsureCacheBuilt();

        // Strategy 1: Item that places this tile
        if (_tileToItemName.TryGetValue(tileType, out string itemName))
        {
            if (_tileIsWandPlaced.GetValueOrDefault(tileType))
                return $"{itemName} (wand-placed)";
            return itemName;
        }

        // Strategy 2: Map entry name (works for vanilla ambient/natural tiles)
        try
        {
            string mapName = Lang.GetMapObjectName(MapHelper.TileToLookup(tileType, 0));
            if (!string.IsNullOrWhiteSpace(mapName))
                return $"{mapName} (natural)";
        }
        catch { /* Some tile types may throw — fall through */ }

        // Strategy 3: Modded tile class name
        if (tileType >= TileID.Count)
        {
            var modTile = TileLoader.GetTile(tileType);
            if (modTile != null)
            {
                string name = SplitPascalCase(modTile.Name);
                return $"{name} (natural)";
            }
        }

        // Fallback
        return $"Tile#{tileType} (natural)";
    }

    /// <summary>
    /// Multi-strategy wall name resolution:
    /// 1. Placeable item lookup (cached)
    /// 2. Map entry name
    /// 3. Modded wall class name (PascalCase split)
    /// 4. Fallback: Wall#N (natural)
    /// </summary>
    private static string GetWallName(ushort wallType)
    {
        EnsureCacheBuilt();

        // Strategy 1: Item that places this wall
        if (_wallToItemName.TryGetValue(wallType, out string itemName))
            return itemName;

        // Strategy 2: Modded wall class name
        if (wallType >= WallID.Count)
        {
            var modWall = WallLoader.GetWall(wallType);
            if (modWall != null)
                return $"{SplitPascalCase(modWall.Name)} (natural)";
        }

        return $"Wall#{wallType} (natural)";
    }

    /// <summary>
    /// Splits a PascalCase identifier into space-separated words.
    /// e.g., "AstralDirtWall" → "Astral Dirt Wall"
    /// </summary>
    private static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length + 4);
        sb.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]))
                sb.Append(' ');
            else if (char.IsUpper(input[i]) && i + 1 < input.Length
                && !char.IsUpper(input[i + 1]) && char.IsLetter(input[i + 1]))
                sb.Append(' ');

            sb.Append(input[i]);
        }

        return sb.ToString();
    }
}
