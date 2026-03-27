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

namespace WorldShapingWandsMod.Common.Networking.Handlers;

/// <summary>
/// Handles multiplayer packet sending and receiving for Void Everything operations.
/// Carefree Mode only — clears tiles, walls, wires, and actuators (NOT liquids).
/// Extracted from the monolithic WandPacketHandler for maintainability.
/// </summary>
public static class VoidEverythingPacketHandler
{
    /// <summary>
    /// Sends a Void Everything operation packet from client to server.
    /// No operation-specific fields — only the common header is needed.
    /// The server will clear tiles, walls, wires, and actuators (NOT liquids).
    /// </summary>
    public static void SendVoidEverythingOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.VoidEverythingOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WandPacketHeaderIO.WriteCommonHeader(packet, header);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming Void Everything operation packet.
    /// On server: validates Carefree Mode, executes server-side void, syncs.
    /// </summary>
    internal static void HandleVoidEverythingOperation(BinaryReader reader, int whoAmI)
    {
        var header = WandPacketHeaderIO.ReadCommonHeader(reader);

        if (!PacketUtilities.ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            // Validate Carefree Mode is enabled on the server
            var config = ModContent.GetInstance<WandServerConfig>();
            if (config == null || !config.EnableCarefreeMode)
            {
                WandPacketHandler.SendOperationResult(header.PlayerWhoAmI, WandPacketType.VoidEverythingOperation, 0, false,
                    "Void Everything requires Carefree Mode.");
                return;
            }

            header = PacketUtilities.EnforceDistanceCap(header);
            var tileSet = PacketUtilities.ComputeShapeTiles(header);
            ServerExecuteVoidEverything(tileSet.Tiles, header.PlayerWhoAmI);
        }
    }

    /// <summary>
    /// Server-side Void Everything execution.
    /// Clears tiles, walls, wires, and actuators (NOT liquids) for all tiles
    /// in the shape. Respects safekeeping protection.
    /// </summary>
    private static void ServerExecuteVoidEverything(
        IEnumerable<Point> tiles, int playerWhoAmI)
    {
        var tilesToProcess = tiles.ToArray();

        // Sort top-to-bottom so multi-tile objects collapse correctly
        Array.Sort(tilesToProcess, (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        // Suppress all drops and effects
        bool wasGen = WorldGen.gen;
        WorldGen.gen = true;

        int voided = 0;
        int skippedProtected = 0;

        try
        {
            foreach (var tile in tilesToProcess)
            {
                if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
                if (SafekeepingSystem.IsProtected(tile.X, tile.Y))
                {
                    skippedProtected++;
                    continue;
                }

                var tileData = Main.tile[tile.X, tile.Y];

                bool hasTile = tileData.HasTile;
                bool hasWall = tileData.WallType > WallID.None;
                bool hasWire = tileData.RedWire || tileData.BlueWire
                            || tileData.GreenWire || tileData.YellowWire;
                bool hasActuator = tileData.HasActuator;

                // Note: Liquids are NOT cleared in MP
                if (!hasTile && !hasWall && !hasWire && !hasActuator)
                    continue;

                // === DESTROY TILE ===
                if (hasTile)
                    WorldGen.KillTile(tile.X, tile.Y, noItem: true);

                // === DESTROY WALL ===
                if (hasWall)
                    WorldGen.KillWall(tile.X, tile.Y);

                // === CLEAR WIRES ===
                if (hasWire)
                {
                    tileData.RedWire = false;
                    tileData.BlueWire = false;
                    tileData.GreenWire = false;
                    tileData.YellowWire = false;
                }

                // === CLEAR ACTUATOR ===
                if (hasActuator)
                {
                    tileData.HasActuator = false;
                    tileData.IsActuated = false;
                }

                NetMessage.SendTileSquare(-1, tile.X, tile.Y, 1);
                voided++;
            }
        }
        finally
        {
            WorldGen.gen = wasGen;
        }

        WandPacketHandler.SendOperationResult(playerWhoAmI, WandPacketType.VoidEverythingOperation, voided, true);
    }
}
