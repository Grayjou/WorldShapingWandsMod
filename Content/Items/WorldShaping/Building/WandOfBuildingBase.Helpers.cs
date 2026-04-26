using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using System;
using System.Collections.Generic;
using SlopeType = WorldShapingWandsMod.Common.Enums.SlopeType;

namespace WorldShapingWandsMod.Content.Items
{
    public abstract partial class WandOfBuildingBase
    {
        /// <summary>
        /// Consumes one item from the player's inventory for the given source item.
        /// Handles tile wand ammo vs. direct consumption.
        /// </summary>
        private static void ConsumeOneItem(Player player, Item sourceItem, Func<Item, bool> placeCondition)
        {
            bool usingTileWand = sourceItem.tileWand >= 0;
            Func<Item, bool> consumeCond = usingTileWand
                ? i => !i.IsAir && i.type == sourceItem.tileWand
                : i => !i.IsAir && i.type == sourceItem.type;

            ItemTypeHelper.ConsumeItems(player.inventory, consumeCond, 1);
        }

        /// <summary>
        /// Checks if the tile at (x, y) has a different slope than the requested one.
        /// Used to determine if an in-place slope update is needed when the tile type matches.
        /// </summary>
        private static bool NeedsSlopeChange(int x, int y, SlopeType slope)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return false;

            return slope switch
            {
                SlopeType.Default => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.Solid,
                SlopeType.VerticalHalf => !tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.Solid,
                SlopeType.BottomRight => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeDownLeft,
                SlopeType.BottomLeft => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeDownRight,
                SlopeType.TopRight => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeUpLeft,
                SlopeType.TopLeft => tile.IsHalfBlock || tile.Slope != Terraria.ID.SlopeType.SlopeUpRight,
                _ => false
            };
        }

        /// <summary>
        /// Applies the selected slope/half-block setting to a placed tile.
        /// Always applies the selected slope type, including forcing full-block for SlopeType.Default.
        /// </summary>
        private static void ApplySlope(int x, int y, SlopeType slope)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;

            if (slope == SlopeType.Default)
            {
                // Force full block (clear any slope/half-block)
                tile.IsHalfBlock = false;
                tile.Slope = Terraria.ID.SlopeType.Solid;
            }
            else if (slope == SlopeType.VerticalHalf)
            {
                tile.IsHalfBlock = true;
                tile.Slope = Terraria.ID.SlopeType.Solid; // clear any slope when setting half-block
            }
            else
            {
                tile.IsHalfBlock = false;

                tile.Slope = slope switch
                {
                    SlopeType.BottomRight => Terraria.ID.SlopeType.SlopeDownLeft,
                    SlopeType.BottomLeft  => Terraria.ID.SlopeType.SlopeDownRight,
                    SlopeType.TopRight    => Terraria.ID.SlopeType.SlopeUpLeft,
                    SlopeType.TopLeft     => Terraria.ID.SlopeType.SlopeUpRight,
                    _ => Terraria.ID.SlopeType.Solid
                };
            }

