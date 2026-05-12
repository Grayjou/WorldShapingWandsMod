using System.Collections.Generic;
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
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfPlanificationBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Plannification/{Name}";
    public override string WandBaseName => "Wand of Planification";

    public override string WandLore => Msg.Get("LoreDelimitation");
    public override bool ShowDivineLore => true;

    protected override WandFamily Family => WandFamily.Planification;
    protected override bool UsesTemplateModeDispatch => true;

    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.Wood, 10)
            .AddCustomShimmerResult(ItemID.CopperBar, 5)
            .AddCustomShimmerResult(ItemID.Amethyst, 3)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecutePlanificationOperation(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.Player.GetModPlayer<PlanificationWandPlayer>().Settings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        wandPlayer.CancelSelection(GetCancelColor(), pwp.Settings.Shape);
    }

    public override bool? UseItem(Player player) => TemplateUseItem(player);

    public override void HoldItem(Player player) => TemplateHoldItem(player);

    protected virtual void ExecutePlanificationOperation(Player player, WandPlayer wandPlayer)
    {
        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        var settings = pwp.Settings;

        var selection = wandPlayer.GetVisualSelection();
        if (!selection.IsActive)
            return;

        var context = settings.Shape.ToShapeContext(
            selection.StartTile,
            selection.EndTile,
            selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var tiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        if (tiles == null || tiles.Length == 0)
        {
            Main.NewText(Msg.Get("NoTilesInShape"), Color.Gray);
            return;
        }

        int activeSlot = pwp.ActiveEditSlot;
        var operandTiles = new HashSet<Point>(tiles);
        var canvasTiles = pwp.GetSlotCanvasWorldTiles(activeSlot);

        if (settings.Mode == DelimitationWandMode.CanvasEdit)
        {
            pwp.ApplyCanvasOperationToSlot(activeSlot, operandTiles, settings.Operation);
            pwp.ClipSelectionToCanvas(activeSlot);
            var canvasCount = pwp.GetSlotCanvasWorldTiles(activeSlot).Count;
            Main.NewText($"Planification slot {activeSlot + 1}: Canvas {settings.Operation} ({canvasCount} total)", WandColors.MsgInfo);
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f }, player.Center);
            return;
        }

        if (canvasTiles.Count == 0)
        {
            if (!settings.AutoCreateCanvas)
            {
                Main.NewText("No canvas to mask against. Enable Auto Create Canvas or switch to Canvas Edit.", Color.Gray);
                return;
            }

            pwp.SetSlotCanvasShape(activeSlot, operandTiles);
            canvasTiles = pwp.GetSlotCanvasWorldTiles(activeSlot);
            Main.NewText($"Planification slot {activeSlot + 1}: Canvas created ({canvasTiles.Count} tiles)", WandColors.MsgInfo);
        }

        operandTiles.IntersectWith(canvasTiles);
        pwp.ApplySelectionOperationToSlot(activeSlot, operandTiles, settings.Operation);
        var selectionCount = pwp.GetSlotSelectionWorldTiles(activeSlot).Count;

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f }, player.Center);
        Main.NewText($"Planification slot {activeSlot + 1}: Selection {settings.Operation} ({selectionCount} total)", WandColors.MsgInfo);
    }
}

public class WandOfPlanificationInstant : WandOfPlanificationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new(255, 120, 180);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfPlanificationSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfPlanificationInstant>();
}

public class WandOfPlanificationSelect : WandOfPlanificationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new(255, 165, 200);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfPlanificationConfirm>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfPlanificationInstant>();
}

public class WandOfPlanificationConfirm : WandOfPlanificationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new(255, 190, 220);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfPlanificationStamp>();

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfPlanificationInstant>();
}

public class WandOfPlanificationStamp : WandOfPlanificationBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.FourClick;
    public override Color ModeColor => new(255, 210, 235);
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfPlanificationInstant>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true;
        Item.UseSound = null;
    }

    public override void AddRecipes() => RegisterNonInstantRecipe<WandOfPlanificationInstant>();
}
