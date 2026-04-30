using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
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
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

public abstract class WandOfCoatingBase : BaseCyclingWand
{
    public override string Texture => $"WorldShapingWandsMod/Content/Items/WorldShaping/Coating/{Name}";
    public override string WandBaseName => "Wand of Coating";
    public override string WandLore => Get("LoreCoating");

    // ── Template Method Pattern ────────────────────────────────────────
    protected override WandFamily Family => WandFamily.Coating;
    protected override bool UsesTemplateModeDispatch => true;

    // ── WandActionProjectile opt-in ────────────────────────────────────
    protected override bool UseWandActionProjectile => true;

    protected override WandAction ResolveCurrentAction()
    {
        var wandPlayer = Main.LocalPlayer.GetModPlayer<WandPlayer>();
        return wandPlayer.CoatingSettings.Mode switch
        {
            CoatingMode.PaintTile   => WandAction.CoatingPaintTile,
            CoatingMode.PaintWall   => WandAction.CoatingPaintWall,
            CoatingMode.ScrapeMoss  => WandAction.CoatingScrapeMoss,
            CoatingMode.HarvestMoss => WandAction.CoatingHarvestMoss,
            // S11: Color Replace is a paint-family operation; reuse the
            // PaintTile projectile sprite rather than minting a new asset
            // (the wand projectile only briefly flashes during cast — the
            // visual cost of distinguishing replace-vs-paint there is far
            // higher than the win, per the S11 “no new art” default).
            CoatingMode.ColorReplace => WandAction.CoatingPaintTile,
            _                       => WandAction.CoatingPaintTile,
        };
    }

