using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace MagicWiring.Common;

public static class WiringHelper
{
    /// <summary>
    /// Maximum inventory slots to search (includes hotbar and main inventory).
    /// </summary>
    private const int MaxInventorySlots = 58;

    /// <summary>
    /// Executes wire/actuator placement or removal on a set of tile positions.
    /// All wire flags are passed explicitly — never reads from WiringSettings directly,
    /// because this method is also called on the server from network packets where
    /// WiringSettings would hold the server's defaults, not the client's choices.
    /// </summary>
    public static void ExecuteWiringOperation(
        List<Point> tiles,
        WiringMode mode,
        bool wireRed, bool wireGreen, bool wireBlue, bool wireYellow, bool actuator,
        Player player)
    {
        foreach (var tile in tiles)
        {
            int x = tile.X;
            int y = tile.Y;

            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                continue;

            if (mode == WiringMode.Place)
                PlaceStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player);
            else
                RemoveStep(x, y, wireRed, wireGreen, wireBlue, wireYellow, actuator, player);
        }
    }

    private static void PlaceStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player)
    {
        Tile tile = Main.tile[x, y];

        // Vanilla wire operations use MessageID.TileManipulation (= 17)
        // with the action sub-type as the FIRST numeric parameter.
        //
        // Action sub-types:
        //   5  = PlaceWire (red)     6  = KillWire (red)
        //   10 = PlaceWire2 (blue)   11 = KillWire2 (blue)
        //   12 = PlaceWire3 (green)  13 = KillWire3 (green)
        //   16 = PlaceWire4 (yellow) 17 = KillWire4 (yellow)
        //   8  = PlaceActuator       9  = KillActuator
        //
        // The WRONG way (what was there before):
        //   NetMessage.SendData(5, -1, -1, null, x, y);
        //   ^ This sends MessageID 5 = SyncEquipment, which syncs inventory slots!
        //
        // The CORRECT way:
        //   NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5, x, y);
        //   ^ MessageID 17 = TileManipulation, action 5 = PlaceWire

        if (red && !tile.RedWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5, x, y);
            }
        }

        if (green && !tile.GreenWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire3(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 12, x, y);
            }
        }

        if (blue && !tile.BlueWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire2(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 10, x, y);
            }
        }

        if (yellow && !tile.YellowWire && HasItem(player, ItemID.Wire))
        {
            if (WorldGen.PlaceWire4(x, y))
            {
                ConsumeItem(player, ItemID.Wire);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 16, x, y);
            }
        }

        if (act && !tile.HasActuator && HasItem(player, ItemID.Actuator))
        {
            if (WorldGen.PlaceActuator(x, y))
            {
                ConsumeItem(player, ItemID.Actuator);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 8, x, y);
            }
        }
    }

    /// <summary>
    /// Removes wires/actuators. All flags are explicit parameters —
    /// this method NEVER reads from WiringSettings, which fixes the
    /// multiplayer bug where the server's default settings (all false)
    /// would be used instead of the requesting client's choices.
    /// </summary>
    private static void RemoveStep(int x, int y,
        bool red, bool green, bool blue, bool yellow, bool act,
        Player player)
    {
        Tile tile = Main.tile[x, y];

        if (red && tile.RedWire)
        {
            WorldGen.KillWire(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 6, x, y);
        }

        if (green && tile.GreenWire)
        {
            WorldGen.KillWire3(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 13, x, y);
        }

        if (blue && tile.BlueWire)
        {
            WorldGen.KillWire2(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 11, x, y);
        }

        if (yellow && tile.YellowWire)
        {
            WorldGen.KillWire4(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 17, x, y);
        }

        if (act && tile.HasActuator)
        {
            WorldGen.KillActuator(x, y);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 9, x, y);
        }
    }

    private static bool HasItem(Player player, int itemType)
    {
        for (int i = 0; i < MaxInventorySlots; i++)
            if (player.inventory[i].type == itemType && player.inventory[i].stack > 0)
                return true;
        return false;
    }

    private static void ConsumeItem(Player player, int itemType)
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
}