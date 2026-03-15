using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfCoatingBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Coating";
    public override string WandLore => "The Deity of Surfaces lets you dress the world in colours of your choosing.";

    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(gold: 6);
    }

    public override bool? UseItem(Player player)
    {
        if (Main.LocalPlayer.mouseInterface)
            return false;

        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (WandSelectionMode != SelectionMode.OneClick)
            wandPlayer.EnsureSelectionCompatibility(WandSelectionMode);

        if (WandSelectionMode != SelectionMode.OneClick && !wandPlayer.TryConsumeFreshLeftClick())
            return false;

        Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
        return HandleUseItem(player, wandPlayer, mouseTile);
    }

    public override void HoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();

        if (!Main.LocalPlayer.mouseInterface
            && wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false;
        }

        // Show a paint cursor icon — use the Paintbrush if painting, Scraper if scraping
        var settings = wandPlayer.CoatingSettings;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = (settings.Mode == CoatingMode.PaintTile || settings.Mode == CoatingMode.PaintWall)
            ? ItemID.Paintbrush
            : ItemID.PaintScraper;
        player.cursorItemIconPush = 26;
    }

    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(WandColors.CancelCoating, wandPlayer.CoatingSettings.Shape);
    }

    protected void ExecuteCoating(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.CoatingSettings;
        var selection = wandPlayer.GetVisualSelection();

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);

        int changed = 0;
        int skipped = 0;

        foreach (Point tile in tileSet.Tiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1))
                continue;

            bool wasChanged = ApplyCoating(tile.X, tile.Y, settings);
            if (wasChanged)
                changed++;
            else
                skipped++;
        }

        // Sync tile changes across network
        if (changed > 0)
        {
            // Broadcast the changed region
            NetMessage.SendTileSquare(-1,
                System.Math.Min(selection.StartTile.X, selection.EndTile.X),
                System.Math.Min(selection.StartTile.Y, selection.EndTile.Y),
                System.Math.Abs(selection.EndTile.X - selection.StartTile.X) + 1,
                System.Math.Abs(selection.EndTile.Y - selection.StartTile.Y) + 1
            );
        }

        // Feedback
        if (changed == 0)
        {
            Main.NewText(Get("CoatingNoChanges"), WandColors.MsgInfo);
            return;
        }

        var config = ModContent.GetInstance<WandConfig>();
        if (config.EnableWandSounds)
        {
            var soundId = (settings.Mode == CoatingMode.PaintTile || settings.Mode == CoatingMode.PaintWall)
                ? SoundID.Item109 // Paintbrush sound
                : SoundID.Item131; // Scrape sound
            SoundEngine.PlaySound(soundId with { Volume = 0.6f }, player.Center);
        }

#pragma warning disable CS0618
        string modeLabel = settings.Mode switch
        {
            CoatingMode.PaintTile   => Get("CoatingPaintedTiles", changed),
            CoatingMode.PaintWall   => Get("CoatingPaintedWalls", changed),
            CoatingMode.ScrapePaint => Get("CoatingScrapedPaint", changed),
            CoatingMode.ScrapeMoss  => Get("CoatingScrapedMoss", changed),
            CoatingMode.HarvestMoss => Get("CoatingHarvestedMoss", changed),
            _                       => $"Coated {changed} tiles"
        };
