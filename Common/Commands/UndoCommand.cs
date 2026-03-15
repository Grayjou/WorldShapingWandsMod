using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Undo;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// Chat command for triggering undo operations.
/// Dev-testing convenience — undo will eventually get a keybind or UI button.
/// Usage: /undo [count]
/// </summary>
public class UndoCommand : ModCommand
{
    public override string Command => "undo";
    public override string Usage => "/undo [count]" +
        "\n  /undo        — Undo one operation" +
        "\n  /undo 3      — Undo 3 operations" +
        "\n  /undo all    — Undo entire stack" +
        "\n  /undo status — Show undo stack depth";

    public override string Description => "Undo wand operations (dev testing)";
    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var undoManager = caller.Player.GetModPlayer<UndoManager>();

        if (args.Length == 0)
        {
            // Single undo
            if (!undoManager.Undo())
                caller.Reply("Nothing to undo.", Color.Gray);
            return;
        }

        switch (args[0].ToLower())
        {
            case "status":
                caller.Reply($"Undo stack: {undoManager.UndoCount} operation(s)", Color.Cyan);
                break;

            case "all":
                int total = 0;
                while (undoManager.Undo()) total++;
                caller.Reply(total > 0
                    ? $"Undone {total} operation(s)."
                    : "Nothing to undo.", total > 0 ? Color.Yellow : Color.Gray);
                break;

            default:
                if (int.TryParse(args[0], out int count) && count > 0)
                {
                    int undone = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (!undoManager.Undo()) break;
                        undone++;
                    }
                    caller.Reply(undone > 0
                        ? $"Undone {undone}/{count} operation(s)."
                        : "Nothing to undo.", undone > 0 ? Color.Yellow : Color.Gray);
                }
                else
                {
                    caller.Reply(Usage, Color.Red);
                }
                break;
        }
    }
}