            // Update tile frame so collision and visuals are immediately correct
            // (without this, sloped platforms act as solid blocks until an adjacent tile update)
            WorldGen.SquareTileFrame(x, y);
        }

        // ── Paint Sprayer helpers ─────────────────────────────────────

        /// <summary>
        /// Finds the first paint item in the player's inventory and returns its paint ID (1–30).
        /// Returns 0 if no paint is found.
        /// </summary>
        internal static byte FindPaintInInventory(Player player)
        {
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (!item.IsAir && item.paint > 0)
                    return item.paint;
            }
            return 0;
        }

        /// <summary>
        /// Applies paint from inventory to a freshly placed tile at (x, y).
        /// Consumes one paint item unless infinite resources are active.
        /// </summary>
        internal static void ApplyPaintSprayerTile(Player player, int x, int y, bool shouldConsume, HashSet<int> changedSlots = null)
        {
            byte paintId = FindPaintInInventory(player);
            if (paintId == 0) return;

            WorldGen.paintTile(x, y, paintId, true);

            if (shouldConsume)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item item = player.inventory[i];
                    if (!item.IsAir && item.paint == paintId)
                    {
                        item.stack--;
                        if (item.stack <= 0) item.TurnToAir();
                        changedSlots?.Add(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Applies paint from inventory to a freshly placed wall at (x, y).
        /// Consumes one paint item unless infinite resources are active.
        /// </summary>
        internal static void ApplyPaintSprayerWall(Player player, int x, int y, bool shouldConsume, HashSet<int> changedSlots = null)
        {
            byte paintId = FindPaintInInventory(player);
            if (paintId == 0) return;

            WorldGen.paintWall(x, y, paintId, true);

            if (shouldConsume)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item item = player.inventory[i];
                    if (!item.IsAir && item.paint == paintId)
                    {
                        item.stack--;
                        if (item.stack <= 0) item.TurnToAir();
                        changedSlots?.Add(i);
                        break;
                    }
                }
            }
        }

        // ── Paint Sprayer source dispatch (tri-state Off / Inventory / CoatingSettings) ────
        // Design reference: dev_notes/inbox/Cavendish 2026-04-19_Session_1/DesignDoc_PaintSprayerSourceToggle.md

        /// <summary>
        /// Reads <see cref="Common.Settings.WandOfCoatingSettings.PaintColor"/> for the given player.
        /// Returns 0 when no usable colour is configured (0 = none, 255 = "Ignore" sentinel).
        /// </summary>
        internal static byte GetCoatingPaintColor(Player player)
        {
            byte color = player?.GetModPlayer<WandPlayer>()?.CoatingSettings?.PaintColor ?? 0;
            return (color == 0 || color == 255) ? (byte)0 : color;
        }

        /// <summary>
        /// Tri-state paint dispatch for tiles. Routes to the inventory consumer or to the
        /// player's coating-settings colour, or no-ops when <paramref name="source"/> is Off.
        /// </summary>
        internal static void ApplyPaintSprayerTile(Player player, int x, int y, bool shouldConsume,
                                                   PaintSprayerSource source, HashSet<int> changedSlots = null)
        {
            switch (source)
            {
                case PaintSprayerSource.Off:
                    return;
                case PaintSprayerSource.Inventory:
                    ApplyPaintSprayerTile(player, x, y, shouldConsume, changedSlots);
                    return;
                case PaintSprayerSource.CoatingSettings:
                    byte color = GetCoatingPaintColor(player);
                    if (color == 0) return;
                    WorldGen.paintTile(x, y, color, true);
                    return;
            }
        }

        /// <summary>Tri-state paint dispatch for walls — see tile overload.</summary>
        internal static void ApplyPaintSprayerWall(Player player, int x, int y, bool shouldConsume,
                                                   PaintSprayerSource source, HashSet<int> changedSlots = null)
        {
            switch (source)
            {
                case PaintSprayerSource.Off:
                    return;
                case PaintSprayerSource.Inventory:
                    ApplyPaintSprayerWall(player, x, y, shouldConsume, changedSlots);
                    return;
                case PaintSprayerSource.CoatingSettings:
                    byte color = GetCoatingPaintColor(player);
                    if (color == 0) return;
                    WorldGen.paintWall(x, y, color, true);
                    return;
            }
        }

        // ── Actuation helper ─────────────────────────────────────────

        /// <summary>
        /// Applies the actuation setting to a tile at (x, y).
        /// <c>null</c> = ignore (leave as-is), <c>true</c> = actuate, <c>false</c> = de-actuate.
        /// </summary>
        internal static void ApplyActuation(int x, int y, bool? actuation)
        {
            if (actuation == null) return;
            var tile = Main.tile[x, y];
            if (!tile.HasTile) return;
            tile.IsActuated = actuation.Value;
        }
    }
}
