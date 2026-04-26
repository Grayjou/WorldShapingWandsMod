namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// The active editing mode of the Select Wand.
/// Determines whether shape operations modify the selection or the canvas.
/// </summary>
public enum DelimitationWandMode
{
    /// <summary>Shape operations modify the <see cref="Selection.TileSelection"/> within the canvas.</summary>
    Selection,

    /// <summary>Shape operations modify the <see cref="Selection.SelectionCanvas"/> boundary itself.</summary>
    CanvasEdit,
}
