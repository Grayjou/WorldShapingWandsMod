#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// Runtime-adjustable development parameters. Values update immediately
/// without rebuild. Gated behind <c>#if DEBUG</c> — does not exist in release.
/// <para>
/// Architecture: static key-value store with typed registrations (float, int, string).
/// Each registration returns a <c>Func&lt;T&gt;</c> that always reads the live value.
/// Chat command <c>/dev</c> provides set/get/list/save/reload/bake operations.
/// JSON persistence at mod source root for cross-session continuity.
/// </para>
/// </summary>
/// <remarks>
/// Design reference: <c>dev_notes/planning/DebugPipelinePlan.md</c> §2
/// </remarks>
public static class DevTunable
{
    // ── Storage ──────────────────────────────────────────────

    private static readonly Dictionary<string, float> _floats = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    // Default values for reset/initialization
    private static readonly Dictionary<string, float> _defaultFloats = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> _defaultInts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _defaultStrings = new(StringComparer.OrdinalIgnoreCase);

    // Metadata for UI/help
    private static readonly Dictionary<string, string> _descriptions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (float min, float max)> _floatRanges = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (int min, int max)> _intRanges = new(StringComparer.OrdinalIgnoreCase);

    // Alphanumeric aliases (A, B, ... Z, AA, AB ...) auto-assigned in registration order.
    // Stable for the life of a mod load. See DesignDoc_DevTunables_Improvements.md §3.
    private static int _nextAliasIndex = 1;
    private static readonly Dictionary<string, string> _aliasByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _keyByAlias = new(StringComparer.OrdinalIgnoreCase);

    // Per-session parked key (resolved canonical form). Not persisted.
    private static string _parkedKey;

    // ── File Path ────────────────────────────────────────────

    /// <summary>
    /// JSON file path at the mod source root for easy external editing.
    /// </summary>
    private static string FilePath
    {
        get
        {
            // Navigate from ModLoader's mod path to the mod source directory
            string modSourceDir = Path.Combine(
                ModLoader.ModPath, "..", "ModSources", "WorldShapingWandsMod");

            // Normalize the path
            modSourceDir = Path.GetFullPath(modSourceDir);

            return Path.Combine(modSourceDir, "dev_tunables.json");
        }
    }

    // ── Registration API ─────────────────────────────────────

    /// <summary>
    /// Registers a float tunable. Call from <see cref="DevTunableDefaults.RegisterAll"/>.
    /// Returns a <c>Func&lt;float&gt;</c> that always reads the current live value.
    /// </summary>
    public static Func<float> RegisterFloat(
        string key, float defaultValue,
        string description = "",
        float min = float.MinValue, float max = float.MaxValue)
    {
        _defaultFloats[key] = defaultValue;
        if (!_floats.ContainsKey(key))
            _floats[key] = defaultValue;
        _descriptions[key] = description;
        _floatRanges[key] = (min, max);
        AssignAlias(key);
        return () => _floats.TryGetValue(key, out float v) ? v : defaultValue;
    }

    /// <summary>
    /// Registers an int tunable.
    /// </summary>
    public static Func<int> RegisterInt(
        string key, int defaultValue,
        string description = "",
        int min = int.MinValue, int max = int.MaxValue)
    {
        _defaultInts[key] = defaultValue;
        if (!_ints.ContainsKey(key))
            _ints[key] = defaultValue;
        _descriptions[key] = description;
        _intRanges[key] = (min, max);
        AssignAlias(key);
        return () => _ints.TryGetValue(key, out int v) ? v : defaultValue;
    }

    /// <summary>
    /// Registers a string tunable (also used for Color as "R,G,B,A").
    /// </summary>
    public static Func<string> RegisterString(
        string key, string defaultValue,
        string description = "")
    {
        _defaultStrings[key] = defaultValue;
        if (!_strings.ContainsKey(key))
            _strings[key] = defaultValue;
        _descriptions[key] = description;
        AssignAlias(key);
        return () => _strings.TryGetValue(key, out string v) ? v : defaultValue;
    }

    // ── Quick-Get (for one-off reads without registration) ───

    /// <summary>
    /// Gets a float value by key. Returns <paramref name="fallback"/> if not registered.
    /// Prefer using the <c>Func&lt;float&gt;</c> returned by <see cref="RegisterFloat"/> instead.
    /// </summary>
    public static float GetFloat(string key, float fallback = 0f)
        => _floats.TryGetValue(key, out float v) ? v : fallback;

