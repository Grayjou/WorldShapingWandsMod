using System.IO;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Networking;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod;

public class WorldShapingWandsMod : Mod
{
    public static WorldShapingWandsMod Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
        ShapeRegistry.Initialize();
#if DEBUG
        DevTunable.Initialize();
#endif
    }

    public override void Unload()
    {
#if DEBUG
        DevTunable.Unload();
#endif
        ShapeRegistry.Unload();
        Instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        WandPacketHandler.HandlePacket(reader, whoAmI);
    }
}
