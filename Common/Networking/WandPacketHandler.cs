using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Networking.Handlers;

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
    /// Void Everything operation: clear tiles, walls, wires, and actuators in the
    /// selection. Carefree Mode only. Liquids are NOT cleared in multiplayer.
    /// </summary>
    VoidEverythingOperation = 8,

    /// <summary>
    /// Server→Client feedback packet reporting operation outcome.
    /// Sent after any operation completes (or fails) on the server.
    /// </summary>
    OperationResult = 10,
}

/// <summary>
/// Central packet dispatch hub for wand multiplayer operations.
/// Routes incoming packets to per-family handler classes and provides
/// shared OperationResult send/receive methods.
///
/// Ported from MagicWiring's WiringPacketHandler with WSW adaptations.
/// Day 2 refactor split into: WandPacketHeader, PacketUtilities,
/// and per-family handlers in Common/Networking/Handlers/.
/// </summary>
public static class WandPacketHandler
{
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
            var clientCfg = WandConfigs.Preferences;
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
    // Main Packet Dispatch
    // ════════════════════════════════════════════════════════════════════

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
                or WandPacketType.CoatingOperation
                or WandPacketType.VoidEverythingOperation;

            if (isOperation && PacketUtilities.IsOnCooldown(whoAmI, packetType))
                return; // Silently drop — client will re-send on next valid window
        }

        switch (packetType)
        {
            case WandPacketType.WiringOperation:
                WiringPacketHandler.HandleWiringOperation(reader, whoAmI);
                break;
            case WandPacketType.BuildingOperation:
                BuildingPacketHandler.HandleBuildingOperation(reader, whoAmI);
                break;
            case WandPacketType.DismantlingOperation:
                DismantlingPacketHandler.HandleDismantlingOperation(reader, whoAmI);
                break;
            case WandPacketType.ReplacementOperation:
                ReplacementPacketHandler.HandleReplacementOperation(reader, whoAmI);
                break;
            case WandPacketType.SafekeepingOperation:
                // TODO: HandleSafekeepingOperation(reader, whoAmI);
                break;
            case WandPacketType.ProtectionBulkSync:
                // TODO: HandleProtectionBulkSync(reader, whoAmI);
                break;
            case WandPacketType.CoatingOperation:
                CoatingPacketHandler.HandleCoatingOperation(reader, whoAmI);
                break;
            case WandPacketType.VoidEverythingOperation:
                VoidEverythingPacketHandler.HandleVoidEverythingOperation(reader, whoAmI);
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
