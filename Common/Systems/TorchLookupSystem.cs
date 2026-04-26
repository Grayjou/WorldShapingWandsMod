using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Builds and tears down the cached torch lookup tables used by
/// <see cref="TorchPlacementHelper"/> for vanilla + modded torch support.
/// Must run after all mod items are registered (PostSetupContent).
/// </summary>
public class TorchLookupSystem : ModSystem
{
    public override void PostSetupContent()
    {
        TorchPlacementHelper.BuildTorchLookup();
    }

    public override void Unload()
    {
        TorchPlacementHelper.UnloadTorchLookup();
    }
}
