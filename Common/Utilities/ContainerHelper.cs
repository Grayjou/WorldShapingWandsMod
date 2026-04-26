using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;

namespace WorldShapingWandsMod.Common.Utilities;

/// <summary>
/// Helper for detecting and destroying containers (chests, dressers, etc.)
/// within a tile selection. Handles locked chest unlocking and item dropping.
/// </summary>
public static class ContainerHelper
{
    /// <summary>
    /// Represents a container found in the selection with its metadata.
    /// </summary>
    public struct ContainerInfo
    {
        public int ChestIndex;
        public Point TopLeft;
        public int TileType;
        public bool IsLocked;
    }

    /// <summary>
    /// Finds all unique containers within the given tile set.
    /// Uses Chest.FindChest on each tile and deduplicates by chest index.
    /// Also detects containers via Main.tileContainer[] for modded chest support.
    /// </summary>
    public static List<ContainerInfo> FindContainers(IEnumerable<Point> tiles)
    {
        var found = new HashSet<int>();
        var results = new List<ContainerInfo>();

        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;

            Tile t = Main.tile[x, y];
            if (!t.HasTile) continue;

            // Check if this tile type is a container (vanilla or modded)
            int tileType = t.TileType;
            if (!IsContainerTile(tileType)) continue;

            // Find the chest at this position
            int chestIndex = Chest.FindChest(x, y);
            if (chestIndex < 0)
            {
                // Try finding from the top-left of the multi-tile object
                var topLeft = FindTopLeft(x, y, tileType);
                chestIndex = Chest.FindChest(topLeft.X, topLeft.Y);
            }

            if (chestIndex < 0 || found.Contains(chestIndex)) continue;
            found.Add(chestIndex);

            var info = new ContainerInfo
            {
                ChestIndex = chestIndex,
                TopLeft = new Point(Main.chest[chestIndex].x, Main.chest[chestIndex].y),
                TileType = tileType,
                IsLocked = Chest.IsLocked(Main.chest[chestIndex].x, Main.chest[chestIndex].y)
            };
            results.Add(info);
        }

