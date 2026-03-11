using Microsoft.Xna.Framework;
using System.Collections.Generic;
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

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfSafekeepingBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Safekeeping";
    public override string WandLore => "The Deity of Timelessness lets you separate space from the powers of creation.";

    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.LightPurple;
        Item.value = Item.buyPrice(gold: 8);
    }

    public override bool? UseItem(Player player)
    {
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    public override void HoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false;
        }

        // Show protection mode cursor icon
        var settings = wandPlayer.SafekeepingSettings;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = settings.Mode == SafekeepingMode.Protect
            ? ItemID.GoldenKey
            : ItemID.ShadowKey;
        player.cursorItemIconPush = 26;
    }

    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(WandColors.CancelSafekeeping, wandPlayer.SafekeepingSettings.Shape);
    }

    protected void ExecuteSafekeeping(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.SafekeepingSettings;
        var selection = wandPlayer.Selection;

        if (!settings.ProtectTiles && !settings.ProtectWalls)
        {
            Main.NewText("No protection targets selected (tiles or walls).", WandColors.MsgError);
            return;
        }

        var context = new ShapeContext(
            selection.StartTile,
            selection.EndTile,
            settings.Shape.FillMode,
            settings.Shape.Thickness,
            HorizontalBias.None,
            VerticalBias.None,
            selection.VerticalFirst,
            settings.Shape.EqualDimensions
        );

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        int tilesChanged = 0;
        int wallsChanged = 0;
        int skipped = 0;

        foreach (Point tile in tileSet.Tiles)
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
                // Play IceBlockPlace (SoundID.Item30) — short, crispy freeze sound
                var config = ModContent.GetInstance<WandConfig>();
                if (config.EnableWandSounds)
                    SoundEngine.PlaySound(SoundID.Item30, player.Center); // IceBlockPlace

                string detail = $"Protected {string.Join(", ", parts)}";
                if (skipped > 0)
                    detail += $" ({skipped} already protected)";
                Main.NewText(detail, WandColors.MsgSafekeeping);
            }
            else
            {
                Main.NewText("No positions to protect in selection.", WandColors.MsgInfo);
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
                Main.NewText($"Unprotected {string.Join(", ", parts)}", Color.LightGreen);
            }
            else
            {
                Main.NewText("No protected positions in selection.", WandColors.MsgInfo);
            }
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
            else if (Main.myPlayer == player.whoAmI)
            {
                ModContent.GetInstance<WandUISystem>().ToggleUIForCurrentWand();
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
