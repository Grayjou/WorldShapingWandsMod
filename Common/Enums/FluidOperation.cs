namespace WorldShapingWandsMod.Common.Enums
{
    /// <summary>
    /// Whether the Wand of Fluids should place or remove liquid.
    /// </summary>
    public enum FluidOperation : byte
    {
        /// <summary>Place liquid within the selection.</summary>
        Fill,

        /// <summary>Remove all liquid within the selection.</summary>
        Drain
    }
}
