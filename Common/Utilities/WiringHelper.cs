using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;

namespace WorldShapingWandsMod.Common.Utilities;

public static class WiringHelper
{
    /// <summary>
    /// Full inventory range (0–57): hotbar + main + coin + ammo slots.
    /// Used for reading, counting, and consuming items — ammo slots are valid wire storage.
    /// </summary>
    private const int MaxInventorySlots = 58;

    /// <summary>
    /// Main inventory range (0–49): hotbar + main inventory only.
    /// Used for inserting/giving items — prevents placing items in coin (50–53) or ammo (54–57) slots.
    /// Bug fix: wire removal was placing wires into coin slots when main inventory was full.
    /// </summary>
    private const int MainInventoryEnd = 50;

    /// <summary>
    /// Checks whether infinite resource mode is active for wires.
    /// Returns true if the master toggle AND the wire-specific toggle are on,
    /// AND either the threshold is 0 (always infinite) or the player holds
    /// enough wire to meet the threshold.
    /// </summary>
    public static bool IsInfiniteWireMode(Player player, WandServerConfig config)
    {
        if (config == null || !config.IsInfiniteForWires)
            return false;

        if (config.InfiniteWireThreshold == 0)
            return true;

        int wireCount = CountWires(player);
        return wireCount >= config.InfiniteWireThreshold;
    }

    /// <summary>
    /// Checks whether infinite resource mode is active for actuators.
    /// Returns true if the master toggle AND the actuator-specific toggle are on,
    /// AND either the threshold is 0 (always infinite) or the player holds
    /// enough actuators to meet the threshold.
    /// </summary>
    public static bool IsInfiniteActuatorMode(Player player, WandServerConfig config)
    {
        if (config == null || !config.IsInfiniteForActuators)
            return false;

        if (config.InfiniteActuatorThreshold == 0)
            return true;

        int actuatorCount = CountActuators(player);
        return actuatorCount >= config.InfiniteActuatorThreshold;
    }

