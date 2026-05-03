using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
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

namespace WorldShapingWandsMod.Content.Items;

// ════════════════════════════════════════════════════════════════════
//  WandOfDelimitationBase — shared logic for all Select Wand modes
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Base class for the Wand of Delimitation family. Instead of executing tile operations,
/// this wand applies geometric shapes to the <see cref="SelectionCanvas"/> or
/// <see cref="TileSelection"/> using boolean operations (Add, Remove, Intersect, XOR, etc.).
/// </summary>
/// <remarks>
/// <para>
/// The Select Wand reuses the same selection-state mechanics as other wands (start tile,
/// end tile, lock) but its "execution" is purely a data operation — modifying the canvas
/// or selection on <see cref="DelimitationWandPlayer"/>.
/// </para>
/// <para>
/// In <see cref="DelimitationWandMode.Selection"/> mode, shape tiles are applied to the
/// <see cref="TileSelection"/> (clipped to the canvas). If no canvas exists and
/// <see cref="DelimitationWandSettings.AutoCreateCanvas"/> is enabled, the first shape
/// automatically becomes the canvas.
/// </para>
/// <para>
/// In <see cref="DelimitationWandMode.CanvasEdit"/> mode, shape tiles are applied to the
/// <see cref="SelectionCanvas"/> itself. The <see cref="TileSelection"/> is then
/// clipped to the new canvas bounds.
/// </para>
/// </remarks>
public abstract class WandOfDelimitationBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Delimitation/{Name}";
    public override string WandBaseName => "Wand of Delimitation";

    /// <summary>
    /// Selection wands have their own lore — they don't share the divine creation lore.
    /// </summary>
    public override string WandLore => Get("LoreDelimitation");
    public override bool ShowDivineLore => true;

    // ── Template Method Hooks ──
    protected override WandFamily Family => WandFamily.Delimitation;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ──
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var dwp = Main.LocalPlayer.GetModPlayer<DelimitationWandPlayer>();
        var settings = dwp.Settings;

        // Signal canvas creation when auto-create is on and no canvas exists
        if (settings.AutoCreateCanvas && !dwp.Canvas.IsActive)
            return WandAction.DelimitationNewCanvas;

        bool isCanvas = settings.Mode == DelimitationWandMode.CanvasEdit;

        return (isCanvas, settings.Operation) switch
        {
            (true,  SelectionOperation.Add)       => WandAction.DelimitationCanvasAdd,
            (true,  SelectionOperation.Remove)     => WandAction.DelimitationCanvasRemove,
            (true,  SelectionOperation.Intersect)  => WandAction.DelimitationCanvasIntersect,
            (true,  SelectionOperation.XOR)        => WandAction.DelimitationCanvasXOR,
            (false, SelectionOperation.Add)        => WandAction.DelimitationSelectionAdd,
            (false, SelectionOperation.Remove)     => WandAction.DelimitationSelectionRemove,
            (false, SelectionOperation.Intersect)  => WandAction.DelimitationSelectionIntersect,
            (false, SelectionOperation.XOR)        => WandAction.DelimitationSelectionXOR,
            _ => WandAction.DelimitationCanvasAdd, // Clear/Invert fallback
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.Wood, 10)
            .AddCustomShimmerResult(ItemID.CopperBar, 5)
            .AddCustomShimmerResult(ItemID.Amethyst, 3)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);
    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteSelectionOperation(player, wandPlayer);
    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.Player.GetModPlayer<DelimitationWandPlayer>().Settings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        wandPlayer.CancelSelection(GetCancelColor(), swp.Settings.Shape);
    }

    public override bool? UseItem(Player player) => TemplateUseItem(player);
    public override void HoldItem(Player player) => TemplateHoldItem(player);

    // ════════════════════════════════════════════════════════════════
    //  Core Execution — apply shape to canvas or selection
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates shape tiles from the current wand selection and applies them
    /// to the canvas or tile selection based on the current <see cref="DelimitationWandMode"/>.
    /// </summary>
    protected void ExecuteSelectionOperation(Player player, WandPlayer wandPlayer)
    {
        var swp = player.GetModPlayer<DelimitationWandPlayer>();

        // Null-slot guard: no active slot means delimitation is disabled.
        // Show a hint and abort rather than silently no-oping.
        if (!swp.IsActive)
        {
            Main.NewText(Get("NoActiveSlot"), Color.Gray);
            return;
        }

        var settings = swp.Settings;

        // Generate shape tiles from the wand's selection state
        var shapeTiles = GenerateShapeTiles(wandPlayer, settings);
        if (shapeTiles == null || shapeTiles.Length == 0)
        {
            Main.NewText(Get("NoTilesInShape"), Color.Gray);
            return;
        }

        if (settings.Mode == DelimitationWandMode.CanvasEdit)
        {
            ExecuteCanvasOperation(swp, settings, shapeTiles);
        }
        else
        {
            ExecuteSelectionOperation(swp, settings, shapeTiles);
        }

        // Audio feedback
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f }, player.Center);
    }

    /// <summary>
    /// Generates the filled shape tiles from the wand player's visual selection,
    /// using the Select Wand's own shape settings.
    /// </summary>
    private static Point[] GenerateShapeTiles(WandPlayer wandPlayer, DelimitationWandSettings settings)
    {
        var selection = wandPlayer.GetVisualSelection();
        if (!selection.IsActive)
            return null;

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        return settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);
    }

    /// <summary>
    /// Applies the current <see cref="CanvasOperation"/> (mapped from <see cref="SelectionOperation"/>)
    /// to the canvas, then clips the selection to the new canvas bounds.
    /// </summary>
    private static void ExecuteCanvasOperation(
        DelimitationWandPlayer swp, DelimitationWandSettings settings, Point[] shapeTiles)
    {
        var canvasOp = ToCanvasOp(settings.Operation);
        int beforeCount = swp.Canvas.Count;
        swp.Canvas.ApplyOperation(shapeTiles, canvasOp);
        int afterCount = swp.Canvas.Count;

        // Clip selection to new canvas bounds
        swp.Selection.ClipToCanvas(swp.Canvas);

        string opName = canvasOp.ToString();
        int delta = Math.Abs(afterCount - beforeCount);
        Main.NewText($"Canvas {opName}: {delta} tiles ({afterCount} total)", Color.Gold);
    }

    /// <summary>
    /// Applies the current <see cref="SelectionOperation"/> to the tile selection,
    /// auto-creating the canvas if needed.
    /// </summary>
    private static void ExecuteSelectionOperation(
        DelimitationWandPlayer swp, DelimitationWandSettings settings, Point[] shapeTiles)
    {
        // Auto-create canvas from first shape if no canvas exists
        if (!swp.Canvas.IsActive && settings.AutoCreateCanvas)
        {
            swp.Canvas.ApplyOperation(shapeTiles, CanvasOperation.Add);
            Main.NewText($"Canvas created ({swp.Canvas.Count} tiles)", Color.Gold);
        }

        int beforeCount = swp.Selection.Count;
        swp.Selection.ApplyOperation(shapeTiles, settings.Operation, swp.Canvas);
        int afterCount = swp.Selection.Count;

        string opName = settings.Operation.ToString();
        int delta = Math.Abs(afterCount - beforeCount);
        Main.NewText($"Selection {opName}: {delta} tiles ({afterCount} total)", Color.Cyan);
    }

    /// <summary>
    /// Maps a <see cref="SelectionOperation"/> to its equivalent <see cref="CanvasOperation"/>.
    /// Operations not applicable to canvas (Intersect, XOR, Invert) map to Add.
    /// </summary>
    private static CanvasOperation ToCanvasOp(SelectionOperation op) => op switch
    {
        SelectionOperation.Add => CanvasOperation.Add,
        SelectionOperation.Remove => CanvasOperation.Remove,
        SelectionOperation.Clear => CanvasOperation.Clear,
        _ => CanvasOperation.Add, // Intersect, XOR, Invert → Add for canvas
    };
}

