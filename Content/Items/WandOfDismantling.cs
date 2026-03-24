using Microsoft.Xna.Framework;
using System;
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
using WorldShapingWandsMod.Common.Networking;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Utilities;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Content.Items;

// Base destruction logic shared by all modes
public abstract class WandOfDismantlingBase : BaseCyclingWand
{
    public override string WandBaseName => "Wand of Dismantling";
    public override string WandLore => "The Deity of Erasure lets you restore nothingness as you wish.";
    
    protected abstract bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile);

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

    // NEW: Base HoldItem handles right-click cancellation for all modes
    public override void HoldItem(Player player)
    {
        var wandPlayer = player.GetModPlayer<WandPlayer>();
        
        // Only cancel on right-click in the WORLD, not when clicking in inventory/UI.
        if (!Main.LocalPlayer.mouseInterface
            && wandPlayer.Selection.IsActive && Main.mouseRight && Main.mouseRightRelease)
        {
            CancelSelection(wandPlayer);
            Main.mouseRightRelease = false; // Consume the click to prevent other actions
        }
    }

    // NEW: Virtual method so derived classes can add behavior on cancel
    protected virtual void CancelSelection(WandPlayer wandPlayer)
    {
        wandPlayer.CancelSelection(WandColors.CancelDismantling, wandPlayer.DismantlingSettings.Shape);
    }

    protected void ExecuteDismantling(Player player, WandPlayer wandPlayer)
    {
        var settings = wandPlayer.DismantlingSettings;

        // If VoidEverything toggle is active, redirect to VoidEverythingOperation
        if (settings.VoidEverything)
        {
            VoidEverythingOperation.Execute(player);
            return;
        }

        var selection = wandPlayer.GetVisualSelection();
        var config = ModContent.GetInstance<WandServerConfig>();
        var clientCfg = ModContent.GetInstance<WandClientConfig>();

        // ── Multiplayer: send packet to server instead of executing locally ──
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            WandPacketHandler.SendDismantlingOperation(
                selection.StartTile, selection.EndTile,
                settings.Shape.Shape, settings.Shape.FillMode,
                settings.Shape.Thickness, settings.Shape.EqualDimensions,
                selection.VerticalFirst, player.whoAmI,
                settings.DestroyTiles, settings.DestroyWalls, settings.DestroyContainers,
                settings.Shape.Slice, settings.Shape.ConnectDiameter,
                settings.Shape.InvertSelection);
            return;
        }

        var context = settings.Shape.ToShapeContext(
            selection.StartTile, selection.EndTile, selection.VerticalFirst);

        var tileSet = ShapeRegistry.GetShapeTiles(settings.Shape.Shape, context);
        var invertedTiles = settings.Shape.ApplyInversion(tileSet.Tiles.ToArray(), context);
        var undoMgr = player.GetModPlayer<UndoManager>();
        var action = undoMgr.BeginAction("Dismantling");

        // Pre-declare collections shared between container pass and tile pass
        var validTiles = new List<ProgressiveTileProcessor.TileDismantlingInfo>();
        var snapshottedTiles = new HashSet<Point>();
        int skipped = 0;

        // === CONTAINER DESTRUCTION PASS ===
        // Must happen BEFORE tile destruction — Chest.FindChest needs tiles intact.
        var containerTiles = new HashSet<Point>();
        int containersDestroyed = 0;
        int containerItemsDropped = 0;

        if (settings.DestroyContainers && settings.DestroyTiles)
        {
            var containers = ContainerHelper.FindContainers(invertedTiles);
            foreach (var container in containers)
            {
                if (SafekeepingSystem.IsProtected(container.TopLeft.X, container.TopLeft.Y))
                    continue;

                // Skip locked containers unless AutoOpenChestsOnDestruction is enabled.
                // IMPORTANT: Still add their tiles to containerTiles so the regular tile
                // destruction pass doesn't destroy them via WorldGen.KillTile (which would
                // bypass the lock check entirely).
                if (container.IsLocked && !config.EffectiveAutoOpenChestsOnDestruction)
                {
                    var lockedData = Terraria.ObjectData.TileObjectData.GetTileData(container.TileType, 0);
                    int lw = lockedData?.Width ?? 2;
                    int lh = lockedData?.Height ?? 2;
                    for (int dx = 0; dx < lw; dx++)
                        for (int dy = 0; dy < lh; dy++)
                            containerTiles.Add(new Point(container.TopLeft.X + dx, container.TopLeft.Y + dy));
                    continue;
                }

                // Snapshot all container tiles before destruction
                var data = Terraria.ObjectData.TileObjectData.GetTileData(container.TileType, 0);
                int cw = data?.Width ?? 2;
                int ch = data?.Height ?? 2;
                for (int dx = 0; dx < cw; dx++)
                    for (int dy = 0; dy < ch; dy++)
                    {
                        var pt = new Point(container.TopLeft.X + dx, container.TopLeft.Y + dy);
                        if (!snapshottedTiles.Contains(pt))
                        {
                            action.AddSnapshot(pt);
                            snapshottedTiles.Add(pt);
                        }
                        containerTiles.Add(pt);
                    }

                var (dropped, destroyed) = ContainerHelper.DestroyContainer(
                    player, container, config.EffectiveSuppressDrops);

                if (destroyed)
                {
                    containersDestroyed++;
                    containerItemsDropped += dropped;
                }
            }
        }

        // Pre-validate all tiles and snapshot them
        // Sort tiles top-to-bottom (ascending Y) so multi-tile objects (trees, tall plants,
        // furniture) above are destroyed first, freeing the support tiles below.
        // Without this, WorldGen.CanKillTile returns false for blocks under trees during
        // pre-validation, causing them to be skipped even though the tree would be gone
        // by the time we reach that block during execution.
        var sortedTiles = invertedTiles.ToArray();
        Array.Sort(sortedTiles, (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        foreach (Point tile in sortedTiles)
        {
            if (!WorldGen.InWorld(tile.X, tile.Y, 1)) continue;
            if (SafekeepingSystem.IsProtected(tile.X, tile.Y)) { skipped++; continue; }

            // Skip tiles already destroyed by the container pass
            if (containerTiles.Contains(tile)) continue;

            var tileData = Main.tile[tile.X, tile.Y];

            // Skip empty space (no tile and no wall) silently — don't count as "skipped"
            if (!tileData.HasTile && tileData.WallType <= WallID.None) continue;

            // Include tiles that pass pickaxe power check even if CanKillTile currently
            // returns false — the tile above may be removed by the time this one is
            // processed (e.g., blocks under trees). The actual CanKillTile check is
            // deferred to execution time in both instant and progressive paths.
            bool willDestroyTile = settings.DestroyTiles
                && tileData.HasTile
                && (config.EffectiveBypassPickaxePower || player.HasEnoughPickPowerToHurtTile(tile.X, tile.Y));

            // Demon Altars: skip unless explicitly allowed in config.
            // When AllowDemonAltarDestruction is OFF, altars are always protected.
            // When it IS ON, require either sufficient hammer power (≥80 = Pwnhammer tier)
            // or the BypassPickaxePower config to also be enabled.
            if (willDestroyTile && tileData.TileType == TileID.DemonAltar)
            {
                if (!config.EffectiveAllowDemonAltarDestruction)
                    willDestroyTile = false;
                else if (GetPlayerMaxHammerPower(player) < 80 && !config.EffectiveBypassPickaxePower)
                    willDestroyTile = false;
            }

            // Delicate tiles: Shadow Orbs / Crimson Hearts, Plantera's Bulbs,
            // Bee Larvae, Life Crystals, Life Fruit — skip unless explicitly allowed.
            // These tiles have irreversible side effects (boss spawns, world flags, etc.).
            if (willDestroyTile && IsDelicateTile(tileData.TileType) && !config.EffectiveAllowDelicateTileDestruction)
            {
                willDestroyTile = false;
            }

            bool willDestroyWall = settings.DestroyWalls
                && tileData.WallType > WallID.None;

            if (!willDestroyTile && !willDestroyWall) { skipped++; continue; }

            // Take a single snapshot per tile before any modification
            if (!snapshottedTiles.Contains(tile))
            {
                action.AddSnapshot(tile);
                snapshottedTiles.Add(tile);
            }

            validTiles.Add(new ProgressiveTileProcessor.TileDismantlingInfo
            {
                Position = tile,
                DestroyTile = willDestroyTile,
                DestroyWall = willDestroyWall,
                SuppressDrops = config.EffectiveSuppressDrops
            });
        }

        if (validTiles.Count == 0 && containersDestroyed == 0)
        {
            Main.NewText(Get("NoTilesDestroyed"), Color.Gray);
            return;
        }

        // Build container message suffix for result reporting
        string containerMsg = containersDestroyed > 0
            ? $", {containersDestroyed} container(s) cleared"
              + (containerItemsDropped > 0 ? $" ({containerItemsDropped} item stack(s) dropped)" : "")
            : "";

        // If only containers were destroyed with no remaining tiles, commit and return
        if (validTiles.Count == 0 && containersDestroyed > 0)
        {
            undoMgr.CommitAction(action);
            if (clientCfg?.EnableWandSounds == true)
                SoundEngine.PlaySound(SoundID.Tink, player.Center);
            Main.NewText($"Cleared {containersDestroyed} container(s)" +
                (containerItemsDropped > 0 ? $" ({containerItemsDropped} item stack(s) dropped)" : ""),
                Color.OrangeRed);
            return;
        }

        // Branch: progressive mode (with drops/sounds) vs instant mode (silent, no drops)
        bool useProgressive = config != null && config.EnableProgressiveMode;

        if (useProgressive)
        {
            // Progressive: enqueue batches for timed processing
            // Tiles are NOT killed yet — the processor will do it with natural effects
            int batchSize = config.ProgressiveBatchSize;
            float interval = config.ProgressiveInterval;

            ProgressiveTileProcessor.EnqueueDismantling(
                player, validTiles, action, undoMgr, batchSize, interval,
                vacuumItems: config.VacuumItems);

            int batches = (int)Math.Ceiling((double)validTiles.Count / batchSize);
            float totalTime = (batches - 1) * interval;
            Main.NewText(
                $"Dismantling {validTiles.Count} tile(s) in {batches} wave(s) (~{totalTime:F1}s)" +
                (skipped > 0 ? $", {skipped} skipped" : "") + containerMsg,
                Color.OrangeRed);
        }
        else
        {
            // Instant: process all at once
            // In single-player, WorldGen.gen controls per-tile sounds/dust/gore AND item drops.
            // We only set gen=true when we actually want to suppress drops:
            //   - SuppressDrops=true → gen=true (no items created, vacuum irrelevant)
            //   - SuppressDrops=false + VacuumItems=true → gen=false (items spawn, vacuum collects them)
            //   - SuppressDrops=false + VacuumItems=false → gen=false (items stay on ground)
            // In multiplayer: always leave gen=false so each KillTile/KillWall sends its
            // own TileManipulation message to the server.
            int destroyedTiles = 0;
            var affectedPositions = new List<Point>();
            bool isMultiplayer = Main.netMode == NetmodeID.MultiplayerClient;
            bool wantVacuum = config.VacuumItems && !config.EffectiveSuppressDrops;

            // Pre-compute the full operation bounds from ALL tiles (not just destroyed ones)
            // so that periodic vacuum sweeps cover cascaded drops from multi-tile objects
            // (trees, bamboo, etc.) that spawn items well outside the explicit tile set.
            Rectangle fullOperationBounds = Rectangle.Empty;
            if (wantVacuum)
                fullOperationBounds = BulkTileOperations.ComputeBounds(
                    validTiles.ConvertAll(t => t.Position));

            bool wasGen = WorldGen.gen;
            if (!isMultiplayer && config.EffectiveSuppressDrops)
                WorldGen.gen = true;

            // Periodic vacuum interval: sweep ground items every N tile destructions
            // to prevent hitting Terraria's 400-item ground cap (Main.maxItems).
            // Each KillTile can cascade via SquareTileFrame to destroy adjacent
            // unsupported tiles (trees, bamboo, torches), creating many more items
            // than the explicit tile count. A sweep every 200 tiles keeps the
            // ground item count well below 400 even with heavy cascading.
            const int VacuumSweepInterval = 200;
            int tilesSinceVacuum = 0;

            foreach (var info in validTiles)
            {
                if (info.DestroyTile)
                {
                    // Re-check CanKillTile at execution time: a tile that was unkillable
                    // during pre-validation (e.g., block under a tree) may now be killable
                    // because the tree above was already destroyed in this same pass.
                    if (!Main.tile[info.Position.X, info.Position.Y].HasTile)
                    {
                        // Tile was already destroyed (e.g., part of a multi-tile object
                        // that collapsed when its anchor was killed)
                    }
                    else if (!WorldGen.CanKillTile(info.Position.X, info.Position.Y))
                    {
                        // Still can't kill — skip it
                    }
                    else
                    {
                        WorldGen.KillTile(info.Position.X, info.Position.Y,
                            fail: false, effectOnly: false, noItem: info.SuppressDrops);
                        destroyedTiles++;
                        affectedPositions.Add(info.Position);
                        tilesSinceVacuum++;
                    }
                }

                if (info.DestroyWall)
                {
                    WorldGen.KillWall(info.Position.X, info.Position.Y);
                    if (!info.DestroyTile)
                        affectedPositions.Add(info.Position);
                }

                // Periodic vacuum sweep: collect items before the 400-item cap is hit.
                // Uses the full operation bounds (pre-computed from ALL tiles) so that
                // cascaded drops from trees/bamboo/multi-tile objects above/beside the
                // selection are also captured.
                if (wantVacuum && tilesSinceVacuum >= VacuumSweepInterval)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                    tilesSinceVacuum = 0;
                }
            }

            WorldGen.gen = wasGen;

            if (affectedPositions.Count > 0)
            {
                if (isMultiplayer)
                    BulkTileOperations.FinalizeFrameOnly(affectedPositions);
                else
                    BulkTileOperations.FinalizeBatch(affectedPositions);

                // Final vacuum sweep: catch any remaining items from the last batch
                // of tile destructions and from FinalizeBatch's frame updates
                // (which can cascade-destroy additional unsupported tiles).
                if (wantVacuum)
                {
                    BulkTileOperations.VacuumItemsInArea(player, fullOperationBounds);
                }
            }

            undoMgr.CommitAction(action);

            // Play completion sound — always when wand sounds enabled.
            // When SuppressDrops is ON (WorldGen.gen = true), Terraria suppresses all
            // per-tile sounds, so this is the only audio feedback the player gets.
            // When SuppressDrops is OFF, this serves as a distinct "operation complete" bookend.
            if (clientCfg?.EnableWandSounds == true)
            {
                SoundEngine.PlaySound(SoundID.Tink, player.Center);
            }

            Main.NewText($"Destroyed {destroyedTiles} tile(s)" +
                (skipped > 0 ? $", {skipped} skipped" : "") + containerMsg,
                Color.OrangeRed);
        }
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        // This still handles right-click when NOT channeling
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

    /// <summary>
    /// Returns the highest hammer power among all items in the player's inventory.
    /// Used to determine if the player can break a Demon Altar (vanilla requires ≥80,
    /// i.e. Pwnhammer tier) without relying on <c>Main.hardMode</c>, which can be
    /// misleading when mods grant high-power hammers before hardmode triggers.
    /// </summary>
    private static int GetPlayerMaxHammerPower(Player player)
    {
        int max = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            var item = player.inventory[i];
            if (!item.IsAir && item.hammer > max)
                max = item.hammer;
        }
        return max;
    }

    /// <summary>
    /// Returns true if the tile type is "delicate" — destroying it has irreversible
    /// side effects (boss spawns, world flags, unique loot).
    /// Protected by AllowDelicateTileDestruction config.
    /// </summary>
    private static bool IsDelicateTile(int tileType)
    {
        return tileType == TileID.ShadowOrbs        // Shadow Orb / Crimson Heart
            || tileType == TileID.PlanteraBulb       // Plantera's Bulb
            || tileType == TileID.Larva              // Bee Larva (Queen Bee)
            || tileType == TileID.LifeFruit;         // Life Fruit
    }

    public override void AddRecipes()
    {
        // Only the Instant variant has a craftable recipe.
        // Other modes are obtained via right-click cycling in inventory.
    }
}