#pragma warning restore CS0618
        Main.NewText(modeLabel, WandColors.MsgCoating);
    }

    /// <summary>
    /// Applies the coating operation to a single tile position.
    /// Returns true if any change was made.
    /// </summary>
    private static bool ApplyCoating(int x, int y, WandOfCoatingSettings settings)
    {
#pragma warning disable CS0618
        return settings.Mode switch
        {
            CoatingMode.PaintTile   => ApplyPaintTile(x, y, settings.PaintColor, settings.ApplyIlluminant, settings.ApplyEcho),
            CoatingMode.PaintWall   => ApplyPaintWall(x, y, settings.PaintColor, settings.ApplyIlluminant, settings.ApplyEcho),
            CoatingMode.ScrapePaint => ApplyScrapePaint(x, y),
            CoatingMode.ScrapeMoss  => ApplyScrapeMoss(x, y),
            CoatingMode.HarvestMoss => ApplyHarvestMoss(x, y),
            _                       => false
        };
#pragma warning restore CS0618
    }

    private static bool ApplyPaintTile(int x, int y, byte color, bool applyIlluminant, bool applyEcho)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile)
            return false;

        bool changed = false;

        // Apply paint color if different, or remove paint when color is 0 (None)
        if (tile.TileColor != color)
        {
            WorldGen.paintTile(x, y, color, true);
            changed = true;
        }

        // Apply Illuminant coating if requested and not already present
        if (applyIlluminant && !tile.IsTileFullbright)
        {
            WorldGen.paintCoatTile(x, y, 1, true);
            changed = true;
        }

        // Apply Echo coating if requested and not already present
        if (applyEcho && !tile.IsTileInvisible)
        {
            WorldGen.paintCoatTile(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ApplyPaintWall(int x, int y, byte color, bool applyIlluminant, bool applyEcho)
    {
        var tile = Main.tile[x, y];
        if (tile.WallType == WallID.None)
            return false;

        bool changed = false;

        // Apply paint color if different, or remove paint when color is 0 (None)
        if (tile.WallColor != color)
        {
            WorldGen.paintWall(x, y, color, true);
            changed = true;
        }

        // Apply Illuminant coating if requested and not already present
        if (applyIlluminant && !tile.IsWallFullbright)
        {
            WorldGen.paintCoatWall(x, y, 1, true);
            changed = true;
        }

        // Apply Echo coating if requested and not already present
        if (applyEcho && !tile.IsWallInvisible)
        {
            WorldGen.paintCoatWall(x, y, 2, true);
            changed = true;
        }

        return changed;
    }

    private static bool ApplyScrapePaint(int x, int y)
    {
        var tile = Main.tile[x, y];
        bool changed = false;

        // Remove paint from tile
        if (tile.HasTile && tile.TileColor != PaintID.None)
        {
            WorldGen.paintTile(x, y, PaintID.None, true);
            changed = true;
        }

        // Remove coatings from tile
        if (tile.HasTile && (tile.IsTileFullbright || tile.IsTileInvisible))
        {
            WorldGen.paintCoatTile(x, y, 0, true);
            changed = true;
        }

        // Remove paint from wall
        if (tile.WallType != WallID.None && tile.WallColor != PaintID.None)
        {
            WorldGen.paintWall(x, y, PaintID.None, true);
            changed = true;
        }

        // Remove coatings from wall
        if (tile.WallType != WallID.None && (tile.IsWallFullbright || tile.IsWallInvisible))
        {
            WorldGen.paintCoatWall(x, y, 0, true);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Removes moss from a tile by converting any moss tile type back to its
    /// underlying substrate (Stone) and clearing any paint on it.
    /// Drops the corresponding moss item (vanilla scrape behaviour).
    /// </summary>
    private static bool ApplyScrapeMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile)
            return false;

        int tileType = tile.TileType;
        if (!MossTileToSubstrate.TryGetValue(tileType, out int substrate))
            return false;

        // Drop the moss as an item (vanilla scrape behaviour)
        if (MossTileToItem.TryGetValue(tileType, out int mossItemId))
            Item.NewItem(new EntitySource_TileBreak(x, y), x * 16, y * 16, 16, 16, mossItemId);

        // Convert the moss tile to its substrate
        Main.tile[x, y].TileType = (ushort)substrate;

        // Strip any paint from the freshly de-mossed tile
        if (Main.tile[x, y].TileColor != PaintID.None)
            WorldGen.paintTile(x, y, PaintID.None, true);

        // Trigger a frame update so the tile renders correctly
        WorldGen.SquareTileFrame(x, y, true);

        return true;
    }

    /// <summary>
    /// Harvests LongMoss by killing it via WorldGen.KillTile, which trims the hanging
    /// growth and may drop a moss item (25% chance, matching vanilla scraper behaviour).
    /// Only targets TileID.LongMoss — does NOT touch short moss stone tiles.
    /// Based on ImproveGame's PaintWand.ModifySelectedTiles remove-mode LongMoss handling.
    /// </summary>
    private static bool ApplyHarvestMoss(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile)
            return false;

        if (tile.TileType != TileID.LongMoss)
            return false;

        int frameX = tile.TileFrameX;

        // Kill the long moss tile (WorldGen.KillTile handles the visual break effect)
        WorldGen.KillTile(x, y);

        // If the tile is still present after KillTile, it wasn't actually killed
        if (tile.HasTile)
            return false;

        // Sync in multiplayer
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);

        // 25% chance to drop a moss item (same rate as vanilla scraper)
        if (Main.rand.NextBool(4))
        {
            int itemType = (frameX / 22) switch
            {
                6  => 4377,  // Lava Moss
                7  => 4378,  // Argon Moss
                8  => 4389,  // Krypton Moss
                9  => 5127,  // Xenon Moss
                10 => 5128,  // Neon Moss
                _  => 4349 + frameX / 22  // Green(0), Brown(1), Red(2), Blue(3), Purple(4), Helium(5)
            };

            int number = Item.NewItem(
                new EntitySource_TileBreak(x, y),
                x * 16, y * 16, 16, 16, itemType);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, number, 1f);
        }

        return true;
    }
    /// Stone-based mosses revert to Stone.
    /// </summary>
    private static readonly Dictionary<int, int> MossTileToSubstrate = new()
    {
        [TileID.GreenMoss]   = TileID.Stone,
        [TileID.BrownMoss]   = TileID.Stone,
        [TileID.RedMoss]     = TileID.Stone,
        [TileID.BlueMoss]    = TileID.Stone,
        [TileID.PurpleMoss]  = TileID.Stone,
        [TileID.LavaMoss]    = TileID.Stone,
        [TileID.ArgonMoss]   = TileID.Stone,
        [TileID.KryptonMoss] = TileID.Stone,
        [TileID.XenonMoss]   = TileID.Stone,
        [TileID.VioletMoss]  = TileID.Stone,
        [TileID.RainbowMoss] = TileID.Stone,
    };

    /// <summary>
    /// Maps moss TileIDs → the ItemID that drops when the moss is scraped
    /// (mirrors vanilla's scraper behaviour).
    /// </summary>
    private static readonly Dictionary<int, int> MossTileToItem = new()
    {
        [TileID.GreenMoss]   = ItemID.GreenMoss,
        [TileID.BrownMoss]   = ItemID.BrownMoss,
        [TileID.RedMoss]     = ItemID.RedMoss,
        [TileID.BlueMoss]    = ItemID.BlueMoss,
        [TileID.PurpleMoss]  = ItemID.PurpleMoss,
        [TileID.LavaMoss]    = ItemID.LavaMoss,
        [TileID.ArgonMoss]   = ItemID.ArgonMoss,
        [TileID.KryptonMoss] = ItemID.KryptonMoss,
        [TileID.XenonMoss]   = ItemID.XenonMoss,
        [TileID.VioletMoss]  = ItemID.VioletMoss,
        [TileID.RainbowMoss] = ItemID.RainbowMoss,
    };

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
        // Only the Instant variant is craftable.
        // Other modes are obtained via right-click cycling.
    }
}