    public static (int placed, int removed) ExecuteWiringOperation(
        IEnumerable<Point> tiles,
        WiringMode mode,
        bool wireRed, bool wireGreen, bool wireBlue, bool wireYellow, bool actuator,
        Player player,
        bool infiniteWires = false,
        bool infiniteActuators = false)
    {
        int placed = 0, removed = 0;
        int wiresRemoved = 0, actuatorsRemoved = 0;

        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;

            if (!WorldGen.InWorld(x, y, 1)) continue;

            // Skip protected positions
            if (SafekeepingSystem.IsProtected(x, y)) continue;

            if (mode == WiringMode.Place)
                placed += PlaceStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player, infiniteWires, infiniteActuators);
            else
                removed += RemoveStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player, ref wiresRemoved, ref actuatorsRemoved);
        }

        // Consolidate wire/actuator drops: give items directly to inventory
        // instead of relying on per-tile ground drops (which hit the 400-item cap).
        if (mode == WiringMode.Remove && (wiresRemoved > 0 || actuatorsRemoved > 0))
        {
            GiveRemovedItems(player, wiresRemoved, actuatorsRemoved);
        }

        return (placed, removed);
    }

    /// <summary>
    /// Pre-consumes wire/actuator items for a multiplayer wiring operation.
    /// Counts how many wires WOULD be placed (checking existing tile state),
    /// then consumes that many items from inventory. Returns total consumed.
    /// Used on the client side before sending the packet to the server.
    /// </summary>
    public static int PreConsumeForOperation(
        IEnumerable<Point> tiles,
        bool wireRed, bool wireGreen, bool wireBlue, bool wireYellow, bool actuator,
        Player player,
        bool infiniteWires = false,
        bool infiniteActuators = false)
    {
        int consumed = 0;

        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;
            if (!WorldGen.InWorld(x, y, 1)) continue;

            Tile t = Main.tile[x, y];

            if (wireRed && !t.RedWire && !infiniteWires && HasItem(player, ItemID.Wire))
            {
                ConsumeItem(player, ItemID.Wire);
                consumed++;
            }
            if (wireGreen && !t.GreenWire && !infiniteWires && HasItem(player, ItemID.Wire))
            {
                ConsumeItem(player, ItemID.Wire);
                consumed++;
            }
            if (wireBlue && !t.BlueWire && !infiniteWires && HasItem(player, ItemID.Wire))
            {
                ConsumeItem(player, ItemID.Wire);
                consumed++;
            }
            if (wireYellow && !t.YellowWire && !infiniteWires && HasItem(player, ItemID.Wire))
            {
                ConsumeItem(player, ItemID.Wire);
                consumed++;
            }
            if (actuator && !t.HasActuator && !infiniteActuators && HasItem(player, ItemID.Actuator))
            {
                ConsumeItem(player, ItemID.Actuator);
                consumed++;
            }
        }

        return consumed;
    }

    private static int PlaceStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player, bool infiniteWires = false, bool infiniteActuators = false)
    {
        int count = 0;
        Tile tile = Main.tile[x, y];

        if (red && !tile.RedWire && (infiniteWires || HasItem(player, ItemID.Wire)))
        {
            if (WorldGen.PlaceWire(x, y))
            {
                if (!infiniteWires) ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5, x, y);
                count++;
            }
        }

        if (green && !tile.GreenWire && (infiniteWires || HasItem(player, ItemID.Wire)))
        {
            if (WorldGen.PlaceWire3(x, y))
            {
                if (!infiniteWires) ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 12, x, y);
                count++;
            }
        }

        if (blue && !tile.BlueWire && (infiniteWires || HasItem(player, ItemID.Wire)))
        {
            if (WorldGen.PlaceWire2(x, y))
            {
                if (!infiniteWires) ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 10, x, y);
                count++;
            }
        }

        if (yellow && !tile.YellowWire && (infiniteWires || HasItem(player, ItemID.Wire)))
        {
            if (WorldGen.PlaceWire4(x, y))
            {
                if (!infiniteWires) ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 16, x, y);
                count++;
            }
        }

        if (act && !tile.HasActuator && (infiniteActuators || HasItem(player, ItemID.Actuator)))
        {
            if (WorldGen.PlaceActuator(x, y))
            {
                if (!infiniteActuators) ConsumeItem(player, ItemID.Actuator);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 8, x, y);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Removes wires/actuators at a single tile, suppressing per-tile item drops.
    /// Instead of letting WorldGen.KillWire drop items on the ground one-by-one
    /// (which hits Terraria's 400-item ground cap), we track counts and consolidate
    /// drops after the full operation via <see cref="GiveRemovedItems"/>.
    /// </summary>
    private static int RemoveStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player, ref int wiresRemoved, ref int actuatorsRemoved)
    {
        int count = 0;
        Tile tile = Main.tile[x, y];

        // Suppress per-tile item drops by setting WorldGen.gen = true.
        // We'll give all items back in a consolidated batch afterward.
        bool wasGen = WorldGen.gen;
        WorldGen.gen = true;

        if (red && tile.RedWire)
        {
            WorldGen.KillWire(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 6, x, y);
            wiresRemoved++;
            count++;
        }

        if (green && tile.GreenWire)
        {
            WorldGen.KillWire3(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 13, x, y);
            wiresRemoved++;
            count++;
        }

        if (blue && tile.BlueWire)
        {
            WorldGen.KillWire2(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 11, x, y);
            wiresRemoved++;
            count++;
        }

        if (yellow && tile.YellowWire)
        {
            WorldGen.KillWire4(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 17, x, y);
            wiresRemoved++;
            count++;
        }

        if (act && tile.HasActuator)
        {
            WorldGen.KillActuator(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 9, x, y);
            actuatorsRemoved++;
            count++;
        }

        WorldGen.gen = wasGen;
        return count;
    }

    /// <summary>
    /// Gives removed wire/actuator items directly to the player's inventory.
    /// Items that don't fit are dropped near the player in consolidated stacks
    /// (max 999 per stack) instead of individual items scattered across tiles.
    /// </summary>
    public static void GiveRemovedItems(Player player, int wireCount, int actuatorCount)
    {
        if (wireCount > 0)
            GiveItemToPlayer(player, ItemID.Wire, wireCount);
        if (actuatorCount > 0)
            GiveItemToPlayer(player, ItemID.Actuator, actuatorCount);
    }

    /// <summary>
    /// Gives a quantity of an item to the player. Tries inventory first;
    /// overflows are dropped near the player in stacks of up to 999.
    /// </summary>
    private static void GiveItemToPlayer(Player player, int itemType, int amount)
    {
        // First, try to fill existing partial stacks in main inventory (0–49 only).
        // We intentionally skip coin slots (50–53) and ammo slots (54–57) to prevent
        // wire/actuator items from being inserted into those special slots.
        for (int i = 0; i < MainInventoryEnd && amount > 0; i++)
        {
            var slot = player.inventory[i];
            if (slot.type == itemType && slot.stack < slot.maxStack)
            {
                int canAdd = Math.Min(amount, slot.maxStack - slot.stack);
                slot.stack += canAdd;
                amount -= canAdd;
            }
        }

        // Next, try empty slots in main inventory only
        for (int i = 0; i < MainInventoryEnd && amount > 0; i++)
        {
            var slot = player.inventory[i];
            if (slot.IsAir)
            {
                int stackSize = Math.Min(amount, 999);
                player.inventory[i] = new Item();
                player.inventory[i].SetDefaults(itemType);
                player.inventory[i].stack = stackSize;
                amount -= stackSize;
            }
        }

        // If still remaining, drop near the player in consolidated stacks
        while (amount > 0)
        {
            int stackSize = Math.Min(amount, 999);
            int idx = Item.NewItem(
                new Terraria.DataStructures.EntitySource_TileBreak((int)(player.Center.X / 16), (int)(player.Center.Y / 16)),
                player.Center, Vector2.Zero, itemType, stackSize);
            if (idx >= 0 && idx < Main.maxItems)
                Main.item[idx].velocity = Vector2.Zero;
            amount -= stackSize;
        }
    }

    public static bool HasItem(Player player, int itemType)
    {
        for (int i = 0; i < MaxInventorySlots; i++)
            if (player.inventory[i].type == itemType && player.inventory[i].stack > 0)
                return true;
        return false;
    }

    public static void ConsumeItem(Player player, int itemType)
    {
        for (int i = 0; i < MaxInventorySlots; i++)
        {
            if (player.inventory[i].type == itemType && player.inventory[i].stack > 0)
            {
                player.inventory[i].stack--;
                if (player.inventory[i].stack <= 0)
                    player.inventory[i].TurnToAir();
                return;
            }
        }
    }

    public static int CountWires(Player player)
    {
        int count = 0;
        for (int i = 0; i < MaxInventorySlots; i++)
            if (player.inventory[i].type == ItemID.Wire)
                count += player.inventory[i].stack;
        return count;
    }

    public static int CountActuators(Player player)
    {
        int count = 0;
        for (int i = 0; i < MaxInventorySlots; i++)
            if (player.inventory[i].type == ItemID.Actuator)
                count += player.inventory[i].stack;
        return count;
    }
}