// OneClick Mode - Click and drag
public class WandOfDismantlingInstant : WandOfDismantlingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.OneClick;
    public override Color ModeColor => new Color(255, 80, 80); // Red — Instant (dangerous)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDismantlingSelect>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.channel = true; // Enable channeling for drag
        Item.UseSound = null; // prevent sound spam during drag — played once on selection start
    }

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        // All logic handled in HoldItem for instant/drag mode
        return false;
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);

        if (Main.myPlayer != player.whoAmI)
            return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        Point mouseTile = GeometryHelper.GetMouseTile();

        if (Main.mouseLeft)
        {
            // Don't start selection if mouse is over UI
            // IsMouseOverUI() checks both mouseInterface AND panel hover,
            // because HoldItem runs before UI.Update sets mouseInterface.
            if (IsMouseOverUI())
                return;

            // Don't restart selection immediately after cancellation
            if (!wandPlayer.CanStartNewSelection())
                return;

            if (!wandPlayer.InstantSelection.IsActive)
            {
                bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                                Math.Abs(Main.MouseWorld.X - player.Center.X);
                wandPlayer.StartInstantSelection(mouseTile, vertical);
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }
            wandPlayer.UpdateInstantSelection(mouseTile);

            // Right-click during a drag cancels the selection without executing.
            if (Main.mouseRight && wandPlayer.InstantSelection.IsActive)
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelDismantling, wandPlayer.DismantlingSettings.Shape);
                return;
            }
        }
        else if (wandPlayer.InstantSelection.IsActive)
        {
            // Don't execute if mouse released over UI (e.g. NPC shop)
            if (IsMouseOverUI())
            {
                wandPlayer.CancelInstantSelection(WandColors.CancelDismantling, wandPlayer.DismantlingSettings.Shape);
                return;
            }

            // Mouse released - execute only if this wand started the selection
            if (wandPlayer.IsInstantSelectionOwnedByCurrentItem() && !IsOnLocalCooldown())
            {
                ExecuteDismantling(player, wandPlayer);
            }
            wandPlayer.ClearInstantSelection();
        }
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddIngredient(ItemID.Rope, 20)
            .AddRecipeGroup(WandRecipeSystem.AnyGemKey, 5)
            .AddIngredient(ItemID.Dynamite, 100)
            .AddIngredient(ItemID.ManaCrystal, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

// TwoClick Mode - Click start, click end
public class WandOfDismantlingSelect : WandOfDismantlingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.TwoClick;
    public override Color ModeColor => new Color(255, 255, 80); // Yellow — Select (caution)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDismantlingConfirm>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // First click - start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickAgain", "confirm area"), Color.Cyan);
            return false; // Don't consume the wand
        }
        else
        {
            // Second click - execute
            if (IsOnLocalCooldown()) return false;
            wandPlayer.UpdateSelection(mouseTile);
            ExecuteDismantling(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfDismantlingInstant>(1)
            .AddCustomShimmerResult(ItemID.Wood, 10)
            .AddCustomShimmerResult(ItemID.Rope, 20)
            .AddCustomShimmerResult(ItemID.Amethyst, 5)
            .AddCustomShimmerResult(ItemID.Dynamite, 50)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}

// ThreeClick Mode - Click start, click end, click confirm
public class WandOfDismantlingConfirm : WandOfDismantlingBase
{
    public override SelectionMode WandSelectionMode => SelectionMode.ThreeClick;
    public override Color ModeColor => new Color(80, 255, 80); // Green — Confirm (safe)
    public override int GetNextModeItemType() => ModContent.ItemType<WandOfDismantlingStamp>();

    protected override bool HandleUseItem(Player player, WandPlayer wandPlayer, Point mouseTile)
    {
        if (!wandPlayer.Selection.IsActive)
        {
            // First click - start selection
            bool vertical = Math.Abs(Main.MouseWorld.Y - player.Center.Y) >
                            Math.Abs(Main.MouseWorld.X - player.Center.X);
            wandPlayer.StartSelection(mouseTile, vertical);
            Main.NewText(Get("SelectStartClickEnd"), Color.Cyan);
            return false; // Don't consume the wand
        }
        else if (!wandPlayer.Selection.IsLocked)
        {
            // Second click - lock selection, await confirmation
            wandPlayer.UpdateSelection(mouseTile);
            wandPlayer.LockSelection();  // LOCK IT HERE
            Main.NewText(Get("ClickToConfirmOrCancel"), Color.Yellow);
            return false; // Don't consume the wand
        }
        else
        {
            // Third click - execute
            if (IsTooFarToConfirm(wandPlayer.Selection, mouseTile)) return false;
            if (IsOnLocalCooldown()) return false;
            ExecuteDismantling(player, wandPlayer);
            wandPlayer.ClearSelection();
            return false; // Don't consume the wand
        }
    }

    public override bool CanUseItem(Player player)
    {
        return base.CanUseItem(player);
    }

    public override void AddRecipes()
    {
        WandRecipeConditions.Register(Type);
        CreateRecipe()
            .AddIngredient<WandOfDismantlingInstant>(1)
            .AddCustomShimmerResult(ItemID.Wood, 10)
            .AddCustomShimmerResult(ItemID.Rope, 20)
            .AddCustomShimmerResult(ItemID.Amethyst, 5)
            .AddCustomShimmerResult(ItemID.Dynamite, 50)
            .AddCustomShimmerResult(ItemID.ManaCrystal, 1)
            .AddCondition(WandRecipeConditions.NonCraftable)
            .Register();
    }
}
