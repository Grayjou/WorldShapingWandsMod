using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldShapingWandsMod.Common.Configs
{
    public class WandConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide; // Can be ServerSide if you want per-world

        [Header("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResources.Header")]
        [DefaultValue(false)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableInfiniteResource.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.EnableInfiniteResource.Tooltip")]
        public bool EnableInfiniteResource { get; set; }

        [DefaultValue(0)]
        [Range(0, 10000)]
        [Increment(100)]
        [LabelKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResourceAmount.Label")]
        [TooltipKey("$Mods.WorldShapingWandsMod.Configs.WandConfig.InfiniteResourceAmount.Tooltip")]
        public int InfiniteResourceAmount { get; set; }

        // You can add other global settings here later
    }
}