    /// <inheritdoc />
    protected override Recipe AddInstantRecipeShimmerResults(Recipe recipe)
        => recipe
            .AddCustomShimmerResult(ItemID.GoldBar, 5)
            .AddCustomShimmerResult(ItemID.SilverBar, 10)
            .AddCustomShimmerResult(ItemID.Paintbrush, 1)
            .AddCustomShimmerResult(ItemID.PaintScraper, 1)
            .AddCustomShimmerResult(ItemID.PaintRoller, 1)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1);

    protected override void ExecuteWandOperation(Player player, WandPlayer wandPlayer)
        => ExecuteCoating(player, wandPlayer);

    protected override ShapeInfo GetWandShape(WandPlayer wandPlayer)
        => wandPlayer.CoatingSettings.Shape;

    protected override void CancelActiveSelection(Player player, WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(GetCancelColor(), GetWandShape(wandPlayer));
    }

    protected override void OnHoldItemFamily(Player player, WandPlayer wandPlayer)
    {
        // Show a cursor icon matching the current mode
        var settings = wandPlayer.CoatingSettings;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = settings.Mode switch
        {
            CoatingMode.PaintTile => ItemID.Paintbrush,
            CoatingMode.PaintWall => ItemID.PaintRoller,
            CoatingMode.ScrapeMoss => ItemID.PaintScraper,
            CoatingMode.HarvestMoss => ItemID.PaintScraper,
            // S11: ColorReplace shares the Paintbrush cursor — it is a paint
            // operation in the player's mental model, just one that targets
            // an existing colour rather than the brush colour.
            CoatingMode.ColorReplace => ItemID.Paintbrush,
            _ => ItemID.Paintbrush
        };
        player.cursorItemIconPush = 26;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(gold: 6);
    }

    public override bool? UseItem(Player player)
    {
        return TemplateUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        TemplateHoldItem(player);
    }

    /// <summary>
    /// S+2 2026-04-25 (S1): Eyedropper — Alt+left-click on a painted tile copies
    /// its colour into the active wand's <c>PaintColor</c> setting and suppresses
    /// the click so the wand doesn't also paint this frame. Per
    /// <c>SessionPlan_WSW_Next3Sessions.md §S+2 Task 5</c> = Decision 6.3 (a).
    ///
    /// <para>Sample source: in <see cref="CoatingMode.PaintWall"/> mode we read
    /// <c>tile.WallColor</c>; in all other modes we read <c>tile.TileColor</c>.
    /// An unpainted (zero) sample is a no-op so the user keeps the click and
    /// the wand proceeds normally — eyedropper only consumes when it has
    /// something useful to give back.</para>
    ///
    /// <para>Returning <c>false</c> from <see cref="CanUseItem"/> blocks the
    /// vanilla use cycle for the click frame; subsequent ticks resume normally.
    /// Local-player only — <c>CanUseItem</c> only fires on the click-owner's
    /// client, so the MP path is implicitly correct.</para>
    /// </summary>
    public override bool CanUseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            var keys = Main.keyState;
            bool altHeld = keys.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt)
                        || keys.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt);
            if (altHeld && TryEyedropper(player))
            {
                return false; // suppress this use; eyedropper consumed the click
            }
        }
        return base.CanUseItem(player);
    }

    private static bool TryEyedropper(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.CoatingSettings;

        // Sample tile under cursor.
        int tx = (int)(Main.MouseWorld.X / 16f);
        int ty = (int)(Main.MouseWorld.Y / 16f);
        if (!WorldGen.InWorld(tx, ty, fluff: 1)) return false;

        Tile tile = Main.tile[tx, ty];
        // Tile is a struct in modern Terraria; null check is harmless on legacy
        // and a no-op on current builds.

        // Mode-aware sampling: PaintWall reads wall paint, everything else
        // (PaintTile / ScrapeMoss / HarvestMoss) reads tile paint. The latter
        // three modes don't *paint*, but eyedropper still copies whatever's
        // visible so a quick mode-flip then re-click paints with the sampled
        // colour without the user having to re-pick.
        byte sampled = settings.Mode == CoatingMode.PaintWall
            ? tile.WallColor
            : tile.TileColor;

        if (sampled == 0)
        {
            // Unpainted tile — no useful sample. Don't consume the click; let
            // the wand's normal use cycle proceed.
            return false;
        }

        if (settings.PaintColor != sampled)
        {
            settings.PaintColor = sampled;
            // Brief audible + visual confirmation. Same SFX vanilla uses for
            // paint-related UI clicks.
            SoundEngine.PlaySound(SoundID.MenuTick, player.position);
        }

        return true; // consumed
    }

    public override void AddRecipes()
    {
        // Only the Instant variant is craftable.
        // Other modes are obtained via right-click cycling.
    }

    /// <summary>
    /// Appends a single localised hint about the Alt+click eyedropper. Done on every
    /// coating wand variant so a player inspecting any of them learns the shortcut.
    /// </summary>
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        // Reference UI.Coating.EyedropperHint so it satisfies Tier 3 orphan-lint and
        // surfaces in-game wherever the wand tooltip is shown.
        tooltips.Add(new TooltipLine(
            Mod,
            "WSW_CoatingEyedropperHint",
            Terraria.Localization.Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.EyedropperHint"))
        {
            OverrideColor = new Color(180, 180, 180)
        });
    }

    protected void ExecuteCoating(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.CoatingSettings;
        var selection = wandPlayer.GetVisualSelection();

        // (S11 2026-04-28; GrayJou worried-client review of S10) Color Replace
        // is now a fully-fledged member of the CoatingMode radio family.
        // Short-circuit to its own packet/loop so it can carry the
        // (source, target, channel) tuple that the regular CoatingOperation
        // packet doesn't model. Per S11 verbatim: *"if we select the
        // ReplacePaint, the Mode should be ReplacePaint (Selected) and
        // PaintTiles should be (Not Selected). [...] the action itself does
        // nothing because I can't select it. It still acts as if the mode
        // was PaintTiles even when I click ReplacePaint."* This branch is
        // the gameplay-side closure of that defect.
        if (settings.Mode == CoatingMode.ColorReplace)
        {
            ExecuteColorReplace(player, wandPlayer, settings, selection);
            return;
        }

        // --- MP: send packet to server and return early ---
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            CoatingPacketHandler.SendCoatingOperation(
                selection.StartTile, selection.EndTile,
                settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                settings.Mode, settings.PaintColor,
                settings.ApplyIlluminant, settings.IgnoreIlluminant,
                settings.ApplyEcho, settings.IgnoreEcho,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection, settings.Repaint);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);

        // Filter by active tile selection (Select Wand integration)
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swp.FilterBySelection(invertedTiles);

        int changed = 0;
        int skipped = 0;

        foreach (Point tile in invertedTiles)
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
            ShowNullResult(wandPlayer, "CoatingNoChanges", WandColors.MsgInfo);
            return;
        }

        var clientCfg = WandConfigs.Preferences;
        if (clientCfg?.EnableWandSounds == true)
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
            CoatingMode.PaintTile   => ApplyPaintTile(x, y, settings.PaintColor,
                settings.ApplyIlluminant, settings.IgnoreIlluminant,
                settings.ApplyEcho, settings.IgnoreEcho, settings.Repaint),
            CoatingMode.PaintWall   => ApplyPaintWall(x, y, settings.PaintColor,
                settings.ApplyIlluminant, settings.IgnoreIlluminant,
                settings.ApplyEcho, settings.IgnoreEcho, settings.Repaint),
            CoatingMode.ScrapePaint => ApplyScrapePaint(x, y),
            CoatingMode.ScrapeMoss  => ApplyScrapeMoss(x, y),
            CoatingMode.HarvestMoss => ApplyHarvestMoss(x, y),
            _                       => false
        };
