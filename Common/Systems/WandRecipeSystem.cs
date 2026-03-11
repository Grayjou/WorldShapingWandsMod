using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Registers custom recipe groups used by wand crafting recipes.
/// </summary>
public class WandRecipeSystem : ModSystem
{
    /// <summary>Any gem (Amethyst, Topaz, Sapphire, Emerald, Ruby, Diamond, Amber).</summary>
    public static RecipeGroup AnyGem;

    /// <summary>Any gold-tier bar (Gold Bar or Platinum Bar).</summary>
    public static RecipeGroup AnyGoldBar;

    /// <summary>Any silver-tier bar (Silver Bar or Tungsten Bar).</summary>
    public static RecipeGroup AnySilverBar;

    /// <summary>Any evil stone (Ebonstone Block or Crimstone Block).</summary>
    public static RecipeGroup AnyEvilStone;

    /// <summary>Any Wand of Building (all 4 modes).</summary>
    public static RecipeGroup AnyWandOfBuilding;

    /// <summary>Any Wand of Dismantling (all 4 modes).</summary>
    public static RecipeGroup AnyWandOfDismantling;

    // Recipe group key constants for use in AddRecipeGroup calls
    public const string AnyGemKey = "WorldShapingWandsMod:AnyGem";
    public const string AnyWandOfBuildingKey = "WorldShapingWandsMod:AnyWandOfBuilding";
    public const string AnyWandOfDismantlingKey = "WorldShapingWandsMod:AnyWandOfDismantling";

    public override void AddRecipeGroups()
    {
        // "Gemstone" instead of "Any Amethyst" — user-requested label
        AnyGem = new RecipeGroup(
            () => Language.GetTextValue("Mods.WorldShapingWandsMod.RecipeGroups.Gemstone"),
            ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire,
            ItemID.Emerald, ItemID.Ruby, ItemID.Diamond, ItemID.Amber);
        RecipeGroup.RegisterGroup(AnyGemKey, AnyGem);

        AnyGoldBar = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldBar)}",
            ItemID.GoldBar, ItemID.PlatinumBar);
        RecipeGroup.RegisterGroup(nameof(ItemID.GoldBar), AnyGoldBar);

        AnySilverBar = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.SilverBar)}",
            ItemID.SilverBar, ItemID.TungstenBar);
        RecipeGroup.RegisterGroup(nameof(ItemID.SilverBar), AnySilverBar);

        AnyEvilStone = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.EbonstoneBlock)}",
            ItemID.EbonstoneBlock, ItemID.CrimstoneBlock);
        RecipeGroup.RegisterGroup(nameof(ItemID.EbonstoneBlock), AnyEvilStone);

        // Wand family groups — for Replacement recipe and any future cross-family recipes
        AnyWandOfBuilding = new RecipeGroup(
            () => Language.GetTextValue("Mods.WorldShapingWandsMod.RecipeGroups.AnyWandOfBuilding"),
            ModContent.ItemType<WandOfBuildingInstant>(),
            ModContent.ItemType<WandOfBuildingSelect>(),
            ModContent.ItemType<WandOfBuildingConfirm>(),
            ModContent.ItemType<WandOfBuildingStamp>());
        RecipeGroup.RegisterGroup(AnyWandOfBuildingKey, AnyWandOfBuilding);

        AnyWandOfDismantling = new RecipeGroup(
            () => Language.GetTextValue("Mods.WorldShapingWandsMod.RecipeGroups.AnyWandOfDismantling"),
            ModContent.ItemType<WandOfDismantlingInstant>(),
            ModContent.ItemType<WandOfDismantlingSelect>(),
            ModContent.ItemType<WandOfDismantlingConfirm>(),
            ModContent.ItemType<WandOfDismantlingStamp>());
        RecipeGroup.RegisterGroup(AnyWandOfDismantlingKey, AnyWandOfDismantling);
    }
}
