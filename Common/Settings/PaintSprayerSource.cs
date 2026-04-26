namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Tri-state selector for the Paint Sprayer feature on the Building and Replacement wands.
/// Determines where the wand pulls paint colours from when auto-painting placed/replaced surfaces.
/// </summary>
/// <remarks>
/// Design reference: <c>dev_notes/inbox/Cavendish 2026-04-19_Session_1/DesignDoc_PaintSprayerSourceToggle.md</c>.
/// <para>
/// Replaces the prior <c>bool PaintSprayer</c> field. The legacy <c>true</c> behaviour maps to
/// <see cref="Inventory"/>; the legacy <c>false</c> behaviour maps to <see cref="Off"/>.
/// Settings are not persisted to disk (they are per-session player state on
/// <see cref="WorldShapingWandsMod.Common.Players.WandPlayer"/>), so no JSON migration is required.
/// </para>
/// </remarks>
public enum PaintSprayerSource : byte
{
    /// <summary>No auto-painting. Default.</summary>
    Off = 0,

    /// <summary>Pull paint from the first paint stack found in the player's inventory (vanilla Paint Sprayer parity). Consumes paint.</summary>
    Inventory = 1,

    /// <summary>Pull paint from <see cref="WandOfCoatingSettings.PaintColor"/>. Does not consume inventory paint.</summary>
    CoatingSettings = 2,
}

/// <summary>Convenience extensions for <see cref="PaintSprayerSource"/>.</summary>
public static class PaintSprayerSourceExtensions
{
    /// <summary>Returns true when the source is anything other than <see cref="PaintSprayerSource.Off"/>.</summary>
    public static bool IsActive(this PaintSprayerSource source) => source != PaintSprayerSource.Off;

    /// <summary>Cycles Off → Inventory → CoatingSettings → Off.</summary>
    public static PaintSprayerSource Next(this PaintSprayerSource source) => source switch
    {
        PaintSprayerSource.Off => PaintSprayerSource.Inventory,
        PaintSprayerSource.Inventory => PaintSprayerSource.CoatingSettings,
        _ => PaintSprayerSource.Off,
    };
}
