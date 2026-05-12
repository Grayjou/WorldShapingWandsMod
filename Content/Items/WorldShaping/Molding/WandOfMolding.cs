using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Items;
// ════════════════════════════════════════════════════════════════════
//  WandOfMoldingBase — shared logic for all Molding Wand modes
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Base class for the Wand of Molding family. Instead of executing tile operations,
/// this wand applies geometric shapes to its own canvas/selection system, and the
/// resulting selection is automatically promoted to a <see cref="CustomShape"/> that
/// all Stamp wands can use immediately.
/// </summary>
/// <remarks>
/// <para>
/// The Molding Wand is the user's original vision for "shape-crafting": build a
/// custom shape from geometric primitives (circles, rectangles, etc.) using boolean
/// operations (union, subtract, intersect, XOR), then stamp that shape with any wand.
/// </para>
/// <para>
/// Unlike the Wand of Delimitation (which creates a filter/boundary for other wands),
/// the Molding Wand's output is a <see cref="CustomShape"/> — a portable, reusable
/// set of tile offsets that Stamp wands consume via <c>CustomShape.GetTilesAt()</c>.
/// </para>
/// <para>
/// The canvas/selection mechanics are identical to Delimitation: canvas constrains
/// the working area, selection accumulates the mold shape. The "execute" step
/// auto-promotes the selection to a CustomShape (no explicit "Promote" button needed).
/// </para>
/// </remarks>
public abstract class WandOfMoldingBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Molding/{Name}";
    public override string WandBaseName => "Wand of Molding";

    public override string WandLore => Get("LoreMolding");
    public override bool ShowDivineLore => true;

    // ── Hint cooldown ────────────────────────────────────────────────
    // Prevents hint spam — at most one contextual hint every 3 seconds.

    /// <summary>
    /// Distance threshold (in tiles) beyond which the "too far from canvas"
    /// hint is shown instead of the "switch mode" hint.
    /// In DEBUG builds, live-tunable via <c>/dev set Molding.HintDistance</c>.
    /// </summary>
    private const float DefaultHintDistanceThreshold = 30f;

    /// <summary>Minimum frames between successive contextual hints (3 seconds).
    /// In DEBUG builds, live-tunable via <c>/dev set Molding.HintCooldown</c>.</summary>
    private const int DefaultHintCooldownFrames = 180;

    // ── Template Method Hooks ──
    protected override WandFamily Family => WandFamily.Molding;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ──
    protected override bool UseWandActionProjectile => true;

    /// <summary>
    /// When the Mold shape is active, the shape is fully defined by its stored tile data —
    /// only an anchor point is needed. Collapse TwoClick/ThreeClick/FourClick definition
    /// to a single click so the user places one anchor and the operation fires immediately.
    /// </summary>
    protected override bool IsDefinitionImmediate
    {
        get
        {
            var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
            return mwp?.Settings.Shape.Shape == Common.Enums.ShapeType.Mold;
        }
    }

    protected override WandAction ResolveCurrentAction()
    {
        var mwp = Main.LocalPlayer.GetModPlayer<MoldingWandPlayer>();
        var settings = mwp.Settings;

        // Signal canvas creation when auto-create is on and no canvas exists
        if (settings.AutoCreateCanvas && !mwp.Canvas.IsActive)
            return WandAction.MoldingNewCanvas;

        bool isCanvas = settings.Mode == MoldingWandMode.CanvasEdit;

        return (isCanvas, settings.Operation) switch
        {
            (true,  SelectionOperation.Add)       => WandAction.MoldingCanvasAdd,
            (true,  SelectionOperation.Remove)     => WandAction.MoldingCanvasRemove,
            (true,  SelectionOperation.Intersect)  => WandAction.MoldingCanvasIntersect,
            (true,  SelectionOperation.XOR)        => WandAction.MoldingCanvasXOR,
            (false, SelectionOperation.Add)        => WandAction.MoldingSelectionAdd,
            (false, SelectionOperation.Remove)     => WandAction.MoldingSelectionRemove,
            (false, SelectionOperation.Intersect)  => WandAction.MoldingSelectionIntersect,
            (false, SelectionOperation.XOR)        => WandAction.MoldingSelectionXOR,
            _ => WandAction.MoldingCanvasAdd, // Clear/Invert fallback
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.SandBlock, 50)
            .AddCustomShimmerResult(ItemID.IronBar, 10)
            .AddCustomShimmerResult(ItemID.ClayBlock, 10)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCustomShimmerResult(ItemID.Gel, 50);
    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteMoldingOperation(player, wandPlayer);
    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.Player.GetModPlayer<MoldingWandPlayer>().Settings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        wandPlayer.CancelSelection(GetCancelColor(), mwp.Settings.Shape);
    }

    public override bool? UseItem(Player player)
    {
        if (ShouldInterceptTransformWorldAction(player))
            return false;
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        if (IsTransformWorldActionArmed(player))
            return;
        TemplateHoldItem(player);
    }

    private static bool IsTransformWorldActionArmed(Player player)
    {
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        if (mwp?.Settings == null || !mwp.Settings.TransformModeEnabled || mwp.Settings.ActiveTransformAction == TransformActionMode.None)
            return false;

        if (IsMouseOverUI())
            return false;

        return true;
    }

    private static bool ShouldInterceptTransformWorldAction(Player player)
    {
        if (!IsTransformWorldActionArmed(player))
            return false;

        if (Main.myPlayer != player.whoAmI)
            return true;

        if (!Main.mouseLeft || Main.LocalPlayer.mouseInterface)
            return true;

        if (!Main.mouseLeftRelease)
            return true;

        Main.mouseLeftRelease = false;

        var mwp = player.GetModPlayer<MoldingWandPlayer>();

        return HandleTransformWorldAction(player, mwp);
    }

    private static bool HandleTransformWorldAction(Player player, MoldingWandPlayer mwp)
    {
        var settings = mwp.Settings;
        Point mouseTile = GeometryHelper.GetMouseTile();

        switch (settings.ActiveTransformAction)
        {
            case TransformActionMode.SetPivotPersistent:
                settings.PersistentPivot = mouseTile;
                settings.PendingTransformMoveStart = null;
                if (Main.myPlayer == player.whoAmI)
                    Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotPersistentHint"), WandColors.MsgInfo);
                return true;

            case TransformActionMode.SetPivotTemporary:
                settings.TemporaryPivot = mouseTile;
                settings.PendingTransformMoveStart = null;
                if (Main.myPlayer == player.whoAmI)
                    Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotTemporaryHint"), WandColors.MsgInfo);
                return true;

            case TransformActionMode.Move:
                if (!mwp.Canvas.IsActive && !mwp.Selection.IsActive)
                    return true;

                if (!settings.PendingTransformMoveStart.HasValue)
                {
                    settings.PendingTransformMoveStart = mouseTile;
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.MoveStartHint"), WandColors.MsgInfo);
                    return true;
                }

                Point start = settings.PendingTransformMoveStart.Value;
                int dx = mouseTile.X - start.X;
                int dy = mouseTile.Y - start.Y;
                settings.PendingTransformMoveStart = null;

                if (dx == 0 && dy == 0)
                    return true;

                if (mwp.Canvas.IsActive)
                    mwp.Canvas.Translate(dx, dy);

                if (mwp.Selection.IsActive)
                    mwp.Selection.Translate(dx, dy);

                if (settings.PersistentPivot.HasValue)
                    settings.PersistentPivot = new Point(settings.PersistentPivot.Value.X + dx, settings.PersistentPivot.Value.Y + dy);

                if (settings.TemporaryPivot.HasValue)
                    settings.TemporaryPivot = new Point(settings.TemporaryPivot.Value.X + dx, settings.TemporaryPivot.Value.Y + dy);

                if (Main.myPlayer == player.whoAmI)
                    Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.MoveHint"), WandColors.MsgInfo);
                return true;
        }

        return false;
    }

    // ════════════════════════════════════════════════════════════════
    //  Core Execution — apply shape to canvas/selection, then auto-promote
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates shape tiles from the current wand selection and applies them
    /// to the canvas or mold selection. After application, auto-promotes the
    /// selection to a <see cref="CustomShape"/> if enabled.
    /// </summary>
    protected void ExecuteMoldingOperation(Player player, WandPlayer wandPlayer)
    {
        var mwp = player.GetModPlayer<MoldingWandPlayer>();
        var settings = mwp.Settings;

        var shapeTiles = StencilOperationEngine.BuildOperandTiles(wandPlayer, settings.Shape);
        if (shapeTiles.Count == 0)
        {
            Main.NewText(Get("NoTilesInShape"), Color.Gray);
            return;
        }

        if (settings.Mode == MoldingWandMode.CanvasEdit)
        {
            ExecuteCanvasOperation(mwp, settings, shapeTiles);
        }
        else
        {
            ExecuteSelectionOperation(mwp, settings, shapeTiles);
        }

        // Auto-promote: the selection immediately becomes the custom shape
        if (mwp.AutoPromote && mwp.Selection.IsActive)
        {
            if (mwp.PromoteMoldToCustomShape())
            {
                Main.NewText(
                    $"Mold updated: {mwp.MoldedShape.Count:N0} tiles — ready for Stamp wands",
                    new Color(0, 220, 220));
            }
        }

        // Audio feedback
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f }, player.Center);
    }

    private static void ExecuteCanvasOperation(
        MoldingWandPlayer mwp, MoldingWandSettings settings, HashSet<Point> shapeTiles)
    {
        int beforeCount = mwp.Canvas.Count;

        var slot = BuildStencilSlotState(mwp);
        StencilOperationEngine.ExecuteCanvasOperation(slot, shapeTiles, settings.Operation);
        ApplyStencilSlotState(mwp, slot);

        int afterCount = mwp.Canvas.Count;
        string opName = StencilOperationEngine.ToCanvasOperation(settings.Operation).ToString();
        int delta = Math.Abs(afterCount - beforeCount);
        Main.NewText($"Mold Canvas {opName}: {delta} tiles ({afterCount} total)", new Color(0, 200, 200));
    }

    /// <summary>
    /// Applies the operation to the mold selection, auto-creating canvas if needed.
    /// When zero cells are changed, shows a contextual hint to guide the user.
    /// </summary>
    private static void ExecuteSelectionOperation(
        MoldingWandPlayer mwp, MoldingWandSettings settings, HashSet<Point> shapeTiles)
    {
        bool hadCanvas = mwp.Canvas.IsActive;
        var slot = BuildStencilSlotState(mwp);

        int beforeCount = mwp.Selection.Count;
        StencilOperationEngine.ExecuteSelectionOperation(slot, shapeTiles, settings.Operation, settings.AutoCreateCanvas);
        ApplyStencilSlotState(mwp, slot);

        if (!hadCanvas && mwp.Canvas.IsActive)
            Main.NewText($"Mold canvas created ({mwp.Canvas.Count} tiles)", new Color(0, 200, 200));

        int afterCount = mwp.Selection.Count;

        string opName = settings.Operation.ToString();
        int delta = Math.Abs(afterCount - beforeCount);

        if (delta == 0)
        {
            ShowMoldingHint(mwp);
        }
        else
        {
            Main.NewText($"Mold {opName}: {delta} tiles ({afterCount} total)", new Color(0, 220, 220));
        }
    }

    private static StencilSlotState BuildStencilSlotState(MoldingWandPlayer mwp)
    {
        var slot = new StencilSlotState();
        slot.SetCanvas(mwp.Canvas.Tiles);
        slot.SetSelection(mwp.Selection.Tiles);
        slot.CustomShape = mwp.MoldedShape;
        return slot;
    }

    private static void ApplyStencilSlotState(MoldingWandPlayer mwp, StencilSlotState slot)
    {
        mwp.Canvas.Clear();
        if (slot.CanvasCount > 0)
            mwp.Canvas.ApplyOperation(slot.CanvasTiles, CanvasOperation.Add);

        mwp.Selection.Clear();
        if (slot.SelectionCount > 0)
            mwp.Selection.ApplyOperation(slot.SelectionTiles, SelectionOperation.Add, mwp.Canvas);

        mwp.ActiveStencil.MoldedShape = slot.CustomShape;
    }

    /// <summary>
    /// Shows a contextual hint when a molding selection operation produced zero
    /// changes. Rate-limited to one hint every <see cref="HintCooldownFrames"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Far from canvas</b> (> <see cref="HintDistanceThreshold"/> tiles from
    /// the nearest edge of the canvas bounding box): suggests using
    /// "Teleport Canvas to Player" button.
    /// </para>
    /// <para>
    /// The distance check uses the <em>minimum distance from the player to the
    /// canvas AABB</em> — not the centroid. This prevents false "too far" hints
    /// when the player is standing near one edge of a large canvas whose center
    /// of mass happens to be far away.
    /// </para>
    /// <para>
    /// <b>Near canvas, Selection mode</b>: suggests switching to Canvas Edit Mode
    /// so the canvas can be expanded first.
    /// </para>
    /// <para>
    /// Falls back to a generic "no change" message when no specific hint applies
    /// (e.g., identical Add on existing tiles, or Remove with nothing to remove).
    /// </para>
    /// </remarks>
    private static void ShowMoldingHint(MoldingWandPlayer mwp)
    {
        // Rate-limit: skip if cooldown hasn't elapsed
        int now = (int)Main.GameUpdateCount;
        int cooldown = DefaultHintCooldownFrames;
        float distThreshold = DefaultHintDistanceThreshold;
        if (now - mwp.LastHintTick < cooldown)
            return;

        if (mwp.Canvas.IsActive)
        {
            // Minimum distance from the player to the nearest edge of the
            // canvas bounding box (AABB).  O(1) — BoundingBox is already
            // cached and updated on every canvas modification.
            float dist = DistanceToCanvasAABB(mwp.Player, mwp.Canvas.BoundingBox);

            if (dist > distThreshold)
            {
                // Far from canvas — suggest teleporting it closer
                Main.NewText(Get("MoldingHintTooFar"), WandColors.MsgHint);
                mwp.LastHintTick = now;
                return;
            }

            if (mwp.Settings.Mode == MoldingWandMode.Selection)
            {
                // Near canvas but shape tiles fell outside it — suggest expanding
                Main.NewText(Get("MoldingHintExpandCanvas"), WandColors.MsgHint);
                mwp.LastHintTick = now;
                return;
            }
        }

        // Fallback: generic "no change" (e.g., adding tiles already selected)
        Main.NewText(Get("MoldingNoChange"), WandColors.MsgHint);
        mwp.LastHintTick = now;
    }

    /// <summary>
    /// Computes the minimum distance (in tiles) from the player's tile position
    /// to the nearest edge of an axis-aligned bounding box.
    /// Returns 0 when the player is inside or on the edge of the AABB.
    /// </summary>
    /// <remarks>
    /// Uses the standard point-to-AABB distance formula:
    /// <c>dx = max(minX - px, 0, px - maxX)</c>,
    /// <c>dy = max(minY - py, 0, py - maxY)</c>,
    /// <c>dist = sqrt(dx² + dy²)</c>.
    /// The <see cref="SelectionCanvas.BoundingBox"/> is already recalculated
    /// on every canvas modification, so this is O(1) with no iteration.
    /// </remarks>
    private static float DistanceToCanvasAABB(Player player, Rectangle bbox)
    {
        // Player position in tile coordinates
        float px = player.Center.X / 16f;
        float py = player.Center.Y / 16f;

        // AABB edges in tile coordinates
        // Rectangle.Right = X + Width, which is maxX + 1 (exclusive).
        // We use (Right - 1) + 1 = Right for the max edge in tile-center
        // coords, but since tiles span [X, X+1), using X and Right directly
        // as the inclusive interval for the continuous AABB is correct.
        float minX = bbox.X;
        float maxX = bbox.X + bbox.Width;  // exclusive right edge
        float minY = bbox.Y;
        float maxY = bbox.Y + bbox.Height; // exclusive bottom edge

        float dx = Math.Max(minX - px, Math.Max(0f, px - maxX));
        float dy = Math.Max(minY - py, Math.Max(0f, py - maxY));

        return MathF.Sqrt(dx * dx + dy * dy);
    }

}

