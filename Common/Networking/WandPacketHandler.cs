using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Networking;

/// <summary>
/// Packet types for all wand operations that need server-side execution.
/// Currently only wiring uses packets — other wands (Building, Dismantling,
/// Replacement) already use per-tile TileManipulation messages or WorldGen.gen
/// batching which syncs natively.
/// </summary>
public enum WandPacketType : byte
{
    /// <summary>
    /// Wiring operation: place or remove wires/actuators across a shape.
    /// Sent client→server, server executes + broadcasts to all clients.
    /// </summary>
    WiringOperation = 1,
}

/// <summary>
/// Handles multiplayer packet sending and receiving for wand operations.
/// Ported from MagicWiring's WiringPacketHandler with WSW adaptations:
/// - Uses WSW's ShapeRegistry for tile computation
/// - Respects WSW's SafekeepingSystem protections
/// - Supports WSW's full shape set (not just MW's limited shapes)
/// - Respects InfiniteResource config
/// </summary>
public static class WandPacketHandler
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
        SliceMode slice = SliceMode.Full, bool connectDiameter = true)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.WiringOperation);
        packet.Write(start.X);
        packet.Write(start.Y);
        packet.Write(end.X);
        packet.Write(end.Y);
        packet.Write((byte)mode);
        packet.Write((byte)shape);
        packet.Write((byte)fillMode);
        packet.Write((byte)thickness);
        packet.Write(equalDimensions);
        packet.Write(wireFlags);
        packet.Write(verticalFirst);
        packet.Write((byte)playerWhoAmI);
        packet.Write((byte)slice);
        packet.Write(connectDiameter);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming wiring operation packet.
    /// On server: validates, executes, and broadcasts to all other clients.
    /// On client: executes locally (received as broadcast from server).
    /// </summary>
    public static void HandleWiringOperation(BinaryReader reader, int whoAmI)
    {
        int startX = reader.ReadInt32();
        int startY = reader.ReadInt32();
        int endX = reader.ReadInt32();
        int endY = reader.ReadInt32();
        var mode = (WiringMode)reader.ReadByte();
        var shape = (ShapeType)reader.ReadByte();
        var fillMode = (ShapeMode)reader.ReadByte();
        int thickness = reader.ReadByte();
        bool equalDimensions = reader.ReadBoolean();
        byte wireFlags = reader.ReadByte();
        bool verticalFirst = reader.ReadBoolean();
        int playerWhoAmI = reader.ReadByte();

        // Read slice and connectDiameter (added in shape slicing update)
        var slice = SliceMode.Full;
        bool connectDiameter = true;
        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            slice = (SliceMode)reader.ReadByte();
            connectDiameter = reader.ReadBoolean();
        }

        // Validate player
        if (playerWhoAmI < 0 || playerWhoAmI >= Main.maxPlayers || !Main.player[playerWhoAmI].active)
            return;

        // Server-side distance enforcement
        if (Main.netMode == NetmodeID.Server)
        {
            var config = ModContent.GetInstance<WandConfig>();
            int cap;
            if (shape == ShapeType.Elbow || shape == ShapeType.CardinalLine || shape == ShapeType.StraightLine)
                cap = config?.SmallSelectionCap ?? 1000;
            else if (fillMode == ShapeMode.Hollow)
                cap = config?.HollowSelectionCap ?? 400;
            else
                cap = config?.BigSelectionCap ?? 200;

            int dx = endX - startX;
            int dy = endY - startY;
            int maxOffset = cap - 1;
            if (maxOffset < 0) maxOffset = 0;
            dx = System.Math.Clamp(dx, -maxOffset, maxOffset);
            dy = System.Math.Clamp(dy, -maxOffset, maxOffset);
            endX = startX + dx;
            endY = startY + dy;
        }

        // Compute shape tiles using WSW's shape registry
        var start = new Point(startX, startY);
        var end = new Point(endX, endY);

        var context = new ShapeContext(
            start, end,
            fillMode, thickness,
            HorizontalBias.None, VerticalBias.None,
            verticalFirst, equalDimensions,
            slice, connectDiameter
        );

        var tileSet = ShapeRegistry.GetShapeTiles(shape, context);
        var (red, green, blue, yellow, actuator) = WandOfWiringSettings.UnpackWireFlags(wireFlags);

        var player = Main.player[playerWhoAmI];

        // Execute the wiring operation
        // On server: use the requesting player for inventory consumption
        // On client (broadcast): apply tile changes locally without consuming items
        // (the originating client already consumed items before sending)
        if (Main.netMode == NetmodeID.Server)
        {
            // Server executes with full validation
            ServerExecuteWiring(tileSet.Tiles, mode, red, green, blue, yellow, actuator, player);

            // Broadcast to all other clients
            ModPacket broadcast = WorldShapingWandsMod.Instance.GetPacket();
            broadcast.Write((byte)WandPacketType.WiringOperation);
            broadcast.Write(startX);
            broadcast.Write(startY);
            broadcast.Write(endX);
            broadcast.Write(endY);
            broadcast.Write((byte)mode);
            broadcast.Write((byte)shape);
            broadcast.Write((byte)fillMode);
            broadcast.Write((byte)thickness);
            broadcast.Write(equalDimensions);
            broadcast.Write(wireFlags);
            broadcast.Write(verticalFirst);
            broadcast.Write((byte)playerWhoAmI);
            broadcast.Send(-1, whoAmI); // Send to all except the sender
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // Client received broadcast — apply tile changes locally
            // Don't consume items (the originating client already did)
            // Don't send NetMessages (server already did the authoritative changes)
            ClientApplyWiring(tileSet.Tiles, mode, red, green, blue, yellow, actuator);
        }
    }

    /// <summary>
    /// Server-side wiring execution. Uses TileManipulation messages for each wire
    /// change so all clients get authoritative state. Does NOT consume items on server
    /// (inventory lives on the client; the client pre-validates before sending).
    /// </summary>
    private static void ServerExecuteWiring(
        System.Collections.Generic.IEnumerable<Point> tiles,
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
        System.Collections.Generic.IEnumerable<Point> tiles,
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

    /// <summary>
    /// Main packet dispatch. Called from Mod.HandlePacket().
    /// </summary>
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var packetType = (WandPacketType)reader.ReadByte();

        switch (packetType)
        {
            case WandPacketType.WiringOperation:
                HandleWiringOperation(reader, whoAmI);
                break;
            default:
                // Unknown packet type — skip silently
                break;
        }
    }
}