#pragma warning restore CS0618
    }

    /// <summary>
    /// Special paint color value meaning "don't change the existing paint".
    /// When selected, the wand only applies/removes coatings without touching paint.
    /// Stored as byte 255 to avoid conflicting with vanilla PaintID range (0–30).
    /// </summary>
    public const byte IgnorePaintColor = 255;

    private static bool ApplyPaintTile(int x, int y, byte color,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile)
            return false;

        bool changed = false;

        // Apply paint color if different, or remove paint when color is 0 (None).
        // IgnorePaintColor (255) = don't touch the existing paint at all.
        // When repaint is false, skip tiles that already have paint applied.
        if (color != IgnorePaintColor && tile.TileColor != color)
        {
            if (!repaint && tile.TileColor != PaintID.None)
            {
                // Tile already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                WorldGen.paintTile(x, y, color, true);
                changed = true;
            }
        }

        // Apply or remove coatings based on toggle state.
        // paintCoatTile(x,y,0) removes BOTH Illuminant and Echo at once,
        // so we must carefully re-apply the one we want to keep.
        // When ignoreXxx is true, keep the existing state for that coating.
        bool hasIlluminant = tile.IsTileFullbright;
        bool hasEcho = tile.IsTileInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            // Clear all coatings first if we need to remove at least one
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatTile(x, y, 0, true);

            // (Re-)apply the coatings we want
            if (wantIlluminant && !tile.IsTileFullbright)
                WorldGen.paintCoatTile(x, y, 1, true);
            if (wantEcho && !tile.IsTileInvisible)
                WorldGen.paintCoatTile(x, y, 2, true);

            changed = true;
        }

        return changed;
    }

    private static bool ApplyPaintWall(int x, int y, byte color,
        bool applyIlluminant, bool ignoreIlluminant,
        bool applyEcho, bool ignoreEcho,
        bool repaint = true)
    {
        var tile = Main.tile[x, y];
        if (tile.WallType == WallID.None)
            return false;

        bool changed = false;

        // Apply paint color if different, or remove paint when color is 0 (None).
        // IgnorePaintColor (255) = don't touch the existing paint at all.
        // When repaint is false, skip walls that already have paint applied.
        if (color != IgnorePaintColor && tile.WallColor != color)
        {
            if (!repaint && tile.WallColor != PaintID.None)
            {
                // Wall already painted and repaint is off — skip paint but still apply coatings below
            }
            else
            {
                WorldGen.paintWall(x, y, color, true);
                changed = true;
            }
        }

        // Apply or remove coatings based on toggle state.
        // paintCoatWall(x,y,0) removes BOTH Illuminant and Echo at once,
        // so we must carefully re-apply the one we want to keep.
        // When ignoreXxx is true, keep the existing state for that coating.
        bool hasIlluminant = tile.IsWallFullbright;
        bool hasEcho = tile.IsWallInvisible;
        bool wantIlluminant = ignoreIlluminant ? hasIlluminant : applyIlluminant;
        bool wantEcho = ignoreEcho ? hasEcho : applyEcho;

        if (hasIlluminant != wantIlluminant || hasEcho != wantEcho)
        {
            // Clear all coatings first if we need to remove at least one
            if ((hasIlluminant && !wantIlluminant) || (hasEcho && !wantEcho))
                WorldGen.paintCoatWall(x, y, 0, true);

            // (Re-)apply the coatings we want
            if (wantIlluminant && !tile.IsWallFullbright)
                WorldGen.paintCoatWall(x, y, 1, true);
            if (wantEcho && !tile.IsWallInvisible)
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

    // ============================================================================
    //  Color Replace execution path (S11 2026-04-28 \u2014 promoted from
    //  CoatingSettingsPanel.FireColorReplace where it lived in S10 as a
    //  panel-button action. With Color Replace now a CoatingMode the
    //  canonical entry point is ExecuteCoating, which delegates here.)
    // ============================================================================

    /// <summary>
    /// Executes the Color Replace operation for the given player + selection.
    /// Mirrors <see cref="ExecuteCoating"/>'s MP / SP split:
    /// <list type="bullet">
    ///   <item>MP client: send <see cref="WandPacketType.ColorReplaceOperation"/>
    ///     packet and return; server-side handler runs the same loop with
    ///     re-validation and broadcasts the changes.</item>
    ///   <item>SP / server-local: iterate the shape's tile set, repaint any
    ///     tile/wall whose current paint matches
    ///     <see cref="WandOfCoatingSettings.ColorReplaceSource"/> to
    ///     <see cref="WandOfCoatingSettings.ColorReplaceTarget"/> on the
    ///     <see cref="WandOfCoatingSettings.ColorReplaceChannel"/> channel.</item>
    /// </list>
    /// Silent fall-through if either side is Ignore (255), or source==target
    /// (no-op tuples per ColorReplacePlan.md \u00a70.4 / \u00a76).
    /// </summary>
    private static void ExecuteColorReplace(
        Player player, WandPlayer wandPlayer,
        WandOfCoatingSettings s, Common.Selection.SelectionState selection)
    {
        // \u00a70.4 silent fall-through.
        if (s.ColorReplaceSource == 255 || s.ColorReplaceTarget == 255)
            return;
        if (s.ColorReplaceSource == s.ColorReplaceTarget)
            return;

        if (!selection.IsActive)
        {
            // The wand only fires on a confirmed cast, so an empty selection
            // here means the player tried to fire without committing one.
            ShowNullResultRaw(wandPlayer,
                Terraria.Localization.Language.GetTextValue(
                    "Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.NoSelection"),
                WandColors.MsgInfo);
            return;
        }

        // \u2500\u2500 MP: send packet to server and return early \u2500\u2500
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            CoatingPacketHandler.SendColorReplaceOperation(
                selection.StartTile, selection.EndTile,
                s.Shape.Shape, s.Shape.FillMode,
                s.Shape.Thickness, s.Shape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                s.ColorReplaceSource, s.ColorReplaceTarget,
                s.ColorReplaceChannel,
                s.Shape.Slice, s.Shape.ConnectDiameter,
                s.Shape.InvertSelection);
            return;
        }

        // \u2500\u2500 SP: resolve the shape's tile set and repaint locally \u2500\u2500
        var context = s.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);
        var tileSet = ShapeRegistry.GetShapeTiles(s.Shape.Shape, context);
        var invertedTiles = s.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);
        var swp = player.GetModPlayer<DelimitationWandPlayer>();
        invertedTiles = swp.FilterBySelection(invertedTiles);

        bool wallChannel = s.ColorReplaceChannel == ColorReplaceChannel.Wall;
        int changed = 0;
        foreach (var p in invertedTiles)
        {
            int x = p.X, y = p.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;

            if (wallChannel)
            {
                if (Common.Systems.SafekeepingSystem.IsWallProtected(x, y)) continue;
                var tile = Main.tile[x, y];
                if (tile.WallType == WallID.None) continue;
                if (tile.WallColor != s.ColorReplaceSource) continue;
                WorldGen.paintWall(x, y, s.ColorReplaceTarget, true);
                changed++;
            }
            else
            {
                if (Common.Systems.SafekeepingSystem.IsTileProtected(x, y)) continue;
                var tile = Main.tile[x, y];
                if (!tile.HasTile) continue;
                if (tile.TileColor != s.ColorReplaceSource) continue;
                WorldGen.paintTile(x, y, s.ColorReplaceTarget, true);
                changed++;
            }
        }

        if (changed == 0)
        {
            ShowNullResultRaw(wandPlayer,
                Terraria.Localization.Language.GetTextValue(
                    "Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.NoChanges"),
                WandColors.MsgInfo);
            return;
        }

        // Broadcast the changed bounding rect for non-MP-client paths.
        NetMessage.SendTileSquare(-1,
            System.Math.Min(selection.StartTile.X, selection.EndTile.X),
            System.Math.Min(selection.StartTile.Y, selection.EndTile.Y),
            System.Math.Abs(selection.EndTile.X - selection.StartTile.X) + 1,
            System.Math.Abs(selection.EndTile.Y - selection.StartTile.Y) + 1);

        var clientCfg = WandConfigs.Preferences;
        if (clientCfg?.EnableWandSounds == true)
            SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.6f }, player.Center);

        Main.NewText(
            Terraria.Localization.Language.GetTextValue(
                "Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.Changed", changed),
            WandColors.MsgCoating);
    }
}