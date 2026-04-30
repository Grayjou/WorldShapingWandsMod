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
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfWiringBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Wiring/{Name}";
    public override string WandBaseName => "Wand of Wiring";
    public override string WandLore => Get("LoreWiring");
    public override bool ShowDivineLore => false;

    // ── Template Method Pattern ────────────────────────────────────────
    // All mode-specific input logic lives in BaseCyclingWand's template methods.
    protected override WandFamily Family => WandFamily.Wiring;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        return wandPlayer.WiringSettings.Mode switch
        {
            WiringMode.Place  => WandAction.WiringAdd,
            WiringMode.Remove => WandAction.WiringRemove,
            _                 => WandAction.WiringAdd,
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.WireKite, 1)
            .AddCustomShimmerResult(ItemID.Wire, 50)
            .AddCustomShimmerResult(ItemID.Actuator, 10);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteWiring(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.WiringSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Always show wires when holding
        player.InfoAccMechShowWires = true;

        // Show wire/actuator cursor icon
        var settings = wandPlayer.WiringSettings;
        if (settings.HasAnySelection)
        {
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

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Lime;
        Item.value = Item.buyPrice(gold: 10);
        Item.mech = true; // Shows wires when held
    }

    public override bool? UseItem(Player player)
    {
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        TemplateHoldItem(player);
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
        var config = WandConfigs.Resources;
        var clientCfg = WandConfigs.Preferences;
        // (S1 2026-04-28 P 1*1) Carefree Mode also bypasses wire/actuator
        // consumption — placing wires shouldn't drain inventory when the
        // player has explicitly opted out of resource costs at the world level.
        bool carefree = WandConfigs.Carefree?.EnableCarefreeMode == true;
        bool infiniteWires = carefree || WiringHelper.IsInfiniteWireMode(player, config);
        bool infiniteActuators = carefree || WiringHelper.IsInfiniteActuatorMode(player, config);

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

        // Filter by active tile selection (Select Wand integration)
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swp.FilterBySelection(invertedTiles);

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

            WiringPacketHandler.SendWiringOperation(
                selection.StartTile, selection.EndTile,
                settings.Mode, settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                settings.PackWireFlags(), selection.VerticalFirst,
                player.whoAmI,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection);

            // Report results on the sending client
            ReportWiringResults(wandPlayer, settings.Mode, consumed, 0, clientCfg);
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

        ReportWiringResults(wandPlayer, settings.Mode, placed, removed, clientCfg);
    }

    private void ReportWiringResults(WandPlayer wandPlayer, WiringMode mode, int placed, int removed, PreferencesConfig clientCfg)
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
            ShowNullResult(wandPlayer, "NoWiringChanges", Color.Gray);
        }
    }



    public override void AddRecipes()
    {
        // Only the Instant variant has a craftable recipe.
        // Other modes are obtained via right-click cycling in inventory.
    }
}