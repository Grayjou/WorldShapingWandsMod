using System;

namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// (S1 2026-04-29 — SubUI Architecture Phase A) Bit-flag companion to the
/// <see cref="WandFamily"/> single-value byte enum. <see cref="WandFamily"/>
/// stays unchanged (it is consumed at ~45 sites as a byte switch dispatch
/// key — converting it to <c>[Flags]</c> would re-number every member and
/// break dictionary lookups in <c>WandColors</c>, the
/// <c>InventoryViewRegistry</c>, and the
/// <c>BaseCyclingWand.GetActionForFamily</c> switch). This companion type
/// gives us the architecture-prescribed <c>OwnerFamilies</c> predicate
/// vocabulary without that breakage: SubPanels declare which families
/// they answer to via a <see cref="WandFamilyMask"/>, and the runtime
/// converts the held-wand's <see cref="WandFamily"/> to a single-bit
/// mask via <see cref="WandFamilyMaskExtensions.AsMask"/> for the
/// predicate test.
///
/// <para>
/// Bit assignments mirror the order of the <see cref="WandFamily"/>
/// members (Building=bit0, Dismantling=bit1, …, Molding=bit9). The
/// <see cref="WandFamily.Unknown"/> sentinel maps to <see cref="None"/>
/// (zero bits set) so a non-WSW held item naturally fails every
/// <c>(mask &amp; held) != 0</c> predicate without special-casing.
/// </para>
///
/// <para>
/// The architecture doc's stub uses bit positions starting at 1&lt;&lt;0
/// for Coating; we instead start at 1&lt;&lt;0 for Building to match the
/// numeric order of the existing <see cref="WandFamily"/> members
/// (Building=1, Dismantling=2, …). Either ordering is correct — the bit
/// value is private to the predicate machinery — and matching the
/// existing enum minimises future cognitive overhead when the two
/// types are referenced side-by-side.
/// </para>
/// </summary>
[Flags]
public enum WandFamilyMask : uint
{
    /// <summary>No families. Matches no held wand.</summary>
    None         = 0,

    Building     = 1u << 0,
    Dismantling  = 1u << 1,
    Replacement  = 1u << 2,
    Wiring       = 1u << 3,
    Safekeeping  = 1u << 4,
    Coating      = 1u << 5,
    Fluids       = 1u << 6,
    Torches      = 1u << 7,
    Delimitation = 1u << 8,
    Molding      = 1u << 9,

    /// <summary>All ten WSW wand families. Use for SubPanels that should
    /// stay visible whenever any WSW wand is held (e.g. the Shape Selector
    /// per the architecture doc's Instance Reference table).</summary>
    All = Building | Dismantling | Replacement | Wiring | Safekeeping
        | Coating | Fluids | Torches | Delimitation | Molding,

    /// <summary>The three families that consume the global paint colour
    /// (paint pickers stay visible across this group). Per the
    /// PaintColor Popout entry in the architecture's Instance Reference
    /// table.</summary>
    PaintConsumers = Coating | Building | Replacement,
}

/// <summary>
/// Conversion helpers between the single-value <see cref="WandFamily"/>
/// enum and the bit-flag <see cref="WandFamilyMask"/> companion.
/// </summary>
public static class WandFamilyMaskExtensions
{
    /// <summary>
    /// Converts a single-value <see cref="WandFamily"/> to its single-bit
    /// <see cref="WandFamilyMask"/> equivalent.
    /// <see cref="WandFamily.Unknown"/> maps to
    /// <see cref="WandFamilyMask.None"/>.
    /// </summary>
    public static WandFamilyMask AsMask(this WandFamily family) => family switch
    {
        WandFamily.Building     => WandFamilyMask.Building,
        WandFamily.Dismantling  => WandFamilyMask.Dismantling,
        WandFamily.Replacement  => WandFamilyMask.Replacement,
        WandFamily.Wiring       => WandFamilyMask.Wiring,
        WandFamily.Safekeeping  => WandFamilyMask.Safekeeping,
        WandFamily.Coating      => WandFamilyMask.Coating,
        WandFamily.Fluids       => WandFamilyMask.Fluids,
        WandFamily.Torches      => WandFamilyMask.Torches,
        WandFamily.Delimitation => WandFamilyMask.Delimitation,
        WandFamily.Molding      => WandFamilyMask.Molding,
        _                       => WandFamilyMask.None,
    };

    /// <summary>
    /// Predicate evaluation: true iff the (single) <paramref name="held"/>
    /// family is a member of this <paramref name="mask"/>. The architecture
    /// doc's <c>ShouldBeVisible</c> contract.
    /// </summary>
    public static bool Contains(this WandFamilyMask mask, WandFamily held)
    {
        if (mask == WandFamilyMask.None) return false;
        return (mask & held.AsMask()) != 0;
    }
}
