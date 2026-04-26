using Terraria;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Projectiles.WandActions.Resolvers;

/// <summary>
/// Resolves the texture asset path for a WandAction within a specific wand family.
/// Each family provides its own resolver, keeping sprite-mapping logic close to
/// the family's code rather than centralized in WandActionProjectile.
/// </summary>
/// <remarks>
/// Implementations are stateless singletons registered in
/// <see cref="ActionSpriteResolverRegistry"/>. The projectile's texture cache
/// stores the result, so resolvers are only called when the action changes.
/// </remarks>
internal interface IActionSpriteResolver
{
    /// <summary>
    /// Returns the full mod-relative texture path (without file extension) for
    /// the given <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The current WandAction value from ai[1].</param>
    /// <param name="projectile">
    /// The projectile instance, available for owner-based lookups (e.g., Fluids
    /// reads the owner's LiquidType setting).
    /// </param>
    /// <returns>
    /// A texture path like
    /// <c>"WorldShapingWandsMod/Content/Projectiles/WandActions/Fluids/WandAction_PourWater"</c>.
    /// </returns>
    string ResolveTexturePath(WandAction action, Projectile projectile);
}
