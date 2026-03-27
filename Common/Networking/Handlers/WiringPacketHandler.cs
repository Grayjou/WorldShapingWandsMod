using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for wiring operations.
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class WiringPacketHandler
{
    /// <summary>
    /// Sends a wiring operation packet from client to server.
    /// The server will validate distance, compute the shape tiles, execute
    /// the wiring operation, and broadcast the result to all clients.
    /// </summary>
    public static void SendWiringOperation(
        Point start, Point end,
        WiringMode mode, ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        byte wireFlags, bool verticalFirst,
        int playerWhoAmI,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.WiringOperation);

        // Common header (23 bytes)
        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);

        // Wiring-specific fields (2 bytes)
        packet.Write(wireFlags);
        packet.Write((byte)mode);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming wiring operation packet.
    /// On server: validates, executes, and broadcasts to all other clients.
    /// On client: executes locally (received as broadcast from server).
    /// </summary>
    internal static void HandleWiringOperation(BinaryReader reader, int whoAmI)
    {
        // Read common header (23 bytes)
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        // Read wiring-specific fields (2 bytes)
        byte wireFlags = reader.ReadByte();
        var mode = (WiringMode)reader.ReadByte();

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        // Server-side distance enforcement
        if (Main.netMode == NetmodeID.Server)
            header = PacketUtilities.EnforceDistanceCap(header);

        // Compute shape tiles using WSW's shape registry
        var tileSet = PacketUtilities.ComputeShapeTiles(header);
        var (red, green, blue, yellow, actuator) = WandOfWiringSettings.UnpackWireFlags(wireFlags);

        var player = Main.player[header.PlayerWhoAmI];

        if (Main.netMode == NetmodeID.Server)
        {
            // Server executes with full validation
            ServerExecuteWiring(tileSet.Tiles, mode, red, green, blue, yellow, actuator, player);

            // Broadcast to all other clients using the same packet format
            ModPacket broadcast = WorldShapingWandsMod.Instance.GetPacket();
            broadcast.Write((byte)WandPacketType.WiringOperation);
            WandPacketHeaderIO.WriteCommonHeader(broadcast, header);
            broadcast.Write(wireFlags);
            broadcast.Write((byte)mode);
            broadcast.Send(-1, whoAmI); // Send to all except the sender
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // Client received broadcast — apply tile changes locally
            ClientApplyWiring(tileSet.Tiles, mode, red, green, blue, yellow, actuator);
        }
    }

    /// <summary>
    /// Server-side wiring execution. Uses TileManipulation messages for each wire
    /// change so all clients get authoritative state. Does NOT consume items on server
    /// (inventory lives on the client; the client pre-validates before sending).
    /// </summary>
    private static void ServerExecuteWiring(
        IEnumerable<Point> tiles,
        WiringMode mode,
        bool red, bool green, bool blue, bool yellow, bool actuator,
        Player player)
    {
        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            Tile t = Main.tile[x, y];

            if (mode == WiringMode.Place)
            {
                if (red && !t.RedWire) WorldGen.PlaceWire(x, y);
                if (green && !t.GreenWire) WorldGen.PlaceWire3(x, y);
                if (blue && !t.BlueWire) WorldGen.PlaceWire2(x, y);
                if (yellow && !t.YellowWire) WorldGen.PlaceWire4(x, y);
                if (actuator && !t.HasActuator) WorldGen.PlaceActuator(x, y);
            }
            else
            {
                if (red && t.RedWire) WorldGen.KillWire(x, y);
                if (green && t.GreenWire) WorldGen.KillWire3(x, y);
                if (blue && t.BlueWire) WorldGen.KillWire2(x, y);
                if (yellow && t.YellowWire) WorldGen.KillWire4(x, y);
                if (actuator && t.HasActuator) WorldGen.KillActuator(x, y);
            }

            // Send authoritative tile state to all clients
            NetMessage.SendTileSquare(-1, x, y, 1);
        }
    }

    /// <summary>
    /// Client-side application of a broadcast wiring operation.
    /// Applies wire changes locally without sending any network messages
    /// (the server already broadcast the authoritative state via SendTileSquare).
    /// </summary>
    private static void ClientApplyWiring(
        IEnumerable<Point> tiles,
        WiringMode mode,
        bool red, bool green, bool blue, bool yellow, bool actuator)
    {
        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;

            Tile t = Main.tile[x, y];

            if (mode == WiringMode.Place)
            {
                if (red && !t.RedWire) WorldGen.PlaceWire(x, y);
                if (green && !t.GreenWire) WorldGen.PlaceWire3(x, y);
                if (blue && !t.BlueWire) WorldGen.PlaceWire2(x, y);
                if (yellow && !t.YellowWire) WorldGen.PlaceWire4(x, y);
                if (actuator && !t.HasActuator) WorldGen.PlaceActuator(x, y);
            }
            else
            {
                if (red && t.RedWire) WorldGen.KillWire(x, y);
                if (green && t.GreenWire) WorldGen.KillWire3(x, y);
                if (blue && t.BlueWire) WorldGen.KillWire2(x, y);
                if (yellow && t.YellowWire) WorldGen.KillWire4(x, y);
                if (actuator && t.HasActuator) WorldGen.KillActuator(x, y);
            }
        }
    }
}
