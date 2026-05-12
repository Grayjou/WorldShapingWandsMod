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
        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        if (pwp?.Settings == null)
            return false;

        return StencilTransformWorldAction.IsArmed(
            pwp.Settings.TransformModeEnabled,
            pwp.Settings.ActiveTransformAction,
            IsMouseOverUI());
    }

    private static bool ShouldInterceptTransformWorldAction(Player player)
    {
        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        if (pwp?.Settings == null)
            return false;

        return StencilTransformWorldAction.ShouldInterceptTransformClick(
            player,
            IsTransformWorldActionArmed(player),
            () => HandleTransformWorldAction(player, pwp));
    }

    private static bool HandleTransformWorldAction(Player player, PlanificationWandPlayer pwp)
    {
        var settings = pwp.Settings;
        int activeSlot = pwp.ActiveEditSlot;
        var slot = BuildStencilSlotState(pwp, activeSlot);

        if (settings.ActiveTransformAction == TransformActionMode.Move && !slot.HasCanvas && !slot.HasSelection)
        {
            Main.NewText("No canvas or selection active — nothing to move.", Color.OrangeRed);
            return true;
        }

        var state = new StencilTransformWorldAction.TransformState
        {
            ActiveAction = settings.ActiveTransformAction,
            PendingMoveStart = settings.PendingTransformMoveStart,
            PersistentPivot = settings.PersistentPivot,
            TemporaryPivot = settings.TemporaryPivot,
        };

        bool handled = StencilTransformWorldAction.Handle(
            state,
            GeometryHelper.GetMouseTile(),
            slot.HasCanvas || slot.HasSelection,
            (dx, dy) => slot.Translate(dx, dy),
            key =>
            {
                if (Main.myPlayer == player.whoAmI)
                    Main.NewText(Language.GetTextValue(key), WandColors.MsgInfo);
            },
            out var updated);

        settings.PendingTransformMoveStart = updated.PendingMoveStart;
        settings.PersistentPivot = updated.PersistentPivot;
        settings.TemporaryPivot = updated.TemporaryPivot;

        if (!handled)
            return false;

        ApplyStencilSlotState(pwp, activeSlot, slot);
        return true;
    }

    private static StencilSlotState BuildStencilSlotState(PlanificationWandPlayer pwp, int slot)
    {
        var state = new StencilSlotState();
        state.SetCanvas(pwp.GetSlotCanvasWorldTiles(slot));
        state.SetSelection(pwp.GetSlotSelectionWorldTiles(slot));
        return state;
    }

    private static void ApplyStencilSlotState(PlanificationWandPlayer pwp, int slot, StencilSlotState state)
    {
        pwp.ClearSlot(slot);

        if (state.CanvasCount > 0)
            pwp.SetSlotCanvasShape(slot, state.CloneCanvas());

        if (state.SelectionCount > 0)
            pwp.SetSlotSelectionShape(slot, state.CloneSelection());
    }

    protected virtual void ExecutePlanificationOperation(Player player, WandPlayer wandPlayer)
    {
        var pwp = player.GetModPlayer<PlanificationWandPlayer>();
        var settings = pwp.Settings;

        var selection = wandPlayer.GetVisualSelection();
        if (!selection.IsActive)
            return;

        var shapeTiles = StencilOperationEngine.BuildOperandTiles(wandPlayer, settings.Shape);
        if (shapeTiles.Count == 0)
        {
            Main.NewText(Msg.Get("NoTilesInShape"), Color.Gray);
            return;
        }

        int activeSlot = pwp.ActiveEditSlot;
        var slot = BuildStencilSlotState(pwp, activeSlot);

        if (settings.Mode == DelimitationWandMode.CanvasEdit)
        {
            int beforeCount = slot.CanvasCount;
            StencilOperationEngine.ExecuteCanvasOperation(slot, shapeTiles, settings.Operation);
            ApplyStencilSlotState(pwp, activeSlot, slot);

            int afterCount = slot.CanvasCount;
            int delta = System.Math.Abs(afterCount - beforeCount);
            Main.NewText($"Planification slot {activeSlot + 1}: Canvas {settings.Operation} ({delta} tiles, {afterCount} total)", WandColors.MsgInfo);
        }
        else
        {
            int beforeCount = slot.SelectionCount;
            bool hadCanvas = slot.HasCanvas;

            if (!StencilOperationEngine.ExecuteSelectionOperation(slot, shapeTiles, settings.Operation, settings.AutoCreateCanvas))
            {
                return;
            }

            ApplyStencilSlotState(pwp, activeSlot, slot);

            if (!hadCanvas && slot.HasCanvas)
                Main.NewText($"Planification slot {activeSlot + 1}: Canvas created ({slot.CanvasCount} tiles)", WandColors.MsgInfo);

            int afterCount = slot.SelectionCount;
            int delta = System.Math.Abs(afterCount - beforeCount);
            Main.NewText($"Planification slot {activeSlot + 1}: Selection {settings.Operation} ({delta} tiles, {afterCount} total)", WandColors.MsgInfo);
        }

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f }, player.Center);
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
