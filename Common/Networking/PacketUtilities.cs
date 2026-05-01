using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Settings;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Common.Networking;

/// <summary>
/// Shared utilities used by multiple per-family packet handlers.
/// Contains rate limiting, distance enforcement, shape computation,
/// player validation, and inventory helpers.
/// </summary>
public static class PacketUtilities
{
    // ════════════════════════════════════════════════════════════════════
    // Server-Side Rate Limiting
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks the last game tick at which each player executed a wand operation.
    /// Keyed by player whoAmI. Used to enforce OperationCooldownTicks on the server.
    /// </summary>
    private static readonly Dictionary<int, ulong> _lastOperationTick = new();

    /// <summary>
    /// Checks whether the player is still on cooldown from a previous operation.
    /// If on cooldown, sends an OperationResult error and returns true (blocked).
    /// If allowed, updates the last operation tick and returns false.
    /// </summary>
    /// <param name="whoAmI">Player index on the server.</param>
    /// <param name="packetType">The packet type being handled (for error reporting).</param>
    /// <returns>True if the operation should be BLOCKED, false if it may proceed.</returns>
    public static bool IsOnCooldown(int whoAmI, WandPacketType packetType)
    {
        var config = WandConfigs.Limits;
        int cooldown = config?.OperationCooldownTicks ?? 12;
        if (cooldown <= 0) return false; // Cooldown disabled

        ulong now = Main.GameUpdateCount;
        if (_lastOperationTick.TryGetValue(whoAmI, out ulong last))
        {
            if (now - last < (ulong)cooldown)
            {
                // Still on cooldown — silently reject. No error spam: the server
                // simply drops the packet. Autoclickers will fire faster than the
                // cooldown, but only one operation per window actually executes.
                return true;
            }
        }

        _lastOperationTick[whoAmI] = now;
        return false;
    }

    /// <summary>
    /// Client-side cooldown check for single-player. Returns true if on cooldown.
    /// Uses the same OperationCooldownTicks config as the server.
    /// </summary>
    private static ulong _spLastOperationTick;

    /// <summary>
    /// Returns true if the local player is on cooldown (single-player only).
    /// Call this from wand execution paths (UseItem / HoldItem) to prevent
    /// autoclicker or click-spam abuse in SP.
    /// </summary>
    public static bool IsLocalPlayerOnCooldown()
    {
        var config = WandConfigs.Limits;
        int cooldown = config?.OperationCooldownTicks ?? 12;
        if (cooldown <= 0) return false;

        ulong now = Main.GameUpdateCount;
        if (now - _spLastOperationTick < (ulong)cooldown)
            return true;

        _spLastOperationTick = now;
        return false;
    }

    // ════════════════════════════════════════════════════════════════════
    // Server-Side Validation Helpers
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clamp the end point to respect server-side distance caps.
    /// Uses the same cap logic as <see cref="Players.WandPlayer.ClampEndToCaps"/>.
    /// Returns a new header with the clamped end point.
    /// </summary>
    public static WandPacketHeader EnforceDistanceCap(WandPacketHeader header)
    {
        var config = WandConfigs.Limits;
        int cap;
        if (header.Shape == ShapeType.Elbow ||
            header.Shape == ShapeType.CardinalLine ||
            header.Shape == ShapeType.StraightLine)
            cap = config?.SmallSelectionCap ?? 1000;
        else if (header.FillMode == ShapeMode.Hollow)
            cap = config?.HollowSelectionCap ?? 400;
        else
            cap = config?.BigSelectionCap ?? 200;

        int dx = header.End.X - header.Start.X;
        int dy = header.End.Y - header.Start.Y;
        int maxOffset = Math.Max(0, cap - 1);
        dx = Math.Clamp(dx, -maxOffset, maxOffset);
        dy = Math.Clamp(dy, -maxOffset, maxOffset);

        var clampedEnd = new Point(header.Start.X + dx, header.Start.Y + dy);

        return new WandPacketHeader(
            header.Start, clampedEnd,
            header.Shape, header.FillMode,
            header.Thickness, header.EqualDimensions,
            header.VerticalFirst, header.PlayerWhoAmI,
            header.Slice, header.ConnectDiameter,
            header.InvertSelection
        );
    }

