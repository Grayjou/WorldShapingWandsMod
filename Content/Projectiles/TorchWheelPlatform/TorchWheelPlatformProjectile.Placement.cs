using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Items;

namespace WorldShapingWandsMod.Content.Projectiles;

public partial class TorchWheelPlatformProjectile
{
    // ================================================================
    //  Placement Logic
    // ================================================================

    // ── Underwater torch override state ─────────────────────────────
    // Set during CanPlaceAt, consumed by TryPlaceTorch.
    // When the cached torch is not waterproof but a waterproof alternative
    // is available, these fields describe the override.
    private bool _useUnderwaterOverride;
    private int _underwaterOverrideItemType;
    private int _underwaterOverrideTileType;
    private int _underwaterOverridePlaceStyle;

    /// <summary>
    /// Checks whether a torch can be placed at the candidate position.
    /// Uses platform-specific validation since standard torch rules don't
    /// recognize tileSolidTop (platforms) as valid anchor points.
    /// Handles underwater torch override via config-driven smart selection.
    /// </summary>
    private bool CanPlaceAt(Point16 pos)
    {
        int x = pos.X, y = pos.Y;
        _useUnderwaterOverride = false;

        // 1. Must be in world bounds
        if (!WorldGen.InWorld(x, y, 1))
            return false;

        // 2. Already a tile here? Don't overwrite.
        Tile tile = Main.tile[x, y];
        if (tile.HasTile)
            return false;

        // 3. Platform-specific torch attachment check
        //    Standard IsValidTorchPosition fails for platforms because they're
        //    tileSolidTop, not tileSolid. We check for platform below directly.
        if (!IsValidPlatformTorchPosition(x, y))
            return false;

        // 4. Outline spacing (S) — path-distance check (use candidate X-based check)
        if (!CheckOutlineSpacing(pos))
            return false;

        // 5. Waterproof check — with smart underwater torch selection
        if (!TorchPlacementHelper.PassesUnderwaterCheck(x, y, _cachedTorchTileType, _cachedTorchPlaceStyle))
        {
            // Cached torch can't survive here. Try to find a waterproof alternative.
            if (!TryResolveUnderwaterOverride(x, y))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to resolve a waterproof torch override when the cached torch
    /// cannot survive at an underwater position. Checks config, inventory, and
    /// biome torch conversion in priority order.
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
                // Verify we have regular torches to "convert" (we'll consume a regular
                // torch but place the waterproof variant)
                var (regularItemType, regularStack, _, _) = TorchPlacementHelper.FindTorchInInventory(owner);
                if (regularItemType > 0 && regularStack > 0)
                {
                    Item autoSample = ContentSamples.ItemsByType[autoItemType];
                    if (autoSample != null && TorchPlacementHelper.IsTorchItem(autoSample, out int autoTileType, out int autoPlaceStyle))
                    {
                        _useUnderwaterOverride = true;
                        // We consume the REGULAR torch but place the WATERPROOF variant
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

    /// <summary>
    /// Checks if a torch can be placed at (x, y) for platform tracing.
    /// Recognizes platform tiles (tileSolidTop) as valid anchors and also
    /// allows wall- or side-attachment like the standard helper.
    /// </summary>
    private static bool IsValidPlatformTorchPosition(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 1)) return false;

        // Primary case: platform directly below
        int belowY = y + 1;
        if (WorldGen.InWorld(x, belowY, 1))
        {
            Tile below = Main.tile[x, belowY];
            if (below.HasTile && TileID.Sets.Platforms[below.TileType])
                return true;
        }

        // Secondary: wall behind (for platforms placed against walls)
        Tile tile = Main.tile[x, y];
        if (tile.WallType != WallID.None)
            return true;

        // Tertiary: solid block to either side (standard torch attachment)
        if (WorldGen.InWorld(x - 1, y, 1))
        {
            Tile left = Main.tile[x - 1, y];
            if (left.HasTile && Main.tileSolid[left.TileType] && !Main.tileSolidTop[left.TileType])
                return true;
        }
        if (WorldGen.InWorld(x + 1, y, 1))
        {
            Tile right = Main.tile[x + 1, y];
            if (right.HasTile && Main.tileSolid[right.TileType] && !Main.tileSolidTop[right.TileType])
                return true;
        }

        return false;
    }

    /// <summary>
    /// For platform traces, path distance equals horizontal distance, so
    /// perform spacing checks using X coordinates directly.
    /// </summary>
    private bool CheckOutlineSpacing(Point16 candidate)
    {
        // Check against torches placed by this projectile
        foreach (var torchPos in _placedTorches)
        {
            int dist = Math.Abs(candidate.X - torchPos.X);
            if (dist > 0 && dist < SpacingS)
                return false;
        }

        // Check against pre-existing torches in the sliding window (world coords)
        foreach (var windowPos in _recentPath)
        {
            if (!WorldGen.InWorld(windowPos.X, windowPos.Y, 1)) continue;
            Tile tile = Main.tile[windowPos.X, windowPos.Y];
            if (tile.HasTile && TileID.Sets.Torch[tile.TileType])
            {
                int dist = Math.Abs(candidate.X - windowPos.X);
                if (dist > 0 && dist < SpacingS)
                    return false;
            }
        }

        return true;
    }

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
                    // Player has the biome torch — consume it directly
                    itemTypeToConsume = biomeItemType;
                    tileTypeToPlace = biomeTileType;
                    placeStyleToPlace = biomePlaceStyle;
                }
                else
                {
                    // Normal biome torch resolution (converts regular torch)
                    var (resolvedTile, resolvedStyle) = TorchPlacementHelper.ResolveBiomeTorch(
                        owner, x, y, tileType, placeStyle, biomeTorchEnabled: true);
                    tileTypeToPlace = resolvedTile;
                    placeStyleToPlace = resolvedStyle;
                }
            }
            else
            {
                // Resolve biome torch if Torch God's Favor is active (standard path)
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

    // ================================================================
    //  Helpers
    // ================================================================

    private static bool IsPlatformAt(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 1)) return false;
        Tile tile = Main.tile[x, y];
        return tile.HasTile && TileID.Sets.Platforms[tile.TileType];
    }

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

    private void SpawnImpactDust()
    {
        for (int i = 0; i < 10; i++)
        {
            var dust = Dust.NewDustDirect(
                Projectile.Center - new Vector2(8f), 16, 16,
                DustID.Smoke, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f),
                150, default, 1f);
            dust.noGravity = false;
        }
    }
}
