namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Operations that modify the <see cref="Selection.SelectionCanvas"/> itself
/// (the boundary region that constrains tile selections).
/// </summary>
public enum CanvasOperation
{
    /// <summary>Add shape tiles to the canvas, expanding the boundary.</summary>
    Add,

    /// <summary>Remove shape tiles from the canvas, shrinking the boundary.</summary>
    Remove,

    /// <summary>Clear all canvas tiles, removing the boundary entirely.</summary>
    Clear,
}
