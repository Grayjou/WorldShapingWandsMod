#if DEBUG
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// Chat command for runtime parameter tuning.
/// <para>
/// Usage:
/// <code>
/// /dev list [filter]              — List all tunables (with [Alias] column)
/// /dev get [key|alias]            — Show value (uses parked key when no arg)
/// /dev set [key|alias] &lt;value&gt;     — Set value (uses parked key when value-only)
/// /dev reset [key|alias|*]        — Reset parked / one / all (explicit *)
/// /dev nudge &lt;delta&gt;              — Adjust parked numeric tunable
/// /dev save                       — Save current values to dev_tunables.json
/// /dev reload                     — Reload values from dev_tunables.json
/// /dev bake [filter]              — Print modified values as ready-to-paste consts
/// </code>
/// Sibling commands: <c>/dev_park &lt;key|alias|-&gt;</c> and short alias <c>/dp</c>
/// for sticky tuning loops. See <c>DesignDoc_DevTunables_Improvements.md</c>.
/// </para>
/// </summary>
/// <remarks>
/// Design reference: <c>dev_notes/planning/DebugPipelinePlan.md</c> §2.5
/// </remarks>
public class DevCommand : ModCommand
{
    public override string Command => "dev";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "[DEBUG] Runtime parameter tuning — /dev list|get|set|reset|nudge|save|reload|bake";

    public override string Usage =>
        "/dev list [filter]            — List tunables (with [Alias] column)\n"
        + "/dev get [key|alias]         — Show value (uses parked key when no arg)\n"
        + "/dev set [key|alias] <value> — Set value (uses parked key when value-only)\n"
        + "/dev reset [key|alias|*]     — Reset parked / one / all\n"
        + "/dev nudge <delta>           — Adjust parked numeric tunable\n"
        + "/dev save                    — Save to dev_tunables.json\n"
        + "/dev reload                  — Reload from dev_tunables.json\n"
        + "/dev bake [filter]           — Print const declarations\n"
        + "Sticky tuning: /dev_park <key|alias|-> or /dp";

    // Colors for consistent feedback
    internal static readonly Color ColorSuccess = new(100, 255, 100);
    internal static readonly Color ColorInfo = new(200, 200, 255);
    internal static readonly Color ColorWarning = new(255, 200, 50);
    internal static readonly Color ColorError = new(255, 80, 80);
    internal static readonly Color ColorBake = new(255, 180, 50);
    internal static readonly Color ColorPark = new(180, 220, 255);

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
        {
            caller.Reply(Usage, ColorWarning);
            EchoParkedHint(caller);
            return;
        }

