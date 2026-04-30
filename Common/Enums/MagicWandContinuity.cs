namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0.2 + §6.2 + §6.5.)
/// Contiguity mode for Magic Wand (Read) flood-fill — the second of the two
/// top-level sections in the Read configuration SubUI (rendered as a
/// 3-option radio).
/// </summary>
/// <remarks>
/// <para>The original v1 design shipped 4-only and deferred 8-neighbour;
/// the S2 → S3 design then absorbed both into a single radio so the
/// player can pick precisely. Non-contiguous is the *"select all matches
/// across the entire domain"* mode (no flood; iterate the canvas
/// membership and collect every cell the predicate accepts) — useful for
/// stencil-canvas-wide bulk re-paints and for *"select every painted
/// tile"* queries.</para>
/// </remarks>
public enum MagicWandContinuity : byte
{
    /// <summary>Standard 4-neighbour orthogonal flood (N/S/E/W). Default.</summary>
    FourNeighbour = 0,

    /// <summary>8-neighbour flood (orthogonal + diagonals).</summary>
    EightNeighbour = 1,

    /// <summary>
    /// No flood. Iterate the entire domain (active stencil canvas
    /// membership for Read on a stencil wand) and collect every cell
    /// matching the predicate, capped at <c>LimitsConfig.MaxSelectionTiles</c>.
    /// </summary>
    NonContiguous = 2,
}
