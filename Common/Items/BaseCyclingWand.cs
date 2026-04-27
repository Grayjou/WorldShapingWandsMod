using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Content.Projectiles;
using WorldShapingWandsMod.Content.Projectiles.WandModes;
using WorldShapingWandsMod.Content.Projectiles.WandActions;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.Items;



public abstract class BaseCyclingWand : ModItem
{
    public abstract SelectionMode WandSelectionMode { get; }
    public abstract string WandBaseName { get; }
    public abstract Color ModeColor { get; }

    // ────────────────────────────────────────────────────────────
    //  Sprite Animation Constants
    // ────────────────────────────────────────────────────────────
    // Spritesheet wands have 4 vertical frames with 2px bottom padding (Terraria standard).
    // Animation: 4 frames over 12 ticks = 3 ticks per frame ≈ 5 FPS cycle.
    private const int AnimFrameCount = 4;
    private const int AnimTicksPerCycle = 12;
    private const int AnimTicksPerFrame = AnimTicksPerCycle / AnimFrameCount; // 3

    private const int DefaultStampChannelFrames = 100;

    private const int DefaultStampRepeatFrames = 3;

    public abstract int GetNextModeItemType();

    /// <summary>
    /// The wand family this item belongs to. Override in each family base.
    /// Used for per-family overlay colors, cancel colors, and projectile dispatch.
    /// </summary>
    protected virtual WandFamily Family => WandFamily.Unknown;

    // ══ WandActionProjectile System (parallel to legacy BaseModeProjectile) ══

    /// <summary>
    /// When true, this wand uses the new WandActionProjectile system instead of
    /// the legacy BaseModeProjectile. Override in family bases to opt in.
    /// </summary>
    protected virtual bool UseWandActionProjectile => false;

    /// <summary>
    /// Returns the current WandAction for this wand based on its active sub-action.
    /// Default implementation maps Family to its primary action.
    /// Override in family bases that have sub-actions (e.g., Building maps PlaceType).
    /// </summary>
    protected virtual WandAction ResolveCurrentAction() => Family switch
    {
        WandFamily.Building      => WandAction.BuildingSolid,
        WandFamily.Dismantling   => WandAction.Dismantling,
        WandFamily.Replacement   => WandAction.Replacement,
        WandFamily.Wiring        => WandAction.WiringAdd,
        WandFamily.Safekeeping   => WandAction.SafekeepingAdd,
        WandFamily.Coating       => WandAction.CoatingPaintTile,
        WandFamily.Fluids        => WandAction.FluidPlace,
        WandFamily.Torches       => WandAction.TorchPlace,
        WandFamily.Delimitation  => WandAction.DelimitationCanvasAdd,
        WandFamily.Molding       => WandAction.MoldingCanvasAdd,
        _                        => WandAction.BuildingSolid,
    };

    /// <summary>
    /// Maps the current SelectionMode to its WandMode equivalent (spritesheet column).
    /// </summary>
    private WandMode ResolveCurrentMode() => WandSelectionMode switch
    {
        SelectionMode.OneClick   => WandMode.Instant,
        SelectionMode.TwoClick   => WandMode.Select,
        SelectionMode.ThreeClick => WandMode.Confirm,
        SelectionMode.FourClick  => WandMode.Stamp,
        _                        => WandMode.Instant,
    };

    /// <summary>
    /// Determines the wand family of the player's currently held item.
    /// Returns <see cref="WandFamily.Unknown"/> if the player is not holding a wand.
    /// </summary>
    public static WandFamily GetCurrentFamily(Player player)
    {
        return (player.HeldItem?.ModItem as BaseCyclingWand)?.Family ?? WandFamily.Unknown;
    }

    // ────────────────────────────────────────────────────────────
    //  Non-Instant Recipe Helpers
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <see cref="Recipe.AddCustomShimmerResult"/> entries that mirror
    /// the Instant wand's crafting ingredients. Override in each family base
    /// so non-instant modes can share a single definition.
    /// </summary>
    /// <returns>The same <paramref name="recipe"/> for fluent chaining.</returns>
    protected virtual Recipe AddInstantRecipeShimmerResults(Recipe recipe) => recipe;

