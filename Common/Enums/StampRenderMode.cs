namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Controls how the selection overlay positions the stamp shape relative
/// to the cursor when the stamp is locked.
/// </summary>
public enum StampRenderMode : byte
{
    /// <summary>
    /// Snap overlay to exact tile grid positions. Pixel-perfect but may
    /// produce visible jitter as the cursor crosses tile boundaries.
    /// </summary>
    Precise,

    /// <summary>
    /// Apply sub-pixel offset based on fractional cursor position within
    /// the current tile. Smooth motion, eliminates tile-boundary jitter.
    /// </summary>
    Smooth,
}
