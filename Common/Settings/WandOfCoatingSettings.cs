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
    public byte PaintColor { get; set; } = 26; // Default: White paint (PaintID.White = 26)

    /// <summary>
    /// Whether to apply Illuminant coating (makes tiles emit 100% light).
    /// Applied independently of paint and Echo — a tile can have both coatings.
    /// Only used in PaintTile/PaintWall modes.
    /// </summary>
    public bool ApplyIlluminant { get; set; } = false;

    /// <summary>
    /// When true, Illuminant coating state is left unchanged on existing tiles.
    /// Overrides ApplyIlluminant — the coating is neither applied nor removed.
    /// </summary>
    public bool IgnoreIlluminant { get; set; } = false;

    /// <summary>
    /// Whether to apply Echo coating (makes tiles invisible).
    /// Applied independently of paint and Illuminant — a tile can have both coatings.
    /// Only used in PaintTile/PaintWall modes.
    /// </summary>
    public bool ApplyEcho { get; set; } = false;

    /// <summary>
    /// When true, Echo coating state is left unchanged on existing tiles.
    /// Overrides ApplyEcho — the coating is neither applied nor removed.
    /// </summary>
    public bool IgnoreEcho { get; set; } = false;

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
            ApplyIlluminant = ApplyIlluminant,
            IgnoreIlluminant = IgnoreIlluminant,
            ApplyEcho = ApplyEcho,
            IgnoreEcho = IgnoreEcho,
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
        PaintColor = 26; // White (PaintID.White = 26)
        ApplyIlluminant = false;
        IgnoreIlluminant = false;
        ApplyEcho = false;
        IgnoreEcho = false;
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
        if (ApplyIlluminant) coatingStr += " +Illuminant";
        if (ApplyEcho) coatingStr += " +Echo";
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
        // PaintColor 0 is valid (transparent/none), clamp to 0–30
        if (PaintColor > 30) PaintColor = 30;
        // ApplyIlluminant and ApplyEcho are bools — no validation needed
        // ScrapePaint (2) has been removed from the UI. Migrate any saved value to ScrapeMoss.
#pragma warning disable CS0618
        if (Mode == CoatingMode.ScrapePaint) Mode = CoatingMode.ScrapeMoss;
#pragma warning restore CS0618
    }
}
