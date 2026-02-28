using Terraria.ModLoader;
using MagicWiring.Networking;

namespace MagicWiring;

public class MagicWiring : Mod
{
    public static MagicWiring Instance => ModContent.GetInstance<MagicWiring>();

    // Packet types for multiplayer
    public enum PacketType : byte
    {
        WiringOperation = 0
    }

    public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI)
    {
        var type = (PacketType)reader.ReadByte();
        switch (type)
        {
            case PacketType.WiringOperation:
                WiringPacketHandler.HandleWiringOperation(reader, whoAmI);
                break;
        }
    }
}
