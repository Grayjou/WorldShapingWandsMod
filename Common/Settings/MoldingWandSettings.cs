using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Per-player settings for the Wand of Molding.
/// Controls which boolean operation is applied, the active editing mode,
/// and shape parameters for the mold creation workflow.
/// </summary>
/// <remarks>
/// Structurally mirrors <see cref="DelimitationWandSettings"/>, but kept separate
/// so each wand system can evolve independently. The Molding wand's "execute"
/// creates a <see cref="Selection.CustomShape"/> directly from the tile selection,
/// rather than applying it as a filter to other wand operations.
/// </remarks>
public class MoldingWandSettings
{
    /// <summary>
    /// The boolean operation applied to the mold <see cref="Selection.TileSelection"/>
    /// when in <see cref="MoldingWandMode.Selection"/> mode.
    /// </summary>
    public SelectionOperation Operation { get; set; } = SelectionOperation.Add;

    /// <summary>
    /// The active editing mode — determines whether shape operations
    /// modify the mold selection or the canvas.
    /// </summary>
    public MoldingWandMode Mode { get; set; } = MoldingWandMode.Selection;

    /// <summary>
    /// When <c>true</c> and no canvas exists, the first shape operation in
    /// Selection mode automatically creates a canvas from the shape.
    /// </summary>
    public bool AutoCreateCanvas { get; set; } = true;

    /// <summary>The shape configuration (shape type, fill mode, outline thickness).</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    // ── Overlay Colors ───────────────────────────────────────────────
    // The molding wand uses a teal/cyan color theme to distinguish from
    // the gold theme of the Delimitation wand.

    /// <summary>
    /// Layer 1: Semi-transparent fill drawn over tiles OUTSIDE the canvas.
    /// </summary>
    public Color OutsideColor { get; set; } = new Color(0, 0, 0, 51);           // Black, 20% alpha

    /// <summary>
    /// Layer 2: Fill drawn over canvas tiles (the working area).
    /// </summary>
    public Color CanvasColor { get; set; } = new Color(200, 255, 255, 102);     // Pale cyan, 40% alpha

    /// <summary>
    /// Layer 3: Fill drawn over selected tiles within the canvas.
    /// </summary>
    public Color TileSelectionColor { get; set; } = new Color(0, 180, 180, 102); // Teal, 40% alpha

    /// <summary>Canvas border color — teal edge segments around the canvas boundary.</summary>
    public static readonly Color CanvasBorderColor = new Color(0, 200, 200);

    /// <summary>
    /// Accent RGB for Canvas Edit mode fill. Alpha is NOT embedded here — the
    /// overlay applies the user's <c>MoldingCanvasFillAlpha</c> config slider via
    /// <c>Color * alpha</c> (premultiplied blend). This replaces the old
    /// <c>CanvasEditFillColor</c> which embedded alpha=40 and bypassed the slider.
    /// </summary>
    public static readonly Color CanvasEditAccentColor = new Color(0, 200, 200, 255);

    /// <summary>Canvas border thickness in pixels.</summary>
    public const int CanvasBorderThickness = 2;

    /// <summary>
    /// Resets all settings to their defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        Operation = SelectionOperation.Add;
        Mode = MoldingWandMode.Selection;
        AutoCreateCanvas = true;
        Shape = ShapeInfo.Default;
    }
}
