using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Items;

/// <summary>
/// Provides helper methods for torch placement validation, first-torch seeding,
/// biome-torch resolution, and waterproof torch detection.
/// Fully supports both vanilla and modded torches via <c>TileID.Sets.Torch</c>.
/// </summary>
public static class TorchPlacementHelper
{
    // ── Cached Lookup Tables (built at PostSetupContent) ─────────

    /// <summary>
    /// Maps (tileType, placeStyle) → itemType for all torch items.
    /// Built once during <see cref="BuildTorchLookup"/> after all mods load.
    /// </summary>
    private static Dictionary<(int tileType, int placeStyle), int> _torchTileToItem;

    /// <summary>
    /// Set of (tileType, placeStyle) combinations that can be placed underwater.
    /// Built once during <see cref="BuildTorchLookup"/>.
    /// </summary>
    private static HashSet<(int tileType, int placeStyle)> _waterproofTorches;

    /// <summary>
    /// Set of (tileType, placeStyle) combinations that are biome torches placed by
    /// mods via <see cref="ModBiome.BiomeTorchItemType"/>. Vanilla biome torches are
    /// handled separately via <see cref="_vanillaBiomeTorchStyles"/> (style-indexed
    /// on TileID.Torches). This cache covers modded biome torches which use entirely
    /// different tile types.
    /// </summary>
    private static HashSet<(int tileType, int placeStyle)> _moddedBiomeTorchTiles;

    /// <summary>
    /// Vanilla biome torch styles for TileID.Torches (type 4).
    /// These are the styles that <c>Player.BiomeTorchPlaceStyle</c> can produce.
    /// Style 0 (plain torch) is excluded — it is the "no biome" default.
    /// </summary>
    private static readonly HashSet<int> _vanillaBiomeTorchStyles = new HashSet<int>
    {
        1,  // Blue      (Snow biome)
        7,  // Ice       (Snow biome alt)
        9,  // Bone      (Underground/Dungeon)
        13, // Jungle    (Jungle biome)
        16, // Desert    (Desert biome)
        17, // Demon     (Corruption biome)
        18, // Crimson   (Crimson biome)
        19, // Hallowed  (Hallow biome)
        20, // Mushroom  (Glowing Mushroom biome)
        21, // Coral     (Ocean biome)
        22, // Corrupt   (Corruption alt)
        23, // Shimmer   (Shimmer / Aether biome)
    };

    /// <summary>
    /// Builds the cached torch lookup tables. Must be called from
    /// <c>ModSystem.PostSetupContent</c> after all mod items are registered.
    /// </summary>
    public static void BuildTorchLookup()
    {
        _torchTileToItem = new Dictionary<(int, int), int>();
        _waterproofTorches = new HashSet<(int, int)>();

        for (int i = 0; i < ItemLoader.ItemCount; i++)
        {
            var sample = ContentSamples.ItemsByType[i];
            if (sample == null || sample.createTile == -1)
                continue;

            if (!TileID.Sets.Torch[sample.createTile])
                continue;

            var key = (sample.createTile, sample.placeStyle);

            // First item wins for a given tile+style combination
            if (!_torchTileToItem.ContainsKey(key))
                _torchTileToItem[key] = i;

            // Track waterproof torches: noWet == false means CAN be placed in water
            // (DefaultToTorch sets noWet based on the allowWaterPlacement parameter).
            // Also check ItemID.Sets.WaterTorches to catch edge cases where a mod
            // sets WaterTorches[itemType] = true without clearing noWet (unusual but
            // possible — tModLoader's engine respects WaterTorches for hold-style
            // overrides independently of noWet).
            if (!sample.noWet || ItemID.Sets.WaterTorches[i])
                _waterproofTorches.Add(key);
        }

        // Build modded biome torch cache from all loaded ModBiomes.
        // Each ModBiome with a BiomeTorchItemType registers the (tileType, placeStyle)
        // of its torch item, so IsBiomeTorch can recognize modded biome torches.
        _moddedBiomeTorchTiles = new HashSet<(int, int)>();
        foreach (var biome in ModContent.GetContent<ModBiome>())
        {
            int itemType = biome.BiomeTorchItemType;
            if (itemType <= 0 || !ContentSamples.ItemsByType.ContainsKey(itemType))
                continue;

            var item = ContentSamples.ItemsByType[itemType];
            if (item == null || item.createTile < 0)
                continue;

            if (!TileID.Sets.Torch[item.createTile])
                continue;

            _moddedBiomeTorchTiles.Add((item.createTile, item.placeStyle));
        }
    }