    /// <summary>
    /// Registers the standard non-instant recipe: one Instant wand as ingredient,
    /// shimmer results matching the Instant recipe, <see cref="WandRecipeConditions.NonCraftable"/>
    /// condition, and <see cref="Recipe.Register"/>.
    /// Call this from <c>AddRecipes()</c> in every Select / Confirm / Stamp variant.
    /// </summary>
    protected void RegisterNonInstantRecipe<TInstant>() where TInstant : BaseCyclingWand
    {
        WandRecipeConditions.Register(Type);
        AddInstantRecipeShimmerResults(
            CreateRecipe()
                .AddIngredient<TInstant>(1))
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }

    /// <summary>
    /// Common lore line shared by all Wands of Creation.
    /// Resolved from localization key <c>Messages.LoreCommon</c>.
    /// </summary>
    private static string CommonLore => Get("LoreCommon");

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

        // NOTE: DrawAnimationVertical was previously registered here for Instant
        // and Stamp modes to animate spritesheet wands in the inventory. However,
        // it caused two problems:
        //   1. Flickering in inventory — frames cycle continuously even when idle.
        //   2. Static sprite when held — channeling wands reset itemAnimation each
        //      frame (player.itemAnimation = player.itemAnimationMax), so Terraria's
        //      animation system never advances.
        // The spritesheet sprites still exist (44×184, 4 frames + 2px padding) and
        // can be animated via a future projectile-based approach or manual PreDraw
        // override. For now, only the first frame is displayed (Terraria's default).

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
        var config = WandConfigs.Limits;
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

    // ── Stamp Channeling ───────────────────────────────────────────────
    /// <summary>
    /// Returns the number of frames the player must hold left-click before
    /// stamp channeling begins. Override in subclasses for wand-specific values
    /// (e.g., Coating uses a shorter channel time for the "Free Paint Picasso" experience).
    /// </summary>
    protected virtual int GetChannelFrames()
    {
        var config = WandConfigs.Stamp;
        return config?.StampChannelFrames ?? DefaultStampChannelFrames;
    }

