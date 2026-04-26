using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Undo;
using WorldShapingWandsMod.Common.Configs;

namespace WorldShapingWandsMod.Common.Commands;

/// <summary>
/// Chat command that inspects undo stack entries without performing an undo.
/// Shows operation details, snapshot count, timestamp, and resource cost.
/// Usage: /undoinfo [index]
/// Gated behind <see cref="SandboxConfig.EnableUndoCommand"/>.
/// </summary>
public class UndoInfoCommand : ModCommand
{
    public override string Command => "undoinfo";
    public override string Usage => "/undoinfo [index]" +
        "\n  /undoinfo    — Show summary of entire undo stack" +
        "\n  /undoinfo 1  — Details for most recent operation" +
        "\n  /undoinfo 3  — Details for 3rd most recent operation";

    public override string Description => "Inspect undo stack entries and resource costs";
    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var serverConfig = WandConfigs.Sandbox;
        if (serverConfig != null && !serverConfig.EnableUndoCommand)
        {
            caller.Reply("Undo is disabled by default — it's experimental and may cause visual artifacts or resource duplication.", Color.OrangeRed);
            caller.Reply("Enable it in Server Settings > World Shaping Wands > Enable Undo Commands.", Color.Gray);
            return;
        }

        var undoManager = caller.Player.GetModPlayer<UndoManager>();

        if (undoManager.UndoCount == 0)
        {
            caller.Reply("Undo stack is empty.", Color.Gray);
            return;
        }

        if (args.Length == 0)
        {
            // Summary of all entries
            ShowStackSummary(caller, undoManager);
            return;
        }

        if (int.TryParse(args[0], out int index) && index >= 1)
        {
            ShowEntryDetails(caller, undoManager, index);
        }
        else
        {
            caller.Reply(Usage, Color.Red);
        }
    }

    private static void ShowStackSummary(CommandCaller caller, UndoManager undoManager)
    {
        caller.Reply($"[c/00CED1:Undo Stack] — {undoManager.UndoCount} operation(s):", Color.White);

        for (int i = 1; i <= undoManager.UndoCount; i++)
        {
            var action = undoManager.PeekAt(i);
            if (action == null) continue;

            string age = FormatAge(action.Timestamp);
            caller.Reply($"  [{i}] {action.Description} — {action.Snapshots.Count} tile(s), {age}",
                Color.LightGray);
        }

        caller.Reply("Use /undoinfo <index> for resource cost details.", Color.Gray);
    }

    private static void ShowEntryDetails(CommandCaller caller, UndoManager undoManager, int index)
    {
        var action = undoManager.PeekAt(index);
        if (action == null)
        {
            caller.Reply($"No undo entry at index {index}. Stack has {undoManager.UndoCount} entries.",
                Color.Red);
            return;
        }

        string age = FormatAge(action.Timestamp);

        caller.Reply($"[c/00CED1:Undo Entry [{index}]]", Color.White);
        caller.Reply($"  Operation: {action.Description}", Color.LightGray);
        caller.Reply($"  Tiles: {action.Snapshots.Count}", Color.LightGray);
        caller.Reply($"  Timestamp: {action.Timestamp:HH:mm:ss} ({age})", Color.LightGray);

        // Resource cost analysis
        var config = WandConfigs.Resources;
        bool infinite = IsInfiniteResources(config);

        if (infinite)
        {
            caller.Reply("  [c/FFD700:Resource cost: FREE (infinite resources)]", Color.Yellow);
        }
        else
        {
            var cost = action.CalculateCost();
            if (cost.IsZeroCost)
            {
                caller.Reply("  Resource cost: None (paint/coating/wire only)", Color.LightGray);
            }
            else
            {
                caller.Reply("  [c/FFD700:Resource impact:]", Color.White);
                // Split FormatForChat into lines for proper chat display
                string formatted = cost.FormatForChat();
                foreach (string line in formatted.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        caller.Reply(line, Color.LightGray);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the current config has infinite resources enabled.
    /// </summary>
    private static bool IsInfiniteResources(ResourcesConfig config)
    {
        if (config == null) return false;
        // EnableInfiniteResource is the master toggle; thresholds of 0 also mean infinite
        return config.EnableInfiniteResource
            && config.InfiniteTileThreshold == 0
            && config.InfiniteWallThreshold == 0;
    }

    private static string FormatAge(System.DateTime timestamp)
    {
        var elapsed = System.DateTime.Now - timestamp;
        if (elapsed.TotalSeconds < 60) return $"{(int)elapsed.TotalSeconds}s ago";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
        return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m ago";
    }
}
