namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Controls whether a shape is drawn outward from the initial click point
/// (which becomes the center) rather than from a corner.
///
/// <para>
/// <b>Off</b>: default drag behaviour — Start and End define the bounding-box corners.
/// </para>
/// <para>
/// <b>Odd</b>: Start is the center tile; the shape expands symmetrically in all
/// directions. The resulting width and height are always odd (2r+1), so the
/// center tile is always perfectly centered. Radius = |drag delta| in each axis.
/// </para>
/// <para>
/// <b>Even</b>: Start is the corner of the central 2×2 quartet; the shape expands
/// in the drag direction. The resulting width and height are always even (2r),
/// which matches power-of-2 design grids. Radius = |drag delta| + 1 in each axis.
/// </para>
///
/// Supported shapes: Rectangle, Diamond, Ellipse.
/// Ignored for shapes without a well-defined bounding-box center (Triangle, Elbow,
/// CardinalLine, StraightLine, Mold, MagicWandRead).
/// </summary>
public enum DrawFromCenterMode : byte
{
    /// <summary>Default drag bbox — Start and End are opposite corners.</summary>
    Off = 0,

    /// <summary>
    /// Start is the center tile. Odd dimensions: width = 2|dx|+1, height = 2|dy|+1.
    /// Single tile when drag = 0. Shape is always symmetric around the cursor.
    /// </summary>
    Odd = 1,

    /// <summary>
    /// Start is the corner of the center 2×2 block. Even dimensions: width = 2(|dx|+1),
    /// height = 2(|dy|+1). Minimum 2×2 when drag = 0.
    /// </summary>
    Even = 2
}

/// <summary>
/// Extension helpers for <see cref="DrawFromCenterMode"/>.
/// </summary>
public static class DrawFromCenterModeExtensions
{
    /// <summary>Cycles Off → Odd → Even → Off.</summary>
    public static DrawFromCenterMode Next(this DrawFromCenterMode mode) => mode switch
    {
        DrawFromCenterMode.Off  => DrawFromCenterMode.Odd,
        DrawFromCenterMode.Odd  => DrawFromCenterMode.Even,
        DrawFromCenterMode.Even => DrawFromCenterMode.Off,
        _                       => DrawFromCenterMode.Off
    };

    /// <summary>Returns true when drawing is center-anchored (any non-Off state).</summary>
    public static bool IsActive(this DrawFromCenterMode mode) => mode != DrawFromCenterMode.Off;
}
