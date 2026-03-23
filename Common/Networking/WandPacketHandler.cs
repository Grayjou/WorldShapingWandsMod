using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
 using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Common.Networking;

/// <summary>
/// Packet types for all wand operations that need server-side execution.
/// Each wand family sends its own packet type so the server can dispatch
/// to the correct handler and apply operation-specific validation.
/// </summary>
public enum WandPacketType : byte
{
    /// <summary>
    /// Wiring operation: place or remove wires/actuators across a shape.
    /// Sent client→server, server executes + broadcasts to all clients.
    /// </summary>
    WiringOperation = 1,

    /// <summary>Building operation: place tiles/walls across a shape. Day 3.</summary>
    BuildingOperation = 2,

    /// <summary>Dismantling operation: remove tiles/walls across a shape. Day 4.</summary>
    DismantlingOperation = 3,

    /// <summary>Replacement operation: swap tiles/walls across a shape. Day 5.</summary>
    ReplacementOperation = 4,

    /// <summary>Safekeeping operation: protect/unprotect tiles across a shape.</summary>
    SafekeepingOperation = 5,

    /// <summary>Bulk sync of protected tile positions (server→client on join).</summary>
    ProtectionBulkSync = 6,

    /// <summary>Coating operation: apply/remove coatings across a shape. Day 5.</summary>
    CoatingOperation = 7,

    /// <summary>
    /// Server→Client feedback packet reporting operation outcome.
    /// Sent after any operation completes (or fails) on the server.
    /// </summary>
    OperationResult = 10,
}

/// <summary>
/// Common header shared by all wand operation packets.
/// Contains shape parameters and player identity — everything needed
/// to recompute the shape on the server. Wire-on-the-protocol: 23 bytes.
/// </summary>
public readonly struct WandPacketHeader
{
    public readonly Point Start;
    public readonly Point End;
    public readonly ShapeType Shape;
    public readonly ShapeMode FillMode;
    public readonly int Thickness;
    public readonly bool EqualDimensions;
    public readonly bool VerticalFirst;
    public readonly int PlayerWhoAmI;
    public readonly SliceMode Slice;
    public readonly bool ConnectDiameter;
    public readonly bool InvertSelection;

    public WandPacketHeader(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false)
    {
        Start = start;
        End = end;
        Shape = shape;
        FillMode = fillMode;
        Thickness = thickness;
        EqualDimensions = equalDimensions;
        VerticalFirst = verticalFirst;
        PlayerWhoAmI = playerWhoAmI;
        Slice = slice;
        ConnectDiameter = connectDiameter;
        InvertSelection = invertSelection;
    }
}

/// <summary>
/// Handles multiplayer packet sending and receiving for wand operations.
/// Ported from MagicWiring's WiringPacketHandler with WSW adaptations:
/// - Uses WSW's ShapeRegistry for tile computation
/// - Respects WSW's SafekeepingSystem protections
/// - Supports WSW's full shape set (not just MW's limited shapes)
/// - Respects InfiniteResource config
///
/// Day 2 refactor: common header struct, reusable helpers, OperationResult packet.
/// </summary>
public static class WandPacketHandler
{
    // ════════════════════════════════════════════════════════════════════
    // Server-Side Rate Limiting
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks the last game tick at which each player executed a wand operation.
    /// Keyed by player whoAmI. Used to enforce OperationCooldownTicks on the server.
    /// </summary>
    private static readonly Dictionary<int, ulong> _lastOperationTick = new();

    /// <summary>
    /// Checks whether the player is still on cooldown from a previous operation.
    /// If on cooldown, sends an OperationResult error and returns true (blocked).
    /// If allowed, updates the last operation tick and returns false.
    /// </summary>
    /// <param name="whoAmI">Player index on the server.</param>
    /// <param name="packetType">The packet type being handled (for error reporting).</param>
    /// <returns>True if the operation should be BLOCKED, false if it may proceed.</returns>
    private static bool IsOnCooldown(int whoAmI, WandPacketType packetType)
    {
        var config = ModContent.GetInstance<WandServerConfig>();
        int cooldown = config?.OperationCooldownTicks ?? 12;
        if (cooldown <= 0) return false; // Cooldown disabled

        ulong now = Main.GameUpdateCount;
        if (_lastOperationTick.TryGetValue(whoAmI, out ulong last))
        {
            if (now - last < (ulong)cooldown)
            {
                // Still on cooldown — silently reject. No error spam: the server
                // simply drops the packet. Autoclickers will fire faster than the
                // cooldown, but only one operation per window actually executes.
                return true;
            }
        }

        _lastOperationTick[whoAmI] = now;
        return false;
    }

