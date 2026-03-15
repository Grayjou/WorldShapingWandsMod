using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// /unlock_chests — scans an 11×11 tile area around the player for locked chests
/// and attempts to unlock each one using keys in the player's inventory.
///
/// Usage:
///   /unlock_chests          — unlock chests, consuming keys as required
///   /unlock_chests ignore   — unlock ALL locked chests without consuming any keys
///                             (diagnostic mode: tests whether unlock logic fires correctly)
///
/// This command was added to diagnose whether the ContainerHelper unlock callbacks were
/// actually working vs. whether the wand pipeline was failing before ever calling them.
/// </summary>
public class UnlockChestsCommand : ModCommand
{
    public override string Command => "unlock_chests";
    public override string Usage => "/unlock_chests [ignore]\n" +
        "Scans 11x11 tiles around the player for locked chests and unlocks them.\n" +
        "  (no args) — unlocks using keys in inventory (keys consumed as needed)\n" +
        "  ignore    — force-unlocks all locked chests without consuming any keys (diagnostic)";
    public override string Description => "Unlock chests in a 11x11 area around the player";
    public override CommandType Type => CommandType.Chat;

    private const int ScanRadius = 5; // 11×11 = radius 5 from center

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var player = caller.Player;
        bool ignoreKeys = args.Length > 0 && args[0].Equals("ignore", System.StringComparison.OrdinalIgnoreCase);

        int playerTileX = (int)(player.Center.X / 16);
        int playerTileY = (int)(player.Center.Y / 16);

        // Scan 11×11 area (radius 5 in each direction)
        var scannedTiles = new List<Point>();
        for (int dx = -ScanRadius; dx <= ScanRadius; dx++)
        {
            for (int dy = -ScanRadius; dy <= ScanRadius; dy++)
            {
                int tx = playerTileX + dx;
                int ty = playerTileY + dy;
                if (WorldGen.InWorld(tx, ty, 1))
                    scannedTiles.Add(new Point(tx, ty));
            }
        }

        // Find all containers in the scan area
        var containers = ContainerHelper.FindContainers(scannedTiles);

        int found = 0;
        int unlocked = 0;
        int alreadyOpen = 0;
        int failed = 0;

        foreach (var container in containers)
        {
            found++;
            int cx = container.TopLeft.X;
            int cy = container.TopLeft.Y;

            if (!container.IsLocked)
            {
                alreadyOpen++;
                continue;
            }

            bool success;
            if (ignoreKeys)
            {
                // Force-unlock: directly call Chest.Unlock (handles most vanilla types)
                // and fall back to manual frame shift for style 1 (Gold Chest).
                success = ForceUnlock(cx, cy, container.TileType);
            }
            else
            {
                success = ContainerHelper.TryUnlockChest(player, cx, cy, container.TileType);
            }

            if (success)
            {
                unlocked++;
                caller.Reply($"  Unlocked chest at ({cx}, {cy})", Color.Green);
            }
            else
            {
                failed++;
                caller.Reply($"  Failed to unlock chest at ({cx}, {cy}) — missing key or prerequisite not met", Color.Yellow);
            }
        }

        // Summary
        if (found == 0)
        {
            caller.Reply("No chests found in 11×11 area around you.", Color.Gray);
            return;
        }

        string modeStr = ignoreKeys ? " [ignore mode — keys NOT required]" : "";
        caller.Reply(
            $"Scan complete{modeStr}: {found} chest(s) found, " +
            $"{unlocked} unlocked, {alreadyOpen} already open, {failed} failed.",
            failed == 0 ? Color.Lime : Color.Orange
        );
    }

    /// <summary>
    /// Force-unlock regardless of key possession. Used for diagnostic purposes.
    /// Chest.Unlock handles all vanilla locked styles:
    ///   Style 2 (Locked Gold), Style 4 (Locked Shadow), Styles 23–27 (Biome),
    ///   Styles 36/38/40 (other), and Containers2 style 13.
    /// It also delegates to TileLoader.UnlockChest for modded chests.
    /// </summary>
    private static bool ForceUnlock(int x, int y, int tileType)
    {
        if (!Chest.IsLocked(x, y)) return true;

        // Chest.Unlock handles frame shifts for all vanilla locked chest styles
        return Chest.Unlock(x, y);
    }
}