        switch (args[0].ToLower())
        {
            case "list":
            case "ls":
                RunList(caller, args);
                break;
            case "get":
                RunGet(caller, args);
                break;
            case "set":
                RunSet(caller, args);
                break;
            case "reset":
                RunReset(caller, args);
                break;
            case "nudge":
                RunNudge(caller, args);
                break;
            case "save":
                RunSave(caller);
                break;
            case "reload":
                RunReload(caller);
                break;
            case "bake":
                RunBake(caller, args);
                break;
            case "park":
                // Convenience alias of /dev_park inside the /dev command surface.
                DevParkHandler.Run(caller, args.Skip(1).ToArray());
                break;
            default:
                caller.Reply($"Unknown subcommand: {args[0]}", ColorError);
                caller.Reply(Usage, ColorWarning);
                break;
        }
    }

    private static void EchoParkedHint(CommandCaller caller)
    {
        if (DevTunable.ParkedKey != null)
            caller.Reply($"Parked: {DevTunable.ParkedKey} = {DevTunable.GetFormatted(DevTunable.ParkedKey)}", ColorPark);
    }

    // ── Subcommand Handlers ──────────────────────────────────

    private static void RunList(CommandCaller caller, string[] args)
    {
        string filter = args.Length > 1 ? args[1] : null;
        var entries = DevTunable.ListAll(filter).ToList();

        if (entries.Count == 0)
        {
            caller.Reply(filter != null
                ? $"No tunables matching '{filter}'."
                : "No tunables registered.", ColorWarning);
            return;
        }

        caller.Reply($"── Dev Tunables ({entries.Count}) ──", ColorInfo);
        foreach (var (alias, key, type, value, defaultVal, desc, modified) in entries)
        {
            string mod = modified ? " *" : "";
            string aliasCol = string.IsNullOrEmpty(alias) ? "      " : $"[{alias,-3}]";
            string parked = (DevTunable.ParkedKey != null && DevTunable.ParkedKey.Equals(key, System.StringComparison.OrdinalIgnoreCase)) ? " <parked>" : "";
            string line = $"  {aliasCol} [{type}] {key} = {value}{mod}{parked}";
            if (!string.IsNullOrEmpty(desc))
                line += $"  // {desc}";
            caller.Reply(line, modified ? ColorWarning : ColorInfo);
        }
        if (entries.Any(e => e.modified))
            caller.Reply("  (* = modified from default)", ColorWarning);
    }

    private static void RunGet(CommandCaller caller, string[] args)
    {
        string rawKey = args.Length > 1 ? args[1] : null;
        string key = rawKey != null ? DevTunable.ResolveKey(rawKey) : DevTunable.ParkedKey;

        if (key == null)
        {
            if (rawKey != null) caller.Reply($"Unknown key/alias: {rawKey}", ColorError);
            else caller.Reply("Usage: /dev get <key|alias>  (or park a key first with /dp <key>)", ColorWarning);
            return;
        }

        string value = DevTunable.GetFormatted(key);
        string defValue = DevTunable.GetDefaultFormatted(key);
        string modified = value != defValue ? " (modified)" : "";
        string aliasTag = DevTunable.GetAlias(key);
        string aliasPart = string.IsNullOrEmpty(aliasTag) ? "" : $" [{aliasTag}]";
        caller.Reply($"{key}{aliasPart} = {value}{modified}  [default: {defValue}]", ColorInfo);
    }

    private static void RunSet(CommandCaller caller, string[] args)
    {
        // Forms:
        //   /dev set <key|alias> <value>   — explicit (also sticky-parks if a key is parked)
        //   /dev set <value>               — uses parked key
        if (args.Length < 2)
        {
            caller.Reply("Usage: /dev set [key|alias] <value>", ColorWarning);
            return;
        }

        string key;
        string rawValue;

        if (args.Length == 2)
        {
            // Could be "set <key>" (missing value) OR "set <value>" (parked-mode).
            string maybeKey = DevTunable.ResolveKey(args[1]);
            if (maybeKey != null)
            {
                caller.Reply($"Usage: /dev set {args[1]} <value>", ColorWarning);
                return;
            }
            if (DevTunable.ParkedKey == null)
            {
                caller.Reply($"Unknown key/alias '{args[1]}' and no parked key. Park first with /dp <key>.", ColorError);
                return;
            }
            key = DevTunable.ParkedKey;
            rawValue = args[1];
        }
        else
        {
            string resolved = DevTunable.ResolveKey(args[1]);
            if (resolved == null)
            {
                // Possibly /dev set <multi-word value> while parked.
                if (DevTunable.ParkedKey != null)
                {
                    key = DevTunable.ParkedKey;
                    rawValue = string.Join(" ", args.Skip(1));
                }
                else
                {
                    caller.Reply($"Unknown key/alias: {args[1]}", ColorError);
                    return;
                }
            }
            else
            {
                key = resolved;
                rawValue = string.Join(" ", args.Skip(2));
                // Sticky-park: explicit set moves the parked cursor (per design §2.1).
                if (DevTunable.ParkedKey != null) DevTunable.ParkedKey = key;
            }
        }

        string type = DevTunable.TrySetAuto(key, rawValue);
        if (type == null)
        {
            caller.Reply($"Failed to set {key}: value '{rawValue}' could not be parsed.", ColorError);
            return;
        }

        string newValue = DevTunable.GetFormatted(key);
        string parkTag = (DevTunable.ParkedKey != null && DevTunable.ParkedKey.Equals(key, System.StringComparison.OrdinalIgnoreCase)) ? ", parked" : "";
        caller.Reply($"{key} = {newValue} ({type}{parkTag})", ColorSuccess);
    }

    private static void RunReset(CommandCaller caller, string[] args)
    {
        if (args.Length > 1)
        {
            string arg = args[1];
            if (arg == "*")
            {
                DevTunable.ResetAll();
                caller.Reply("All tunables reset to defaults.", ColorSuccess);
                return;
            }
            string key = DevTunable.ResolveKey(arg);
            if (key != null && DevTunable.ResetOne(key))
            {
                caller.Reply($"Reset {key} = {DevTunable.GetFormatted(key)}", ColorSuccess);
            }
            else
            {
                caller.Reply($"Unknown key/alias: {arg}", ColorError);
            }
            return;
        }

        // No arg: reset parked-only (per design §2.1 — explicit '*' for unsafe nuke).
        if (DevTunable.ParkedKey != null)
        {
            string key = DevTunable.ParkedKey;
            DevTunable.ResetOne(key);
            caller.Reply($"Reset parked {key} = {DevTunable.GetFormatted(key)}", ColorSuccess);
        }
        else
        {
            caller.Reply("No parked key. Use /dev reset * to reset all (explicit), or park first with /dp <key>.", ColorWarning);
        }
    }

    private static void RunNudge(CommandCaller caller, string[] args)
    {
        if (args.Length < 2)
        {
            caller.Reply("Usage: /dev nudge <delta>  (requires a parked key)", ColorWarning);
            return;
        }
        if (DevTunable.ParkedKey == null)
        {
            caller.Reply("No parked key. Park first with /dp <key>.", ColorError);
            return;
        }

        string key = DevTunable.ParkedKey;
        string deltaRaw = args[1];
        string type = DevTunable.Nudge(key, deltaRaw);
        if (type == null)
        {
            caller.Reply($"Cannot nudge {key}: not numeric or invalid delta '{deltaRaw}'.", ColorError);
            return;
        }
        caller.Reply($"{key} = {DevTunable.GetFormatted(key)} ({type}, nudged {deltaRaw})", ColorSuccess);
    }

    private static void RunSave(CommandCaller caller)
    {
        DevTunable.SaveToFile();
        caller.Reply("Tunables saved to dev_tunables.json", ColorSuccess);
    }

    private static void RunReload(CommandCaller caller)
    {
        DevTunable.LoadFromFile();
        caller.Reply("Tunables reloaded from dev_tunables.json", ColorSuccess);
    }

    private static void RunBake(CommandCaller caller, string[] args)
    {
        string filter = args.Length > 1 ? args[1] : null;
        var lines = DevTunable.Bake(filter).ToList();

        if (lines.Count == 0)
        {
            caller.Reply("No modified values to bake." +
                (filter != null ? $" (filter: {filter})" : ""), ColorWarning);
            return;
        }

        caller.Reply("── Baked Constants (copy to source) ──", ColorBake);
        foreach (string line in lines)
        {
            caller.Reply($"  {line}", ColorBake);
        }
        caller.Reply($"── {lines.Count} value(s) ready to paste ──", ColorBake);
    }
}