    /// <summary>
    /// Client-side cooldown check for single-player. Returns true if on cooldown.
    /// Uses the same OperationCooldownTicks config as the server.
    /// </summary>
    private static ulong _spLastOperationTick;

    /// <summary>
    /// Returns true if the local player is on cooldown (single-player only).
    /// Call this from wand execution paths (UseItem / HoldItem) to prevent
    /// autoclicker or click-spam abuse in SP.
    /// </summary>
    public static bool IsLocalPlayerOnCooldown()
    {
        var config = ModContent.GetInstance<WandServerConfig>();
        int cooldown = config?.OperationCooldownTicks ?? 12;
        if (cooldown <= 0) return false;

        ulong now = Main.GameUpdateCount;
        if (now - _spLastOperationTick < (ulong)cooldown)
            return true;

        _spLastOperationTick = now;
        return false;
    }

    // ════════════════════════════════════════════════════════════════════
    // Common Header Helpers
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Write the common 23-byte header shared by all wand operation packets.
    /// Format: Start(8) + End(8) + Shape(1) + FillMode(1) + Thickness(1) +
    ///         EqualDimensions(1) + VerticalFirst(1) + PlayerWhoAmI(1) +
    ///         Slice(1) + ConnectDiameter(1) + InvertSelection(1) = 23 bytes.
    /// </summary>
    public static void WriteCommonHeader(ModPacket packet, WandPacketHeader header)
    {
        packet.Write(header.Start.X);
        packet.Write(header.Start.Y);
        packet.Write(header.End.X);
        packet.Write(header.End.Y);
        packet.Write((byte)header.Shape);
        packet.Write((byte)header.FillMode);
        packet.Write((byte)header.Thickness);
        packet.Write(header.EqualDimensions);
        packet.Write(header.VerticalFirst);
        packet.Write((byte)header.PlayerWhoAmI);
        packet.Write((byte)header.Slice);
        packet.Write(header.ConnectDiameter);
        packet.Write(header.InvertSelection);
    }

    /// <summary>
    /// Read the common 23-byte header from an incoming packet.
    /// </summary>
    public static WandPacketHeader ReadCommonHeader(BinaryReader reader)
    {
        return new WandPacketHeader(
            start: new Point(reader.ReadInt32(), reader.ReadInt32()),
            end: new Point(reader.ReadInt32(), reader.ReadInt32()),
            shape: (ShapeType)reader.ReadByte(),
            fillMode: (ShapeMode)reader.ReadByte(),
            thickness: reader.ReadByte(),
            equalDimensions: reader.ReadBoolean(),
            verticalFirst: reader.ReadBoolean(),
            playerWhoAmI: reader.ReadByte(),
            slice: (SliceMode)reader.ReadByte(),
            connectDiameter: reader.ReadBoolean(),
            invertSelection: reader.ReadBoolean()
        );
    }

    // ════════════════════════════════════════════════════════════════════
    // Server-Side Validation Helpers
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clamp the end point to respect server-side distance caps.
    /// Uses the same cap logic as <see cref="Players.WandPlayer.ClampEndToCaps"/>.
    /// Returns a new header with the clamped end point.
    /// </summary>
    private static WandPacketHeader EnforceDistanceCap(WandPacketHeader header)
    {
        var config = ModContent.GetInstance<WandServerConfig>();
        int cap;
        if (header.Shape == ShapeType.Elbow ||
            header.Shape == ShapeType.CardinalLine ||
            header.Shape == ShapeType.StraightLine)
            cap = config?.SmallSelectionCap ?? 1000;
        else if (header.FillMode == ShapeMode.Hollow)
            cap = config?.HollowSelectionCap ?? 400;
        else
            cap = config?.BigSelectionCap ?? 200;

        int dx = header.End.X - header.Start.X;
        int dy = header.End.Y - header.Start.Y;
        int maxOffset = Math.Max(0, cap - 1);
        dx = Math.Clamp(dx, -maxOffset, maxOffset);
        dy = Math.Clamp(dy, -maxOffset, maxOffset);

        var clampedEnd = new Point(header.Start.X + dx, header.Start.Y + dy);

        return new WandPacketHeader(
            header.Start, clampedEnd,
            header.Shape, header.FillMode,
            header.Thickness, header.EqualDimensions,
            header.VerticalFirst, header.PlayerWhoAmI,
            header.Slice, header.ConnectDiameter,
            header.InvertSelection
        );
    }

