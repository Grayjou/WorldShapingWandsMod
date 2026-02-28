using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using MagicWiring.Common;

namespace MagicWiring.Networking;

public static class WiringPacketHandler
{
    /// <summary>
    /// Sends a wiring operation packet to the server.
    /// Called from the projectile when the drag ends.
    /// </summary>
    public static void SendWiringOperation(
        Point start, Point end, WiringMode mode, WiringShape shape,
        byte wireFlags, bool verticalFirst, int playerWhoAmI)
    {
        ModPacket packet = MagicWiring.Instance.GetPacket();
        packet.Write((byte)MagicWiring.PacketType.WiringOperation);
        packet.Write(start.X); packet.Write(start.Y);
        packet.Write(end.X); packet.Write(end.Y);
        packet.Write((byte)mode);
        packet.Write((byte)shape);
        packet.Write(wireFlags);
        packet.Write(verticalFirst); // NEW: for WireKite sync
        packet.Write((byte)playerWhoAmI);
        packet.Send();
    }

    /// <summary>
    /// Handles incoming wiring operation packets on the server.
    /// Validates the operation and broadcasts it to all clients.
    /// </summary>
    public static void HandleWiringOperation(BinaryReader reader, int whoAmI)
    {
        int startX = reader.ReadInt32(), startY = reader.ReadInt32();
        int endX = reader.ReadInt32(), endY = reader.ReadInt32();
        var mode = (WiringMode)reader.ReadByte();
        var shape = (WiringShape)reader.ReadByte();
        byte wireFlags = reader.ReadByte();
        bool verticalFirst = reader.ReadBoolean();
        int playerWhoAmI = reader.ReadByte();

        if (playerWhoAmI < 0 || playerWhoAmI >= Main.maxPlayers || !Main.player[playerWhoAmI].active)
            return;

        // Server-side distance enforcement
        if (Main.netMode == NetmodeID.Server)
        {
            var config = ModContent.GetInstance<MagicWiringConfig>();
            int maxDist = config?.MaxWiringDistance ?? 200;
            if (maxDist > 0)
            {
                var start = new Point(startX, startY);
                var end = new Point(endX, endY);
                var (clamped, _) = ShapeHelper.ClampDistance(start, end, maxDist);
                endX = clamped.X; endY = clamped.Y;
            }
        }

        var player = Main.player[playerWhoAmI];
        var tiles = ShapeHelper.GetShapeTiles(new Point(startX, startY), new Point(endX, endY), shape, verticalFirst);
        var (red, green, blue, yellow, actuator) = WiringSettings.UnpackWireFlags(wireFlags);

        WiringHelper.ExecuteWiringOperation(tiles, mode, red, green, blue, yellow, actuator, player);

        if (Main.netMode == NetmodeID.Server)
        {
            var broadcast = MagicWiring.Instance.GetPacket();
            broadcast.Write((byte)MagicWiring.PacketType.WiringOperation);
            broadcast.Write(startX); broadcast.Write(startY);
            broadcast.Write(endX); broadcast.Write(endY);
            broadcast.Write((byte)mode);
            broadcast.Write((byte)shape);
            broadcast.Write(wireFlags);
            broadcast.Write(verticalFirst);
            broadcast.Write((byte)playerWhoAmI);
            broadcast.Send(-1, whoAmI);
        }
    }
}