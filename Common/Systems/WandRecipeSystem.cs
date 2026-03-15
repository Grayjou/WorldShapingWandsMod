using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Systems;

/// <summary>
/// Registers custom recipe groups used by wand crafting recipes.
/// Handles cross-mod compatibility:
///   - MagicStorage: uses RegisterGroupClone to merge AnySilverBar automatically.
///     Our group registered under nameof(ItemID.SilverBar) will be merged by MS via UnionWith.
///   - Calamity: same pattern for AnyGoldBar via nameof(ItemID.GoldBar).
///   - Thorium: Opal and Aquamarine are added to our AnyGem group if Thorium is loaded.
/// </summary>
public class WandRecipeSystem : ModSystem
{
    /// <summary>Any gem (Amethyst, Topaz, Sapphire, Emerald, Ruby, Diamond, Amber + Thorium gems if loaded).</summary>
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
        // ── AnyGem ──────────────────────────────────────────────
        // "Gemstone" instead of "Any Amethyst" — user-requested label.
        // Start with vanilla gems, then extend with cross-mod gems.
        var gemItems = new List<int>
        {
            ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire,
            ItemID.Emerald, ItemID.Ruby, ItemID.Diamond, ItemID.Amber
        };

        // Thorium: Add Opal and Aquamarine if Thorium Mod is loaded.
        // Uses ModLoader.TryGetMod + Mod.TryFind to safely resolve ModItem types.
        if (ModLoader.TryGetMod("ThoriumMod", out Mod thorium))
        {
            if (thorium.TryFind<ModItem>("Opal", out var opal))
                gemItems.Add(opal.Type);
            else
                Mod.Logger.Info("Thorium loaded but Opal not found — skipping AnyGem extension.");

            if (thorium.TryFind<ModItem>("Aquamarine", out var aquamarine))
                gemItems.Add(aquamarine.Type);
            else
                Mod.Logger.Info("Thorium loaded but Aquamarine not found — skipping AnyGem extension.");
        }

        AnyGem = new RecipeGroup(
            () => Language.GetTextValue("Mods.WorldShapingWandsMod.RecipeGroups.Gemstone"),
            gemItems.ToArray());
        RecipeGroup.RegisterGroup(AnyGemKey, AnyGem);

        // ── AnyGoldBar ──────────────────────────────────────────
        // Registered under nameof(ItemID.GoldBar) — the vanilla convention.
        // If MagicStorage or Calamity is loaded, their RegisterGroupClone
        // will call UnionWith on the ValidItems set, automatically merging
        // any additional bars (e.g., Calamity's modded gold-tier bars).
        AnyGoldBar = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldBar)}",
            ItemID.GoldBar, ItemID.PlatinumBar);
        RecipeGroup.RegisterGroup(nameof(ItemID.GoldBar), AnyGoldBar);

        // ── AnySilverBar ────────────────────────────────────────
        // Same pattern. MagicStorage's RegisterGroupClone merges automatically
        // via UnionWith when it detects a group already registered under this key.
        AnySilverBar = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.SilverBar)}",
            ItemID.SilverBar, ItemID.TungstenBar);
        RecipeGroup.RegisterGroup(nameof(ItemID.SilverBar), AnySilverBar);

        // ── AnyEvilStone ────────────────────────────────────────
        AnyEvilStone = new RecipeGroup(
            () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.EbonstoneBlock)}",
            ItemID.EbonstoneBlock, ItemID.CrimstoneBlock);
        RecipeGroup.RegisterGroup(nameof(ItemID.EbonstoneBlock), AnyEvilStone);

        // ── Wand family groups ──────────────────────────────────
        // For Replacement recipe and any future cross-family recipes.
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