    /// <summary>
    /// Returns the number of frames between repeat executions while channeling.
    /// </summary>
    protected virtual int GetRepeatFrames()
    {
        var config = WandConfigs.Stamp;
        return config?.StampRepeatFrames ?? DefaultStampRepeatFrames;
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

    // ── Alt-Use (Right-Click While Holding) ──────────────────────────────
    // Shared across ALL wand families: right-click cancels active selection,
    // or toggles the wand's settings UI if no selection is active.
    // Subclasses override CancelActiveSelection to provide wand-specific cancel color/shape.

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive)
            {
                CancelActiveSelection(player, wandPlayer);
            }
            else if (Main.myPlayer == player.whoAmI)
            {
                ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Cancels the active shape selection with wand-specific cancel color/shape.
    /// Override in each wand base to provide the correct shape settings.
    /// Default implementation uses the per-family cancel color and the wand player's generic settings.
    /// </summary>
    protected virtual void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), new Settings.ShapeInfo(
            wandPlayer.Settings.ShapeType, wandPlayer.Settings.ShapeMode,
            wandPlayer.Settings.Thickness, slice: wandPlayer.Settings.Slice));
    }

    // ────────────────────────────────────────────────────────────
    //  Template Method Pattern — Mode-Specific Base Classes
    // ────────────────────────────────────────────────────────────
    //
    // Family bases that opt into the template method pattern override these three
    // abstract members instead of duplicating mode-specific input boilerplate in
    // each concrete Instant/Select/Confirm/Stamp file.
    //
    // Family bases that have NOT been migrated yet continue to use the old pattern
    // (abstract HandleUseItem + per-mode HoldItem overrides) — fully compatible.
    //
    // Migration order (simplest first): Safekeeping, Wiring, Coating, Fluids,
    // Torches, Dismantling, Replacement, Building, Delimitation.
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the wand's core operation (e.g., protect tiles, place wires).
    /// Override in each family base. Only called by the default mode implementations.
    /// Family bases that haven't migrated to the template pattern should NOT override this.
    /// </summary>
    protected virtual void ExecuteWandOperation(Player player, WandPlayer wandPlayer) { }

    /// <summary>
    /// Returns the cancel color for this wand family, using the per-family hue-temperature rule.
    /// Relies on the Family property. Override only if a family needs a truly custom cancel color.
    /// </summary>
    protected virtual Color GetCancelColor() => WandColors.GetCancelColorForFamily(Family);

    /// <summary>
    /// Returns the shape info for this wand family from the player's settings.
    /// Used by the default mode implementations for cancel + shape preview.
    /// </summary>
    protected virtual ShapeInfo GetWandShape(WandPlayer wandPlayer) => new ShapeInfo(
        wandPlayer.Settings.ShapeType, wandPlayer.Settings.ShapeMode,
        wandPlayer.Settings.Thickness, slice: wandPlayer.Settings.Slice);

    /// <summary>
    /// Optional pre-execution guard. Return false to block the operation
    /// (e.g., Building checks HasTilesInInventory). Default: always execute.
    /// </summary>
    protected virtual bool CanExecute(Player player, WandPlayer wandPlayer) => true;

    /// <summary>
    /// Whether this family base uses the template method pattern for mode dispatch.
    /// Family bases that have been migrated override this to return true.
    /// When true, BaseCyclingWand provides default UseItem/HoldItem implementations
    /// that dispatch to ExecuteWandOperation based on WandSelectionMode.
    /// </summary>
    protected virtual bool UsesTemplateModeDispatch => false;

    /// <summary>
    /// Hook for family-specific HoldItem behavior (cursor icon, wire visibility, etc.).
    /// Called by the template HoldItem after the common cancel/selection logic.
    /// Only used when <see cref="UsesTemplateModeDispatch"/> is true.
    /// </summary>
    protected virtual void OnHoldItemFamily(Player player, WandPlayer wandPlayer) { }

    // ══ Wand Action Projectile Management ══════════════════════════════════════════
    // Spawns and sustains the new unified WandActionProjectile above the player's
    // head. Opt-in via UseWandActionProjectile. Passes WandAction + WandMode.

    /// <summary>
    /// Spawns or refreshes a <see cref="WandActionProjectile"/> above the player.
    /// Called from the same sites as <see cref="ManageModeProjectile"/> when
    /// <see cref="UseWandActionProjectile"/> is true.
    /// </summary>
    protected void ManageWandActionProjectile(Player player)
    {
        if (Main.myPlayer != player.whoAmI) return;

        WandAction action = ResolveCurrentAction();
        WandMode mode = ResolveCurrentMode();

        int projType = ModContent.ProjectileType<WandActionProjectile>();
        bool shouldShow = ShouldShowActionProjectile(player.GetModPlayer<WandPlayer>());
        // Look for an existing WandActionProjectile owned by this player
        int existing = -1;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile p = Main.projectile[i];
            if (p.active && p.owner == player.whoAmI && p.type == projType)
            {
                existing = i;
                break;
            }
        }

        if (shouldShow){
            if (existing >= 0)
            {
                
                // Refresh the existing projectile
                Projectile p = Main.projectile[existing];
                p.ai[1] = (float)action;
                p.localAI[0] = (float)mode;
                p.timeLeft = WandActionProjectile.GetModeLifetime(mode);
            }
            else
            {
                // Spawn a new one
                float bobSeed = Main.rand.NextFloat() * 100f;
                int idx = Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Top + new Microsoft.Xna.Framework.Vector2(0, -24),
                    Microsoft.Xna.Framework.Vector2.Zero,
                    projType,
                    0, 0f, player.whoAmI,
                    ai0: bobSeed, ai1: (float)action);

                if (idx >= 0 && idx < Main.maxProjectiles)
                    Main.projectile[idx].localAI[0] = (float)mode;
            }
        }
    }

    // ── Mode Projectile Management ──────────────────────────────────
    // Spawns and sustains the cosmetic mode indicator projectile above the player's
    // head. The projectile type is determined by WandSelectionMode, and the family
    // column index is passed via ai[1].

    /// <summary>
    /// Returns the projectile type ID for this wand's current selection mode.
    /// </summary>
    private int GetModeProjectileType() => WandSelectionMode switch
    {
        SelectionMode.OneClick   => ModContent.ProjectileType<InstantModeProjectile>(),
        SelectionMode.TwoClick   => ModContent.ProjectileType<SelectModeProjectile>(),
        SelectionMode.ThreeClick => ModContent.ProjectileType<ConfirmModeProjectile>(),
        SelectionMode.FourClick  => ModContent.ProjectileType<StampModeProjectile>(),
        _ => -1
    };

    /// <summary>
    /// Determines whether the mode indicator projectile should be visible for
    /// the current wand mode and selection state. Mode-specific rules:
    /// <list type="bullet">
    ///   <item><b>Instant</b>: Always shown while holding the wand.</item>
    ///   <item><b>Select</b>: Shown only when a selection is active (1st click done).
    ///     Signals "your next click will EXECUTE."</item>
    ///   <item><b>Confirm</b>: Shown only when the selection is locked (2nd click done).
    ///     Signals "your next click will CONFIRM and execute."</item>
    ///   <item><b>Stamp</b>: Shown only when the stamp is locked (3rd click done).
    ///     Signals "your next click/channel will STAMP."</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <b>Multi-Point Shape Compatibility (v2.0 note):</b>
    /// This method gates on <see cref="WandPlayer.SelectionClickStep"/> which tracks
    /// <i>semantic</i> boundaries (selection started / shape locked / stamp locked),
    /// NOT raw click counts. For multi-point shapes (Arc=3pts, Polygon=Npts),
    /// intermediate shape-defining clicks happen while clickStep remains at 1.
    /// The transition to clickStep=2 only fires when <see cref="SelectionState.IsShapeComplete"/>
    /// is true and <see cref="WandPlayer.LockSelection"/> is called.
    /// <para/>
    /// For v2.0 multi-point shapes in TwoClick mode, the condition should evolve to
    /// check <c>PointsPlaced &gt;= PointsRequired - 1</c> instead of <c>clickStep &gt;= 1</c>.
    /// See: <c>dev_notes/architecture/ModeProjectile_MultiPointCompatibility.md</c>
    /// </remarks>
    /// 
    private bool ShouldShowActionProjectile(WandPlayer wandPlayer)
    {
        // Action projectile is visible whenever the player is in a valid mode with an active selection.
        // It uses the same show-condition as the mode projectile but is only spawned when UseWandActionProjectile is true.
        return WandSelectionMode switch
        {
            SelectionMode.OneClick => true,
            SelectionMode.TwoClick => wandPlayer.Selection.IsActive && wandPlayer.SelectionClickStep >= 1,
            SelectionMode.ThreeClick => wandPlayer.Selection.IsActive && wandPlayer.Selection.IsLocked && wandPlayer.SelectionClickStep >= 2,
            SelectionMode.FourClick => wandPlayer.Selection.IsActive && wandPlayer.IsStampLocked && wandPlayer.SelectionClickStep >= 3,
            _ => false
        };
    }
    private bool ShouldShowModeProjectile(WandPlayer wandPlayer)
    {
        return WandSelectionMode switch
        {
            // Instant: always visible while held — the wand IS executing on hold
            SelectionMode.OneClick => true,

            // Select: visible after 1st click (selection active) — next click executes
            // v2.0 multi-point: will evolve to check PointsPlaced >= PointsRequired - 1
            SelectionMode.TwoClick => wandPlayer.Selection.IsActive
                                      && wandPlayer.SelectionClickStep >= 1,

            // Confirm: visible after shape complete + locked — next click confirms
            // (clickStep=2 only fires after ALL shape points are placed + LockSelection)
            SelectionMode.ThreeClick => wandPlayer.Selection.IsActive
                                        && wandPlayer.Selection.IsLocked
                                        && wandPlayer.SelectionClickStep >= 2,

            // Stamp: visible after stamp locked — next click/channel stamps
            // (clickStep=3 only fires after LockStamp, well after shape completion)
            SelectionMode.FourClick => wandPlayer.Selection.IsActive
                                       && wandPlayer.IsStampLocked
                                       && wandPlayer.SelectionClickStep >= 3,

            _ => false
        };
    }

    /// <summary>
    /// Manages the mode indicator projectile lifecycle. Called every frame from
    /// <see cref="TemplateHoldItem"/>. Spawn/refresh behavior is gated by
    /// <see cref="ShouldShowModeProjectile"/> — each mode has distinct rules
    /// about when the projectile should be visible based on selection state.
    /// <para>
    /// When the mode's show-condition is NOT met, any existing projectile is left
    /// to die naturally via its <c>timeLeft</c> timeout (graceful fade-out).
    /// When the wrong mode type is alive (e.g., after right-click mode cycle),
    /// it is killed immediately to avoid visual confusion.
    /// </para>
    /// </summary>
    private void ManageModeProjectile(Player player)
    {
        if (Main.myPlayer != player.whoAmI) return;

        int desiredType = GetModeProjectileType();
        if (desiredType < 0) return;

        int familyIndex = Family != WandFamily.Unknown
            ? (int)Family - 1   // 1-based enum → 0-based column
            : 0;                // Fallback to Building column

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        bool shouldShow = ShouldShowModeProjectile(wandPlayer);

        // Scan for existing mode projectiles owned by this player
        int existingIndex = -1;
        bool existingIsCorrectType = false;

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            if (!proj.active || proj.owner != player.whoAmI) continue;
            if (proj.ModProjectile is not BaseModeProjectile) continue;

            if (proj.type == desiredType)
            {
                // Correct type found
                existingIndex = i;
                existingIsCorrectType = true;
                break;
            }
            else
            {
                // Wrong mode type — kill it (mode was cycled)
                proj.Kill();
                // Don't break — there might be leftover duplicates
            }
        }

        if (existingIsCorrectType)
        {
            if (shouldShow)
            {
                // Refresh the existing projectile and update its family column
                var modeProj = (BaseModeProjectile)Main.projectile[existingIndex].ModProjectile;
                modeProj.Refresh();

                // Update family column in case the player switched wand families
                // while keeping the same mode (e.g., Building Select → Coating Select)
                Main.projectile[existingIndex].ai[1] = familyIndex;
            }
            // else: condition no longer met — let it die naturally via timeLeft fade-out
        }
        else if (shouldShow)
        {
            // Spawn a new one
            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Top,
                Vector2.Zero,
                desiredType,
                0, 0f,
                player.whoAmI,
                ai0: 0f,              // ai[0] = bobbing frame counter
                ai1: familyIndex);    // ai[1] = wand family column index (0-9)
        }
    }

    // ── Template UseItem (dispatches to HandleUseItem based on mode) ──
    // Family bases using the template pattern get this for free.

    /// <summary>
    /// Default UseItem implementation for families using the template pattern.
    /// Handles channeling keep-alive, UI blocking, compatibility checks, and
    /// dispatches to the appropriate mode handler.
    /// </summary>
    protected bool? TemplateUseItem(Player player)
    {
        // Keep the use-cycle alive while the mouse is held (channeling mode).
        if (WandSelectionMode == SelectionMode.OneClick && Item.channel)
        {
            player.itemAnimation = player.itemAnimationMax;
            return Main.mouseLeft ? false : true;
        }
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        // Clear incompatible selections.
        if (WandSelectionMode != SelectionMode.OneClick)
            wandPlayer.EnsureSelectionCompatibility(WandSelectionMode);

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.GetMouseTile();

        return WandSelectionMode switch
        {
            SelectionMode.OneClick => false, // All logic in HoldItem
            SelectionMode.TwoClick => TemplateHandleSelectMode(player, wandPlayer, mouseTile),
            SelectionMode.ThreeClick => TemplateHandleConfirmMode(player, wandPlayer, mouseTile),
            SelectionMode.FourClick => TemplateHandleStampMode(player, wandPlayer, mouseTile),
            _ => false
        };
    }

    private bool TemplateHandleSelectMode(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            if (UseWandActionProjectile) ManageWandActionProjectile(player); else ManageModeProjectile(player);
            Main.NewText(Get("SelectStartClickAgain", "apply"), WandColors.MsgPrompt);
            return false;
        }
        else
        {
            if (IsOnLocalCooldown()) return false;
            wandPlayer.UpdateSelection(mouseTile);
            if (CanExecute(player, wandPlayer))
                ExecuteWandOperation(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false;
        }
    }

    private bool TemplateHandleConfirmMode(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickEnd"), WandColors.MsgPrompt);
            return false;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            if (UseWandActionProjectile) ManageWandActionProjectile(player); else ManageModeProjectile(player);
            Main.NewText(Get("ClickToConfirmOrCancel"), WandColors.MsgConfirm);
            return false;
        }
        else
        {
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            if (IsOnLocalCooldown()) return false;
            if (CanExecute(player, wandPlayer))
                ExecuteWandOperation(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false;
        }
    }

    private bool TemplateHandleStampMode(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("StampClickEnd"), WandColors.MsgPrompt);
            return false;
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();
            Main.NewText(Get("StampClickLock"), WandColors.MsgConfirm);
            return false;
        }
        else if (!wandPlayer.IsStampLocked)
        {
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            wandPlayer.LockStamp(mouseTile);
            if (UseWandActionProjectile) ManageWandActionProjectile(player); else ManageModeProjectile(player);
            Main.NewText(Get("StampLocked", "apply"), Color.LimeGreen);
            return false;
        }
        else
        {
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            wandPlayer.MoveStampTo(mouseTile);

            if (StampChannelingHelper.HandleStampClick(wandPlayer, GetChannelFrames(), IsOnLocalCooldown()))
            {
                if (CanExecute(player, wandPlayer))
                    ExecuteWandOperation(player, wandPlayer);
            }
            return false;
        }
    }

    /// <summary>
    /// Default HoldItem implementation for families using the template pattern.
    /// Handles instant mode drag-selection and stamp channeling universally.
    /// </summary>
    protected void TemplateHoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();

        // Family-specific cancel on right-click in world
        if (!Main.LocalPlayer.mouseInterface
            && wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
            Main.mouseRightRelease = false;
        }

        // Family-specific HoldItem behavior (cursor icon, etc.)
        OnHoldItemFamily(player, wandPlayer);

        // Spawn/refresh the mode indicator projectile above the player's head.
        // Mode-aware: Instant=always, Select=after 1st click, Confirm=after 2nd click,
        // Stamp=after 3rd click. The projectile signals "your next action will EXECUTE."
        if (UseWandActionProjectile)
            ManageWandActionProjectile(player);
        else
            ManageModeProjectile(player);

        // Mode-specific input handling
        if (Main.myPlayer != player.whoAmI) return;

        if (WandSelectionMode == SelectionMode.OneClick)
        {
            TemplateInstantHoldItem(player, wandPlayer);
        }
        else if (WandSelectionMode == SelectionMode.FourClick)
        {
            TemplateStampHoldItem(player, wandPlayer);
        }
    }

    private void TemplateInstantHoldItem(Player player, WandPlayer wandPlayer)
    {
        Point mouseTile = GeometryHelper.GetMouseTile();

        if (Main.mouseLeft)
        {
            if (IsMouseOverUI()) return;
            if (!wandPlayer.CanStartNewSelection()) return;

            if (!wandPlayer.InstantSelection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartInstantSelection(mouseTile, vertical);
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }

            wandPlayer.UpdateInstantSelection(mouseTile);

            if (Main.mouseRight && wandPlayer.InstantSelection.IsActive)
            {
                wandPlayer.CancelInstantSelection(GetCancelColor(), GetWandShape(wandPlayer));
                return;
            }
        }
        else if (wandPlayer.InstantSelection.IsActive)
        {
            if (IsMouseOverUI())
            {
                wandPlayer.CancelInstantSelection(GetCancelColor(), GetWandShape(wandPlayer));
                return;
            }

            if (wandPlayer.IsInstantSelectionOwnedByCurrentItem() && !IsOnLocalCooldown())
            {
                if (CanExecute(player, wandPlayer))
                    ExecuteWandOperation(player, wandPlayer);
            }
            wandPlayer.ClearInstantSelection();
        }
    }

    private void TemplateStampHoldItem(Player player, WandPlayer wandPlayer)
    {
        if (!wandPlayer.IsStampLocked || !wandPlayer.Selection.IsActive)
            return;

        // Keep the wand visible in the player's hand while stamp-channeling.
        // Without this, the sprite disappears between UseItem ticks because
        // Terraria's itemAnimation naturally counts down to zero.
        if (player.controlUseItem)
            player.itemAnimation = player.itemAnimationMax;

        Point mouseTile = GeometryHelper.GetMouseTile();
        // W-S3-3 (S3 2026-04-24, per Cavendish Notes_PauseModeOverride.md, closes
        // S2 G-3): in Precise mode the stamp must FREEZE while the game is paused
        // — a frozen world should not have a moving UI overlay (GrayJou's S3
        // verbatim: "this is the only behavior that makes sense on a paused
        // game"). Smooth-mode pause behaviour also freezes now (W-S4-1, closes
        // S3 R-1) — the v3 UpdateSmoothAnchor self-gates against pause per
        // DesignDoc_StampSmoothingV3.md §3.4, so an always-call here is safe.
        var stampRenderMode = WandConfigs.Overlay?.StampRenderMode ?? StampRenderMode.Precise;
        if (!(Main.gamePaused && stampRenderMode == StampRenderMode.Precise))
            wandPlayer.MoveStampTo(mouseTile);

        // ── W-S4-1 (S4 2026-04-24, Cavendish DesignDoc_StampSmoothingV3.md §3.3) ──
        // Stamp Smoothing v3: feed the precise anchor (bbox-min + StampAnchorOffset,
        // multiplied by 16) into the WandPlayer's exponential-ease updater each
        // logic tick. The draw-time consumer reads SmoothAnchorWorld directly —
        // no easing happens at draw rate, so high-FPS playtests won't accumulate
        // drift. UpdateSmoothAnchor self-gates against pause and snaps on first
        // appearance; call unconditionally.
        if (wandPlayer.IsStampLocked && wandPlayer.Selection.IsActive)
        {
            var sel = wandPlayer.Selection;
            int bboxMinX = System.Math.Min(sel.StartTile.X, sel.EndTile.X);
            int bboxMinY = System.Math.Min(sel.StartTile.Y, sel.EndTile.Y);
            var anchorWorld = new Microsoft.Xna.Framework.Vector2(
                (bboxMinX + wandPlayer.StampAnchorOffset.X) * 16f,
                (bboxMinY + wandPlayer.StampAnchorOffset.Y) * 16f);
            wandPlayer.UpdateSmoothAnchor(anchorWorld);
        }

        if (StampChannelingHelper.UpdateChanneling(
                player, wandPlayer, GetChannelFrames(), GetRepeatFrames()))
        {
            // S5 2026-04-23 (W-1, Cavendish S7 Diagnosis_StampChannelingSpeed.md Issue B):
            // The `!IsOnLocalCooldown()` gate has been REMOVED from this channel-tick
            // path. `OperationCooldownTicks` (default 12) was silently capping the
            // effective channel rate when `StampRepeatFrames` was below it — repeats
            // raced the cooldown and dropped every other tick, halving the visible
            // channel speed at default settings (10 < 12 → ~20-frame cadence).
            //
            // The cooldown is anti-spam protection for the click path (single-fire
            // and instant-on-release branches still call IsOnLocalCooldown — see
            // L678/L708/L828). Channeling is intentionally repetitive and is throttled
            // by `StampRepeatFrames`; gating it on the click-cooldown was a defaults
            // collision, not a deliberate design.
            //
            // Issues A (cooldown function purity) and C (speculative consumption in
            // HandleStampClick) from Cavendish's diagnosis were intentionally left
            // OUT of scope per GrayJou's S8 narrowing.
            if (!IsTooFarToConfirm(wandPlayer.Selection, mouseTile)
                && CanExecute(player, wandPlayer))
            {
                ExecuteWandOperation(player, wandPlayer);
            }
        }

        int channelFrames = GetChannelFrames();
        StampChannelingHelper.EmitChannelingDust(wandPlayer, channelFrames, mouseTile);
        StampChannelingHelper.TryPlayChargeSound(wandPlayer, channelFrames);
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
        var config = WandConfigs.Preferences;
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
                    Get("LoreShiftHint")));
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

        // Use the inventory texture's own dimensions, NOT the main texture's frame.
        // The main texture may be a spritesheet (e.g. 44×184 for 4-frame wands)
        // while the inventory texture is always a single 44×44 sprite.
        var invFrame = new Rectangle(0, 0, invTex.Value.Width, invTex.Value.Height);
        var invOrigin = new Vector2(invFrame.Width / 2f, invFrame.Height / 2f);
        spriteBatch.Draw(invTex.Value, position, invFrame, drawColor, 0f, invOrigin, scale, SpriteEffects.None, 0f);
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