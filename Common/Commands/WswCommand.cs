using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// Central /wsw command hub for World Shaping Wands.
///
/// Subcommands:
///   /wsw toolinfo           — Displays pickaxe, hammer, axe power and mining speed.
///   /wsw paintdemo [args]   — Places a row/column of tiles/walls painted in all 31 colors.
///     Aliases: pd
///     /wsw pd [r|c] [t|w]
///       r = row (default), c = column
///       t = tile (default), w = wall
///     Example: /wsw pd c w  — column of walls
///              /wsw pd      — row of tiles (default)
/// </summary>
public class WswCommand : ModCommand
{
    public override string Command => "wsw";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "World Shaping Wands command hub. Use /wsw help for subcommands.";
    public override string Usage => "/wsw <subcommand> [args]\n" +
        "Subcommands:\n" +
        "  toolinfo         — Show pickaxe, hammer, axe power and mining speed\n" +
        "  paintdemo (pd)   — Place a demo strip of all 31 paint colors\n" +
        "    [r|c] [t|w]    — r=row (default), c=column; t=tile (default), w=wall\n" +
        "  inventoryview (iv) — List candidate item types + choice for the held wand\n" +
#if DEBUG
        "  overlay          — [DEBUG] Print current selection/overlay state snapshot\n" +
#endif
        "  help             — Show this help text";

    private const int PaintCount = 31; // Colors 0–30

    // Paint color names indexed by PaintID (0–30)
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
        if (args.Length == 0)
        {
            caller.Reply(Usage, Color.Yellow);
            return;
        }

