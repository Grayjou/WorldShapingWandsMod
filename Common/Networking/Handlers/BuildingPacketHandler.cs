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
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for building operations.
/// Covers both tile building and wall building.
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class BuildingPacketHandler
{
    /// <summary>
    /// Sends a building operation packet from client to server.
    /// Packet format: common header (23) + PlaceType(1) + SlopeType(1) +
    ///   OverwriteSlope(1) + ExhaustionMode(1) + replaceEnabled(1) +
    ///   itemType(2) + placeStyle(2) = 31 bytes total.
    /// </summary>
    public static void SendBuildingOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        PlaceType placeType, SlopeType slopeType, bool overwriteSlope,
        BlockExhaustionMode exhaustionMode, bool replaceEnabled,
        short itemType, short placeStyle,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false,
        bool paintSprayer = false, bool? actuation = null)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.BuildingOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);

        // Building-specific fields (9 bytes + 2 new = 11 bytes)
        packet.Write((byte)placeType);
        packet.Write((byte)slopeType);
        packet.Write(overwriteSlope);
        packet.Write((byte)exhaustionMode);
        packet.Write(replaceEnabled);
        packet.Write(itemType);
        packet.Write(placeStyle);
        packet.Write(paintSprayer);
        // Actuation tri-state: 0=ignore, 1=on, 2=off
        packet.Write((byte)(actuation == null ? 0 : actuation.Value ? 1 : 2));
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming building operation packet.
    /// On server: validates, executes, syncs tiles, broadcasts to other clients.
    /// On client: no-op (server already sent SendTileSquare for each affected tile).
    /// </summary>
    internal static void HandleBuildingOperation(BinaryReader reader, int whoAmI)
    {
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        // Building-specific fields
        var placeType = (PlaceType)reader.ReadByte();
        var slopeType = (SlopeType)reader.ReadByte();
        bool overwriteSlope = reader.ReadBoolean();
        var exhaustionMode = (BlockExhaustionMode)reader.ReadByte();
        bool replaceEnabled = reader.ReadBoolean();
        short itemType = reader.ReadInt16();
        short placeStyle = reader.ReadInt16();
        bool paintSprayer = reader.ReadBoolean();
        byte actuationByte = reader.ReadByte();
        bool? actuation = actuationByte == 0 ? null : actuationByte == 1 ? true : false;

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = PacketUtilities.EnforceDistanceCap(header);
            var tileSet = PacketUtilities.ComputeShapeTiles(header);

            bool isWall = (placeType == PlaceType.Wall);
            if (isWall)
            {
                ServerExecuteWallBuilding(
                    tileSet.Tiles, header.PlayerWhoAmI,
                    itemType, placeStyle, exhaustionMode,
                    replaceEnabled, paintSprayer);
            }
            else
            {
                ServerExecuteTileBuilding(
                    tileSet.Tiles, header.PlayerWhoAmI,
                    placeType, slopeType, overwriteSlope,
                    itemType, placeStyle, exhaustionMode,
                    replaceEnabled, paintSprayer, actuation);
            }

            // Broadcast to all other clients so they can resync
            // (SendTileSquare already covered authoritative tile state,
            //  but inventory changes need SyncEquipment)
        }
        // Clients don't need to handle the broadcast — SendTileSquare
        // from the server already updated their tile state.
    }

    /// <summary>
    /// Server-side tile building execution.
    /// Places tiles across the shape, consuming items from the player's
    /// server-side inventory. Sends SendTileSquare for each changed tile
    /// and SyncEquipment for consumed inventory slots.
    /// </summary>
    private static void ServerExecuteTileBuilding(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        PlaceType placeType,
        SlopeType slopeType,
        bool overwriteSlope,
        short itemType,
        short placeStyle,
        BlockExhaustionMode exhaustionMode,
        bool replaceEnabled,
        bool paintSprayer = false,
        bool? actuation = null)
    {
        var player = Main.player[playerWhoAmI];
        var config = WandConfigs.Resources;

        // Find the item in the player's server-side inventory
        Func<Item, bool> condition = i => !i.IsAir && i.type == itemType;

        // Determine if consumption is needed (infinite resource check)
        bool shouldConsume = true;
        if (config != null && config.IsInfiniteForPlaceType(placeType))
        {
            ItemTypeHelper.CountItems(player.inventory, condition, out int grandTotal);
            int threshold = config.GetThresholdForPlaceType(placeType);
            if (threshold == 0 || grandTotal >= threshold)
                shouldConsume = false;
        }

        var tilesToProcess = tiles.ToArray();

        // Cancel mode: pre-check total stock
        if (exhaustionMode == BlockExhaustionMode.Cancel)
        {
            // For tile wands, check ammo type instead
            int checkIdx = PacketUtilities.FindItemSlot(player, condition);
            if (checkIdx < 0)
            {
                WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false, "No suitable items found.");
                return;
            }

            Item checkItem = player.inventory[checkIdx];
            bool isTileWand = checkItem.tileWand >= 0;
            Func<Item, bool> stockCond = isTileWand
                ? i => !i.IsAir && i.type == checkItem.tileWand
                : condition;

            ItemTypeHelper.CountItems(player.inventory, stockCond, out int totalAvailable);
            if (!shouldConsume) { /* infinite — proceed */ }
            else if (totalAvailable < tilesToProcess.Length)
            {
                WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false,
                    $"Need {tilesToProcess.Length} items but only have {totalAvailable}.");
                return;
            }
        }

        // Sort gravity-affected blocks bottom-to-top
        if (itemType > 0)
        {
            int createTile = -1;
            // Find what createTile value the item produces
            int tempIdx = PacketUtilities.FindItemSlot(player, condition);
            if (tempIdx >= 0) createTile = player.inventory[tempIdx].createTile;

            if (createTile >= TileID.Dirt && Main.tileSand[createTile])
                Array.Sort(tilesToProcess, (a, b) => b.Y.CompareTo(a.Y));
        }

        int placed = 0;
        int replaced = 0;
        bool interrupted = false;
        var changedSlots = new HashSet<int>();
        bool wasGen = WorldGen.gen;
        var sandbox = WandConfigs.Sandbox;
        bool suppressDrops = sandbox?.EffectiveSuppressDrops ?? true;

        foreach (Point tile in tilesToProcess)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsTileProtected(x, y)) continue;

            int idx = PacketUtilities.FindItemSlot(player, condition);
            if (idx < 0)
            {
                if (exhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                continue;
            }

            Item srcItem = player.inventory[idx];
            bool isTileWand = srcItem.tileWand >= 0;
            int tileTypeToPlace = srcItem.createTile;
            if (tileTypeToPlace < 0) continue;

            var existingTile = Main.tile[x, y];

            if (existingTile.HasTile)
            {
                // Grass seed: convert substrate, don't destroy
                if (placeType == PlaceType.GrassSeed)
                {
                    WorldGen.gen = true;
                    if (WorldGen.PlaceTile(x, y, tileTypeToPlace, mute: true, forced: false,
                        plr: playerWhoAmI, style: placeStyle))
                    {
                        WandOfBuildingBase.ApplyActuation(x, y, actuation);
                        if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                        placed++;
                        if (shouldConsume) PacketUtilities.ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                    }
                    WorldGen.gen = wasGen;
                    NetMessage.SendTileSquare(-1, x, y, 1);
                    continue;
                }

                // Same tile type AND same style — only apply slope
                if (existingTile.TileType == (ushort)tileTypeToPlace
                    && ItemTypeHelper.IsSameTileStyle(existingTile, placeStyle))
                {
                    if (overwriteSlope)
                    {
                        PacketUtilities.ApplySlopeServer(x, y, slopeType);
                        replaced++;
                        NetMessage.SendTileSquare(-1, x, y, 1);
                    }
                    continue;
                }

                // Substrate variant skip (same-type guard: same TileType with different
                // style should be replaceable, e.g., Stone Platform → Solar Platform)
                if (existingTile.TileType != (ushort)tileTypeToPlace
                    && ItemTypeHelper.IsTileVariantOf(existingTile.TileType, tileTypeToPlace))
                    continue;

                if (!replaceEnabled) continue;

                if (sandbox != null && !sandbox.EffectiveBypassPickaxePower
                    && !player.HasEnoughPickPowerToHurtTile(x, y)) continue;
                if (!WorldGen.CanKillTile(x, y)) continue;

                // Replace
                WorldGen.gen = suppressDrops;
                bool didReplace = false;

                if (tileTypeToPlace != 0 && WorldGen.ReplaceTile(x, y, (ushort)tileTypeToPlace, placeStyle))
                {
                    didReplace = true;
                }
                else
                {
                    WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: suppressDrops);
                    if (!Main.tile[x, y].HasTile &&
                        WorldGen.PlaceTile(x, y, tileTypeToPlace, mute: true, forced: false,
                            plr: playerWhoAmI, style: placeStyle))
                    {
                        didReplace = true;
                    }
                }
                WorldGen.gen = wasGen;

                if (didReplace)
                {
                    if (overwriteSlope) PacketUtilities.ApplySlopeServer(x, y, slopeType);
                    WandOfBuildingBase.ApplyActuation(x, y, actuation);
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                    replaced++;
                    if (shouldConsume) PacketUtilities.ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                    NetMessage.SendTileSquare(-1, x, y, 1);
                }
            }
            else
            {
                // Empty tile — place
                WorldGen.gen = true;
                if (WorldGen.PlaceTile(x, y, tileTypeToPlace, mute: true, forced: false,
                    plr: playerWhoAmI, style: placeStyle))
                {
                    if (overwriteSlope) PacketUtilities.ApplySlopeServer(x, y, slopeType);
                    WandOfBuildingBase.ApplyActuation(x, y, actuation);
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                    placed++;
                    if (shouldConsume) PacketUtilities.ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                }
                WorldGen.gen = wasGen;
                NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }

        WorldGen.gen = wasGen;

        // Sync all modified inventory slots to all clients
        foreach (int slot in changedSlots)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerWhoAmI, slot);

        // Server vacuum: replacements (KillTile) may have spawned drops.
        if (!suppressDrops && sandbox?.VacuumItems == true && replaced > 0 && tilesToProcess.Length > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(
                new List<Point>(tilesToProcess));
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        int total = placed + replaced;
        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, total, true,
            interrupted ? "Ran out of blocks" : null);
    }

    /// <summary>
    /// Server-side wall building execution.
    /// </summary>
    private static void ServerExecuteWallBuilding(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        short itemType,
        short placeStyle,
        BlockExhaustionMode exhaustionMode,
        bool replaceEnabled,
        bool paintSprayer = false)
    {
        var player = Main.player[playerWhoAmI];
        var config = WandConfigs.Resources;

        Func<Item, bool> condition = i => !i.IsAir && i.type == itemType;

        bool shouldConsume = true;
        if (config != null && config.IsInfiniteForPlaceType(PlaceType.Wall))
        {
            ItemTypeHelper.CountItems(player.inventory, condition, out int grandTotal);
            int threshold = config.GetThresholdForPlaceType(PlaceType.Wall);
            if (threshold == 0 || grandTotal >= threshold)
                shouldConsume = false;
        }

        var tilesToProcess = tiles.ToArray();

        if (exhaustionMode == BlockExhaustionMode.Cancel)
        {
            ItemTypeHelper.CountItems(player.inventory, condition, out int totalAvailable);
            if (shouldConsume && totalAvailable < tilesToProcess.Length)
            {
                WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false,
                    $"Need {tilesToProcess.Length} wall items but only have {totalAvailable}.");
                return;
            }
        }

        // Resolve the wall type from the item
        int wallTypeToPlace = -1;
        int tempIdx = PacketUtilities.FindItemSlot(player, condition);
        if (tempIdx >= 0) wallTypeToPlace = player.inventory[tempIdx].createWall;
        if (wallTypeToPlace < 0)
        {
            WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false, "No wall item found.");
            return;
        }

        int placed = 0;
        int replaced = 0;
        bool interrupted = false;
        var changedSlots = new HashSet<int>();
        bool wasGen = WorldGen.gen;
        var sandbox = WandConfigs.Sandbox;
        bool suppressDrops = sandbox?.EffectiveSuppressDrops ?? true;

        foreach (Point tile in tilesToProcess)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsWallProtected(x, y)) continue;

            int idx = PacketUtilities.FindItemSlot(player, condition);
            if (idx < 0)
            {
                if (exhaustionMode == BlockExhaustionMode.Interrupt) { interrupted = true; break; }
                continue;
            }
            Item srcItem = player.inventory[idx];

            var t = Main.tile[x, y];

            if (t.WallType != WallID.None)
            {
                if (!replaceEnabled) continue;
                if (t.WallType == (ushort)wallTypeToPlace) continue;

                WorldGen.gen = suppressDrops;
                WorldGen.KillWall(x, y, fail: false);
                if (t.WallType == WallID.None)
                {
                    WorldGen.PlaceWall(x, y, wallTypeToPlace, mute: true);
                    if (t.WallType == (ushort)wallTypeToPlace)
                    {
                        if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerWall(player, x, y, shouldConsume, changedSlots);
                        replaced++;
                        if (shouldConsume) PacketUtilities.ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                    }
                }
                WorldGen.gen = wasGen;
                NetMessage.SendTileSquare(-1, x, y, 1);
            }
            else
            {
                WorldGen.gen = true;
                WorldGen.PlaceWall(x, y, wallTypeToPlace, mute: true);
                if (t.WallType == (ushort)wallTypeToPlace)
                {
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerWall(player, x, y, shouldConsume, changedSlots);
                    placed++;
                    if (shouldConsume) PacketUtilities.ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                }
                WorldGen.gen = wasGen;
                NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }

        WorldGen.gen = wasGen;

        // Sync modified inventory slots
        foreach (int slot in changedSlots)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, playerWhoAmI, slot);

        // Server vacuum: wall replacements (KillWall) may have spawned drops.
        if (!suppressDrops && sandbox?.VacuumItems == true && replaced > 0 && tilesToProcess.Length > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(
                new List<Point>(tilesToProcess));
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        int total = placed + replaced;
        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, total, true,
            interrupted ? "Ran out of wall items" : null);
    }
}
