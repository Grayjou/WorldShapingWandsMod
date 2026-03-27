using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for dismantling operations.
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class DismantlingPacketHandler
{
    /// <summary>
    /// Sends a dismantling operation packet from client to server.
    /// Packet format: common header (23) + destroyTiles(1) + destroyWalls(1) +
    ///   destroyContainers(1) = 25 bytes total.
    /// SuppressDrops and BypassPickaxePower are server-side config values —
    /// the server reads them from WandServerConfig directly.
    /// </summary>
    public static void SendDismantlingOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        bool destroyTiles, bool destroyWalls, bool destroyContainers,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.DismantlingOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);

        // Dismantling-specific fields (3 bytes)
        packet.Write(destroyTiles);
        packet.Write(destroyWalls);
        packet.Write(destroyContainers);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming dismantling operation packet.
    /// On server: validates, executes server-side tile/wall destruction, syncs.
    /// </summary>
    internal static void HandleDismantlingOperation(BinaryReader reader, int whoAmI)
    {
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        // Dismantling-specific fields
        bool destroyTiles = reader.ReadBoolean();
        bool destroyWalls = reader.ReadBoolean();
        bool destroyContainers = reader.ReadBoolean();

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = PacketUtilities.EnforceDistanceCap(header);
            var tileSet = PacketUtilities.ComputeShapeTiles(header);

            ServerExecuteDismantling(
                tileSet.Tiles, header.PlayerWhoAmI,
                destroyTiles, destroyWalls, destroyContainers);
        }
        // Clients don't need to handle the broadcast — SendTileSquare
        // from the server already updated their tile state.
    }

    /// <summary>
    /// Server-side dismantling execution.
    /// Validates pick power, handles containers, destroys tiles/walls,
    /// and sends tile updates to all clients.
    /// </summary>
    private static void ServerExecuteDismantling(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        bool destroyTiles,
        bool destroyWalls,
        bool destroyContainers)
    {
        var player = Main.player[playerWhoAmI];
        var config = ModContent.GetInstance<WandServerConfig>();
        bool suppressDrops = config?.EffectiveSuppressDrops ?? false;
        bool bypassPickPower = config?.EffectiveBypassPickaxePower ?? false;
        bool allowDemonAltars = config?.EffectiveAllowDemonAltarDestruction ?? false;
        bool allowDelicateTiles = config?.EffectiveAllowDelicateTileDestruction ?? false;
        bool autoOpenChests = config?.EffectiveAutoOpenChestsOnDestruction ?? false;

        var tilesToProcess = tiles.ToArray();

        // Sort top-to-bottom so multi-tile objects above are destroyed first,
        // freeing the support tiles below (same logic as client-side).
        Array.Sort(tilesToProcess, (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        int destroyed = 0;
        bool wasGen = WorldGen.gen;

        // === CONTAINER DESTRUCTION PASS ===
        // Must happen BEFORE tile destruction — Chest.FindChest needs tiles intact.
        var containerTiles = new HashSet<Point>();
        int containersDestroyed = 0;

        if (destroyContainers && destroyTiles)
        {
            // Find unique container positions in the shape
            var processedChests = new HashSet<int>();
            foreach (var tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) continue;

                var t = Main.tile[tile.X, tile.Y];
                if (!t.HasTile) continue;
                if (!Main.tileContainer[t.TileType]) continue;

                int chestIdx = Chest.FindChest(tile.X, tile.Y);
                if (chestIdx < 0)
                {
                    // Try the tile above — multi-tile containers may not have
                    // their anchor at this position
                    chestIdx = Chest.FindChest(tile.X, tile.Y - 1);
                }
                if (chestIdx < 0 || processedChests.Contains(chestIdx)) continue;
                processedChests.Add(chestIdx);

                var chest = Main.chest[chestIdx];
                if (chest == null) continue;

                // Check if locked (golden, shadow, etc.) — skip unless auto-open is on.
                // IMPORTANT: Still add their tiles to containerTiles so the regular tile
                // destruction pass doesn't destroy them via WorldGen.KillTile (which would
                // bypass the lock check entirely).
                bool isLocked = Chest.IsLocked(chest.x, chest.y);
                if (isLocked && !autoOpenChests)
                {
                    var lockedTileData = Terraria.ObjectData.TileObjectData.GetTileData(
                        Main.tile[chest.x, chest.y].TileType, 0);
                    int lw = lockedTileData?.Width ?? 2;
                    int lh = lockedTileData?.Height ?? 2;
                    for (int dx = 0; dx < lw; dx++)
                        for (int dy = 0; dy < lh; dy++)
                            containerTiles.Add(new Point(chest.x + dx, chest.y + dy));
                    continue;
                }

                // If the container is locked, try to unlock it (requires key, consumes it).
                // If unlock fails (no key), protect the tiles and skip — same as SP path.
                if (isLocked)
                {
                    if (!ContainerHelper.TryUnlockChest(player, chest.x, chest.y,
                        Main.tile[chest.x, chest.y].TileType))
                    {
                        var failData = Terraria.ObjectData.TileObjectData.GetTileData(
                            Main.tile[chest.x, chest.y].TileType, 0);
                        int fw = failData?.Width ?? 2;
                        int fh = failData?.Height ?? 2;
                        for (int dx = 0; dx < fw; dx++)
                            for (int dy = 0; dy < fh; dy++)
                                containerTiles.Add(new Point(chest.x + dx, chest.y + dy));
                        continue; // Can't unlock — skip this container
                    }
                    // Unlock succeeded — sync the frame change to all clients
                    var unlockData = Terraria.ObjectData.TileObjectData.GetTileData(
                        Main.tile[chest.x, chest.y].TileType, 0);
                    int uw = unlockData?.Width ?? 2;
                    NetMessage.SendTileSquare(-1, chest.x, chest.y, uw + 1);
                }

                // Find the actual top-left of this container for tile tracking
                var tileData = Terraria.ObjectData.TileObjectData.GetTileData(
                    Main.tile[chest.x, chest.y].TileType, 0);
                int cw = tileData?.Width ?? 2;
                int ch = tileData?.Height ?? 2;
                for (int dx = 0; dx < cw; dx++)
                    for (int dy = 0; dy < ch; dy++)
                        containerTiles.Add(new Point(chest.x + dx, chest.y + dy));

                // Drop chest contents (or suppress if config says so)
                if (!suppressDrops)
                {
                    for (int slot = 0; slot < Chest.maxItems; slot++)
                    {
                        var item = chest.item[slot];
                        if (item != null && !item.IsAir)
                        {
                            Item.NewItem(
                                new Terraria.DataStructures.EntitySource_TileBreak(chest.x, chest.y),
                                chest.x * 16, chest.y * 16, 32, 32,
                                item.type, item.stack, false, item.prefix);
                            item.TurnToAir();
                        }
                    }
                }
                else
                {
                    // Clear items silently
                    for (int slot = 0; slot < Chest.maxItems; slot++)
                        chest.item[slot]?.TurnToAir();
                }

                // Destroy the chest and kill its tiles
                Chest.DestroyChest(chest.x, chest.y);
                WorldGen.KillTile(chest.x, chest.y, fail: false, effectOnly: false, noItem: true);
                NetMessage.SendTileSquare(-1, chest.x, chest.y, cw + 1);
                containersDestroyed++;
            }
        }

        // === TILE / WALL DESTRUCTION PASS ===
        foreach (var tile in tilesToProcess)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;
            if (containerTiles.Contains(tile)) continue; // Already handled

            var t = Main.tile[x, y];
            bool didAnything = false;

            if (destroyTiles && t.HasTile)
            {
                // Pick power check
                if (!bypassPickPower && !player.HasEnoughPickPowerToHurtTile(x, y))
                    continue;

                // Demon altar protection
                if (t.TileType == TileID.DemonAltar)
                {
                    if (!allowDemonAltars) continue;
                    // Check hammer power
                    int maxHammer = PacketUtilities.GetPlayerMaxHammerPower(player);
                    if (maxHammer < 80 && !bypassPickPower) continue;
                }

                // Delicate tile protection: shadow orbs, plantera bulbs, bee larvae, etc.
                if (PacketUtilities.IsDelicateTile(t.TileType) && !allowDelicateTiles)
                    continue;

                // CanKillTile check — may fail for tiles under supported objects
                // but the top-to-bottom sort should handle most cases
                if (!WorldGen.CanKillTile(x, y)) continue;

                WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: suppressDrops);
                destroyed++;
                didAnything = true;
            }

            if (destroyWalls && t.WallType > WallID.None)
            {
                WorldGen.KillWall(x, y, fail: false);
                didAnything = true;
                if (!destroyTiles) destroyed++; // Only count if not already counted
            }

            if (didAnything)
                NetMessage.SendTileSquare(-1, x, y, 1);
        }

        WorldGen.gen = wasGen;

        // Server vacuum: teleport scattered drops to the player's feet.
        if (!suppressDrops && config?.VacuumItems == true && tilesToProcess.Length > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(
                new List<Point>(tilesToProcess));
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        int total = destroyed + containersDestroyed;
        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.DismantlingOperation, total, true);
    }
}
