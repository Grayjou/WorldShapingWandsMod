namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// The active editing mode of the Wand of Molding.
/// Determines whether shape operations modify the mold selection or the canvas.
/// </summary>
/// <remarks>
/// Identical to <see cref="DelimitationWandMode"/> in structure. Kept as a separate enum
/// so the Molding and Delimitation wand systems remain fully independent.
/// </remarks>
public enum MoldingWandMode
{
    /// <summary>Shape operations modify the mold <see cref="Selection.TileSelection"/> within the canvas.</summary>
    Selection,

    /// <summary>Shape operations modify the mold <see cref="Selection.SelectionCanvas"/> boundary itself.</summary>
    CanvasEdit,
}