    /// <summary>
    /// Clears cached lookup tables. Call from <c>ModSystem.Unload</c>.
    /// </summary>
    public static void UnloadTorchLookup()
    {
        _torchTileToItem = null;
        _waterproofTorches = null;
        _moddedBiomeTorchTiles = null;
    }

    // ── Biome Torch Identification ─────────────────────────────────

    /// <summary>
    /// Determines whether the tile at a given position is a biome torch — i.e.,
    /// a torch that was placed by the biome torch system (Torch God's Favor or
    /// modded biome resolution). Decorative torches (Rainbow, Ultrabright, etc.)
    /// return false, so they are never accidentally converted.
    /// </summary>
    /// <remarks>
    /// <b>Layer 1 — Vanilla</b>: Tile type <see cref="TileID.Torches"/> (type 4)
    /// uses <see cref="_vanillaBiomeTorchStyles"/> indexed by <c>frameY / 22</c>.
    /// Style 0 (plain torch) IS in the set but is excluded here because converting
    /// plain torches matches Torch God's Favor behavior — plain torches are the
    /// "no biome" default, not an intentional biome selection.
    /// <para/>
    /// <b>Layer 2 — Modded</b>: Modded biome torches are entirely different tile
    /// types (not TileID 4). They're identified via the
    /// <see cref="_moddedBiomeTorchTiles"/> cache built from
    /// <see cref="ModBiome.BiomeTorchItemType"/> during <see cref="BuildTorchLookup"/>.
    /// </remarks>
    /// <param name="tile">The tile to check.</param>
    /// <returns>True if the tile is a biome torch (vanilla or modded), false otherwise.</returns>
    public static bool IsBiomeTorch(Tile tile)
    {
        if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
            return false;

        // Layer 1: Vanilla torches (TileID.Torches, type 4)
        if (tile.TileType == TileID.Torches)
        {
            int style = tile.TileFrameY / 22;

            // Exclude the base torch (style 0) — it's the "no biome" default.
            // Players who placed regular torches did so intentionally.
            // This matches Torch God's Favor behavior: it only converts
            // biome torches that are mismatched, never plain torches.
            if (style == 0)
                return false;

            return _vanillaBiomeTorchStyles.Contains(style);
        }

        // Layer 2: Modded biome torches (different tile types entirely)
        if (_moddedBiomeTorchTiles != null)
        {
            int moddedStyle = tile.TileFrameY / 22;
            return _moddedBiomeTorchTiles.Contains((tile.TileType, moddedStyle));
        }

        return false;
    }

    // ── Torch Item Detection ─────────────────────────────────────

    /// <summary>
    /// Determines if an item is a torch item and returns its placement info.
    /// Works for all vanilla AND modded torches.
    /// </summary>
    public static bool IsTorchItem(Item item, out int tileType, out int placeStyle)
    {
        tileType = item.createTile;
        placeStyle = item.placeStyle;

        if (tileType < 0) return false;
        return TileID.Sets.Torch[tileType];
    }

    /// <summary>
    /// Finds the item type that places a specific torch tile+style combination.
    /// Uses the cached lookup built at PostSetupContent.
    /// Works for all vanilla AND modded torches.
    /// </summary>
    public static int GetItemForTorchTile(int tileType, int placeStyle)
    {
        if (_torchTileToItem == null) return -1;
        return _torchTileToItem.TryGetValue((tileType, placeStyle), out int itemType)
            ? itemType : -1;
    }

    // ── Placement Validity ───────────────────────────────────────────

