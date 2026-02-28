using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;

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
}
