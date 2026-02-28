using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Utilities;

public static class WiringHelper
{
    private const int MaxInventorySlots = 58;

    public static (int placed, int removed) ExecuteWiringOperation(
        IEnumerable<Point> tiles,
        WiringMode mode,
        bool wireRed, bool wireGreen, bool wireBlue, bool wireYellow, bool actuator,
        Player player)
    {
        int placed = 0, removed = 0;

        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;

            if (!WorldGen.InWorld(x, y, 1)) continue;

            if (mode == WiringMode.Place)
                placed += PlaceStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player);
            else
                removed += RemoveStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player);
        }

        return (placed, removed);
    }

    private static int PlaceStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player)
    {
        int count = 0;
        Tile tile = Main.tile[x, y];

        if (red && !tile.RedWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5, x, y);
                count++;
            }
        }

        if (green && !tile.GreenWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire3(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 12, x, y);
                count++;
            }
        }

        if (blue && !tile.BlueWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire2(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 10, x, y);
                count++;
            }
        }

        if (yellow && !tile.YellowWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire4(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 16, x, y);
                count++;
            }
        }

        if (act && !tile.HasActuator && HasItem(player, ItemID.Actuator))
        {
            if (WorldGen.PlaceActuator(x, y))
            {
                ConsumeItem(player, ItemID.Actuator);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 8, x, y);
                count++;
            }
        }

        return count;
    }

    private static int RemoveStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player)
    {
        int count = 0;
        Tile tile = Main.tile[x, y];

        if (red && tile.RedWire)
        {
            WorldGen.KillWire(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 6, x, y);
            count++;
        }

        if (green && tile.GreenWire)
        {
            WorldGen.KillWire3(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 13, x, y);
            count++;
        }

        if (blue && tile.BlueWire)
        {
            WorldGen.KillWire2(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 11, x, y);
            count++;
        }

        if (yellow && tile.YellowWire)
        {
            WorldGen.KillWire4(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 17, x, y);
            count++;
        }

        if (act && tile.HasActuator)
        {
            WorldGen.KillActuator(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 9, x, y);
            count++;
        }

        return count;
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