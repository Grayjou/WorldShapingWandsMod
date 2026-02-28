namespace WorldShapingWandsMod.Common.Enums
{
    /// <summary>
    /// Defines how the shape is rendered/filled.
    /// </summary>
    public enum ShapeMode : byte
    {
        /// <summary>Completely filled shape.</summary>
        Filled = 0,
        
        /// <summary>Only the border/edge tiles.</summary>
        Hollow = 1,
        
        /// <summary>Outline with configurable thickness (future).</summary>
        Outline = 2,
    }
}