    /// <summary>
    /// Checks whether a torch can be placed at the specified world tile position.
    /// A torch needs a solid tile adjacent (below, left, or right) OR a wall behind it,
    /// and the tile itself must be empty (or an existing torch if overwriting).
    /// Detects both vanilla and modded torches via <c>TileID.Sets.Torch</c>.
    /// </summary>
    /// <remarks>
    /// Terraria natively supports torches attached to the side of solid blocks
    /// (not just sitting on top). The previous version only checked for solid below
    /// or wall behind, which caused the TorchWheel projectile to skip valid positions
    /// on vertical wall faces.
    /// </remarks>
    public static bool IsValidTorchPosition(int x, int y, bool allowOverwrite = false)
    {
        if (!WorldGen.InWorld(x, y, 1))
            return false;

        var tile = Main.tile[x, y];

        // Position must be empty, or an existing torch (if overwrite is on)
        if (tile.HasTile)
        {
            if (!allowOverwrite || !TileID.Sets.Torch[tile.TileType])
                return false;
        }

        // Must have a wall behind OR a solid tile adjacent (below, left, or right).
        // Terraria torches can attach to the side of solid blocks — the game's
        // TileObjectData system handles orientation automatically via PlaceTile.
        bool hasWallBehind = tile.WallType > WallID.None;
        if (hasWallBehind)
            return true;

        // Check solid below
        if (WorldGen.InWorld(x, y + 1, 1))
        {
            var below = Main.tile[x, y + 1];
            if (below.HasTile && WorldGen.SolidTile(below))
                return true;
        }

        // Check solid to the left
        if (WorldGen.InWorld(x - 1, y, 1))
        {
            var left = Main.tile[x - 1, y];
            if (left.HasTile && WorldGen.SolidTile(left))
                return true;
        }

        // Check solid to the right
        if (WorldGen.InWorld(x + 1, y, 1))
        {
            var right = Main.tile[x + 1, y];
            if (right.HasTile && WorldGen.SolidTile(right))
                return true;
        }

        return false;
    }

    // ── First Torch Finding ──────────────────────────────────────────

    /// <summary>
    /// Finds the best seed position for torch tiling within a set of candidate positions.
    /// Scans top-down, left-right for the first valid placement, then checks nearby
    /// for an existing torch to align the grid to.
    /// </summary>
    /// <param name="positions">All tile positions in the selection shape.</param>
    /// <param name="selectionBounds">Bounding rectangle of the selection.</param>
    /// <param name="spacingX">Horizontal spacing for nearby search.</param>
    /// <param name="spacingY">Vertical spacing for nearby search.</param>
    /// <param name="allowOverwrite">Whether existing torches count as valid positions.</param>
    /// <param name="alignToExisting">When true, snap onto a nearby existing torch if one is found.</param>
    /// <returns>The seed position, or null if no valid position exists.</returns>
    public static Point? FindFirstTorch(
        List<Point> positions,
        Rectangle selectionBounds,
        int spacingX,
        int spacingY,
        bool allowOverwrite,
        bool alignToExisting = true)
    {
        // Sort positions top-down, left-right (Y ascending, then X ascending)
        positions.Sort((a, b) =>
        {
            int cmp = a.Y.CompareTo(b.Y);
            return cmp != 0 ? cmp : a.X.CompareTo(b.X);
        });

        // Find the first valid torch position
        Point? firstValid = null;
        foreach (var pos in positions)
        {
            if (IsValidTorchPosition(pos.X, pos.Y, allowOverwrite))
            {
                firstValid = pos;
                break;
            }
        }

        if (firstValid == null)
            return null;

        if (!alignToExisting)
            return firstValid;

        // Snap onto closest existing torch anywhere in the selection bounds.
        Point? existingTorch = SnapToExistingTorch(firstValid.Value, selectionBounds);
        return existingTorch ?? firstValid;
    }

