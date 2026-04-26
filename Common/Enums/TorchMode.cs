namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Operation mode for the Wand of Torches.
/// Determines what action is performed on torch tiles.
/// </summary>
public enum TorchMode : byte
{
    /// <summary>Place new torches at valid positions.</summary>
    Place = 0,

    /// <summary>Replace existing torches with the selected torch type.</summary>
    Replace = 1,

    /// <summary>Remove existing torches from the selection.</summary>
    Remove = 2,

    /// <summary>Convert existing torches to the current biome torch variant.</summary>
    Convert = 3,
}