// ── Park Commands ───────────────────────────────────────────

/// <summary>
/// Shared handler for <c>/dev_park</c> and <c>/dp</c>.
/// Echoes the parked key when called with no args; sets/clears the park otherwise.
/// </summary>
internal static class DevParkHandler
{
    public static void Run(CommandCaller caller, string[] args)
    {
        if (args.Length == 0)
        {
            if (DevTunable.ParkedKey == null)
            {
                caller.Reply("No key parked. Usage: /dp <key|alias>  |  /dp - to unpark", DevCommand.ColorWarning);
                return;
            }
            string k = DevTunable.ParkedKey;
            caller.Reply($"Parked on {k} (currently {DevTunable.GetFormatted(k)}, default {DevTunable.GetDefaultFormatted(k)})", DevCommand.ColorPark);
            return;
        }

        string arg = args[0];
        if (arg == "-" || arg.Equals("off", System.StringComparison.OrdinalIgnoreCase) || arg.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
        {
            DevTunable.ParkedKey = null;
            caller.Reply("Unparked.", DevCommand.ColorPark);
            return;
        }

        try
        {
            DevTunable.ParkedKey = arg;
            string k = DevTunable.ParkedKey;
            caller.Reply($"Parked on {k} (currently {DevTunable.GetFormatted(k)}, default {DevTunable.GetDefaultFormatted(k)})", DevCommand.ColorPark);
        }
        catch (System.ArgumentException ex)
        {
            caller.Reply(ex.Message, DevCommand.ColorError);
        }
    }
}

/// <summary>Long-form park command. See <see cref="DevParkHandler"/>.</summary>
public class DevParkCommand : ModCommand
{
    public override string Command => "dev_park";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "[DEBUG] Park a tunable for sticky /dev set/get/nudge.";
    public override string Usage => "/dev_park <key|alias>  |  /dev_park - (unpark)  |  /dev_park (echo)";
    public override void Action(CommandCaller caller, string input, string[] args)
        => DevParkHandler.Run(caller, args);
}

/// <summary>Short alias of <see cref="DevParkCommand"/>.</summary>
public class DpCommand : ModCommand
{
    public override string Command => "dp";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "[DEBUG] Short alias of /dev_park.";
    public override string Usage => "/dp <key|alias>  |  /dp - (unpark)  |  /dp (echo)";
    public override void Action(CommandCaller caller, string input, string[] args)
        => DevParkHandler.Run(caller, args);
}
#endif
