namespace WorldShapingWandsMod.Common.Geometry;

/// <summary>
/// (C-S3 2026-05-03 — <c>ImplementationTicket_ActuationFilter_MagicRead.md</c>)
/// Read-time admission filter for Magic Wand Read. Controls which actuated
/// state of tiles is included in the captured flood-fill result.
/// Scope: Magic Wand Read only. Apply-time actuation behaviour is unchanged.
/// </summary>
public enum ActuationFilter : byte
{
    /// <summary>Admit both actuated and non-actuated tiles. Default.</summary>
    Both = 0,
    /// <summary>Admit only tiles that are NOT currently actuated (inActive=false).</summary>
    NonActuatedOnly = 1,
    /// <summary>Admit only tiles that ARE currently actuated (inActive=true).</summary>
    ActuatedOnly = 2,
}

public static class ActuationFilterExtensions
{
    /// <summary>
    /// Returns true if a tile in the given actuated state should be admitted
    /// into the captured shape.
    /// </summary>
    public static bool Admits(this ActuationFilter filter, bool tileIsActuated)
        => filter switch
        {
            ActuationFilter.Both           => true,
            ActuationFilter.NonActuatedOnly => !tileIsActuated,
            ActuationFilter.ActuatedOnly    => tileIsActuated,
            _ => true,
        };

    /// <summary>Cycle order per Design G-2: Both → NonActuated → Actuated → Both.</summary>
    public static ActuationFilter Next(this ActuationFilter filter)
        => filter switch
        {
            ActuationFilter.Both           => ActuationFilter.NonActuatedOnly,
            ActuationFilter.NonActuatedOnly => ActuationFilter.ActuatedOnly,
            ActuationFilter.ActuatedOnly    => ActuationFilter.Both,
            _ => ActuationFilter.Both,
        };
}
