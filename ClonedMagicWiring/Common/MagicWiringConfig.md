using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MagicWiring.Common;

/// <summary>
/// Server-side config so the host controls max distance for all players.
/// Using ServerSide prevents players from bypassing limits locally.
/// </summary>
public class MagicWiringConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("WiringLimits")]

    [Label("Max Wiring Distance (tiles)")]
    [Tooltip("Maximum distance in tiles for wand operations. Set to 0 for unlimited.")]
    [Range(0, 1000)]
    [DefaultValue(200)]
    public int MaxWiringDistance;

    [Label("Show Distance Warning")]
    [Tooltip("Flash a warning when the selection is clamped to max distance.")]
    [DefaultValue(true)]
    public bool ShowDistanceWarning;
}