    /// <summary>
    /// Scans the entire selection bounding box for any existing torch tile and
    /// returns the one closest (by Manhattan distance) to <paramref name="seed"/>.
    /// Returns null if no existing torch is found within bounds.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="FindNearbyExistingTorch"/>, this scans the full selection
    /// rather than a single spacing window, so it picks up existing torches that
    /// the user wants the new grid to align with regardless of how far they sit
    /// from the chosen reference seed.
    /// </remarks>
    public static Point? SnapToExistingTorch(Point seed, Rectangle selectionBounds)
    {
        Point? best = null;
        int bestDist = int.MaxValue;

        int x0 = selectionBounds.Left;
        int x1 = selectionBounds.Right;   // exclusive
        int y0 = selectionBounds.Top;
        int y1 = selectionBounds.Bottom;  // exclusive

        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                if (!WorldGen.InWorld(x, y, 1))
                    continue;

                var tile = Main.tile[x, y];
                if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
                    continue;

                int dist = System.Math.Abs(x - seed.X) + System.Math.Abs(y - seed.Y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new Point(x, y);
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Searches for an existing torch within the tiling search area around a position.
    /// Detects both vanilla and modded torches via <c>TileID.Sets.Torch</c>.
    /// </summary>
    private static Point? FindNearbyExistingTorch(
        Point center, Rectangle selectionBounds, int spacingX, int spacingY)
    {
        int searchX = spacingX + 1;
        int searchY = spacingY + 1;

        for (int dy = -searchY; dy <= searchY; dy++)
        {
            for (int dx = -searchX; dx <= searchX; dx++)
            {
                int wx = center.X + dx;
                int wy = center.Y + dy;

                if (!selectionBounds.Contains(new Point(wx, wy)))
                    continue;

                if (!WorldGen.InWorld(wx, wy, 1))
                    continue;

                var tile = Main.tile[wx, wy];
                if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
                    return new Point(wx, wy);
            }
        }

        return null;
    }


    // -- BBox Top-Left Seed ---------------------------------------------------

    /// <summary>
    /// Returns the top-left corner of the selection bounding box as the tiling seed.
    /// Unlike <see cref="FindFirstTorch"/>, this does not scan for valid positions;
    /// the BFS expansion will handle filtering. This guarantees a deterministic
    /// grid origin anchored to the bounding box regardless of tile contents.
    /// </summary>
    /// <param name="selectionBounds">Bounding rectangle of the selection.</param>
    /// <returns>The top-left point of the bounding box.</returns>
    public static Point GetBboxTopLeftSeed(Rectangle selectionBounds)
    {
        return new Point(selectionBounds.Left, selectionBounds.Top);
    }

    /// <summary>
    /// Returns the top-right corner of the selection bounding box as the tiling seed.
    /// </summary>
    public static Point GetBboxTopRightSeed(Rectangle selectionBounds)
    {
        return new Point(selectionBounds.Right - 1, selectionBounds.Top);
    }

    /// <summary>
    /// Returns the bottom-left corner of the selection bounding box as the tiling seed.
    /// </summary>
    public static Point GetBboxBottomLeftSeed(Rectangle selectionBounds)
    {
        return new Point(selectionBounds.Left, selectionBounds.Bottom - 1);
    }

    /// <summary>
    /// Returns the bottom-right corner of the selection bounding box as the tiling seed.
    /// </summary>
    public static Point GetBboxBottomRightSeed(Rectangle selectionBounds)
    {
        return new Point(selectionBounds.Right - 1, selectionBounds.Bottom - 1);
    }

    // ── Underwater / Waterproof Checks ─────────────────────────────

    /// <summary>
    /// Returns whether the given torch type+style can survive at (x, y).
    /// If the tile is submerged, the torch must be in the waterproof set.
    /// </summary>
    public static bool PassesUnderwaterCheck(int x, int y, int tileType, int placeStyle)
    {
        var tile = Main.tile[x, y];
        bool isSubmerged = tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;
        if (!isSubmerged) return true;

        return IsWaterproofTorch(tileType, placeStyle);
    }

    /// <summary>
    /// Returns whether the torch (tileType, placeStyle) can be placed in water.
    /// Uses the cached waterproof set built at PostSetupContent.
    /// </summary>
    public static bool IsWaterproofTorch(int tileType, int placeStyle)
    {
        if (_waterproofTorches == null) return false;
        return _waterproofTorches.Contains((tileType, placeStyle));
    }

    // ── Biome Torch Resolution ───────────────────────────────────

    /// <summary>
    /// Resolves the torch to place based on biome torch settings.
    /// If biome torch mode is active (wand setting OR Torch God's Favor),
    /// returns the biome-appropriate torch for the given position.
    /// Otherwise returns the player-selected torch unchanged.
    /// Works with all vanilla AND modded torches via
    /// <c>Player.BiomeTorchPlaceStyle</c>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="biomeTorchEnabled"/> is true (the wand's BiomeTorch
    /// toggle), biome resolution proceeds even without Torch God's Favor. The
    /// player is explicitly opting in via the wand UI, so we honor that choice.
    /// This also fixes underwater placement: when the biome torch is waterproof
    /// (e.g., Coral Torch in Ocean biome), the resolved torch correctly passes
    /// the underwater check — previously blocked because UsingBiomeTorches was
    /// false without Torch God's Favor.
    /// </remarks>
    /// <returns>
    /// A tuple (tileType, placeStyle) for the resolved torch to place.
    /// </returns>
    public static (int tileType, int placeStyle) ResolveBiomeTorch(
        Player player, int x, int y,
        int selectedTileType, int selectedPlaceStyle,
        bool biomeTorchEnabled)
    {
        // Only resolve if biome torch mode is on AND the selected torch is
        // a regular torch (TileID.Torches, style 0). Specific torches stay as-is.
        if (!biomeTorchEnabled
            || selectedTileType != TileID.Torches
            || selectedPlaceStyle != 0)
            return (selectedTileType, selectedPlaceStyle);

        // When the wand's BiomeTorch toggle is on, resolve biome torch
        // regardless of Torch God's Favor. The wand setting is an explicit
        // opt-in that should not require the vanilla progression gate.
        //
        // The tModLoader 2-param overload of BiomeTorchPlaceStyle adds
        // modded biome torch support (CurrentSceneEffect.biomeTorchItemType),
        // but it checks UsingBiomeTorches internally — which requires the
        // player to have Torch God's Favor enabled. Without it the method
        // early-returns and no resolution happens.
        //
        // Fix: Temporarily enable UsingBiomeTorches state so the tModLoader
        // overload proceeds, then restore the original state. This gives us
        // BOTH the TGF bypass AND modded biome torch resolution.
        int type = selectedTileType;
        int style = selectedPlaceStyle;

        bool originalUnlocked = player.unlockedBiomeTorches;
        int originalTorchToggle = player.builderAccStatus[BuilderToggle.TorchBiome.Type];

        player.unlockedBiomeTorches = true;
        player.builderAccStatus[BuilderToggle.TorchBiome.Type] = 0; // 0 = enabled

        player.BiomeTorchPlaceStyle(ref type, ref style);

        player.unlockedBiomeTorches = originalUnlocked;
        player.builderAccStatus[BuilderToggle.TorchBiome.Type] = originalTorchToggle;

        return (type, style);
    }

    // ── Inventory Search ─────────────────────────────────────────

    /// <summary>
    /// Finds the first torch item in the player's inventory and returns
    /// its item type, total stack count, tile type, and place style.
    /// Returns (-1, 0, -1, -1) if no torches found.
    /// Works with all vanilla AND modded torches.
    /// <para>InventoryView v1 (S6 2026-04-22): when <paramref name="chosenItemType"/>
    /// is non-null and the chosen torch type is still in inventory, that type is
    /// preferred (so its stack is summed and its tile type / place style returned).
    /// Stale pins fall back to the natural scan order.</para>
    /// </summary>
    public static (int itemType, int totalStack, int tileType, int placeStyle)
        FindTorchInInventory(Player player, int? chosenItemType = null)
    {
        int bestType = -1;
        int totalStack = 0;
        int bestTileType = -1;
        int bestPlaceStyle = -1;

        // Choice pre-pass: pick the chosen torch as bestType if it's still valid.
        if (chosenItemType.HasValue && chosenItemType.Value > 0)
        {
            int choice = chosenItemType.Value;
            for (int i = 0; i < 50; i++)
            {
                var it = player.inventory[i];
                if (it.IsAir || it.type != choice) continue;
                if (!IsTorchItem(it, out int pinTileType, out int pinPlaceStyle)) continue;
                bestType = choice;
                bestTileType = pinTileType;
                bestPlaceStyle = pinPlaceStyle;
                break;
            }
        }

        for (int i = 0; i < 50; i++) // Main inventory slots only
        {
            var item = player.inventory[i];
            if (item.IsAir) continue;

            if (!IsTorchItem(item, out int tileType, out int placeStyle))
                continue;

            if (bestType == -1)
            {
                bestType = item.type;
                bestTileType = tileType;
                bestPlaceStyle = placeStyle;
            }

            // Only sum stacks of the same torch type
            if (item.type == bestType)
                totalStack += item.stack;
        }

        return (bestType, totalStack, bestTileType, bestPlaceStyle);
    }

    /// <summary>
    /// Returns <c>true</c> if the player has at least one torch in their inventory.
    /// </summary>
    public static bool HasTorches(Player player)
    {
        var (itemType, _, _, _) = FindTorchInInventory(player);
        return itemType > 0;
    }

    // ── Underwater Torch Selection ──────────────────────────────────

    /// <summary>
    /// Finds the first waterproof torch in the player's inventory (main 0–49,
    /// then Void Bag if <paramref name="searchVoidBag"/> is true).
    /// Returns (-1, 0, -1, -1) if no waterproof torch found.
    /// </summary>
    public static (int itemType, int totalStack, int tileType, int placeStyle)
        FindWaterproofTorchInInventory(Player player, bool searchVoidBag = false)
    {
        // Search main inventory first
        var result = FindWaterproofTorchInSlots(player.inventory, 0, 50);
        if (result.itemType > 0)
            return result;

        // Search Void Bag if enabled and equipped
        if (searchVoidBag && player.IsVoidVaultEnabled)
        {
            result = FindWaterproofTorchInSlots(player.bank4.item, 0, player.bank4.item.Length);
            if (result.itemType > 0)
                return result;
        }

        return (-1, 0, -1, -1);
    }

    /// <summary>
    /// Scans a range of item slots for the first waterproof torch.
    /// </summary>
    private static (int itemType, int totalStack, int tileType, int placeStyle)
        FindWaterproofTorchInSlots(Item[] items, int startSlot, int endSlot)
    {
        int bestType = -1;
        int totalStack = 0;
        int bestTileType = -1;
        int bestPlaceStyle = -1;

        for (int i = startSlot; i < endSlot; i++)
        {
            var item = items[i];
            if (item.IsAir) continue;

            if (!IsTorchItem(item, out int tileType, out int placeStyle))
                continue;

            if (!IsWaterproofTorch(tileType, placeStyle))
                continue;

            if (bestType == -1)
            {
                bestType = item.type;
                bestTileType = tileType;
                bestPlaceStyle = placeStyle;
            }

            if (item.type == bestType)
                totalStack += item.stack;
        }

        return (bestType, totalStack, bestTileType, bestPlaceStyle);
    }

    /// <summary>
    /// Finds the biome torch item in the player's inventory. If the biome torch
    /// differs from the currently held torch and the player actually has the biome
    /// torch in inventory, returns that torch's info. Otherwise returns (-1, ...).
    /// This avoids wasting biome torch stacks the player has manually collected.
    /// </summary>
    public static (int itemType, int totalStack, int tileType, int placeStyle)
        FindBiomeTorchInInventory(Player player, int x, int y,
            int currentTileType, int currentPlaceStyle)
    {
        // Resolve what the biome torch would be at this position
        var (biomeTileType, biomePlaceStyle) = ResolveBiomeTorch(
            player, x, y, currentTileType, currentPlaceStyle,
            biomeTorchEnabled: player.UsingBiomeTorches);

        // If biome torch is the same as what we already have, no special handling needed
        if (biomeTileType == currentTileType && biomePlaceStyle == currentPlaceStyle)
            return (-1, 0, -1, -1);

        // Look up what item places this biome torch
        int biomeItemType = GetItemForTorchTile(biomeTileType, biomePlaceStyle);
        if (biomeItemType <= 0)
            return (-1, 0, -1, -1);

        // Search inventory for this specific item
        int totalStack = 0;
        bool found = false;
        for (int i = 0; i < 50; i++)
        {
            var item = player.inventory[i];
            if (item.IsAir || item.type != biomeItemType) continue;
            totalStack += item.stack;
            found = true;
        }

        if (!found)
            return (-1, 0, -1, -1);

        return (biomeItemType, totalStack, biomeTileType, biomePlaceStyle);
    }

    // ── Enhanced Torch Consumption (with Void Bag) ──────────────────

    /// <summary>
    /// Consumes one torch of the specified type from the player's inventory.
    /// Searches main inventory first, then Void Bag if enabled and
    /// <paramref name="searchVoidBag"/> is true.
    /// </summary>
    /// <returns>True if a torch was consumed, false if not found.</returns>
    public static bool ConsumeTorchFromPlayer(Player player, int torchItemType, bool searchVoidBag = false)
    {
        // Try main inventory first
        if (ConsumeFromSlots(player.inventory, 0, 50, torchItemType))
            return true;

        // Try Void Bag if enabled
        if (searchVoidBag && player.IsVoidVaultEnabled)
        {
            if (ConsumeFromSlots(player.bank4.item, 0, player.bank4.item.Length, torchItemType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Consumes one item of the specified type from a slot range.
    /// </summary>
    private static bool ConsumeFromSlots(Item[] items, int startSlot, int endSlot, int itemType)
    {
        for (int i = startSlot; i < endSlot; i++)
        {
            Item item = items[i];
            if (item.type == itemType && item.stack > 0)
            {
                item.stack--;
                if (item.stack <= 0)
                    item.TurnToAir();
                return true;
            }
        }
        return false;
    }

    // ── Auto-Waterproof Torch Resolution ─────────────────────────────

    /// <summary>
    /// Resolves which waterproof torch to use for auto-conversion based on
    /// the player's position (Ocean vs non-Ocean) and config settings.
    /// Returns the item ID of the waterproof torch to convert regular torches to.
    /// </summary>
    public static int ResolveAutoWaterproofTorch(Player player)
    {
        var config = WandConfigs.TorchWheel;
        bool isOcean = player.ZoneBeach;

        int resolved;
        if (isOcean)
        {
            resolved = config.OceanWaterproofTorch switch
            {
                OceanWaterproofTorch.CoralTorch => ItemID.CoralTorch,
                OceanWaterproofTorch.EvilTorch => GetEvilTorchItemId(),
                OceanWaterproofTorch.CursedTorch => ItemID.CursedTorch,
                OceanWaterproofTorch.IchorTorch => ItemID.IchorTorch,
                _ => ItemID.CoralTorch,
            };
        }
        else
        {
            resolved = config.NonOceanWaterproofTorch switch
            {
                NonOceanWaterproofTorch.EvilTorch => GetEvilTorchItemId(),
                NonOceanWaterproofTorch.CursedTorch => ItemID.CursedTorch,
                NonOceanWaterproofTorch.IchorTorch => ItemID.IchorTorch,
                NonOceanWaterproofTorch.CoralTorch => ItemID.CoralTorch,
                _ => GetEvilTorchItemId(),
            };
        }

        // Gate evil torches behind Hardmode (§1A/§1C)
        if (config.EvilTorchRequiresHardmode && !Main.hardMode && IsEvilTorch(resolved))
        {
            if (config.SubstituteCoralTorchPreHardmode)
                return ItemID.CoralTorch;
            return -1; // No conversion — caller should place held torch unchanged
        }

        return resolved;
    }

    /// <summary>
    /// Returns true if the given item ID is an evil torch (Ichor or Cursed).
    /// </summary>
    private static bool IsEvilTorch(int itemId)
        => itemId == ItemID.IchorTorch || itemId == ItemID.CursedTorch;

    /// <summary>
    /// Returns the appropriate evil torch based on the current world evil.
    /// Crimson → Ichor Torch, Corruption → Cursed Torch.
    /// Falls back to Cursed Torch for mixed/unknown worlds.
    /// </summary>
    private static int GetEvilTorchItemId()
    {
        // WorldGen.crimson is true for Crimson worlds, false for Corruption
        return WorldGen.crimson ? ItemID.IchorTorch : ItemID.CursedTorch;
    }
}
