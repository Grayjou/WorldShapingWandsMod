namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the operation to perform on the world.
/// </summary>
public enum OperationType : byte
{
    /// <summary>Place tiles/objects.</summary>
    Place = 0,
    
    /// <summary>Remove tiles/objects.</summary>
    Remove = 1,
    
    /// <summary>Replace existing tiles with new ones.</summary>
    Replace = 2,
}