namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the slope/shape configuration for tiles.
/// </summary>
public enum SlopeType : byte
{
    /// <summary>Full tile with no slope.</summary>
    Default = 0,

    /// <summary>Vertically halved tile.</summary>
    VerticalHalf = 1,

    /// <summary>Slope with bottom-right corner raised.</summary>
    BottomRight = 2,

    /// <summary>Slope with bottom-left corner raised.</summary>
    BottomLeft = 3,

    /// <summary>Slope with top-right corner raised.</summary>
    TopRight = 4,

    /// <summary>Slope with top-left corner raised.</summary>
    TopLeft = 5,
}

