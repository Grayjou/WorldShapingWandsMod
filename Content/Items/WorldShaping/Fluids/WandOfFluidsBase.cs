using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Algorithms;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Projectiles;
using static WorldShapingWandsMod.Common.Utilities.Msg;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

namespace WorldShapingWandsMod.Content.Items;


/// <summary>
/// Abstract base class for all Wand of Fluids variants.
/// Handles liquid placement, draining, rain fill, and pocket fill operations.
/// Four concrete subclasses (Instant, Select, Confirm, Stamp) provide mode behavior.
/// </summary>
public abstract partial class WandOfFluidsBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Fluids/{Name}";
    public override string WandBaseName => "Wand of Fluids";
    public override string WandLore => Get("LoreFluids");

    // ── Template Method Pattern ────────────────────────────────────────
    protected override WandFamily Family => WandFamily.Fluids;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.FluidsSettings;

        if (settings.PlaceBubble)
            return WandAction.FluidBubble;

        if (settings.Operation == FluidOperation.Drain)
        {
            if (!settings.SelectiveDrain)
                return WandAction.FluidDrainAny;

            return settings.LiquidType switch
            {
                LiquidTypeSelection.Water   => WandAction.FluidDrainWater,
                LiquidTypeSelection.Lava    => WandAction.FluidDrainLava,
                LiquidTypeSelection.Honey   => WandAction.FluidDrainHoney,
                LiquidTypeSelection.Shimmer => WandAction.FluidDrainShimmer,
                _                           => WandAction.FluidDrainAny,
            };
        }

        // Fill mode — sprite depends on liquid type AND fill algorithm
        return settings.FillMode switch
        {
            FluidFillMode.RainFill   => WandAction.FluidRainFill,
            FluidFillMode.PocketFill => WandAction.FluidPocketFill,
            _                        => WandAction.FluidPlace,  // FullLiquid (default)
        };
    }

    /// <summary>
    /// Returns a fluid-specific cancel color based on the current liquid type and operation,
    /// providing complementary-hue contrast to the active overlay color.
    /// </summary>
    protected override Color GetCancelColor()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        return WandColors.GetCancelColorForFluids(wandPlayer.FluidsSettings);
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.SilverBar, 10)
            .AddCustomShimmerResult(ItemID.Glass, 30)
            .AddCustomShimmerResult(ItemID.WaterBucket, 3)
            .AddCustomShimmerResult(ItemID.LavaBucket, 3)
            .AddCustomShimmerResult(ItemID.HoneyBucket, 3)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteFluidOperation(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.FluidsSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Show a cursor icon matching the current place type / operation
        var settings = wandPlayer.FluidsSettings;
        player.cursorItemIconEnabled = true;
        if (settings.Operation == FluidOperation.Drain)
        {
            // Drain mode: show sponge icon regardless of liquid type
            player.cursorItemIconID = ItemID.UltraAbsorbantSponge;
        }
        else
        {
            player.cursorItemIconID = settings.PlaceBubble
                ? ItemID.Bubble
                : settings.LiquidType switch
                {
                    LiquidTypeSelection.Water => ItemID.WaterBucket,
                    LiquidTypeSelection.Lava => ItemID.LavaBucket,
                    LiquidTypeSelection.Honey => ItemID.HoneyBucket,
                    LiquidTypeSelection.Shimmer => ItemID.BottomlessShimmerBucket,
                    _ => ItemID.WaterBucket
                };
        }
        player.cursorItemIconPush = 26;

        // Spawn and sustain the cosmetic held projectile (local player only)
        if (Main.myPlayer == player.whoAmI)
        {
            ManageHeldProjectile(player, wandPlayer);
        }
    }

    /// <summary>
    /// Spawns and sustains the cosmetic <see cref="WandOfFluidsProjectile"/>.
    /// Called every frame from <see cref="OnHoldItemFamily"/>. If the projectile
    /// already exists, this is a no-op. If it doesn't, a new one is spawned with
    /// the current selection mode and fluid state packed into <c>ai[0]</c>/<c>ai[1]</c>.
    /// </summary>
    /// <remarks>
    /// The projectile is spawned manually (not via <c>Item.shoot</c>) to avoid the
    /// cursor seizure that occurs when <c>Item.shoot</c>/<c>Item.channel</c>/<c>Item.noUseGraphic</c>
    /// are set on the multi-mode base class. See Session 10 S3 root-cause analysis.
    /// </remarks>
    private void ManageHeldProjectile(Player player, WandPlayer wandPlayer)
    {
        int projType = ModContent.ProjectileType<WandOfFluidsProjectile>();

        // Check if we already own one
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (Main.projectile[i].active
                && Main.projectile[i].owner == player.whoAmI
                && Main.projectile[i].type == projType)
            {
                return; // Already alive — AI() refreshes timeLeft each frame
            }
        }

        // Pack fluid state for ai[1]: liquidType in low nibble, operation in next nibble, bubble flag in bit 7
        var settings = wandPlayer.FluidsSettings;
        int packed = (int)settings.LiquidType
            | ((int)settings.Operation << 4)
            | (settings.PlaceBubble ? 0x80 : 0);

        Projectile.NewProjectile(
            player.GetSource_ItemUse(Item),
            player.MountedCenter,
            Vector2.Zero,
            projType,
            0, 0f,
            player.whoAmI,
            ai0: (float)WandSelectionMode,
            ai1: packed);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.LightPurple;
        Item.value = Item.buyPrice(gold: 8);
        Item.noUseGraphic = true; // Projectile handles visual — prevent double-draw
    }

    public override bool? UseItem(Player player)
    {
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        TemplateHoldItem(player);
    }

    // ════════════════════════════════════════════════════════════════
    //  Core Fluid Operations
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes the fluid operation (fill or drain) based on current settings.
    /// Determines which fill algorithm to run and dispatches accordingly.
    /// </summary>
    protected void ExecuteFluidOperation(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.FluidsSettings;
        var shapePositions = GetShapePositions(wandPlayer);

        if (shapePositions == null || shapePositions.Count == 0)
        {
            ShowNullResult(wandPlayer, "NoTilesInSelection", WandColors.MsgWarning);
            return;
        }

        // Bubble mode: exclusive from liquids, only works with FullLiquid fill
        if (settings.PlaceBubble)
        {
            if (settings.Operation == FluidOperation.Drain)
            {
                // Drain mode with bubble selected: drain bubbles (remove bubble tiles)
                ExecuteBubbleDrain(player, shapePositions);
            }
            else
            {
                // Bubble fill — silently uses FullLiquid logic regardless of FillMode
                ExecuteBubbleFill(player, shapePositions);
            }
            return;
        }

        if (settings.Operation == FluidOperation.Drain)
        {
            ExecuteDrain(player, shapePositions, settings);
        }
        else
        {
            // Coat in Bubble: place a bubble shell around the selection BEFORE filling liquid
            // Only applies to FullLiquid mode (not RainFill or BasinFill)
            if (settings.CoatInBubble && settings.FillMode == FluidFillMode.FullLiquid)
            {
                int shellPlaced = ExecuteCoatInBubble(player, shapePositions);
                if (shellPlaced > 0)
                    Main.NewText(Get("FluidsBubbleCoated", shellPlaced), WandColors.MsgFluids);
            }

            switch (settings.FillMode)
            {
                case FluidFillMode.FullLiquid:
                    if (settings.MixLiquids)
                        ExecuteMixLiquids(player, shapePositions, settings);
                    else
                        ExecuteFullLiquid(player, shapePositions, settings);
                    break;
                case FluidFillMode.RainFill:
                    ExecuteRainFill(player, shapePositions, settings, wandPlayer);
                    break;
                case FluidFillMode.PocketFill:
                    ExecutePocketFill(player, shapePositions, settings, wandPlayer);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the set of tile positions defined by the current shape selection.
    /// Uses the same ShapeRegistry pipeline as all other wand families.
    /// </summary>
    private static List<Point> GetShapePositions(WandPlayer wandPlayer)
    {
        var settings = wandPlayer.FluidsSettings;
        var selection = wandPlayer.GetVisualSelection();

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var tiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swp = wandPlayer.Player.GetModPlayer<DelimitationWandPlayer>();
        tiles = swp.FilterBySelection(tiles);

        return new List<Point>(tiles);
    }
}
