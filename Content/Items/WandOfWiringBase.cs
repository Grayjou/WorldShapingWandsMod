using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfWiringBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Wiring";

    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Lime;
        Item.value = Item.buyPrice(gold: 10);
        Item.mech = true; // Shows wires when held
    }

    public override bool? UseItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    public override void HoldItem(Player player)
    {
        // Always show wires when holding
        player.InfoAccMechShowWires = true;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false;
        }

        // Show wire/actuator count in cursor
        var settings = wandPlayer.WiringSettings;
        if (settings.HasAnySelection)
        {
            int wires = WiringHelper.CountWires(player);
            int actuators = WiringHelper.CountActuators(player);

            if (settings.WireRed || settings.WireGreen || settings.WireBlue || settings.WireYellow)
            {
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = ItemID.Wire;
                player.cursorItemIconPush = 26;
            }
            else if (settings.Actuator)
            {
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = ItemID.Actuator;
                player.cursorItemIconPush = 26;
            }
        }
    }

    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.ClearSelection();
        Main.NewText("Selection cancelled.", Color.Yellow);
    }

    protected void ExecuteWiring(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.WiringSettings;
        var selection = wandPlayer.Selection;

        if (!settings.HasAnySelection)
        {
            Main.NewText("No wires or actuators selected.", Color.Red);
            return;
        }

        // Check materials for Place mode
        if (settings.Mode == WiringMode.Place)
        {
            int wiresNeeded = settings.WireRed || settings.WireGreen ||
                              settings.WireBlue || settings.WireYellow ? 1 : 0;

            if (wiresNeeded > 0 && !WiringHelper.HasItem(player, ItemID.Wire))
            {
                Main.NewText("No wire in inventory.", Color.Red);
                return;
            }

            if (settings.Actuator && !WiringHelper.HasItem(player, ItemID.Actuator))
            {
                Main.NewText("No actuators in inventory.", Color.Red);
                return;
            }
        }

        var context = new ShapeContext(
            selection.StartTile,
            selection.EndTile,
            settings.Shape.FillMode,
            settings.Shape.Thickness,
            HorizontalBias.None,
            VerticalBias.None,
            selection.VerticalFirst
        );

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        var (placed, removed) = WiringHelper.ExecuteWiringOperation(
            tileSet.Tiles,
            settings.Mode,
            settings.WireRed, settings.WireGreen, settings.WireBlue, settings.WireYellow,
            settings.Actuator,
            player
        );

        // Report results
        if (settings.Mode == WiringMode.Place && placed > 0)
        {
            Main.NewText($"Placed {placed} wire segments.", Color.LimeGreen);
        }
        else if (settings.Mode == WiringMode.Remove && removed > 0)
        {
            Main.NewText($"Removed {removed} wire segments.", Color.Cyan);
        }
        else
        {
            Main.NewText("No changes made.", Color.Gray);
        }

        // TODO: Send network packet for multiplayer
        // if (Main.netMode == NetmodeID.MultiplayerClient)
        //     WiringPacketHandler.SendWiringOperation(...);
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            var wandPlayer = player.GetModPlayer<WandPlayer>();
            if (wandPlayer.Selection.IsActive)
            {
                CancelSelection(wandPlayer);
            }
            else
            {
                ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
            }
            return false;
        }
        return true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.WireKite, 1)
            .AddIngredient(ItemID.Wire, 50)
            .AddIngredient(ItemID.Actuator, 10)
            .AddTile(TileID.TinkerersWorkbench)
            .Register();
    }
}