    /// <summary>
    /// Recompute the shape tiles from a packet header.
    /// Used by all server handlers to get the authoritative tile set.
    /// When InvertSelection is set, returns tiles within the bounding rect
    /// that are NOT in the original shape (negative space).
    /// </summary>
    private static ShapeTileSet ComputeShapeTiles(WandPacketHeader header)
    {
        var context = new ShapeContext(
            header.Start, header.End,
            header.FillMode, header.Thickness,
            HorizontalBias.None, VerticalBias.None,
            header.VerticalFirst, header.EqualDimensions,
            header.Slice, header.ConnectDiameter
        );
        var tileSet = ShapeRegistry.GetShapeTiles(header.Shape, context);

        if (!header.InvertSelection || !ShapeInfo.ShapeSupportsInversion(header.Shape))
            return tileSet;

        // Apply inversion: bounding rect minus original shape tiles
        var shapeInfo = new ShapeInfo(header.Shape, header.FillMode,
            header.Thickness, header.EqualDimensions,
            header.Slice, header.ConnectDiameter, header.InvertSelection);
        var invertedTiles = shapeInfo.ApplyInversion(tileSet.Tiles.ToArray(), context);
        return new ShapeTileSet(invertedTiles);
    }

    /// <summary>
    /// Validate that a player index refers to an active player.
    /// </summary>
    private static bool ValidatePlayer(int playerWhoAmI)
    {
        return playerWhoAmI >= 0 && playerWhoAmI < Main.maxPlayers
            && Main.player[playerWhoAmI].active;
    }

