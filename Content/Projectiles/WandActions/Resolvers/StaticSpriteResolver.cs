using System.Collections.Generic;
using Terraria;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Content.Projectiles.WandActions.Resolvers;

/// <summary>
/// General-purpose sprite resolver for families whose actions map to static,
/// known file names with no dynamic logic. Covers the majority of families:
/// Building, Dismantling, Replacement, Wiring, Safekeeping, Coating, Torches,
/// Delimitation, and Molding.
/// </summary>
/// <remarks>
/// Each instance is constructed with a family folder name and a dictionary of
/// action→sprite-suffix mappings. Actions not in the dictionary fall back to
/// <c>WandAction_{EnumName}</c> (the enum member name), which is the convention
/// for Building where the sprite file names match exactly.
/// </remarks>
internal sealed class StaticSpriteResolver : IActionSpriteResolver
{
    private const string BasePath = "WorldShapingWandsMod/Content/Projectiles/WandActions";

    private readonly string _familyFolder;
    private readonly Dictionary<WandAction, string> _suffixOverrides;

    /// <summary>
    /// Creates a resolver for a single family folder.
    /// </summary>
    /// <param name="familyFolder">
    /// Subfolder name under <c>Content/Projectiles/WandActions/</c>
    /// (e.g., <c>"Dismantling"</c>, <c>"Torches"</c>).
    /// </param>
    /// <param name="suffixOverrides">
    /// Maps WandAction values to their sprite file name suffix (without the
    /// <c>WandAction_</c> prefix). Only needed when the file name differs
    /// from the enum member name. May be null for families that use enum names directly.
    /// </param>
    public StaticSpriteResolver(string familyFolder, Dictionary<WandAction, string> suffixOverrides = null)
    {
        _familyFolder = familyFolder;
        _suffixOverrides = suffixOverrides ?? new Dictionary<WandAction, string>();
    }

    public string ResolveTexturePath(WandAction action, Projectile projectile)
    {
        string suffix = _suffixOverrides.TryGetValue(action, out var name)
            ? name
            : action.ToString();

        return $"{BasePath}/{_familyFolder}/WandAction_{suffix}";
    }
}
