using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Items;



public abstract class BaseCyclingWand : ModItem
{
    public abstract SelectionMode WandSelectionMode { get; }
    public abstract string WandBaseName { get; }
    public abstract Color ModeColor { get; }
    public abstract int GetNextModeItemType();

    /// <summary>
    /// Common lore line shared by all Wands of Creation.
    /// </summary>
    private const string CommonLore = "The Gods of Space let you enact your thoughts into reality.";

    /// <summary>
    /// Wand-specific lore line. Override in each wand base to provide unique flavor text.
    /// Return null or empty to show only the common lore.
    /// </summary>
    public virtual string WandLore => null;

    /// <summary>
    /// Whether to display the divine common lore line. Override to false for
    /// non-divine wands (e.g., Wiring) that have their own standalone lore.
    /// </summary>
    public virtual bool ShowDivineLore => true;

    private const string InventorySuffix = "Inventory";
    // ────────────────────────────────────────────────────────────
    //  Dual Sprite System
    // ────────────────────────────────────────────────────────────
    // The main texture (WandName.png) is used during the swing animation — should be
    // a clean wand silhouette without action indicators.
    // The inventory texture (WandNameInventory.png) is shown in inventory, hotbar,
    // tooltips, and dropped-in-world — the informational sprite with action icons.
    //
    // If Inventory.png doesn't exist, drawing falls through to default behavior.
    //
    // Static dictionary: SetStaticDefaults runs on the template instance only;
    // instance fields would be default (false/null) on all real item instances.
    // Using a static dictionary keyed by item Type ensures all instances can access
    // the loaded texture.
    // ────────────────────────────────────────────────────────────
    private static readonly Dictionary<int, Asset<Texture2D>> _inventoryTextures = new();
    
    public virtual string ModeSuffix => WandSelectionMode switch
    {
        SelectionMode.OneClick => "Instant",
        SelectionMode.TwoClick => "Select", 
        SelectionMode.ThreeClick => "Confirm",
        SelectionMode.FourClick => "Stamp",
        _ => ""
    };

