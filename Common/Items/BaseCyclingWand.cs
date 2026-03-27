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
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

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

    // ── Confirm Distance Safeguard ─────────────────────────────────────
    /// <summary>
    /// Checks whether the player is too far from the selection bounding box
    /// to confirm/execute. Uses the <c>MaxConfirmDistance</c> server config.
    /// Returns true if the click should be BLOCKED (too far).
    /// Shows a warning message to the player when blocked.
    /// </summary>
    /// <param name="selection">The active selection to check distance against.</param>
    /// <param name="mouseTile">The tile the player is clicking on.</param>
    protected static bool IsTooFarToConfirm(SelectionState selection, Point mouseTile)
    {
        var config = ModContent.GetInstance<WandServerConfig>();
        int maxDist = config?.MaxConfirmDistance ?? 50;
        if (maxDist <= 0) return false; // Distance check disabled

        int dist = selection.DistanceFromBBox(mouseTile);
        if (dist > maxDist)
        {
            Main.NewText(
                $"Too far from selection to confirm ({dist} tiles, max {maxDist}). " +
                "Move closer or cancel with right-click.",
                Color.OrangeRed);
            return true;
        }
        return false;
    }

    // ── Rate Limiting (SP) ─────────────────────────────────────────────
    /// <summary>
    /// Checks the operation cooldown for single-player. Returns true if the
    /// execution should be BLOCKED. In MP the server handles rate limiting
    /// at the packet dispatch level.
    /// </summary>
    protected static bool IsOnLocalCooldown()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return false; // Server handles it
        return PacketUtilities.IsLocalPlayerOnCooldown();
    }

    /// <summary>
    /// Checks if the cursor is currently interacting with UI in a way that should
    /// block wand tile-interaction. Blocks when:
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>Main.mapFullscreen</c> — the full-screen map is open.
    ///   </description></item>
    ///   <item><description>
    ///     <c>IsCursorOverPanel()</c> — cursor is physically over a wand settings panel.
    ///   </description></item>
    ///   <item><description>
    ///     <c>Main.LocalPlayer.mouseInterface</c> when it is set by something OTHER
    ///     than our own settings panel. This catches minimap drag, NPC shop, sign
    ///     edit, inventory screen, etc. We distinguish this by only calling the
    ///     UpdateUI path when cursor is over the panel (see WandUISystem.UpdateUI),
    ///     so mouseInterface is NOT set by our panel when the cursor is on tiles.
    ///     Vanilla already sets mouseInterface=true when the inventory is open,
    ///     so there is no need to check Main.playerInventory separately.
    ///   </description></item>
    /// </list>
    /// </summary>
    protected static bool IsMouseOverUI()
    {
        if (Main.mapFullscreen) return true;
        if (ModContent.GetInstance<WandUISystem>()?.IsCursorOverPanel() ?? false) return true;
        if (Main.LocalPlayer.mouseInterface) return true;
        return false;
    }

    public override bool CanRightClick() => true;

    public override void RightClick(Player player)
    {
        // Inventory right-click always cycles mode — never cancels selection.
        // Selection cancellation is handled by right-click while HOLDING the wand
        // (via CanUseItem/AltFunctionUse), not via inventory interaction.
        // Cancelling selection on mode-cycle was frustrating: the user just wants
        // to switch from Instant to Confirm without redoing their selection.

        // Cycle to next mode
        int nextType = GetNextModeItemType();
        int stack = Item.stack;
        bool wasFavorited = Item.favorited;
        
        Item.SetDefaults(nextType);
        Item.stack = stack + 1; // +1 because right-click consumes one
        Item.favorited = wasFavorited;

        // After SetDefaults, Item.ModItem is the new wand instance.
        // Read ModeSuffix from it so the message names the mode we just switched TO,
        // not the one we switched FROM (which is what 'this.ModeSuffix' would return
        // since 'this' still refers to the old class instance).
        string newModeSuffix = (Item.ModItem as BaseCyclingWand)?.ModeSuffix ?? ModeSuffix;
        Color newModeColor = (Item.ModItem as BaseCyclingWand)?.ModeColor ?? ModeColor;
        Main.NewText(Get("SwitchedToMode", newModeSuffix), newModeColor);
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
            SelectionMode.OneClick => Get("ModeInstant"),
            SelectionMode.TwoClick => Get("ModeSelect"),
            SelectionMode.ThreeClick => Get("ModeConfirm"),
            SelectionMode.FourClick => Get("ModeStamp"),
            _ => ""
        };
        
        tooltips.Add(new TooltipLine(Mod, "ModeInfo", $"[c/{hexColor}:Mode: {ModeSuffix}]"));
        tooltips.Add(new TooltipLine(Mod, "ModeDesc", modeDescription));
        tooltips.Add(new TooltipLine(Mod, "CycleHint", Get("CycleHint")));

        // ── Pickaxe power hint for wands that interact with tiles ──────
        if (WandBaseName is "Wand of Building" or "Wand of Dismantling" or "Wand of Replacement")
        {
            tooltips.Add(new TooltipLine(Mod, "PickaxeHint",
                $"[c/888888:{Get("PickaxeHint")}]"));
        }

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

            tooltips.Add(new TooltipLine(Mod, "ObtainHint", Get("ObtainHint")));
        }

        // ── Lore (Shift-gated) ──────────────────────────────
        var config = ModContent.GetInstance<WandClientConfig>();
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