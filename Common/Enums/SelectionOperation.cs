namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Boolean operations that can be applied to a <see cref="Selection.TileSelection"/>
/// using a shape as the operand, constrained by the active <see cref="Selection.SelectionCanvas"/>.
/// </summary>
public enum SelectionOperation
{
    /// <summary>Union — add shape tiles to the selection.</summary>
    Add,

    /// <summary>Difference — remove shape tiles from the selection.</summary>
    Remove,

    /// <summary>Intersection — keep only tiles that are in both the selection and the shape.</summary>
    Intersect,

    /// <summary>Symmetric difference — toggle each tile (add if absent, remove if present).</summary>
    XOR,

    /// <summary>Clear — remove all tiles from the selection (shape is ignored).</summary>
    Clear,

    /// <summary>Invert — select all canvas tiles that are NOT currently selected (shape is ignored).</summary>
    Invert,
}