// ════════════════════════════════════════════════════════════════════
//  WandOfMoldingInstant — OneClick (Instant) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Instant (OneClick) mode for the Wand of Molding.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfMoldingInstant : WandOfMoldingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(0, 220, 220);  // Teal — Instant
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfMoldingSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.SandBlock, 50)
            .AddIngredient(ItemID.WaterBucket, 1)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddRecipeGroup(nameof(ItemID.IronBar), 10)
            .AddIngredient(ItemID.ClayBlock, 10)
            .AddIngredient(ItemID.Gel, 50)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

// ════════════════════════════════════════════════════════════════════
//  WandOfMoldingSelect — TwoClick (Select) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Select (TwoClick) mode for the Wand of Molding.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfMoldingSelect : WandOfMoldingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(50, 235, 235);  // Bright teal — Select
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfMoldingConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfMoldingInstant>();
}

// ════════════════════════════════════════════════════════════════════
//  WandOfMoldingConfirm — ThreeClick (Confirm) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Molding.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfMoldingConfirm : WandOfMoldingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(0, 180, 180);  // Dark teal — Confirm
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfMoldingStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfMoldingInstant>();
}

// ════════════════════════════════════════════════════════════════════
//  WandOfMoldingStamp — FourClick (Stamp) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Stamp (FourClick) mode for the Wand of Molding.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfMoldingStamp : WandOfMoldingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new Color(0, 140, 140);  // Deepest teal — Stamp
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfMoldingInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfMoldingInstant>();
}