        switch (args[0].ToLower())
        {
            case "toolinfo":
                RunToolInfo(caller);
                break;

            case "paintdemo":
            case "pd":
                RunPaintDemo(caller, args);
                break;

            case "inventoryview":
            case "iv":
                RunInventoryView(caller);
                break;

#if DEBUG
            case "overlay":
                RunOverlaySnapshot(caller);
                break;
#endif

            case "help":
                caller.Reply(Usage, Color.Yellow);
                break;

            default:
                caller.Reply($"Unknown subcommand '{args[0]}'. Use /wsw help for available commands.", Color.Red);
                break;
        }
    }

    // ── toolinfo ──────────────────────────────────────────────────────
    /// <summary>
    /// Displays the player's current pickaxe, hammer, axe power and mining speed.
    /// Scans the full inventory for the best tools.
    /// </summary>
    private static void RunToolInfo(CommandCaller caller)
    {
        var player = caller.Player;

        int bestPick = 0;
        int bestHammer = 0;
        int bestAxe = 0;
        int pickSpeed = 0;

        // Scan inventory for best tools
        for (int i = 0; i < 58; i++)
        {
            var item = player.inventory[i];
            if (item == null || item.IsAir) continue;

            if (item.pick > bestPick)
            {
                bestPick = item.pick;
                pickSpeed = item.useTime;
            }
            if (item.hammer > bestHammer) bestHammer = item.hammer;
            if (item.axe > bestAxe) bestAxe = item.axe;
        }

        // Axe power is displayed as axe * 5 in vanilla tooltips
        int displayAxe = bestAxe * 5;

        caller.Reply("─── Tool Info ───", Color.Gold);
        caller.Reply($"  Pickaxe Power: {bestPick}%", bestPick > 0 ? Color.Lime : Color.Gray);
        caller.Reply($"  Hammer Power:  {bestHammer}%", bestHammer > 0 ? Color.Lime : Color.Gray);
        caller.Reply($"  Axe Power:     {displayAxe}%", displayAxe > 0 ? Color.Lime : Color.Gray);
        if (bestPick > 0)
            caller.Reply($"  Mining Speed:  {pickSpeed} (useTime of best pickaxe)", Color.LightGray);
        else
            caller.Reply("  Mining Speed:  N/A (no pickaxe found)", Color.Gray);
    }

    // ── paintdemo ─────────────────────────────────────────────────────
    /// <summary>
    /// Places a strip of 31 tiles or walls with all paint colors (0–30).
    /// Consumes tiles from the player's inventory. Warns if the area isn't clear.
    ///
    /// Args after "pd"/"paintdemo":
    ///   [r|row|c|col] [t|tile|w|wall]
    ///   Defaults: row, tile
    /// </summary>
    private static void RunPaintDemo(CommandCaller caller, string[] args)
    {
        var player = caller.Player;

        // Parse optional direction and target arguments
        bool isColumn = false;
        bool isWall = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "r":
                case "row":
                    isColumn = false;
                    break;
                case "c":
                case "col":
                case "column":
                    isColumn = true;
                    break;
                case "t":
                case "tile":
                    isWall = false;
                    break;
                case "w":
                case "wall":
                    isWall = true;
                    break;
                default:
                    caller.Reply($"Unknown paintdemo argument '{args[i]}'. Expected: r/c/t/w", Color.Red);
                    return;
            }
        }

        // Determine the tile type from the player's first matching inventory item
        int startX = (int)(player.position.X / 16f) + 2;
        int startY = (int)(player.position.Y / 16f) + 2;

        if (isWall)
        {
            // Find the first wall item in inventory
            int wallItemType = -1;
            int wallItemSlot = -1;
            int wallType = -1;

            for (int i = 0; i < 58; i++)
            {
                var item = player.inventory[i];
                if (item == null || item.IsAir) continue;
                if (item.createWall > 0)
                {
                    wallItemType = item.type;
                    wallItemSlot = i;
                    wallType = item.createWall;
                    break;
                }
            }

            if (wallItemType < 0)
            {
                caller.Reply("No wall items found in your inventory.", Color.Red);
                return;
            }

            // Check if player has enough
            int available = CountItem(player, wallItemType);
            if (available < PaintCount)
            {
                caller.Reply($"Not enough walls. Need {PaintCount}, have {available} " +
                    $"({Lang.GetItemNameValue(wallItemType)}).", Color.Red);
                return;
            }

            // Check area is clear
            int blocked = 0;
            for (int i = 0; i < PaintCount; i++)
            {
                int tx = isColumn ? startX : startX + i;
                int ty = isColumn ? startY + i : startY;
                if (!WorldGen.InWorld(tx, ty, 1)) { blocked++; continue; }
                var t = Main.tile[tx, ty];
                if (t.WallType > WallID.None) blocked++;
            }

            if (blocked > 0)
            {
                caller.Reply($"Area not clear: {blocked} position(s) already have walls. " +
                    "Clear the area first.", Color.Red);
                return;
            }

            // Place and paint walls
            ConsumeItems(player, wallItemType, PaintCount);
            string dir = isColumn ? "column" : "row";
            caller.Reply($"Placing {PaintCount} walls ({Lang.GetItemNameValue(wallItemType)}) " +
                $"as a {dir} starting at ({startX}, {startY})", Color.Yellow);

            for (int i = 0; i < PaintCount; i++)
            {
                int tx = isColumn ? startX : startX + i;
                int ty = isColumn ? startY + i : startY;

                WorldGen.PlaceWall(tx, ty, wallType, true);
                WorldGen.paintWall(tx, ty, (byte)i, true);

                string name = i < PaintColorNames.Length ? PaintColorNames[i] : $"? ({i})";
                caller.Reply($"  ({tx},{ty}) → {name}", Color.LightGray);
            }

            // Sync tile changes
            int regionX = isColumn ? startX : startX;
            int regionY = isColumn ? startY : startY;
            int regionW = isColumn ? 1 : PaintCount;
            int regionH = isColumn ? PaintCount : 1;
            NetMessage.SendTileSquare(-1, regionX, regionY, regionW, regionH);

            caller.Reply("Paint demo (walls) complete!", Color.Lime);
        }
        else
        {
            // Tile mode — find the first placeable tile in inventory
            int tileItemType = -1;
            int tileItemSlot = -1;
            int tileType = -1;

            for (int i = 0; i < 58; i++)
            {
                var item = player.inventory[i];
                if (item == null || item.IsAir) continue;
                if (item.createTile >= TileID.Dirt) // createTile >= 0 means item places a tile
                {
                    tileItemType = item.type;
                    tileItemSlot = i;
                    tileType = item.createTile;
                    break;
                }
            }

            if (tileItemType < 0)
            {
                caller.Reply("No placeable tile items found in your inventory.", Color.Red);
                return;
            }

            // Check if player has enough
            int available = CountItem(player, tileItemType);
            if (available < PaintCount)
            {
                caller.Reply($"Not enough tiles. Need {PaintCount}, have {available} " +
                    $"({Lang.GetItemNameValue(tileItemType)}).", Color.Red);
                return;
            }

            // Check area is clear
            int blocked = 0;
            for (int i = 0; i < PaintCount; i++)
            {
                int tx = isColumn ? startX : startX + i;
                int ty = isColumn ? startY + i : startY;
                if (!WorldGen.InWorld(tx, ty, 1)) { blocked++; continue; }
                var t = Main.tile[tx, ty];
                if (t.HasTile) blocked++;
            }

            if (blocked > 0)
            {
                caller.Reply($"Area not clear: {blocked} position(s) already have tiles. " +
                    "Clear the area first.", Color.Red);
                return;
            }

            // Place and paint tiles
            ConsumeItems(player, tileItemType, PaintCount);
            string dir = isColumn ? "column" : "row";
            caller.Reply($"Placing {PaintCount} tiles ({Lang.GetItemNameValue(tileItemType)}) " +
                $"as a {dir} starting at ({startX}, {startY})", Color.Yellow);

            for (int i = 0; i < PaintCount; i++)
            {
                int tx = isColumn ? startX : startX + i;
                int ty = isColumn ? startY + i : startY;

                WorldGen.PlaceTile(tx, ty, tileType, mute: true, forced: true);
                WorldGen.SquareTileFrame(tx, ty, true);
                WorldGen.paintTile(tx, ty, (byte)i, true);

                string name = i < PaintColorNames.Length ? PaintColorNames[i] : $"? ({i})";
                caller.Reply($"  ({tx},{ty}) → {name}", Color.LightGray);
            }

            // Sync tile changes
            int regionX = isColumn ? startX : startX;
            int regionY = isColumn ? startY : startY;
            int regionW = isColumn ? 1 : PaintCount;
            int regionH = isColumn ? PaintCount : 1;
            NetMessage.SendTileSquare(-1, regionX, regionY, regionW, regionH);

            caller.Reply("Paint demo (tiles) complete!", Color.Lime);
        }
    }

    // ── overlay (DEBUG) ───────────────────────────────────────────────