// ════════════════════════════════════════════════════════════════════
//  WandOfDelimitationInstant — OneClick (Instant) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Instant (OneClick) mode for the Wand of Delimitation.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfDelimitationInstant : WandOfDelimitationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 215, 0);  // Gold — Instant
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDelimitationSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfDelimitationInstant>();
}

// ════════════════════════════════════════════════════════════════════
//  WandOfDelimitationSelect — TwoClick (Select) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Select (TwoClick) mode for the Wand of Delimitation.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfDelimitationSelect : WandOfDelimitationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 235, 100);  // Bright gold — Select
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDelimitationConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfDelimitationInstant>();
}

// ════════════════════════════════════════════════════════════════════
//  WandOfDelimitationConfirm — ThreeClick (Confirm) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Confirm (ThreeClick) mode for the Wand of Delimitation.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfDelimitationConfirm : WandOfDelimitationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(218, 165, 32);  // Goldenrod — Confirm
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDelimitationStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfDelimitationInstant>();
}

// ════════════════════════════════════════════════════════════════════
//  WandOfDelimitationStamp — FourClick (Stamp) mode
// ════════════════════════════════════════════════════════════════════

/// <summary>
/// Stamp (FourClick) mode for the Wand of Delimitation.
/// All mode-specific input logic lives in BaseCyclingWand's template methods.
/// </summary>
public class WandOfDelimitationStamp : WandOfDelimitationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new Color(184, 134, 11);  // Dark goldenrod — Stamp
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDelimitationInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfDelimitationInstant>();
}