        return results;
    }

    /// <summary>
    /// Checks if a tile type is a container (chest, dresser, or modded container).
    /// Uses Main.tileContainer[] which is set by both vanilla and modded chests.
    /// </summary>
    public static bool IsContainerTile(int tileType)
    {
        if (tileType < 0 || tileType >= Main.tileContainer.Length) return false;
        return Main.tileContainer[tileType];
    }

    /// <summary>
    /// Finds the top-left tile of a multi-tile object given any tile in it.
    /// </summary>
    private static Point FindTopLeft(int x, int y, int tileType)
    {
        Tile t = Main.tile[x, y];
        var data = TileObjectData.GetTileData(tileType, 0);
        if (data == null) return new Point(x, y);

        int frameX = t.TileFrameX;
        int frameY = t.TileFrameY;
        int subX = frameX % (data.CoordinateFullWidth) / (data.CoordinateWidth + data.CoordinatePadding);
        int subY = frameY / (data.CoordinateHeights[0] + data.CoordinatePadding);

        return new Point(x - subX, y - subY);
    }

    /// <summary>
    /// Attempts to unlock a locked chest. Checks if the player has the appropriate key.
    /// Returns true if the chest was successfully unlocked (or was already unlocked).
    ///
    /// Key facts about vanilla chest styles and locking (TileID.Containers = 21):
    ///
    /// STYLE MAPPING (style = TileFrameX / 36):
    ///   Style 1  = Gold Chest (UNLOCKED)    → Lock(+36) → Style 2  = Gold Chest (LOCKED)
    ///   Style 3  = Shadow Chest (UNLOCKED)  → Lock(+36) → Style 4  = Shadow Chest (LOCKED)
    ///   Styles 18–22 = Biome Chests (UNLOCKED) → Lock(+180) → Styles 23–27 = Biome Chests (LOCKED)
    ///   Styles 35,37,39 = Other (UNLOCKED)  → Lock(+36) → Styles 36,38,40 = Other (LOCKED)
    ///
    /// Chest.IsLocked returns true for: styles 2, 4, 23–27, 36, 38, 40 (and Containers2 style 13).
    /// Chest.Unlock handles ALL of these — shifts frameX back and plays sound/dust.
    ///
    /// KEY REQUIREMENTS (our responsibility to check before calling Chest.Unlock):
    ///   Style 2  (Locked Gold):   Golden Key — CONSUMED on use
    ///   Style 4  (Locked Shadow): Shadow Key — NOT consumed (stays in inventory)
    ///   Styles 23–27 (Biome):     Respective biome key — CONSUMED, requires Plantera defeated
    ///   Styles 36,38,40:          Chest.Unlock handles directly (no key needed from us)
    ///   Containers2 style 13:     Biome key — CONSUMED, requires Plantera defeated
    ///
    /// - Modded chests (type >= TileID.Count): handled by TileLoader.UnlockChest
    ///   which Chest.Unlock calls internally.
    /// </summary>
    public static bool TryUnlockChest(Player player, int x, int y, int tileType)
    {
        if (!Chest.IsLocked(x, y)) return true; // Already unlocked

        // Sandbox: bypass key requirements — unlock without consuming keys
        var config = WandConfigs.Sandbox;
        if (config?.EffectiveIgnoreLockedKeyRequirements == true)
        {
            // Chest.Unlock handles frame shifts for all vanilla locked styles
            // (styles 2, 4, 23–27, 36, 38, 40, and Containers2 style 13).
            // It also delegates to TileLoader.UnlockChest for modded chests.
            bool unlocked = Chest.Unlock(x, y);
            if (!unlocked)
            {
                // Chest.Unlock failed — unknown chest type we can't force-unlock
                return false;
            }
            return true;
        }

        Tile t = Main.tile[x, y];
        int style = t.TileFrameX / 36;

        // --- Locked Gold Chest (TileID.Containers, style 2) ---
        // Golden Key is consumed. Chest.Unlock handles the frame shift (style 2 → style 1).
        if (tileType == TileID.Containers && style == 2)
        {
            if (!HasItem(player, ItemID.GoldenKey))
                return false;

            bool unlocked = Chest.Unlock(x, y);
            if (unlocked) ConsumeItem(player, ItemID.GoldenKey);
            return unlocked;
        }

        // --- Locked Shadow Chest (TileID.Containers, style 4) ---
        // Shadow Key is NOT consumed. Chest.Unlock handles the frame shift (style 4 → style 3).
        if (tileType == TileID.Containers && style == 4)
        {
            if (!HasItem(player, ItemID.ShadowKey))
                return false;

            return Chest.Unlock(x, y);
        }

        // --- Biome chests (TileID.Containers, styles 23–27) ---
        // Requires respective biome key (consumed) and Plantera defeated.
        // Chest.Unlock handles the frame shift (shift -180, e.g. style 23 → style 18).
        if (tileType == TileID.Containers)
        {
            int? biomeKey = GetBiomeKeyForContainerStyle(style);
            if (biomeKey.HasValue)
            {
                if (!NPC.downedPlantBoss) return false; // Biome chests sealed until Plantera
                if (!HasItem(player, biomeKey.Value)) return false;
                bool unlocked = Chest.Unlock(x, y);
                if (unlocked) ConsumeItem(player, biomeKey.Value);
                return unlocked;
            }
        }

        // --- Biome chests (TileID.Containers2, style 13) ---
        if (tileType == TileID.Containers2)
        {
            int? biomeKey = GetBiomeKeyForContainers2Style(style);
            if (biomeKey.HasValue)
            {
                if (!NPC.downedPlantBoss) return false;
                if (!HasItem(player, biomeKey.Value)) return false;
                bool unlocked = Chest.Unlock(x, y);
                if (unlocked) ConsumeItem(player, biomeKey.Value);
                return unlocked;
            }
        }

        // --- Styles 36, 38, 40 and other vanilla locked chests ---
        // Chest.Unlock handles these directly (no key needed from us).
        // Also handles modded chests via TileLoader.UnlockChest.
        return Chest.Unlock(x, y);
    }

    /// <summary>
    /// Maps a TileID.Containers style index to the required biome key item ID.
    /// These are the jungle/biome chest styles that appear in the dungeon after Plantera.
    /// </summary>
    private static int? GetBiomeKeyForContainerStyle(int style)
    {
        return style switch
        {
            23 => ItemID.JungleKey,
            24 => ItemID.CorruptionKey,
            25 => ItemID.CrimsonKey,
            26 => ItemID.HallowedKey,
            27 => ItemID.FrozenKey,
            36 => ItemID.DungeonDesertKey, // Desert biome chest
            _ => null
        };
    }

    /// <summary>
    /// Maps a TileID.Containers2 style index to the required biome key item ID.
    /// Style 13 is the Jungle chest in TileID.Containers2 (value 467).
    /// </summary>
    private static int? GetBiomeKeyForContainers2Style(int style)
    {
        return style switch
        {
            13 => ItemID.JungleKey,
            _ => null
        };
    }

    /// <summary>
    /// Drops all items from a chest into the world as pickups.
    /// </summary>
    public static int DropChestContents(int chestIndex, Point position)
    {
        var chest = Main.chest[chestIndex];
        if (chest == null) return 0;

        int dropped = 0;
        for (int i = 0; i < chest.item.Length; i++)
        {
            Item item = chest.item[i];
            if (item == null || item.IsAir) continue;

            Item.NewItem(
                new EntitySource_TileBreak(position.X, position.Y),
                position.X * 16, position.Y * 16, 32, 32,
                item.type, item.stack, false, item.prefix);

            item.TurnToAir();
            dropped++;
        }
        return dropped;
    }

    /// <summary>
    /// Destroys a container: drops its contents, removes the chest data,
    /// and kills the tiles. Returns the number of items dropped.
    /// </summary>
    public static (int itemsDropped, bool destroyed) DestroyContainer(
        Player player, ContainerInfo container, bool suppressDrops)
    {
        // Try to unlock if locked
        if (container.IsLocked)
        {
            if (!TryUnlockChest(player, container.TopLeft.X, container.TopLeft.Y, container.TileType))
                return (0, false); // Can't unlock — skip this container
        }

        // Drop contents (unless suppressing drops)
        int dropped = 0;
        if (!suppressDrops)
            dropped = DropChestContents(container.ChestIndex, container.TopLeft);
        else
        {
            // Still clear the items even if suppressing drops
            var chest = Main.chest[container.ChestIndex];
            if (chest != null)
                for (int i = 0; i < chest.item.Length; i++)
                    chest.item[i]?.TurnToAir();
        }

        // Remove the chest from the world array
        Chest.DestroyChestDirect(container.ChestIndex, container.TopLeft.X, container.TopLeft.Y);

        // Kill the tiles that make up the container
        // Use KillTile with noItem=true since we already handled contents
        var data = TileObjectData.GetTileData(container.TileType, 0);
        int width = data?.Width ?? 2;
        int height = data?.Height ?? 2;

        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int tx = container.TopLeft.X + dx;
                int ty = container.TopLeft.Y + dy;
                if (WorldGen.InWorld(tx, ty, 1) && Main.tile[tx, ty].HasTile)
                {
                    WorldGen.KillTile(tx, ty, fail: false, effectOnly: false, noItem: true);
                }
            }
        }

        return (dropped, true);
    }

    private static bool HasItem(Player player, int itemType)
    {
        for (int i = 0; i < 58; i++)
            if (player.inventory[i].type == itemType && player.inventory[i].stack > 0)
                return true;
        return false;
    }

    private static void ConsumeItem(Player player, int itemType)
    {
        for (int i = 0; i < 58; i++)
        {
            if (player.inventory[i].type == itemType && player.inventory[i].stack > 0)
            {
                player.inventory[i].stack--;
                if (player.inventory[i].stack <= 0)
                    player.inventory[i].TurnToAir();
                return;
            }
        }
    }
}
