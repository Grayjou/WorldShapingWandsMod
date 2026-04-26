using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Centralized vanilla Terraria paint palette.
/// Indices 0..30 match the in-game <c>PaintID</c> enum (0 = none, 1..30 = colored paints).
/// Extracted from <c>CoatingSettingsPanel</c> in Session 2026-04-20 S1 so the same RGB
/// values can be reused by other UI elements (preview swatches, debug overlays, future
/// paint-related wands) without duplicating magic numbers.
/// </summary>
/// <remarks>
/// Kept in its own file rather than appended to <see cref="WandColors"/> to avoid bloating
/// that already-large palette class. Extend here when adding the Deep / Negative / Shadow
/// variants from future Terraria updates.
/// </remarks>
public static class PaintPalette
{
    /// <summary>Number of palette entries including the transparent "none" slot at index 0.</summary>
    public const int Count = 31;

    /// <summary>Highest valid colored paint index (inclusive).</summary>
    public const int MaxColoredIndex = 30;

    /// <summary>
    /// Vanilla paint RGB values indexed by <c>PaintID</c> (0..30).
    /// Index 0 = <see cref="Color.Transparent"/> (no paint).
    /// </summary>
    public static readonly Color[] Colors = new Color[]
    {
        Color.Transparent,           // 0  None
        new Color(195, 39, 39),      // 1  Red
        new Color(219, 118, 33),     // 2  Orange
        new Color(228, 210, 21),     // 3  Yellow
        new Color(100, 196, 72),     // 4  Lime
        new Color(53, 165, 38),      // 5  Green
        new Color(0, 142, 124),      // 6  Teal
        new Color(0, 165, 209),      // 7  Cyan
        new Color(0, 118, 209),      // 8  Sky Blue
        new Color(0, 56, 221),       // 9  Blue
        new Color(147, 0, 209),      // 10 Purple
        new Color(102, 0, 209),      // 11 Violet
        new Color(209, 0, 142),      // 12 Pink
        new Color(124, 0, 0),        // 13 Deep Red
        new Color(140, 60, 0),       // 14 Deep Orange
        new Color(140, 128, 0),      // 15 Deep Yellow
        new Color(34, 100, 0),       // 16 Deep Lime
        new Color(0, 80, 0),         // 17 Deep Green
        new Color(0, 68, 60),        // 18 Deep Teal
        new Color(0, 68, 110),       // 19 Deep Cyan
        new Color(0, 40, 124),       // 20 Deep Sky Blue
        new Color(0, 0, 110),        // 21 Deep Blue
        new Color(70, 0, 124),       // 22 Deep Purple
        new Color(56, 0, 110),       // 23 Deep Violet
        new Color(110, 0, 68),       // 24 Deep Pink
        new Color(20, 20, 20),       // 25 Black
        new Color(252, 252, 252),    // 26 White
        new Color(127, 127, 127),    // 27 Gray
        new Color(151, 107, 75),     // 28 Brown
        new Color(0, 0, 0),          // 29 Shadow
        new Color(230, 0, 255),      // 30 Negative
    };

    /// <summary>Display names matching <see cref="Colors"/>. Index 0 = "None".</summary>
    public static readonly string[] Names = new string[]
    {
        "None",
        "Red", "Orange", "Yellow", "Lime", "Green",
        "Teal", "Cyan", "Sky Blue", "Blue", "Purple", "Violet", "Pink",
        "Deep Red", "Deep Orange", "Deep Yellow", "Deep Lime", "Deep Green",
        "Deep Teal", "Deep Cyan", "Deep Sky Blue", "Deep Blue",
        "Deep Purple", "Deep Violet", "Deep Pink",
        "Black", "White", "Gray", "Brown", "Shadow", "Negative",
    };

    /// <summary>
    /// Safe accessor — returns the color at <paramref name="paintIndex"/> or
    /// <see cref="Color.Transparent"/> when out of range.
    /// </summary>
    public static Color GetColor(int paintIndex)
        => (paintIndex >= 0 && paintIndex < Colors.Length) ? Colors[paintIndex] : Color.Transparent;

    /// <summary>
    /// Safe accessor — returns the display name at <paramref name="paintIndex"/> or
    /// <c>"None"</c> when out of range.
    /// </summary>
    public static string GetName(int paintIndex)
        => (paintIndex >= 0 && paintIndex < Names.Length) ? Names[paintIndex] : "None";
}
