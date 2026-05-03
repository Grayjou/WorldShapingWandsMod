using WorldShapingWandsMod.Common.Selection;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// One slot in the Delimitation Wand's multi-stencil row.
/// Mirrors the per-slot model used by <see cref="MoldingWandPlayer"/>'s stencil system.
/// Each slot holds its own <see cref="SelectionCanvas"/>, <see cref="TileSelection"/>,
/// and optional <see cref="CustomShape"/>; all are independent across slots.
/// </summary>
internal sealed class DelimSlot
{
    /// <summary>The canvas (boundary region) for this slot.</summary>
    public SelectionCanvas Canvas { get; } = new();

    /// <summary>The tile selection (subset of the canvas) for this slot.</summary>
    public TileSelection Selection { get; } = new();

    /// <summary>
    /// A user-defined shape captured from <see cref="Selection"/> via Promote.
    /// <c>null</c> means no custom shape for this slot.
    /// </summary>
    public CustomShape CustomShape { get; set; }

    /// <summary>
    /// <c>true</c> when all three data members are empty/null (slot is unused).
    /// </summary>
    public bool IsEmpty => !Canvas.IsActive && !Selection.IsActive && CustomShape == null;

    /// <summary>
    /// Clears all data in this slot (canvas, selection, custom shape).
    /// </summary>
    public void ClearAll()
    {
        Canvas.Clear();
        Selection.Clear();
        CustomShape = null;
    }
}
