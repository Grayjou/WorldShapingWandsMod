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
// Bubble shell, fill, drain, inventory helpers, achievement gate — partial of WandOfFluidsBase. See WandOfFluidsBase.cs for the class header & overrides.
public abstract partial class WandOfFluidsBase
{
    // ── Coat in Bubble (Shell) ───────────────────────────────────────

    /// <summary>
    /// Computes the bubble shell: tiles that are 4-directionally adjacent to the
    /// selection boundary but NOT part of the selection itself. This is a 1-tile
    /// dilation of the selection outline — only the exterior ring.
    ///
    /// Performance note: We only iterate the boundary tiles (not all selection tiles),
    /// then check their 4-neighbors for candidates outside the selection. For a shape
    /// with N tiles and perimeter P, this is O(P) — much cheaper than iterating all N.
    /// </summary>
    private static HashSet<Point> ComputeBubbleShell(List<Point> selectionPositions)
    {
        var selectionSet = new HashSet<Point>(selectionPositions);
        var shell = new HashSet<Point>();

        // Get boundary tiles — tiles in the selection that have at least one
        // 4-neighbor missing. This is the "inner edge" of the selection.
        var boundary = GeometryHelper.GetBoundaryTiles4(selectionSet);

        // For each boundary tile, check its 4 cardinal neighbors.
        // Any neighbor that is NOT in the selection is a shell candidate.
        foreach (var tile in boundary)
        {
            if (!selectionSet.Contains(new Point(tile.X + 1, tile.Y)))
                shell.Add(new Point(tile.X + 1, tile.Y));
            if (!selectionSet.Contains(new Point(tile.X - 1, tile.Y)))
                shell.Add(new Point(tile.X - 1, tile.Y));
            if (!selectionSet.Contains(new Point(tile.X, tile.Y + 1)))
                shell.Add(new Point(tile.X, tile.Y + 1));
            if (!selectionSet.Contains(new Point(tile.X, tile.Y - 1)))
                shell.Add(new Point(tile.X, tile.Y - 1));
        }

        return shell;
    }

    /// <summary>
    /// Places Bubble Blocks as a containment shell around the liquid selection.
    /// Only places bubbles at shell positions (exterior ring adjacent to selection).
    /// Skips tiles that already have a solid block. Consumes Bubble items from
    /// inventory unless Carefree Mode is active.
    /// Returns the number of bubble blocks placed.
    /// </summary>
    private static int ExecuteCoatInBubble(Player player, List<Point> selectionPositions)
    {
        var shell = ComputeBubbleShell(selectionPositions);
        if (shell.Count == 0)
            return 0;

        int placed = 0;
        var config = WandConfigs.Carefree;
        bool isInfinite = config?.EnableCarefreeMode ?? false;

        foreach (var pos in shell)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            // Skip tiles that already have a solid block (don't overwrite terrain)
            if (tile.HasTile)
                continue;

            // Check inventory for Bubble items (unless infinite)
            if (!isInfinite && !HasBubbleInInventory(player))
                break;

            WorldGen.PlaceTile(x, y, TileID.Bubble, mute: true, forced: false);

            if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Bubble)
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1);

                if (!isInfinite)
                    ConsumeBubbleFromInventory(player);

                placed++;
            }
        }

        return placed;
    }

    // ── Bubble Fill ──────────────────────────────────────────────────

    /// <summary>
    /// Places Bubble Blocks (TileID.Bubble = 379) in every air tile within the selection.
    /// Identical logic to WandOfBuilding but restricted to bubble blocks.
    /// Only works on air tiles — does not replace existing tiles.
    /// Consumes Bubble items from inventory unless Carefree Mode is active.
    /// </summary>
    private static void ExecuteBubbleFill(Player player, List<Point> positions)
    {
        int placed = 0;
        var config = WandConfigs.Carefree;
        bool isInfinite = config?.EnableCarefreeMode ?? false;

        foreach (var pos in positions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            // Only place in empty (air) tiles — no replacement
            if (tile.HasTile)
                continue;

            // Check inventory for Bubble items (unless infinite)
            if (!isInfinite && !HasBubbleInInventory(player))
                break;

            WorldGen.PlaceTile(x, y, TileID.Bubble, mute: true, forced: false);

            if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Bubble)
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendTileSquare(-1, x, y, 1);

                // Consume one Bubble from inventory
                if (!isInfinite)
                    ConsumeBubbleFromInventory(player);

                placed++;
            }
        }

        Main.NewText(Get("FluidsBubblePlaced", placed),
            WandColors.MsgFluids);
    }

    /// <summary>
    /// Removes Bubble Blocks within the selection (drain mode + bubble type).
    /// </summary>
    private static void ExecuteBubbleDrain(Player player, List<Point> positions)
    {
        int removed = 0;

        foreach (var pos in positions)
        {
            int x = pos.X;
            int y = pos.Y;

            if (!WorldGen.InWorld(x, y, 1))
                continue;

            var tile = Main.tile[x, y];

            if (!tile.HasTile || tile.TileType != TileID.Bubble)
                continue;

            WorldGen.KillTile(x, y, noItem: true);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, x, y, 1);

            removed++;
        }

        Main.NewText(Get("FluidsBubbleRemoved", removed),
            WandColors.MsgFluids);
    }

    // ── Bubble Inventory Helpers ─────────────────────────────────────

    /// <summary>
    /// Checks whether the player has any Bubble items (ItemID.Bubble) in inventory.
    /// </summary>
    private static bool HasBubbleInInventory(Player player)
    {
        for (int i = 0; i < 50; i++)
        {
            if (player.inventory[i].type == ItemID.Bubble && player.inventory[i].stack > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Consumes one Bubble item from the player's inventory.
    /// </summary>
    private static void ConsumeBubbleFromInventory(Player player)
    {
        for (int i = 0; i < 50; i++)
        {
            if (player.inventory[i].type == ItemID.Bubble && player.inventory[i].stack > 0)
            {
                player.inventory[i].stack--;
                if (player.inventory[i].stack <= 0)
                    player.inventory[i].TurnToAir();
                return;
            }
        }
    }

    // ── Achievement Gate Checks ──────────────────────────────────────

    /// <summary>
    /// Checks whether the player has unlocked the specified liquid type
    /// via the achievement-based progression gate system.
    /// Drain mode is always available regardless of this check.
    /// </summary>
    protected static bool HasLiquidAccess(Player player, LiquidTypeSelection liquidType)
    {
        // TODO: Implement achievement gate checks — Phase 2
        // Water:   25 Angler Quests completed (config-deactivatable)
        // Honey:   25 Angler Quests + Queen Bee defeated (config-deactivatable)
        // Lava:    40 Angler Quests OR Evil Biome Boss defeated (config-deactivatable)
        // Shimmer: Moon Lord defeated (config-deactivatable)

        // For now, always return true — gates will be implemented later
        return true;
    }
}