    /// <summary>
    /// Recompute the shape tiles from a packet header.
    /// Used by all server handlers to get the authoritative tile set.
    /// When InvertSelection is set, returns tiles within the bounding rect
    /// that are NOT in the original shape (negative space).
    /// </summary>
    public static ShapeTileSet ComputeShapeTiles(WandPacketHeader header)
    {
        var context = new ShapeContext(
            header.Start, header.End,
            header.FillMode, header.Thickness,
            HorizontalBias.None, VerticalBias.None,
            header.VerticalFirst, header.EqualDimensions,
            header.Slice, header.ConnectDiameter,
            header.InvertHalfOrientation
        );
        var tileSet = ShapeRegistry.GetShapeTiles(header.Shape, context);

        if (!header.InvertSelection || !ShapeInfo.ShapeSupportsInversion(header.Shape))
            return tileSet;

        // Apply inversion: bounding rect minus original shape tiles
        var shapeInfo = new ShapeInfo(header.Shape, header.FillMode,
            header.Thickness, header.EqualDimensions,
            header.Slice, header.ConnectDiameter, header.InvertSelection,
            header.InvertHalfOrientation);
        var invertedTiles = shapeInfo.ApplyInversion(tileSet.Tiles.ToArray(), context);
        return new ShapeTileSet(invertedTiles);
    }

    /// <summary>
    /// Validate that a player index refers to an active player.
    /// </summary>
    public static bool ValidatePlayer(int playerWhoAmI)
    {
        return playerWhoAmI >= 0 && playerWhoAmI < Main.maxPlayers
            && Main.player[playerWhoAmI].active;
    }

    // ════════════════════════════════════════════════════════════════════
    // Inventory Helpers (used by Building, Replacement handlers)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Find the first inventory slot matching a condition (server-side).
    /// Searches hotbar (0-9), main inventory (10-49), then ammo/misc (50-57).
    /// </summary>
    public static int FindItemSlot(Player player, Func<Item, bool> condition)
    {
        for (int i = 0; i < 58; i++)
        {
            if (!player.inventory[i].IsAir && condition(player.inventory[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Consume one item from the server-side player inventory.
    /// Handles tile wands (consume ammo type) vs direct items.
    /// Tracks modified slots for SyncEquipment.
    /// </summary>
    public static void ConsumeOneServerItem(
        Player player, Item sourceItem, Func<Item, bool> baseCondition,
        HashSet<int> changedSlots)
    {
        bool isTileWand = sourceItem.tileWand >= 0;
        Func<Item, bool> consumeCond = isTileWand
            ? i => !i.IsAir && i.type == sourceItem.tileWand
            : baseCondition;

        for (int i = 0; i < 58; i++)
        {
            if (consumeCond(player.inventory[i]))
            {
                player.inventory[i].stack--;
                if (player.inventory[i].stack <= 0)
                    player.inventory[i].TurnToAir();
                changedSlots.Add(i);
                return;
            }
        }
    }

    /// <summary>
    /// Applies slope settings on the server side.
    /// Mirror of WandOfBuildingBase.ApplySlope for server execution.
    /// </summary>
    public static void ApplySlopeServer(int x, int y, SlopeType slope)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile) return;

        if (slope == SlopeType.Default)
        {
            tile.IsHalfBlock = false;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else if (slope == SlopeType.VerticalHalf)
        {
            tile.IsHalfBlock = true;
            tile.Slope = Terraria.ID.SlopeType.Solid;
        }
        else
        {
            tile.IsHalfBlock = false;
            tile.Slope = slope switch
            {
                SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpLeft, // These are inverted, don't ask me why
                SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpRight,
                _ => Terraria.ID.SlopeType.Solid
            };
        }
        WorldGen.SquareTileFrame(x, y);
    }

    /// <summary>
    /// Returns the highest hammer power among all items in the player's inventory.
    /// Server-side mirror of WandOfDismantlingBase.GetPlayerMaxHammerPower.
    /// </summary>
    public static int GetPlayerMaxHammerPower(Player player)
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
    /// Server-side mirror of WandOfDismantling.IsDelicateTile.
    /// </summary>
    public static bool IsDelicateTile(int tileType)
    {
        return tileType == TileID.ShadowOrbs        // Shadow Orb / Crimson Heart
            || tileType == TileID.PlanteraBulb       // Plantera's Bulb
            || tileType == TileID.Larva              // Bee Larva (Queen Bee)
            || tileType == TileID.LifeFruit;         // Life Fruit
    }
}
