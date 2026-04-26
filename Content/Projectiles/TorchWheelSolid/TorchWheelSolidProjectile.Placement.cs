using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Items;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelSolidProjectile
{
    // ================================================================
    //  Placement Logic
    // ================================================================

    private bool CanPlaceAt(Point16 pos)
    {
        int x = pos.X, y = pos.Y;
        _useUnderwaterOverride = false;

        // 1. Valid torch attachment (solid below, wall behind, etc.)
        if (!TorchPlacementHelper.IsValidTorchPosition(x, y, allowOverwrite: false))
            return false;

        // 2. Already a torch here? (We don't overwrite — no UI for that)
        Tile tile = Main.tile[x, y];
        if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
            return false;

        // 3. Outline spacing (S) — path-distance to previous torches
        if (!CheckOutlineSpacing())
            return false;

        // 4. Absolute spacing (D) — Manhattan distance to ALL placed torches
        if (!CheckAbsoluteSpacing(pos))
            return false;

        // 5. Waterproof check — with smart underwater torch selection
        if (!TorchPlacementHelper.PassesUnderwaterCheck(x, y, _cachedTorchTileType, _cachedTorchPlaceStyle))
        {
            if (!TryResolveUnderwaterOverride(x, y))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to resolve a waterproof torch override when the cached torch
    /// cannot survive at an underwater position. Checks config, inventory, and
    /// auto-conversion in priority order.
    /// </summary>
    private bool TryResolveUnderwaterOverride(int x, int y)
    {
        var config = WandConfigs.TorchWheel;
        if (config == null || !config.UnderwaterTorchLookup)
            return false;

        Player owner = Main.player[Projectile.owner];

        // Priority 0: Biome torch resolution — if the biome-resolved torch is
        // waterproof, use it as the override. This ensures the TorchWheel respects
        // Torch God's Favor in underwater biomes (e.g., Coral Torch in Ocean).
        if (config.SmartBiomeTorchLookup && owner.UsingBiomeTorches
            && _cachedTorchTileType == TileID.Torches && _cachedTorchPlaceStyle == 0)
        {
            var (biomeTile, biomeStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                owner, x, y, _cachedTorchTileType, _cachedTorchPlaceStyle,
                biomeTorchEnabled: true);

            if (TorchPlacementHelper.PassesUnderwaterCheck(x, y, biomeTile, biomeStyle))
            {
                _useUnderwaterOverride = true;
                _underwaterOverrideItemType = _cachedTorchItemType;
                _underwaterOverrideTileType = biomeTile;
                _underwaterOverridePlaceStyle = biomeStyle;
                return true;
            }
        }

        // Priority 1: Find any waterproof torch in inventory
        var (wpItemType, _, wpTileType, wpPlaceStyle) =
            TorchPlacementHelper.FindWaterproofTorchInInventory(owner);

        if (wpItemType > 0)
        {
            _useUnderwaterOverride = true;
            _underwaterOverrideItemType = wpItemType;
            _underwaterOverrideTileType = wpTileType;
            _underwaterOverridePlaceStyle = wpPlaceStyle;
            return true;
        }

        // Priority 2: Auto-convert regular torches if config allows
        if (config.AutoWaterproofTorches)
        {
            int autoItemType = TorchPlacementHelper.ResolveAutoWaterproofTorch(owner);
            if (autoItemType > 0)
            {
                var (regularItemType, regularStack, _, _) = TorchPlacementHelper.FindTorchInInventory(owner);
                if (regularItemType > 0 && regularStack > 0)
                {
                    Item autoSample = ContentSamples.ItemsByType[autoItemType];
                    if (autoSample != null && TorchPlacementHelper.IsTorchItem(autoSample, out int autoTileType, out int autoPlaceStyle))
                    {
                        _useUnderwaterOverride = true;
                        _underwaterOverrideItemType = regularItemType;
                        _underwaterOverrideTileType = autoTileType;
                        _underwaterOverridePlaceStyle = autoPlaceStyle;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool CheckOutlineSpacing()
    {
        int candidateIdx = _pathIndex - WindowSize / 2;

        // Check against torches placed by this projectile
        foreach (var (_, torchIdx) in _placedTorches)
        {
            int dist = Math.Abs(candidateIdx - torchIdx);
            if (dist > 0 && dist < SpacingS)
                return false;
        }

        // Check against pre-existing torches found along the outline path.
        // These were recorded during the forward walk when the wheel stepped
        // onto a tile that already had a torch in the world.
        foreach (int existingIdx in _existingTorchPathIndices)
        {
            int dist = Math.Abs(candidateIdx - existingIdx);
            if (dist > 0 && dist < SpacingS)
                return false;
        }

        return true;
    }

    private bool CheckAbsoluteSpacing(Point16 candidate)
    {
        // Check against torches placed by THIS projectile
        foreach (var (torchPos, _) in _placedTorches)
        {
            int manhattan = Math.Abs(candidate.X - torchPos.X)
                          + Math.Abs(candidate.Y - torchPos.Y);
            if (manhattan < SpacingD)
                return false;
        }

        // Check against pre-existing torches ONLY within the sliding window (outline).
        // Previously this checked a full Manhattan-area neighborhood around the candidate,
        // but that incorrectly detected torches behind solid blocks that provide no
        // illumination to the outline area. By restricting the check to positions
        // the outline has recently visited, we only consider torches that are actually
        // relevant to the path being traced.
        foreach (var windowPos in _recentPath)
        {
            int wx = windowPos.X, wy = windowPos.Y;
            if (!WorldGen.InWorld(wx, wy, 1)) continue;

            Tile worldTile = Main.tile[wx, wy];
            if (worldTile.HasTile && TileID.Sets.Torch[worldTile.TileType])
            {
                int manhattan = Math.Abs(candidate.X - wx)
                              + Math.Abs(candidate.Y - wy);
                if (manhattan < SpacingD)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Places a torch at the candidate position, consuming from inventory.
    /// Resolves biome torch via Torch God's Favor. Handles underwater
    /// torch overrides and smart biome torch lookup.
    /// </summary>
    private bool TryPlaceTorch(Point16 pos)
    {
        Player owner = Main.player[Projectile.owner];
        int x = pos.X, y = pos.Y;
        var config = WandConfigs.TorchWheel;

        int itemTypeToConsume;
        int tileTypeToPlace;
        int placeStyleToPlace;

        if (_useUnderwaterOverride)
        {
            // Underwater override: use the resolved waterproof torch
            itemTypeToConsume = _underwaterOverrideItemType;
            tileTypeToPlace = _underwaterOverrideTileType;
            placeStyleToPlace = _underwaterOverridePlaceStyle;
        }
        else
        {
            // Normal path: re-find torch in inventory (stack may have changed)
            var (itemType, _, tileType, placeStyle) = TorchPlacementHelper.FindTorchInInventory(owner);
            if (itemType <= 0) return false;

            itemTypeToConsume = itemType;
            tileTypeToPlace = tileType;
            placeStyleToPlace = placeStyle;

            // Smart Biome Torch Lookup: if biome torch is active and player has
            // the actual biome torch item, consume that instead of converting
            if (config != null && config.SmartBiomeTorchLookup && owner.UsingBiomeTorches)
            {
                var (biomeItemType, _, biomeTileType, biomePlaceStyle) =
                    TorchPlacementHelper.FindBiomeTorchInInventory(
                        owner, x, y, tileType, placeStyle);

                if (biomeItemType > 0)
                {
                    itemTypeToConsume = biomeItemType;
                    tileTypeToPlace = biomeTileType;
                    placeStyleToPlace = biomePlaceStyle;
                }
                else
                {
                    var (resolvedTile, resolvedStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                        owner, x, y, tileType, placeStyle, biomeTorchEnabled: true);
                    tileTypeToPlace = resolvedTile;
                    placeStyleToPlace = resolvedStyle;
                }
            }
            else
            {
                var (resolvedTile, resolvedStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                    owner, x, y, tileTypeToPlace, placeStyleToPlace,
                    biomeTorchEnabled: owner.UsingBiomeTorches);
                tileTypeToPlace = resolvedTile;
                placeStyleToPlace = resolvedStyle;
            }
        }

        // Place the tile
        WorldGen.PlaceTile(x, y, tileTypeToPlace, mute: false, forced: false,
            plr: owner.whoAmI, style: placeStyleToPlace);

        // Verify placement succeeded
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || !TileID.Sets.Torch[tile.TileType])
            return false;

        // Consume one torch from inventory (with Void Bag support)
        TorchPlacementHelper.ConsumeTorchFromPlayer(owner, itemTypeToConsume);

        // Sync in multiplayer
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendTileSquare(-1, x, y, 1);

        return true;
    }

    /// <summary>
    /// Consumes one torch of the specified type from the player's inventory.
    /// </summary>
    [System.Obsolete("Use TorchPlacementHelper.ConsumeTorchFromPlayer instead")]
    private static void ConsumeTorchFromInventory(Player player, int torchItemType)
    {
        TorchPlacementHelper.ConsumeTorchFromPlayer(player, torchItemType);
    }

    // ================================================================
    //  Window & State Helpers
    // ================================================================

    private Point16 GetWindowCenter()
    {
        int centerIndex = _recentPath.Count / 2;
        int i = 0;
        foreach (var pos in _recentPath)
        {
            if (i == centerIndex) return pos;
            i++;
        }
        return _recentPath.Peek();
    }

    /// <summary>
    /// Converts a tile-coordinate delta (dx, dy) into a <see cref="CardinalDirection"/>.
    /// Used to determine the actual movement direction from pre-scan path positions.
    /// </summary>
    private static CardinalDirection DirectionFromDelta(int dx, int dy)
    {
        if (Math.Abs(dx) > Math.Abs(dy))
            return dx > 0 ? CardinalDirection.Right : CardinalDirection.Left;
        return dy > 0 ? CardinalDirection.Down : CardinalDirection.Up;
    }

    /// <summary>
    /// Packs (x, y, dir) into a single long for fast HashSet lookup.
    /// x and y are 16 bits each (sufficient for Terraria worlds),
    /// dir is 2 bits.
    /// </summary>
    private static long PackState(int x, int y, CardinalDirection dir)
    {
        return ((long)(x & 0xFFFF) << 18) | ((long)(y & 0xFFFF) << 2) | (long)dir;
    }
}