    /// <summary>
    /// Gets an int value by key.
    /// </summary>
    public static int GetInt(string key, int fallback = 0)
        => _ints.TryGetValue(key, out int v) ? v : fallback;

    /// <summary>
    /// Gets a string value by key.
    /// </summary>
    public static string GetString(string key, string fallback = "")
        => _strings.TryGetValue(key, out string v) ? v : fallback;

    // ── Setters (used by chat command) ───────────────────────

    /// <summary>
    /// Sets a float tunable value. Returns false if the key is not registered.
    /// </summary>
    public static bool SetFloat(string key, float value)
    {
        if (!_defaultFloats.ContainsKey(key)) return false;
        var (min, max) = _floatRanges[key];
        _floats[key] = Math.Clamp(value, min, max);
        return true;
    }

    /// <summary>
    /// Sets an int tunable value. Returns false if the key is not registered.
    /// </summary>
    public static bool SetInt(string key, int value)
    {
        if (!_defaultInts.ContainsKey(key)) return false;
        var (min, max) = _intRanges[key];
        _ints[key] = Math.Clamp(value, min, max);
        return true;
    }

    /// <summary>
    /// Sets a string tunable value. Returns false if the key is not registered.
    /// </summary>
    public static bool SetString(string key, string value)
    {
        if (!_defaultStrings.ContainsKey(key)) return false;
        _strings[key] = value;
        return true;
    }

    /// <summary>
    /// Attempts to set a value by key with auto type detection.
    /// Tries int, then float, then string (in that order).
    /// Returns the type that was set, or null if key not found.
    /// </summary>
    public static string TrySetAuto(string key, string rawValue)
    {
        if (_defaultInts.ContainsKey(key) && int.TryParse(rawValue, out int iv))
        {
            SetInt(key, iv);
            return "int";
        }
        if (_defaultFloats.ContainsKey(key) && float.TryParse(rawValue, out float fv))
        {
            SetFloat(key, fv);
            return "float";
        }
        if (_defaultStrings.ContainsKey(key))
        {
            SetString(key, rawValue);
            return "string";
        }
        return null;
    }

    /// <summary>
    /// Gets the current value of a key as a formatted string. Returns null if not found.
    /// </summary>
    public static string GetFormatted(string key)
    {
        if (_floats.ContainsKey(key)) return _floats[key].ToString("F4");
        if (_ints.ContainsKey(key)) return _ints[key].ToString();
        if (_strings.ContainsKey(key)) return _strings[key];
        return null;
    }

    /// <summary>
    /// Gets the default value of a key as a formatted string. Returns null if not found.
    /// </summary>
    public static string GetDefaultFormatted(string key)
    {
        if (_defaultFloats.ContainsKey(key)) return _defaultFloats[key].ToString("F4");
        if (_defaultInts.ContainsKey(key)) return _defaultInts[key].ToString();
        if (_defaultStrings.ContainsKey(key)) return _defaultStrings[key];
        return null;
    }

    // ── JSON Persistence ─────────────────────────────────────