#if DEBUG
    /// <summary>
    /// Prints the current overlay/selection state snapshot to chat on demand.
    /// Design: C-S4 2026-05-03 (DesignDoc_OverlayDebugSnapshot_OnDemand.md §4).
    /// </summary>
    private static void RunOverlaySnapshot(CommandCaller caller)
    {
        var player = caller.Player;
        var wp = player.GetModPlayer<global::WorldShapingWandsMod.Common.Players.WandPlayer>();
        var overlay = ModContent.GetInstance<global::WorldShapingWandsMod.Common.Drawing.SelectionOverlay>();
        var snap = global::WorldShapingWandsMod.Common.Debug.OverlaySnapshot.Capture(player, wp, overlay);
        Main.NewText("[OVL on demand] " + snap.ToChatLine(), Color.Magenta);
    }
#endif

    // ── Helpers ───────────────────────────────────────────────────────
    private static int CountItem(Player player, int itemType)
    {
        int count = 0;
        for (int i = 0; i < 58; i++)
        {
            if (player.inventory[i].type == itemType)
                count += player.inventory[i].stack;
        }
        return count;
    }

    private static void ConsumeItems(Player player, int itemType, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < 58 && remaining > 0; i++)
        {
            if (player.inventory[i].type != itemType) continue;
            int take = System.Math.Min(player.inventory[i].stack, remaining);
            player.inventory[i].stack -= take;
            remaining -= take;
            if (player.inventory[i].stack <= 0)
                player.inventory[i].TurnToAir();
        }
    }

    // ── inventoryview ──────────────────────────────────────
    /// <summary>
    /// Smoke-test for the InventoryView v1 backend (S5 2026-04-22).
    /// Prints, for the player's currently-held wand:
    ///   1. The wand family,
    ///   2. Whether it participates in InventoryView,
    ///   3. The provider's panel title key,
    ///   4. Each source: title key, candidate item names, and current choice (if any).
    /// Proves the registry/sources/choices wire end-to-end before any UI is built.
    /// </summary>
    private static void RunInventoryView(CommandCaller caller)
    {
        var player = caller.Player;
        var wp = player.GetModPlayer<global::WorldShapingWandsMod.Common.Players.WandPlayer>();
        var family = global::WorldShapingWandsMod.Common.Items.BaseCyclingWand.GetCurrentFamily(player);

        caller.Reply("─── InventoryView ───", Color.Gold);
        caller.Reply($"  Held wand family: {family}", Color.LightGray);

        var provider = global::WorldShapingWandsMod.Common.UI.InventoryView.InventoryViewRegistry.GetProvider(player);
        if (provider == null)
        {
            caller.Reply("  Family does not participate in InventoryView.", Color.Gray);
            caller.Reply("  (Only Building / Torches / Replacement participate.)", Color.Gray);
            return;
        }

        caller.Reply($"  Panel title key: {provider.PanelTitleKey}", Color.LightGray);
        caller.Reply($"  Sources: {provider.Sources.Count}", Color.LightGray);

        for (int s = 0; s < provider.Sources.Count; s++)
        {
            var src = provider.Sources[s];
            int? choice = src.GetSelectedItemType(wp);
            string choiceStr = choice.HasValue ? $"{choice.Value} ({Lang.GetItemNameValue(choice.Value)})" : "<none>";
            caller.Reply($"  [{s}] {src.TitleKey}", Color.Cyan);
            caller.Reply($"      choice = {choiceStr}", Color.LightGray);

            int count = 0;
            foreach (int t in src.GetCandidateItemTypes(player))
            {
                count++;
                if (count <= 12)
                    caller.Reply($"      • {t,5}  {Lang.GetItemNameValue(t)}", Color.LightGray);
            }
            if (count == 0)
                caller.Reply("      (no candidates in inventory)", Color.Gray);
            else if (count > 12)
                caller.Reply($"      … +{count - 12} more (total {count})", Color.Gray);
            else
                caller.Reply($"      total: {count}", Color.Gray);
        }
    }
}
