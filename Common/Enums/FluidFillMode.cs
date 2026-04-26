namespace WorldShapingWandsMod.Common.Enums
{
    /// <summary>
    /// Determines which algorithm the Wand of Fluids uses to place liquids.
    /// </summary>
    public enum FluidFillMode : byte
    {
        /// <summary>Places liquid in every non-solid tile within the selection shape.</summary>
        FullLiquid,

        /// <summary>
        /// Rain-seeds at the first block of each column in the selection area.
        /// Uses rain cloud projectiles that raycast downward and fill basins naturally.
        /// Only fills terrain-trapped positions reachable from above.
        /// </summary>
        RainFill,

        /// <summary>
        /// Fills sealed enclosed cavities within the selection area.
        /// Similar to RainFill but includes pockets below overhangs.
        /// </summary>
        PocketFill
    }
}
