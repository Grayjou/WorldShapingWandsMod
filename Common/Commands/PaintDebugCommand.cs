using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// Debug command for verifying paint color indices.
///
/// Usage:
///   /paintdbg row         — Paints 31 tiles in a row near the player (colors 0–30)
///                           and prints color index + name for each to chat.
///   /paintdbg hover       — Prints the paint color of the tile under the cursor.
///   /paintdbg clear       — Strips paint from a 40-tile wide row at the player's feet.
///   /paintdbg names       — Lists all 31 color names and their PaintID values to chat.
/// </summary>
public class PaintDebugCommand : ModCommand
{
    public override string Command => "paintdbg";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Debug paint colour indices. Usage: /paintdbg row|hover|clear|names";
    public override string Usage => "/paintdbg row|hover|clear|names";

    // Mirror of the CoatingSettingsPanel colour names so we can compare them here
    // Indexed by PaintID (0–30): 0=None, 1–12=basic, 13–24=deep,
    // 25=Black, 26=White, 27=Gray, 28=Brown, 29=Shadow, 30=Negative
    private static readonly string[] PaintColorNames =
    {
        "None (0)",
        "Red (1)", "Orange (2)", "Yellow (3)", "Lime (4)", "Green (5)",
        "Teal (6)", "Cyan (7)", "Sky Blue (8)", "Blue (9)", "Purple (10)", "Violet (11)", "Pink (12)",
        "Deep Red (13)", "Deep Orange (14)", "Deep Yellow (15)", "Deep Lime (16)", "Deep Green (17)",
        "Deep Teal (18)", "Deep Cyan (19)", "Deep Sky Blue (20)", "Deep Blue (21)",
        "Deep Purple (22)", "Deep Violet (23)", "Deep Pink (24)",
        "Black (25)", "White (26)", "Gray (27)", "Brown (28)", "Shadow (29)", "Negative (30)",
    };

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0) { caller.Reply(Usage); return; }

        var player = caller.Player;

        switch (args[0].ToLower())
        {
            case "row":
                PaintRow(caller, player);
                break;

            case "hover":
                PrintHoverPaint(caller, player);
                break;

            case "clear":
                ClearRow(caller, player);
                break;

            case "names":
                PrintColorNames(caller);
                break;

            default:
                caller.Reply($"Unknown subcommand '{args[0]}'. {Usage}", Color.Red);
                break;
        }
    }

    // ── row ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Places/paints 31 stone tiles in a row starting 2 tiles right of the player,
    /// assigning paint colors 0–30 respectively.
    /// </summary>
    private static void PaintRow(CommandCaller caller, Player player)
    {
        int startX = (int)(player.position.X / 16f) + 2;
        int startY = (int)(player.position.Y / 16f) + 2;

        caller.Reply($"Painting debug row at tile ({startX}, {startY}) — 31 tiles, colors 0–30", Color.Yellow);

        for (int i = 0; i <= 30; i++)
        {
            int tx = startX + i;
            int ty = startY;

            // Place a stone tile if there isn't already a solid tile there
            if (!Main.tile[tx, ty].HasTile)
            {
                WorldGen.PlaceTile(tx, ty, TileID.Stone, mute: true, forced: true);
                WorldGen.SquareTileFrame(tx, ty, true);
            }

            // Apply paint (color 0 = None, which strips paint)
            WorldGen.paintTile(tx, ty, (byte)i, true);

            // Print confirmation line
            string name = i < PaintColorNames.Length ? PaintColorNames[i] : $"Unknown ({i})";
            caller.Reply($"  Tile ({tx},{ty}) → color {i} = {name}", Color.LightGray);
        }

        caller.Reply("Done. Walk along the row to see each colour in-game.", Color.Lime);
    }

    // ── hover ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Reads the paint color of the tile currently under the cursor.
    /// </summary>
    private static void PrintHoverPaint(CommandCaller caller, Player player)
    {
        int tx = (int)(Main.MouseWorld.X / 16f);
        int ty = (int)(Main.MouseWorld.Y / 16f);

        if (!WorldGen.InWorld(tx, ty))
        {
            caller.Reply("Cursor is outside the world.", Color.Red);
            return;
        }

        var tile = Main.tile[tx, ty];
        byte tileColor = tile.TileColor;
        byte wallColor = tile.WallColor;

        string tileName = tileColor < PaintColorNames.Length ? PaintColorNames[tileColor] : $"Unknown ({tileColor})";
        string wallName = wallColor < PaintColorNames.Length ? PaintColorNames[wallColor] : $"Unknown ({wallColor})";

        caller.Reply($"Tile at ({tx},{ty}):", Color.Yellow);
        caller.Reply($"  TileType = {tile.TileType}  TileColor = {tileColor} ({tileName})", Color.LightGray);
        caller.Reply($"  WallType = {tile.WallType}  WallColor = {wallColor} ({wallName})", Color.LightGray);
    }

    // ── clear ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Strips paint from 40 tiles in a row at the player's feet (cleanup after /paintdbg row).
    /// </summary>
    private static void ClearRow(CommandCaller caller, Player player)
    {
        int startX = (int)(player.position.X / 16f) + 2;
        int startY = (int)(player.position.Y / 16f) + 2;

        for (int i = 0; i < 40; i++)
        {
            int tx = startX + i;
            WorldGen.paintTile(tx, startY, PaintID.None, true);
        }

        caller.Reply($"Cleared paint on 40 tiles starting at ({startX},{startY}).", Color.Lime);
    }

    // ── names ─────────────────────────────────────────────────────────────
    private static void PrintColorNames(CommandCaller caller)
    {
        caller.Reply("Paint color index → name mapping:", Color.Yellow);
        for (int i = 0; i < PaintColorNames.Length; i++)
            caller.Reply($"  {i,2} = {PaintColorNames[i]}", Color.LightGray);
    }
}
