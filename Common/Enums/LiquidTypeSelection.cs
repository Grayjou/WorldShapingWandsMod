namespace WorldShapingWandsMod.Common.Enums
{
    /// <summary>
    /// The type of liquid the Wand of Fluids operates with.
    /// Maps to Terraria's internal liquid type IDs.
    /// </summary>
    public enum LiquidTypeSelection : byte
    {
        /// <summary>Water (LiquidID.Water = 0).</summary>
        Water = 0,

        /// <summary>Lava (LiquidID.Lava = 1).</summary>
        Lava = 1,

        /// <summary>Honey (LiquidID.Honey = 2).</summary>
        Honey = 2,

        /// <summary>Shimmer (LiquidID.Shimmer = 3).</summary>
        Shimmer = 3
    }
}
