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
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for replacement operations.
/// Covers both tile replacement and wall replacement.
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class ReplacementPacketHandler
{
    /// <summary>
    /// Sends a replacement operation packet from client to server.
    /// Packet format: common header (23) + sourceObjectType(1) + targetObjectType(1) +
    ///   sourceTileOrWallType(2) + targetTileOrWallType(2) + targetItemType(2) +
    ///   isWallMode(1) = 31 bytes total.
    ///
    /// The client resolves source/target types from inventory and sends them directly
    /// so the server doesn't need to replicate inventory scanning logic.
    /// </summary>
    public static void SendReplacementOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        ObjectType sourceObjectType, ObjectType targetObjectType,
        ushort sourceTileOrWallType, ushort targetTileOrWallType,
        short targetItemType, bool isWallMode,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false, bool paintSprayer = false)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.ReplacementOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);

        // Replacement-specific fields (9 bytes + 1 for paintSprayer)
        packet.Write((byte)sourceObjectType);
        packet.Write((byte)targetObjectType);
        packet.Write(sourceTileOrWallType);
        packet.Write(targetTileOrWallType);
        packet.Write(targetItemType);
        packet.Write(isWallMode);
        packet.Write(paintSprayer);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming replacement operation packet.
    /// On server: validates, executes server-side tile/wall replacement, syncs.
    /// </summary>
    internal static void HandleReplacementOperation(BinaryReader reader, int whoAmI)
    {
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        // Replacement-specific fields
        var sourceObjectType = (ObjectType)reader.ReadByte();
        var targetObjectType = (ObjectType)reader.ReadByte();
        ushort sourceTileOrWallType = reader.ReadUInt16();
        ushort targetTileOrWallType = reader.ReadUInt16();
        short targetItemType = reader.ReadInt16();
        bool isWallMode = reader.ReadBoolean();
        bool paintSprayer = reader.ReadBoolean();

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = PacketUtilities.EnforceDistanceCap(header);
            var tileSet = PacketUtilities.ComputeShapeTiles(header);

            if (isWallMode)
            {
                ServerExecuteWallReplacement(
                    tileSet.Tiles, header.PlayerWhoAmI,
                    sourceTileOrWallType, targetTileOrWallType,
                    targetItemType, targetObjectType == ObjectType.Air,
                    paintSprayer);
            }
            else
            {
                ServerExecuteTileReplacement(
                    tileSet.Tiles, header.PlayerWhoAmI,
                    sourceObjectType, sourceTileOrWallType,
                    targetTileOrWallType, targetItemType,
                    targetObjectType == ObjectType.Air,
                    paintSprayer);
            }
        }
    }

    /// <summary>
    /// Server-side tile replacement execution.
    /// Replaces tiles matching sourceTileType with targetTileType,
    /// consuming target items from the player's server-side inventory.
    /// Uses ReplaceTile first, falls back to KillTile+PlaceTile.
    /// </summary>
    private static void ServerExecuteTileReplacement(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        ObjectType sourceObjectType,
        ushort sourceTileType,
        ushort targetTileType,
        short targetItemType,
        bool eraseMode,
        bool paintSprayer = false)
    {
        var player = Main.player[playerWhoAmI];
        var config = ModContent.GetInstance<WandServerConfig>();
        bool suppressDrops = config?.EffectiveSuppressDrops ?? true;
        bool bypassPickPower = config?.EffectiveBypassPickaxePower ?? false;

        // Target item condition for consumption
        Func<Item, bool> targetCondition = i => !i.IsAir && i.type == targetItemType;

        // Determine if consumption is needed
        bool shouldConsume = true;
        if (!eraseMode && config != null && config.IsInfiniteForObjectType(sourceObjectType))
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int grandTotal);
            int threshold = config.GetThresholdForObjectType(sourceObjectType);
            if (threshold == 0 || grandTotal >= threshold)
                shouldConsume = false;
        }

        int replaced = 0;
        var changedSlots = new HashSet<int>();
        bool wasGen = WorldGen.gen;
        var affectedPositions = new List<Point>();

        foreach (Point tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            var t = Main.tile[x, y];
            if (!t.HasTile) continue;

            // Match source tile using variant logic (e.g., grass matches dirt)
            if (!ItemTypeHelper.IsTileVariantOf(t.TileType, sourceTileType))
                continue;

            // Pick power check
            if (!bypassPickPower && !player.HasEnoughPickPowerToHurtTile(x, y))
                continue;

            // Check item availability (when consuming)
            if (!eraseMode && shouldConsume)
            {
                if (PacketUtilities.FindItemSlot(player, targetCondition) < 0) break; // No more items
            }

            // Preserve slope/half-block
            var oldSlope = t.Slope;
            bool oldHalf = t.IsHalfBlock;

            bool didReplace = false;

            if (!eraseMode)
            {
                // Try ReplaceTile first (handles tiles under multi-tile objects)
                // TileID.Dirt == 0: WorldGen.ReplaceTile treats type 0 as "no tile"
                if (targetTileType != 0 && WorldGen.ReplaceTile(x, y, targetTileType, 0))
                {
                    didReplace = true;
                }
                else if (WorldGen.CanKillTile(x, y))
                {
                    // Fallback: KillTile + PlaceTile
                    WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: suppressDrops);
                    if (!Main.tile[x, y].HasTile)
                    {
                        WorldGen.PlaceTile(x, y, targetTileType, mute: true, forced: false, plr: playerWhoAmI);
                        didReplace = Main.tile[x, y].HasTile;
                    }
                }
            }
            else
            {
                // Erase to Air
                if (WorldGen.CanKillTile(x, y))
                {
                    WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: suppressDrops);
                    didReplace = !Main.tile[x, y].HasTile;
                }
            }

            if (didReplace)
            {
                // Restore slope/half-block
                var placed = Main.tile[x, y];
                if (placed.HasTile)
                {
                    placed.Slope = oldSlope;
                    placed.IsHalfBlock = oldHalf;
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                }

                replaced++;
                affectedPositions.Add(tile);

                // Consume one target item
                if (!eraseMode && shouldConsume)
                {
                    int consumeIdx = PacketUtilities.FindItemSlot(player, targetCondition);
                    if (consumeIdx >= 0)
                        PacketUtilities.ConsumeOneServerItem(player, player.inventory[consumeIdx],
                            targetCondition, changedSlots);
                }

                NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }

        WorldGen.gen = wasGen;

        // Sync modified inventory slots
        foreach (int slot in changedSlots)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerWhoAmI, slot);

        // Server vacuum: tile replacements (KillTile fallback) may have spawned drops.
        if (!suppressDrops && config?.VacuumItems == true && affectedPositions.Count > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(affectedPositions);
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.ReplacementOperation, replaced, true);
    }

    /// <summary>
    /// Server-side wall replacement execution.
    /// Replaces walls matching sourceWallType with targetWallType,
    /// consuming target items from the player's server-side inventory.
    /// Handles hanging objects that depend on the wall for support.
    /// </summary>
    private static void ServerExecuteWallReplacement(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        ushort sourceWallType,
        ushort targetWallType,
        short targetItemType,
        bool eraseMode,
        bool paintSprayer = false)
    {
        var player = Main.player[playerWhoAmI];
        var config = ModContent.GetInstance<WandServerConfig>();
        bool suppressDrops = config?.EffectiveSuppressDrops ?? true;

        // Target item condition for consumption
        Func<Item, bool> targetCondition = i => !i.IsAir && i.type == targetItemType;

        // Determine if consumption is needed
        bool shouldConsume = true;
        if (!eraseMode && config != null && config.IsInfiniteForObjectType(ObjectType.Wall))
        {
            ItemTypeHelper.CountItems(player.inventory, targetCondition, out int grandTotal);
            int threshold = config.GetThresholdForObjectType(ObjectType.Wall);
            if (threshold == 0 || grandTotal >= threshold)
                shouldConsume = false;
        }

        int replaced = 0;
        var changedSlots = new HashSet<int>();
        var affectedPositions = new List<Point>();

        foreach (Point tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            var t = Main.tile[x, y];
            if (t.WallType != sourceWallType) continue;

            // Check item availability (when consuming, non-erase)
            if (!eraseMode && shouldConsume)
            {
                int idx = PacketUtilities.FindItemSlot(player, targetCondition);
                if (idx < 0) break; // No more items
            }

            // Destroy hanging objects (torches, banners) that depend on this wall
            bool hasHanging = TileHelper.WouldTileLoseSupport(x, y);
            if (hasHanging && t.HasTile)
            {
                WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: suppressDrops);
            }

            if (eraseMode)
            {
                WorldGen.KillWall(x, y, fail: false);
                if (t.WallType == WallID.None)
                {
                    replaced++;
                    affectedPositions.Add(tile);
                }
            }
            else
            {
                WorldGen.KillWall(x, y, fail: false);
                if (t.WallType == WallID.None)
                {
                    WorldGen.PlaceWall(x, y, targetWallType, mute: true);
                    if (t.WallType == targetWallType)
                    {
                        if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerWall(player, x, y, shouldConsume, changedSlots);
                        replaced++;
                        affectedPositions.Add(tile);

                        // Consume one target item
                        if (shouldConsume)
                        {
                            int consumeIdx = PacketUtilities.FindItemSlot(player, targetCondition);
                            if (consumeIdx >= 0)
                                PacketUtilities.ConsumeOneServerItem(player, player.inventory[consumeIdx],
                                    targetCondition, changedSlots);
                        }
                    }
                }
            }

            // Frame the wall and sync
            Framing.WallFrame(x, y);
            NetMessage.SendTileSquare(-1, x, y, 1);
        }

        // Sync modified inventory slots
        foreach (int slot in changedSlots)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerWhoAmI, slot);

        // Server vacuum: wall replacements (KillWall, KillTile for hanging objects) may spawn drops.
        if (!suppressDrops && config?.VacuumItems == true && affectedPositions.Count > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(affectedPositions);
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.ReplacementOperation, replaced, true);
    }
}
