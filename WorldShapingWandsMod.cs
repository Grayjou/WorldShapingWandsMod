using System.IO;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Networking;

namespace WorldShapingWandsMod;

public class WorldShapingWandsMod : Mod
{
    public static WorldShapingWandsMod Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
        ShapeRegistry.Initialize();
    }

    public override void Unload()
    {
        ShapeRegistry.Unload();
        Instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        WandPacketHandler.HandlePacket(reader, whoAmI);
    }
}
