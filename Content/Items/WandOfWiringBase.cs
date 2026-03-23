using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfWiringBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Wiring";
    public override string WandLore => "An advanced mechanism wrench, refined beyond the Grand Design. Where gods shape matter, engineers shape purpose.";
    public override bool ShowDivineLore => false;

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
        // Keep the use-cycle alive while the mouse is held (channeling mode).
        // Without this, itemAnimation expires and UseItem won't fire again.
        if (WandSelectionMode == SelectionMode.OneClick && Item.channel)
        {
            player.itemAnimation = player.itemAnimationMax;
            return Main.mouseLeft ? false : true;
        }
        // Don't do anything if the mouse is over UI
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        // Clear incompatible selections (e.g., a 2-step selection on a 2-click wand).
        // Skip for OneClick — instant wands manage their own lifecycle in HoldItem.
        if (WandSelectionMode != SelectionMode.OneClick)
            wandPlayer.EnsureSelectionCompatibility(WandSelectionMode);

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.GetMouseTile();
        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    public override void HoldItem(Player player)
    {
        // Always show wires when holding
        player.InfoAccMechShowWires = true;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        // Only cancel on right-click in the WORLD, not when clicking in inventory/UI.
        if (!Main.LocalPlayer.mouseInterface
            && wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
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
        wandPlayer.CancelSelection(WandColors.CancelWiring, wandPlayer.WiringSettings.Shape);
    }

    protected void ExecuteWiring(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.WiringSettings;
        var selection = wandPlayer.GetVisualSelection();

        if (!settings.HasAnySelection)
        {
            Main.NewText(Get("NoWiresSelected"), Color.Red);
            return;
        }

        // Check materials for Place mode (respects per-type InfiniteResource config)
        var config = ModContent.GetInstance<WandServerConfig>();
        var clientCfg = ModContent.GetInstance<WandClientConfig>();
        bool infiniteWires = WiringHelper.IsInfiniteWireMode(player, config);
        bool infiniteActuators = WiringHelper.IsInfiniteActuatorMode(player, config);

        if (settings.Mode == WiringMode.Place && !infiniteWires)
        {
            int wiresNeeded = settings.WireRed || settings.WireGreen ||
                              settings.WireBlue || settings.WireYellow ? 1 : 0;

            if (wiresNeeded > 0 && !WiringHelper.HasItem(player, ItemID.Wire))
            {
                Main.NewText(Get("NoWireInInventory"), Color.Red);
                return;
            }
        }

        if (settings.Mode == WiringMode.Place && !infiniteActuators)
        {
            if (settings.Actuator && !WiringHelper.HasItem(player, ItemID.Actuator))
            {
                Main.NewText(Get("NoActuatorsInInventory"), Color.Red);
                return;
            }
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // In multiplayer, send a packet to the server instead of direct manipulation.
        // The server will execute the operation authoritatively and broadcast to all clients.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // Pre-consume items on the client before sending the packet.
            // The server applies tile changes; the client handles inventory.
            int consumed = 0;
            if (settings.Mode == WiringMode.Place)
            {
                consumed = WiringHelper.PreConsumeForOperation(
                    invertedTiles, settings.WireRed, settings.WireGreen,
                    settings.WireBlue, settings.WireYellow, settings.Actuator, player,
                    infiniteWires, infiniteActuators);
            }

            WandPacketHandler.SendWiringOperation(
                selection.StartTile, selection.EndTile,
                settings.Mode, settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                settings.PackWireFlags(), selection.VerticalFirst,
                player.whoAmI,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection);

            // Report results on the sending client
            ReportWiringResults(settings.Mode, consumed, 0, clientCfg);
            return;
        }

        // Single-player: execute directly with consumption
        var (placed, removed) = WiringHelper.ExecuteWiringOperation(
            invertedTiles,
            settings.Mode,
            settings.WireRed, settings.WireGreen, settings.WireBlue, settings.WireYellow,
            settings.Actuator,
            player,
            infiniteWires,
            infiniteActuators
        );

        ReportWiringResults(settings.Mode, placed, removed, clientCfg);
    }

    private void ReportWiringResults(WiringMode mode, int placed, int removed, WandClientConfig clientCfg)
    {
        if (mode == WiringMode.Place && placed > 0)
        {
            if (clientCfg?.EnableWandSounds == true)
                SoundEngine.PlaySound(SoundID.Item64, Main.LocalPlayer.Center);

            Main.NewText(Get("WiresPlaced", placed), Color.LimeGreen);
        }
        else if (mode == WiringMode.Remove && removed > 0)
        {
            if (clientCfg?.EnableWandSounds == true)
                SoundEngine.PlaySound(SoundID.Item64, Main.LocalPlayer.Center);

            Main.NewText(Get("WiresRemoved", removed), Color.Cyan);
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Only show "no changes" in SP — in MP the server handles the operation
            // and the client may not know the exact outcome yet
            Main.NewText(Get("NoWiringChanges"), Color.Gray);
        }
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
                // Only toggle UI on the client
                if (Main.myPlayer == player.whoAmI)
                {
                    ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
                }
            }
            return false;
        }
        return true;
    }

    public override void AddRecipes()
    {
        // Only the Instant variant has a craftable recipe.
        // Other modes are obtained via right-click cycling in inventory.
    }
}