    public override void SetStaticDefaults()
    {
        // Staff-like holding animation (held from handle, like Thunder Zapper)
        Item.staff[Type] = true;

        // Try to load the inventory-specific texture into the static dictionary.
        // SetStaticDefaults runs once per item Type on the template instance.
        string inventoryPath = Texture + InventorySuffix;
        if (ModContent.HasAsset(inventoryPath))
        {
            _inventoryTextures[Type] = ModContent.Request<Texture2D>(inventoryPath, AssetRequestMode.ImmediateLoad);
        }
    }

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 38;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.UseSound = SoundID.Item1;
        Item.useAnimation = 12;
        Item.useTime = 12;
        Item.channel = false;
        Item.rare = ItemRarityID.Blue;
        Item.autoReuse = false;
        Item.noMelee = true;
    }

    public override bool CanRightClick() => true;

    public override void RightClick(Player player)
    {
        // Check if we should cycle or open settings
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        if (wandPlayer.Selection.IsActive)
        {
            // Cancel selection instead of cycling
            wandPlayer.ClearSelection();
            Main.NewText("Selection cancelled.", Color.Yellow);
            Item.stack++; // Prevent consumption
            return;
        }

        // Cycle to next mode
        int nextType = GetNextModeItemType();
        int stack = Item.stack;
        bool wasFavorited = Item.favorited;
        
        Item.SetDefaults(nextType);
        Item.stack = stack + 1; // +1 because right-click consumes one
        Item.favorited = wasFavorited;
        
        Main.NewText($"Switched to {ModeSuffix} mode", ModeColor);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var nameLine = tooltips.Find(x => x.Name == "ItemName" && x.Mod == "Terraria");
        if (nameLine != null)
        {
            nameLine.OverrideColor = ModeColor;
        }

        string hexColor = $"{ModeColor.R:X2}{ModeColor.G:X2}{ModeColor.B:X2}";
        
        string modeDescription = WandSelectionMode switch
        {
            SelectionMode.OneClick => "Click and drag to select area",
            SelectionMode.TwoClick => "Click start, then click end",
            SelectionMode.ThreeClick => "Click start, click end, click to confirm",
            SelectionMode.FourClick => "Click start, click end, click to lock stamp, click to repeat",
            _ => ""
        };
        
        tooltips.Add(new TooltipLine(Mod, "ModeInfo", $"[c/{hexColor}:Mode: {ModeSuffix}]"));
        tooltips.Add(new TooltipLine(Mod, "ModeDesc", modeDescription));
        tooltips.Add(new TooltipLine(Mod, "CycleHint", "Right-click in inventory to cycle modes"));
        tooltips.Add(new TooltipLine(Mod, "ShimmerHint", "[c/BBBBBB:Shimmer decrafts crafted wands into ingredients]"));

        // ── Non-craftable variant: suppress misleading crafting tooltips ──────
        // tModLoader and mods like MoreObtainingTooltips inject tooltip lines based
        // on recipe presence alone, without evaluating conditions. NonCraftable keeps
        // the recipe hidden at stations but doesn't stop those mods. We scrub their
        // lines here and replace them with a clear obtain hint.
        if (WandRecipeConditions.IsNonCraftable(Type))
        {
            // Prefix-matched to cover tML ("CraftingHeader", "Craft1" …) and
            // MoreObtainingTooltips ("Recipe…", "ObtainCraft…") without hardcoding
            // exact names that could differ across versions.
            tooltips.RemoveAll(t =>
                t.Mod != Mod.Name && (
                    t.Name.StartsWith("Craft")
                    || t.Name.StartsWith("Recipe")
                    || t.Name.StartsWith("ObtainCraft")
                ));

            tooltips.Add(new TooltipLine(Mod, "ObtainHint",
                "[c/AAAAAA:Obtain other modes by right-clicking this wand in your inventory]"));
        }

        // ── Lore (Shift-gated) ──────────────────────────────
        var config = ModContent.GetInstance<WandConfig>();
        if (config?.ShowLoreTooltips == true)
        {
            bool shiftHeld = Main.keyState.IsKeyDown(Keys.LeftShift)
                          || Main.keyState.IsKeyDown(Keys.RightShift);

            if (shiftHeld)
            {
                if (ShowDivineLore)
                {
                    tooltips.Add(new TooltipLine(Mod, "LoreCommon",
                        $"[c/AAAAFF:{CommonLore}]"));
                }

                if (!string.IsNullOrEmpty(WandLore))
                {
                    tooltips.Add(new TooltipLine(Mod, "LoreWand",
                        $"[c/CCCCFF:{WandLore}]"));
                }
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "LoreHint",
                    "[c/888888:Hold [Shift] for lore]"));
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Inventory / Hotbar / Tooltip Drawing
    // ────────────────────────────────────────────────────────────
    // Intercept inventory rendering to draw the descriptive Inventory texture
    // instead of the plain wand. Returns false to skip default drawing.
    // The in-use/swing animation still uses the main texture (the clean wand).

    public override bool PreDrawInInventory(
        SpriteBatch spriteBatch,
        Vector2 position,
        Rectangle frame,
        Color drawColor,
        Color itemColor,
        Vector2 origin,
        float scale)
    {
        if (!_inventoryTextures.TryGetValue(Type, out var invTex)) return true;

        spriteBatch.Draw(invTex.Value, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
        return false;
    }

    // ────────────────────────────────────────────────────────────
    //  Dropped-in-World Drawing
    // ────────────────────────────────────────────────────────────
    // When the item is dropped on the ground, show the descriptive texture
    // so players can identify it visually.

    public override bool PreDrawInWorld(
        SpriteBatch spriteBatch,
        Color lightColor,
        Color alphaColor,
        ref float rotation,
        ref float scale,
        int whoAmI)
    {
        if (!_inventoryTextures.TryGetValue(Type, out var invTex)) return true;

        Texture2D altTex = invTex.Value;
        Vector2 drawPos = Item.position - Main.screenPosition
                          + new Vector2(Item.width / 2f, Item.height / 2f);
        Vector2 drawOrigin = altTex.Size() / 2f;

        spriteBatch.Draw(altTex, drawPos, null, lightColor, rotation, drawOrigin, scale, SpriteEffects.None, 0f);
        return false;
    }
}