    /// <summary>
    /// Saves all current tunable values to the JSON file.
    /// </summary>
    public static void SaveToFile()
    {
        var data = new Dictionary<string, object>();
        foreach (var kv in _floats.OrderBy(x => x.Key))
            data[$"float:{kv.Key}"] = kv.Value;
        foreach (var kv in _ints.OrderBy(x => x.Key))
            data[$"int:{kv.Key}"] = kv.Value;
        foreach (var kv in _strings.OrderBy(x => x.Key))
            data[$"string:{kv.Key}"] = kv.Value;

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string dir = Path.GetDirectoryName(FilePath);
        if (dir != null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Loads tunable values from the JSON file. Only loads keys that are registered.
    /// </summary>
    public static void LoadFromFile()
    {
        if (!File.Exists(FilePath)) return;

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (data == null) return;

            foreach (var kv in data)
            {
                string[] parts = kv.Key.Split(':', 2);
                if (parts.Length != 2) continue;

                switch (parts[0])
                {
                    case "float" when _defaultFloats.ContainsKey(parts[1]):
                        _floats[parts[1]] = Convert.ToSingle(kv.Value);
                        break;
                    case "int" when _defaultInts.ContainsKey(parts[1]):
                        _ints[parts[1]] = Convert.ToInt32(kv.Value);
                        break;
                    case "string" when _defaultStrings.ContainsKey(parts[1]):
                        _strings[parts[1]] = kv.Value?.ToString() ?? "";
                        break;
                }
            }
        }
        catch (Exception)
        {
            // Silently ignore corrupt JSON — user can re-save or delete the file.
            // In debug builds, the error will be visible in the IDE output.
        }
    }

    // ── Reset ────────────────────────────────────────────────

    /// <summary>Resets all tunables to their registered default values.</summary>
    public static void ResetAll()
    {
        foreach (var kv in _defaultFloats) _floats[kv.Key] = kv.Value;
        foreach (var kv in _defaultInts) _ints[kv.Key] = kv.Value;
        foreach (var kv in _defaultStrings) _strings[kv.Key] = kv.Value;
    }

    /// <summary>Resets a single tunable to its default. Returns false if not found.</summary>
    public static bool ResetOne(string key)
    {
        if (_defaultFloats.ContainsKey(key))
        {
            _floats[key] = _defaultFloats[key];
            return true;
        }
        if (_defaultInts.ContainsKey(key))
        {
            _ints[key] = _defaultInts[key];
            return true;
        }
        if (_defaultStrings.ContainsKey(key))
        {
            _strings[key] = _defaultStrings[key];
            return true;
        }
        return false;
    }

    // ── Listing ──────────────────────────────────────────────

    /// <summary>
    /// Lists all registered tunables, optionally filtered by a key prefix.
    /// Returns (alias, key, type, currentValue, defaultValue, description, isModified).
    /// </summary>
    public static IEnumerable<(string alias, string key, string type, string value, string defaultVal, string desc, bool modified)>
        ListAll(string filter = null)
    {
        foreach (var kv in _floats.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            float def = _defaultFloats[kv.Key];
            yield return (_aliasByKey.GetValueOrDefault(kv.Key, ""), kv.Key, "float", kv.Value.ToString("F4"), def.ToString("F4"),
                _descriptions.GetValueOrDefault(kv.Key, ""),
                Math.Abs(kv.Value - def) > 0.0001f);
        }
        foreach (var kv in _ints.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            int def = _defaultInts[kv.Key];
            yield return (_aliasByKey.GetValueOrDefault(kv.Key, ""), kv.Key, "int", kv.Value.ToString(), def.ToString(),
                _descriptions.GetValueOrDefault(kv.Key, ""),
                kv.Value != def);
        }
        foreach (var kv in _strings.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            string def = _defaultStrings[kv.Key];
            yield return (_aliasByKey.GetValueOrDefault(kv.Key, ""), kv.Key, "string", kv.Value, def,
                _descriptions.GetValueOrDefault(kv.Key, ""),
                kv.Value != def);
        }
    }

    // ── Bake ─────────────────────────────────────────────────

    /// <summary>
    /// Generates ready-to-paste const declarations for all modified (non-default) tunables.
    /// If <paramref name="filter"/> is provided, only matching keys are included.
    /// </summary>
    public static IEnumerable<string> Bake(string filter = null)
    {
        foreach (var kv in _floats.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            float def = _defaultFloats[kv.Key];
            if (Math.Abs(kv.Value - def) > 0.0001f)
            {
                string name = kv.Key.Split('.').Last();
                yield return $"private const float {name} = {kv.Value:F4}f;  // was {def:F4}f";
            }
        }
        foreach (var kv in _ints.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            int def = _defaultInts[kv.Key];
            if (kv.Value != def)
            {
                string name = kv.Key.Split('.').Last();
                yield return $"private const int {name} = {kv.Value};  // was {def}";
            }
        }
        foreach (var kv in _strings.OrderBy(x => x.Key))
        {
            if (filter != null && !kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            string def = _defaultStrings[kv.Key];
            if (kv.Value != def)
            {
                string name = kv.Key.Split('.').Last();
                yield return $"private const string {name} = \"{kv.Value}\";  // was \"{def}\"";
            }
        }
    }

    // ── Aliases & Parking (DesignDoc_DevTunables_Improvements.md) ───

    /// <summary>
    /// Converts a 1-based registration index to a base-26 alphabetic alias.
    /// 1 -> "A", 26 -> "Z", 27 -> "AA", 28 -> "AB", ...
    /// </summary>
    private static string IndexToAlias(int n)
    {
        var sb = new StringBuilder();
        while (n > 0)
        {
            n--;
            sb.Insert(0, (char)('A' + (n % 26)));
            n /= 26;
        }
        return sb.ToString();
    }

    private static void AssignAlias(string key)
    {
        if (_aliasByKey.ContainsKey(key)) return; // re-registration on hot reload — keep stable
        string alias = IndexToAlias(_nextAliasIndex++);
        _aliasByKey[key] = alias;
        _keyByAlias[alias] = key;
    }

    /// <summary>
    /// Resolves a user-supplied input (canonical key OR alphanumeric alias) to the canonical key.
    /// Returns null if the input matches neither a registered key nor a known alias.
    /// </summary>
    public static string ResolveKey(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;
        if (_defaultFloats.ContainsKey(input) || _defaultInts.ContainsKey(input) || _defaultStrings.ContainsKey(input))
            return input;
        if (_keyByAlias.TryGetValue(input, out var key))
            return key;
        return null;
    }

    /// <summary>Returns the alias for a registered key, or empty string if none.</summary>
    public static string GetAlias(string key)
        => _aliasByKey.TryGetValue(key, out var a) ? a : "";

    /// <summary>Total count of registered tunables (across all types).</summary>
    public static int RegisteredCount
        => _defaultFloats.Count + _defaultInts.Count + _defaultStrings.Count;

    /// <summary>
    /// The currently parked key (resolved canonical form), or null if none.
    /// Setting accepts either a canonical key or an alias; throws if neither resolves.
    /// Set to null to unpark.
    /// </summary>
    public static string ParkedKey
    {
        get => _parkedKey;
        set
        {
            if (value == null) { _parkedKey = null; return; }
            string resolved = ResolveKey(value);
            if (resolved == null)
                throw new ArgumentException($"Cannot park: unknown key '{value}'");
            _parkedKey = resolved;
        }
    }

    /// <summary>
    /// Adjusts a numeric (float or int) tunable by a delta. Returns the type set
    /// ("float"/"int") or null if the key is missing or non-numeric.
    /// Used by <c>/dev nudge</c>.
    /// </summary>
    public static string Nudge(string key, string deltaRaw)
    {
        if (_defaultFloats.ContainsKey(key) && float.TryParse(deltaRaw, out float fDelta))
        {
            SetFloat(key, GetFloat(key) + fDelta);
            return "float";
        }
        if (_defaultInts.ContainsKey(key) && int.TryParse(deltaRaw, out int iDelta))
        {
            SetInt(key, GetInt(key) + iDelta);
            return "int";
        }
        return null;
    }

    // ── Lifecycle ────────────────────────────────────────────

    /// <summary>
    /// Called from <see cref="WorldShapingWandsMod.Load"/> inside <c>#if DEBUG</c>.
    /// Registers all tunables then loads persisted values from disk.
    /// </summary>
    public static void Initialize()
    {
        DevTunableDefaults.RegisterAll();
        LoadFromFile();
    }

    /// <summary>
    /// Called from <see cref="WorldShapingWandsMod.Unload"/> inside <c>#if DEBUG</c>.
    /// Clears all dictionaries to prevent stale data on mod reload.
    /// </summary>
    public static void Unload()
    {
        _floats.Clear();
        _ints.Clear();
        _strings.Clear();
        _defaultFloats.Clear();
        _defaultInts.Clear();
        _defaultStrings.Clear();
        _descriptions.Clear();
        _floatRanges.Clear();
        _intRanges.Clear();
        _aliasByKey.Clear();
        _keyByAlias.Clear();
        _nextAliasIndex = 1;
        _parkedKey = null;
    }

    // ================================================================
    //  Color Parsing Utility
    // ================================================================

    /// <summary>
    /// Parses a "R,G,B" string (e.g., "180,200,255") into a <see cref="Microsoft.Xna.Framework.Color"/>.
    /// Returns the fallback color if parsing fails.
    /// Useful for consuming <c>DevTunable.RegisterString()</c> color tunables at call sites.
    /// </summary>
    /// <example>
    /// <code>
    /// Color hint = DevTunable.ParseColor(DevTunableDefaults.Color_MsgHint(), new Color(180, 200, 255));
    /// </code>
    /// </example>
    public static Microsoft.Xna.Framework.Color ParseColor(string rgb, Microsoft.Xna.Framework.Color fallback)
    {
        if (string.IsNullOrWhiteSpace(rgb))
            return fallback;

        string[] parts = rgb.Split(',');
        if (parts.Length < 3)
            return fallback;

        if (byte.TryParse(parts[0].Trim(), out byte r) &&
            byte.TryParse(parts[1].Trim(), out byte g) &&
            byte.TryParse(parts[2].Trim(), out byte b))
        {
            return new Microsoft.Xna.Framework.Color(r, g, b);
        }

        return fallback;
    }
}
#endif
