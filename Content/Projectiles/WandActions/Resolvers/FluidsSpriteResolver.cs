using Terraria;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Content.Projectiles.WandActions.Resolvers;

/// <summary>
/// Sprite resolver for the Fluids family. Handles three categories:
/// <list type="bullet">
///   <item><b>Static actions</b>: FluidDrainAny → "Drain", FluidBubble → "PlaceBubble"</item>
///   <item><b>Selective drain actions</b>: FluidDrainWater/Lava/Honey/Shimmer
///     resolve to per-liquid drain sprites (DrainWater, DrainLava, etc.).</item>
///   <item><b>Dynamic fill actions</b>: FluidPlace / FluidRainFill / FluidPocketFill
///     resolve by the owner's current <see cref="LiquidTypeSelection"/> (Water/Lava/Honey/Shimmer)
///     and use a per-fill-mode prefix.</item>
/// </list>
/// </summary>
/// <remarks>
/// <b>RainFill sprites</b>: Dedicated RainFill sprites are now wired (duplicated from Pour).
/// Update the actual sprite files when custom art is ready.
/// <b>PocketFill sprite swap</b>: When dedicated sprites arrive, change the
/// prefix string below. Everything else (liquid suffix,
/// folder path, caching) remains unchanged — a true one-line swap per fill mode.
/// </remarks>
internal sealed class FluidsSpriteResolver : IActionSpriteResolver
{
    private const string Folder = "WorldShapingWandsMod/Content/Projectiles/WandActions/Fluids";

    public string ResolveTexturePath(WandAction action, Projectile projectile)
    {
        // ── Static drain (drain all) ──
        if (action == WandAction.FluidDrainAny)
            return $"{Folder}/WandAction_Drain";

        // ── Selective drain (per-liquid type) ──
        // Falls back to Drain sprite until dedicated per-liquid drain art arrives
        if (action is WandAction.FluidDrainWater or WandAction.FluidDrainLava
                    or WandAction.FluidDrainHoney or WandAction.FluidDrainShimmer)
        {
            string drainLiquid = action switch
            {
                WandAction.FluidDrainWater   => "DrainWater",
                WandAction.FluidDrainLava    => "DrainLava",
                WandAction.FluidDrainHoney   => "DrainHoney",
                WandAction.FluidDrainShimmer => "DrainShimmer",
                _                            => "Drain",
            };
            return $"{Folder}/WandAction_{drainLiquid}";
        }

        if (action == WandAction.FluidBubble)
            return $"{Folder}/WandAction_PlaceBubble";

        // ── Dynamic fill actions (liquid-type × fill-mode) ──
        Player owner = Main.player[projectile.owner];
        var wandPlayer = owner.GetModPlayer<WandPlayer>();

        // Per-fill-mode prefix:
        //   FullLiquid  → "Pour"       (PourWater, PourLava, PourHoney, PourShimmer)
        //   RainFill    → "RainFill"   (RainFillWater, RainFillLava, RainFillHoney, RainFillShimmer)
        //   PocketFill  → "Pour"       ← change to "Pocket" when dedicated sprites arrive
        string prefix = action switch
        {
            WandAction.FluidRainFill   => "RainFill",
            WandAction.FluidPocketFill => "Pour",
            _                          => "Pour",
        };

        string liquid = wandPlayer.FluidsSettings.LiquidType switch
        {
            LiquidTypeSelection.Water   => "Water",
            LiquidTypeSelection.Lava    => "Lava",
            LiquidTypeSelection.Honey   => "Honey",
            LiquidTypeSelection.Shimmer => "Shimmer",
            _                           => "Water",
        };

        return $"{Folder}/WandAction_{prefix}{liquid}";
    }
}
