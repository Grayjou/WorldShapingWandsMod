using Microsoft.Xna.Framework;
using System.Collections.Generic;
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
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfSafekeepingBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Safekeeping/{Name}";
    public override string WandBaseName => "Wand of Safekeeping";
    public override string WandLore => Get("LoreSafekeeping");

    // ── Template Method Pattern ────────────────────────────────────────
    // Safekeeping is the first family migrated to the template pattern.
    // No more per-mode HandleUseItem/HoldItem boilerplate — all mode logic
    // lives in BaseCyclingWand's TemplateUseItem/TemplateHoldItem.
    protected override WandFamily Family => WandFamily.Safekeeping;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        return wandPlayer.SafekeepingSettings.Mode switch
        {
            SafekeepingMode.Protect   => WandAction.SafekeepingAdd,
            SafekeepingMode.Unprotect => WandAction.SafekeepingRemove,
            _                         => WandAction.SafekeepingAdd,
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.SilverBar, 10)
            .AddCustomShimmerResult(ItemID.Amethyst, 5)
            .AddCustomShimmerResult(ItemID.Obsidian, 20)
            .AddCustomShimmerResult(ItemID.EbonstoneBlock, 10)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteSafekeeping(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.SafekeepingSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Show protection mode cursor icon
        var settings = wandPlayer.SafekeepingSettings;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = settings.Mode == SafekeepingMode.Protect
            ? ItemID.GoldenKey
            : ItemID.ShadowKey;
        player.cursorItemIconPush = 26;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.LightPurple;
        Item.value = Item.buyPrice(gold: 8);
    }

    public override bool? UseItem(Player player)
    {
        if (!UsesTemplateModeDispatch)
            return base.UseItem(player);
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        if (UsesTemplateModeDispatch)
        {
            TemplateHoldItem(player);
            return;
        }
        base.HoldItem(player);
    }

    protected void ExecuteSafekeeping(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.SafekeepingSettings;
        var selection = wandPlayer.GetVisualSelection();

        if (!settings.ProtectTiles && !settings.ProtectWalls)
        {
            Main.NewText(Get("NoProtectionTargets"), WandColors.MsgError);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swp.FilterBySelection(invertedTiles);

        int tilesChanged = 0;
        int wallsChanged = 0;
        int skipped = 0;

        foreach (Point tile in invertedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1))
                continue;

            if (settings.Mode == SafekeepingMode.Protect)
            {
                if (settings.ProtectTiles)
                {
                    if (!SafekeepingSystem.IsTileProtected(tile))
                    {
                        SafekeepingSystem.ProtectTile(tile);
                        tilesChanged++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                if (settings.ProtectWalls)
                {
                    if (!SafekeepingSystem.IsWallProtected(tile))
                    {
                        SafekeepingSystem.ProtectWall(tile);
                        wallsChanged++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            else // Unprotect
            {
                if (settings.ProtectTiles)
                {
                    if (SafekeepingSystem.UnprotectTile(tile))
                        tilesChanged++;
                }

                if (settings.ProtectWalls)
                {
                    if (SafekeepingSystem.UnprotectWall(tile))
                        wallsChanged++;
                }
            }
        }

        // Build result message
        var parts = new List<string>();

        if (settings.Mode == SafekeepingMode.Protect)
        {
            if (tilesChanged > 0)
                parts.Add($"{tilesChanged} tiles");
            if (wallsChanged > 0)
                parts.Add($"{wallsChanged} walls");

            if (parts.Count > 0)
            {
                // Play Quiet click sound (SoundID.MaxMana) at half volume — short, crispy sound
                var config = WandConfigs.Preferences;
                if (config?.EnableWandSounds == true)
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.5f }, player.Center);

                string detail = $"Protected {string.Join(", ", parts)}";
                if (skipped > 0)
                    detail += $" ({skipped} already protected)";
                Main.NewText(detail, WandColors.MsgSafekeeping);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoPositionsToProtect", WandColors.MsgInfo);
            }
        }
        else
        {
            if (tilesChanged > 0)
                parts.Add($"{tilesChanged} tiles");
            if (wallsChanged > 0)
                parts.Add($"{wallsChanged} walls");

            if (parts.Count > 0)
            {
                // Play Unlock sound for unprotect operations
                var config = WandConfigs.Preferences;
                if (config?.EnableWandSounds == true)
                    SoundEngine.PlaySound(SoundID.Unlock, player.Center);

                Main.NewText($"Unprotected {string.Join(", ", parts)}", Color.LightGreen);
            }
            else
            {
                ShowNullResult(wandPlayer, "NoProtectedPositions", WandColors.MsgInfo);
            }
        }
    }

    public override void AddRecipes()
    {
        // Only the Instant variant has a craftable recipe.
        // Other modes are obtained via right-click cycling in inventory.
    }
}