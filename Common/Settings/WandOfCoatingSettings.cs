using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Coating.
/// Stores which coating operation to apply (paint tile, paint wall, scrape, or scrape moss),
/// which paint color to use, which coating (Illuminant/Echo) to apply, and the
/// shape/selection configuration.
/// </summary>
public class WandOfCoatingSettings
{
    /// <summary>The coating operation to perform.</summary>
    public CoatingMode Mode { get; set; } = CoatingMode.PaintTile;

    /// <summary>
    /// The paint color index (1–30 = vanilla paint colors, 0 = none/transparent).
    /// Ignored for ScrapeMoss and HarvestMoss modes.
    /// See <see cref="Terraria.ID.PaintID"/> for numeric values.
    /// </summary>
    public byte PaintColor { get; set; } = 255; // Default: Ignore (don't change existing paint)

    /// <summary>
    /// Tri-state control for Illuminant coating.
    /// Ignore = leave existing coating unchanged, Apply = force on, Remove = force off.
    /// </summary>
    public TriStateValue Illuminant { get; set; } = TriStateValue.Ignore;

    /// <summary>
    /// Tri-state control for Echo coating.
    /// Ignore = leave existing coating unchanged, Apply = force on, Remove = force off.
    /// </summary>
    public TriStateValue Echo { get; set; } = TriStateValue.Ignore;

    // ── Legacy bool accessors (consumed by WandOfCoatingBase & networking) ──

    /// <summary>Legacy: whether to apply Illuminant. Delegates to <see cref="Illuminant"/>.</summary>
    public bool ApplyIlluminant
    {
        get => Illuminant == TriStateValue.Apply;
        set { if (value) Illuminant = TriStateValue.Apply; }
    }

    /// <summary>Legacy: whether to ignore Illuminant. Delegates to <see cref="Illuminant"/>.</summary>
    public bool IgnoreIlluminant
    {
        get => Illuminant == TriStateValue.Ignore;
        set { if (value) Illuminant = TriStateValue.Ignore; }
    }

    /// <summary>Legacy: whether to apply Echo. Delegates to <see cref="Echo"/>.</summary>
    public bool ApplyEcho
    {
        get => Echo == TriStateValue.Apply;
        set { if (value) Echo = TriStateValue.Apply; }
    }

    /// <summary>Legacy: whether to ignore Echo. Delegates to <see cref="Echo"/>.</summary>
    public bool IgnoreEcho
    {
        get => Echo == TriStateValue.Ignore;
        set { if (value) Echo = TriStateValue.Ignore; }
    }

    /// <summary>
    /// When true (default), the wand will repaint tiles/walls that already have a different paint color.
    /// When false, already-painted tiles/walls are skipped — only unpainted surfaces are painted.
    /// </summary>
    public bool Repaint { get; set; } = true;

    /// <summary>The shape configuration.</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>The starting point of the selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>The ending point of the selection.</summary>
    public Point EndPoint { get; set; }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandOfCoatingSettings Clone()
    {
        return new WandOfCoatingSettings
        {
            Mode = Mode,
            PaintColor = PaintColor,
            Illuminant = Illuminant,
            Echo = Echo,
            Repaint = Repaint,
            Shape = Shape,
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        Mode = CoatingMode.PaintTile;
        PaintColor = 255; // Ignore (don't change existing paint)
        Illuminant = TriStateValue.Ignore;
        Echo = TriStateValue.Ignore;
        Repaint = true;
        Shape = ShapeInfo.Default;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        string coatingStr = "";
        if (Illuminant == TriStateValue.Apply) coatingStr += " +Illuminant";
        if (Echo == TriStateValue.Apply) coatingStr += " +Echo";
#pragma warning disable CS0618
        string modeStr = Mode switch
        {
            CoatingMode.PaintTile   => $"Paint Tile (color {PaintColor}{coatingStr})",
            CoatingMode.PaintWall   => $"Paint Wall (color {PaintColor}{coatingStr})",
            CoatingMode.ScrapePaint => "Scrape Paint (legacy)",
            CoatingMode.ScrapeMoss  => "Scrape Moss",
            CoatingMode.HarvestMoss => "Harvest Moss",
            _                       => Mode.ToString()
        };
#pragma warning restore CS0618
        return $"{modeStr} - {Shape.GetDescription()}";
    }

    /// <summary>
    /// Validates all settings values.
    /// </summary>
    public void Validate()
    {
        Shape.Validate();
        // PaintColor 0 is valid (transparent/none), 1-30 are vanilla paints,
        // 255 is IgnorePaintColor (don't change existing paint)
        if (PaintColor > 30 && PaintColor != 255)
            PaintColor = 30;
        // ScrapePaint (2) has been removed from the UI. Migrate any saved value to ScrapeMoss.
#pragma warning disable CS0618
        if (Mode == CoatingMode.ScrapePaint) Mode = CoatingMode.ScrapeMoss;
#pragma warning restore CS0618
    }
}