    // ════════════════════════════════════════════════════════════════════
    // OperationResult Packet (Server → Client)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Server → originating client: report operation outcome.
    /// </summary>
    public static void SendOperationResult(
        int toClient, WandPacketType originalType,
        int tilesAffected, bool success, string error = null)
    {
        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.OperationResult);
        packet.Write((byte)originalType);
        packet.Write(tilesAffected);
        packet.Write(success);
        packet.Write(error ?? "");
        packet.Send(toClient);
    }

    /// <summary>
    /// Client-side handler for OperationResult packets.
    /// Displays success messages with tile count and plays completion sounds,
    /// or shows error messages on failure.
    /// </summary>
    private static void HandleOperationResult(BinaryReader reader)
    {
        var originalType = (WandPacketType)reader.ReadByte();
        int tilesAffected = reader.ReadInt32();
        bool success = reader.ReadBoolean();
        string error = reader.ReadString();

        if (!success && !string.IsNullOrEmpty(error))
        {
            Main.NewText(error, WandColors.MsgError);
            return;
        }

        // Success feedback: show message and play sound.
        // In MP, the client sends a packet and returns immediately — there is
        // no local execution path, so the server must report back.
        if (success && tilesAffected > 0)
        {
            var clientCfg = ModContent.GetInstance<WandClientConfig>();
            var player = Main.LocalPlayer;

            switch (originalType)
            {
                case WandPacketType.DismantlingOperation:
                    Main.NewText($"Destroyed {tilesAffected} tile(s)", Color.OrangeRed);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.Tink, player.Center);
                    break;

                case WandPacketType.BuildingOperation:
                    Main.NewText($"Placed {tilesAffected} tile(s)", Color.Cyan);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.Item168 with { Volume = 0.5f }, player.Center);
                    break;

                case WandPacketType.ReplacementOperation:
                    Main.NewText($"Replaced {tilesAffected} tile(s)", WandColors.MsgReplacement);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.25f }, player.Center);
                    break;

                case WandPacketType.CoatingOperation:
                    Main.NewText($"Coated {tilesAffected} tile(s)", WandColors.MsgCoating);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.6f }, player.Center);
                    break;

                case WandPacketType.SafekeepingOperation:
                    Main.NewText($"Protected {tilesAffected} tile(s)", WandColors.MsgSafekeeping);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.5f }, player.Center);
                    break;

                case WandPacketType.WiringOperation:
                    Main.NewText($"Wiring: {tilesAffected} tile(s) affected", WandColors.MsgWiring);
                    if (clientCfg?.EnableWandSounds == true)
                        SoundEngine.PlaySound(SoundID.Item64, player.Center);
                    break;

                default:
                    Main.NewText($"Operation complete: {tilesAffected} tile(s)", WandColors.MsgInfo);
                    break;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Wiring Operation Packets
    // ════════════════════════════════════════════════════════════════════

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
        WriteCommonHeader(packet, header);

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
    private static void HandleWiringOperation(BinaryReader reader, int whoAmI)
    {
        // Read common header (23 bytes)
        var header = ReadCommonHeader(reader);

        // Read wiring-specific fields (2 bytes)
        byte wireFlags = reader.ReadByte();
        var mode = (WiringMode)reader.ReadByte();

        if (!ValidatePlayer(header.PlayerWhoAmI))
            return;

        // Server-side distance enforcement
        if (Main.netMode == NetmodeID.Server)
            header = EnforceDistanceCap(header);

        // Compute shape tiles using WSW's shape registry
        var tileSet = ComputeShapeTiles(header);
        var (red, green, blue, yellow, actuator) = WandOfWiringSettings.UnpackWireFlags(wireFlags);

        var player = Main.player[header.PlayerWhoAmI];

        if (Main.netMode == NetmodeID.Server)
        {
            // Server executes with full validation
            ServerExecuteWiring(tileSet.Tiles, mode, red, green, blue, yellow, actuator, player);

            // Broadcast to all other clients using the same packet format
            ModPacket broadcast = WorldShapingWandsMod.Instance.GetPacket();
            broadcast.Write((byte)WandPacketType.WiringOperation);
            WriteCommonHeader(broadcast, header);
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

    // ════════════════════════════════════════════════════════════════════
    // Building Operation Packets (Day 3)
    // ════════════════════════════════════════════════════════════════════

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
        WriteCommonHeader(packet, header);

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
    private static void HandleBuildingOperation(BinaryReader reader, int whoAmI)
    {
        var header = ReadCommonHeader(reader);

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

        if (!ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = EnforceDistanceCap(header);
            var tileSet = ComputeShapeTiles(header);

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
        var config = ModContent.GetInstance<WandServerConfig>();

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
            int checkIdx = FindItemSlot(player, condition);
            if (checkIdx < 0)
            {
                SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false, "No suitable items found.");
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
                SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false,
                    $"Need {tilesToProcess.Length} items but only have {totalAvailable}.");
                return;
            }
        }

        // Sort gravity-affected blocks bottom-to-top
        if (itemType > 0)
        {
            int createTile = -1;
            // Find what createTile value the item produces
            int tempIdx = FindItemSlot(player, condition);
            if (tempIdx >= 0) createTile = player.inventory[tempIdx].createTile;

            if (createTile >= TileID.Dirt && Main.tileSand[createTile])
                Array.Sort(tilesToProcess, (a, b) => b.Y.CompareTo(a.Y));
        }

        int placed = 0;
        int replaced = 0;
        bool interrupted = false;
        var changedSlots = new HashSet<int>();
        bool wasGen = WorldGen.gen;
        bool suppressDrops = config?.SuppressDrops ?? true;

        foreach (Point tile in tilesToProcess)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            // Find source item for this tile
            int idx = FindItemSlot(player, condition);
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
                        if (shouldConsume) ConsumeOneServerItem(player, srcItem, condition, changedSlots);
                    }
                    WorldGen.gen = wasGen;
                    NetMessage.SendTileSquare(-1, x, y, 1);
                    continue;
                }

                // Same tile type — only apply slope
                if (existingTile.TileType == (ushort)tileTypeToPlace)
                {
                    if (overwriteSlope)
                    {
                        ApplySlopeServer(x, y, slopeType);
                        replaced++;
                        NetMessage.SendTileSquare(-1, x, y, 1);
                    }
                    continue;
                }

                // Substrate variant skip
                if (ItemTypeHelper.IsTileVariantOf(existingTile.TileType, tileTypeToPlace))
                    continue;

                if (!replaceEnabled) continue;

                if (config != null && !config.BypassPickaxePower
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
                    if (overwriteSlope) ApplySlopeServer(x, y, slopeType);
                    WandOfBuildingBase.ApplyActuation(x, y, actuation);
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                    replaced++;
                    if (shouldConsume) ConsumeOneServerItem(player, srcItem, condition, changedSlots);
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
                    if (overwriteSlope) ApplySlopeServer(x, y, slopeType);
                    WandOfBuildingBase.ApplyActuation(x, y, actuation);
                    if (paintSprayer) WandOfBuildingBase.ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                    placed++;
                    if (shouldConsume) ConsumeOneServerItem(player, srcItem, condition, changedSlots);
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
        if (!suppressDrops && config?.VacuumItems == true && replaced > 0 && tilesToProcess.Length > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(
                new List<Point>(tilesToProcess));
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        int total = placed + replaced;
        SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, total, true,
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
        var config = ModContent.GetInstance<WandServerConfig>();

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
                SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false,
                    $"Need {tilesToProcess.Length} wall items but only have {totalAvailable}.");
                return;
            }
        }

        // Resolve the wall type from the item
        int wallTypeToPlace = -1;
        int tempIdx = FindItemSlot(player, condition);
        if (tempIdx >= 0) wallTypeToPlace = player.inventory[tempIdx].createWall;
        if (wallTypeToPlace < 0)
        {
            SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, 0, false, "No wall item found.");
            return;
        }

        int placed = 0;
        int replaced = 0;
        bool interrupted = false;
        var changedSlots = new HashSet<int>();
        bool wasGen = WorldGen.gen;
        bool suppressDrops = config?.SuppressDrops ?? true;

        foreach (Point tile in tilesToProcess)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            int idx = FindItemSlot(player, condition);
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
                        if (shouldConsume) ConsumeOneServerItem(player, srcItem, condition, changedSlots);
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
                    if (shouldConsume) ConsumeOneServerItem(player, srcItem, condition, changedSlots);
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
        if (!suppressDrops && config?.VacuumItems == true && replaced > 0 && tilesToProcess.Length > 0)
        {
            var bounds = BulkTileOperations.ComputeBounds(
                new List<Point>(tilesToProcess));
            BulkTileOperations.ServerVacuumItemsToPlayer(player, bounds);
        }

        int total = placed + replaced;
        SendOperationResult(playerWhoAmI, WandPacketType.BuildingOperation, total, true,
            interrupted ? "Ran out of wall items" : null);
    }

    // ════════════════════════════════════════════════════════════════════
    // Dismantling Operation Packets (Day 4)
    // ════════════════════════════════════════════════════════════════════

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
        WriteCommonHeader(packet, header);

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
    private static void HandleDismantlingOperation(BinaryReader reader, int whoAmI)
    {
        var header = ReadCommonHeader(reader);

        // Dismantling-specific fields
        bool destroyTiles = reader.ReadBoolean();
        bool destroyWalls = reader.ReadBoolean();
        bool destroyContainers = reader.ReadBoolean();

        if (!ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = EnforceDistanceCap(header);
            var tileSet = ComputeShapeTiles(header);

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
        bool suppressDrops = config?.SuppressDrops ?? false;
        bool bypassPickPower = config?.BypassPickaxePower ?? false;
        bool allowDemonAltars = config?.AllowDemonAltarDestruction ?? false;
        bool allowDelicateTiles = config?.AllowDelicateTileDestruction ?? false;
        bool autoOpenChests = config?.AutoOpenChestsOnDestruction ?? false;

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
                    int maxHammer = GetPlayerMaxHammerPower(player);
                    if (maxHammer < 80 && !bypassPickPower) continue;
                }

                // Delicate tile protection: shadow orbs, plantera bulbs, bee larvae, etc.
                if (IsDelicateTile(t.TileType) && !allowDelicateTiles)
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
        SendOperationResult(playerWhoAmI, WandPacketType.DismantlingOperation, total, true);
    }

    /// <summary>
    /// Returns the highest hammer power among all items in the player's inventory.
    /// Server-side mirror of WandOfDismantlingBase.GetPlayerMaxHammerPower.
    /// </summary>
    private static int GetPlayerMaxHammerPower(Player player)
    {
        int max = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            var item = player.inventory[i];
            if (!item.IsAir && item.hammer > max)
                max = item.hammer;
        }
        return max;
    }

    /// <summary>
    /// Returns true if the tile type is "delicate" — destroying it has irreversible
    /// side effects (boss spawns, world flags, unique loot).
    /// Protected by AllowDelicateTileDestruction config.
    /// Server-side mirror of WandOfDismantling.IsDelicateTile.
    /// </summary>
    private static bool IsDelicateTile(int tileType)
    {
        return tileType == TileID.ShadowOrbs        // Shadow Orb / Crimson Heart
            || tileType == TileID.PlanteraBulb       // Plantera's Bulb
            || tileType == TileID.Larva              // Bee Larva (Queen Bee)
            || tileType == TileID.Heart              // Life Crystal
            || tileType == TileID.LifeFruit          // Life Fruit
            || tileType == TileID.LihzahrdAltar;     // Lihzahrd Altar (Golem)
    }

    // ════════════════════════════════════════════════════════════════════
    // Replacement Operation Packets (Day 5)
    // ════════════════════════════════════════════════════════════════════

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
        WriteCommonHeader(packet, header);

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
    private static void HandleReplacementOperation(BinaryReader reader, int whoAmI)
    {
        var header = ReadCommonHeader(reader);

        // Replacement-specific fields
        var sourceObjectType = (ObjectType)reader.ReadByte();
        var targetObjectType = (ObjectType)reader.ReadByte();
        ushort sourceTileOrWallType = reader.ReadUInt16();
        ushort targetTileOrWallType = reader.ReadUInt16();
        short targetItemType = reader.ReadInt16();
        bool isWallMode = reader.ReadBoolean();
        bool paintSprayer = reader.ReadBoolean();

        if (!ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = EnforceDistanceCap(header);
            var tileSet = ComputeShapeTiles(header);

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
        bool suppressDrops = config?.SuppressDrops ?? true;
        bool bypassPickPower = config?.BypassPickaxePower ?? false;

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
                if (FindItemSlot(player, targetCondition) < 0) break; // No more items
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
                    int consumeIdx = FindItemSlot(player, targetCondition);
                    if (consumeIdx >= 0)
                        ConsumeOneServerItem(player, player.inventory[consumeIdx],
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

        SendOperationResult(playerWhoAmI, WandPacketType.ReplacementOperation, replaced, true);
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
        bool suppressDrops = config?.SuppressDrops ?? true;

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
                int idx = FindItemSlot(player, targetCondition);
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
                            int consumeIdx = FindItemSlot(player, targetCondition);
                            if (consumeIdx >= 0)
                                ConsumeOneServerItem(player, player.inventory[consumeIdx],
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

        SendOperationResult(playerWhoAmI, WandPacketType.ReplacementOperation, replaced, true);
    }

    // ════════════════════════════════════════════════════════════════════
    // Coating Operation Packets (Day 5)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends a coating operation packet from client to server.
    /// Packet format: common header (23) + CoatingMode(1) + paintColor(1) +
    ///   applyIlluminant(1) + ignoreIlluminant(1) +
    ///   applyEcho(1) + ignoreEcho(1) = 28 bytes total.
    /// </summary>
    public static void SendCoatingOperation(
        Point start, Point end,
        ShapeType shape, ShapeMode fillMode,
        int thickness, bool equalDimensions,
        bool verticalFirst, int playerWhoAmI,
        CoatingMode mode, byte paintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        SliceMode slice = SliceMode.Full, bool connectDiameter = true,
        bool invertSelection = false, bool repaint = true)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = WorldShapingWandsMod.Instance.GetPacket();
        packet.Write((byte)WandPacketType.CoatingOperation);

        var header = new WandPacketHeader(
            start, end, shape, fillMode,
            thickness, equalDimensions,
            verticalFirst, playerWhoAmI,
            slice, connectDiameter, invertSelection
        );
        WriteCommonHeader(packet, header);

        // Coating-specific fields (6 bytes)
        packet.Write((byte)mode);
        packet.Write(paintColor);
        packet.Write(applyIlluminant);
        packet.Write(ignoreIlluminant);
        packet.Write(applyEcho);
        packet.Write(ignoreEcho);
        packet.Write(repaint);
        packet.Send();
    }

    /// <summary>
    /// Handles an incoming coating operation packet.
    /// On server: validates, executes server-side coating, syncs.
    /// </summary>
    private static void HandleCoatingOperation(BinaryReader reader, int whoAmI)
    {
        var header = ReadCommonHeader(reader);

        // Coating-specific fields
        var mode = (CoatingMode)reader.ReadByte();
        byte paintColor = reader.ReadByte();
        bool applyIlluminant = reader.ReadBoolean();
        bool ignoreIlluminant = reader.ReadBoolean();
        bool applyEcho = reader.ReadBoolean();
        bool ignoreEcho = reader.ReadBoolean();
        bool repaint = reader.ReadBoolean();

        if (!ValidatePlayer(header.PlayerWhoAmI))
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            header = EnforceDistanceCap(header);
            var tileSet = ComputeShapeTiles(header);

            ServerExecuteCoating(
                tileSet.Tiles, header.PlayerWhoAmI,
                mode, paintColor,
                applyIlluminant, ignoreIlluminant,
                applyEcho, ignoreEcho,
                repaint);
        }
    }

    /// <summary>
    /// Server-side coating execution.
    /// Applies paint/coating/scrape operations to tiles or walls in the shape,
    /// using the same logic as client-side WandOfCoatingBase.ApplyCoating.
    /// </summary>
    private static void ServerExecuteCoating(
        IEnumerable<Point> tiles,
        int playerWhoAmI,
        CoatingMode mode,
        byte paintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint)
    {
        /// <summary>IgnorePaintColor sentinel — same as WandOfCoatingBase.IgnorePaintColor.</summary>
        const byte IgnorePaintColor = 255;

        int changed = 0;

        foreach (Point tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            bool wasChanged = false;

#pragma warning disable CS0618
            switch (mode)
            {
                case CoatingMode.PaintTile:
                    wasChanged = ServerApplyPaintTile(x, y, paintColor, IgnorePaintColor,
                        applyIlluminant, ignoreIlluminant, applyEcho, ignoreEcho, repaint);
                    break;
                case CoatingMode.PaintWall:
                    wasChanged = ServerApplyPaintWall(x, y, paintColor, IgnorePaintColor,
                        applyIlluminant, ignoreIlluminant, applyEcho, ignoreEcho, repaint);
                    break;
                case CoatingMode.ScrapePaint:
                    wasChanged = ServerApplyScrapePaint(x, y);
                    break;
                case CoatingMode.ScrapeMoss:
                    wasChanged = ServerApplyScrapeMoss(x, y);
                    break;
                case CoatingMode.HarvestMoss:
                    wasChanged = ServerApplyHarvestMoss(x, y);
                    break;
            }
#pragma warning restore CS0618

            if (wasChanged)
            {
                changed++;
                // SendTileSquare is still needed for moss operations (ScrapeMoss/HarvestMoss)
                // which change tile type. For paint/coating, the helpers now use broadCast:true
                // which sends dedicated MessageID 63/64 packets, but SendTileSquare provides
                // an extra safety sync for any tile-level state changes.
                NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }

        SendOperationResult(playerWhoAmI, WandPacketType.CoatingOperation, changed, true);
    }

    // ── Coating server helpers (mirror WandOfCoatingBase.Apply* methods) ──

    private static bool ServerApplyPaintTile(int x, int y, byte color, byte ignorePaintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;

        bool changed = false;

        if (color != ignorePaintColor && tile.TileColor != color)
        {
            if (!repaint && tile.TileColor != PaintID.None)
            {
                // Tile already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                // broadCast: true → sends MessageID.PaintTile (63) which correctly
                // handles color=0 (paint removal). SendTileSquare (20) silently skips
                // paint bytes when color==0, leaving clients with stale paint.
                WorldGen.paintTile(x, y, color, true);
                changed = true;
            }
        }

        bool hasIlluminant = tile.IsTileFullbright;
        bool hasEcho = tile.IsTileInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatTile(x, y, 0, true);
            if (wantIlluminant && !tile.IsTileFullbright)
                WorldGen.paintCoatTile(x, y, 1, true);
            if (wantEcho && !tile.IsTileInvisible)
                WorldGen.paintCoatTile(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyPaintWall(int x, int y, byte color, byte ignorePaintColor,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (tile.WallType == WallID.None) return false;

        bool changed = false;

        if (color != ignorePaintColor && tile.WallColor != color)
        {
            if (!repaint && tile.WallColor != PaintID.None)
            {
                // Wall already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                // broadCast: true → sends MessageID.PaintWall (64) for correct sync.
                WorldGen.paintWall(x, y, color, true);
                changed = true;
            }
        }

        bool hasIlluminant = tile.IsWallFullbright;
        bool hasEcho = tile.IsWallInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatWall(x, y, 0, true);
            if (wantIlluminant && !tile.IsWallFullbright)
                WorldGen.paintCoatWall(x, y, 1, true);
            if (wantEcho && !tile.IsWallInvisible)
                WorldGen.paintCoatWall(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyScrapePaint(int x, int y)
    {
        var tile = Main.tile[x, y];
        bool changed = false;

        if (tile.HasTile && tile.TileColor != PaintID.None)
        {
            WorldGen.paintTile(x, y, PaintID.None, true);
            changed = true;
        }
        if (tile.HasTile && (tile.IsTileFullbright || tile.IsTileInvisible))
        {
            WorldGen.paintCoatTile(x, y, 0, true);
            changed = true;
        }
        if (tile.WallType != WallID.None && tile.WallColor != PaintID.None)
        {
            WorldGen.paintWall(x, y, PaintID.None, true);
            changed = true;
        }
        if (tile.WallType != WallID.None && (tile.IsWallFullbright || tile.IsWallInvisible))
        {
            WorldGen.paintCoatWall(x, y, 0, true);
            changed = true;
        }

        return changed;
    }

    private static bool ServerApplyScrapeMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;

        // Moss tiles are converted back to Stone by checking known moss→substrate mappings.
        // This mirrors WandOfCoatingBase.ApplyScrapeMoss but uses inline checks
        // since the server doesn't have access to the client-side dictionary.
        int tileType = tile.TileType;
        int substrate = -1;

        if (tileType == TileID.GreenMoss || tileType == TileID.BrownMoss ||
            tileType == TileID.RedMoss || tileType == TileID.BlueMoss ||
            tileType == TileID.PurpleMoss || tileType == TileID.LavaMoss ||
            tileType == TileID.ArgonMoss || tileType == TileID.KryptonMoss ||
            tileType == TileID.XenonMoss || tileType == TileID.VioletMoss ||
            tileType == TileID.RainbowMoss)
        {
            substrate = TileID.Stone;
        }

        if (substrate < 0) return false;

        Main.tile[x, y].TileType = (ushort)substrate;
        if (Main.tile[x, y].TileColor != PaintID.None)
            WorldGen.paintTile(x, y, PaintID.None, true);
        WorldGen.SquareTileFrame(x, y, true);

        return true;
    }

    private static bool ServerApplyHarvestMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return false;
        if (tile.TileType != TileID.LongMoss) return false;

        WorldGen.KillTile(x, y);
        if (tile.HasTile) return false; // Tile wasn't actually killed

        return true;
    }

    // ── Building helper methods ───────────────────────────────────────

    /// <summary>
    /// Find the first inventory slot matching a condition (server-side).
    /// Searches hotbar (0-9), main inventory (10-49), then ammo/misc (50-57).
    /// </summary>
    private static int FindItemSlot(Player player, Func<Item, bool> condition)
    {
        for (int i = 0; i < 58; i++)
        {
            if (!player.inventory[i].IsAir && condition(player.inventory[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Consume one item from the server-side player inventory.
    /// Handles tile wands (consume ammo type) vs direct items.
    /// Tracks modified slots for SyncEquipment.
    /// </summary>
    private static void ConsumeOneServerItem(
        Player player, Item sourceItem, Func<Item, bool> baseCondition,
        HashSet<int> changedSlots)
    {
        bool isTileWand = sourceItem.tileWand >= 0;
        Func<Item, bool> consumeCond = isTileWand
            ? i => !i.IsAir && i.type == sourceItem.tileWand
            : baseCondition;

        for (int i = 0; i < 58; i++)
        {
            if (consumeCond(player.inventory[i]))
            {
                player.inventory[i].stack--;
                if (player.inventory[i].stack <= 0)
                    player.inventory[i].TurnToAir();
                changedSlots.Add(i);
                return;
            }
        }
    }

    /// <summary>
    /// Applies slope settings on the server side.
    /// Mirror of WandOfBuildingBase.ApplySlope for server execution.
    /// </summary>
    private static void ApplySlopeServer(int x, int y, SlopeType slope)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return;

        if (slope == SlopeType.Default)
        {
            tile.IsHalfBlock = false;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else if (slope == SlopeType.VerticalHalf)
        {
            tile.IsHalfBlock = true;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else
        {
            tile.IsHalfBlock = false;
            tile.Slope = slope switch
            {
                SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpRight,
                SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpLeft,
                _ => Terraria.ID.SlopeType.Solid
            };
        }
        WorldGen.SquareTileFrame(x, y);
    }

    /// <summary>
    /// Main packet dispatch. Called from Mod.HandlePacket().
    /// Server-side: applies per-player rate limiting before dispatching to handlers.
    /// </summary>
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var packetType = (WandPacketType)reader.ReadByte();

        // Rate-limit all operation packets on the server. Non-operation packets
        // (OperationResult, ProtectionBulkSync) are exempt.
        if (Main.netMode == NetmodeID.Server)
        {
            bool isOperation = packetType is WandPacketType.WiringOperation
                or WandPacketType.BuildingOperation
                or WandPacketType.DismantlingOperation
                or WandPacketType.ReplacementOperation
                or WandPacketType.SafekeepingOperation
                or WandPacketType.CoatingOperation;

            if (isOperation && IsOnCooldown(whoAmI, packetType))
                return; // Silently drop — client will re-send on next valid window
        }

        switch (packetType)
        {
            case WandPacketType.WiringOperation:
                HandleWiringOperation(reader, whoAmI);
                break;
            case WandPacketType.BuildingOperation:
                HandleBuildingOperation(reader, whoAmI);
                break;
            case WandPacketType.DismantlingOperation:
                HandleDismantlingOperation(reader, whoAmI);
                break;
            case WandPacketType.ReplacementOperation:
                HandleReplacementOperation(reader, whoAmI);
                break;
            case WandPacketType.SafekeepingOperation:
                // TODO: HandleSafekeepingOperation(reader, whoAmI);
                break;
            case WandPacketType.ProtectionBulkSync:
                // TODO: HandleProtectionBulkSync(reader, whoAmI);
                break;
            case WandPacketType.CoatingOperation:
                HandleCoatingOperation(reader, whoAmI);
                break;
            case WandPacketType.OperationResult:
                HandleOperationResult(reader);
                break;
            default:
                // Unknown packet type — skip silently
                break;
        }
    }
}
