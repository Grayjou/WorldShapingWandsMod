using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Per-player settings for the Select Wand.
/// Controls which operation is applied to the selection or canvas,
/// the active editing mode, and visual preferences.
/// </summary>
public class DelimitationWandSettings
{
    /// <summary>
    /// The boolean operation applied to the <see cref="Selection.TileSelection"/>
    /// when in <see cref="DelimitationWandMode.Selection"/> mode.
    /// </summary>
    public SelectionOperation Operation { get; set; } = SelectionOperation.Add;

    /// <summary>
    /// The active editing mode — determines whether shape operations
    /// modify the selection or the canvas.
    /// </summary>
    public DelimitationWandMode Mode { get; set; } = DelimitationWandMode.Selection;

    /// <summary>
    /// When <c>true</c> and no canvas exists, the first shape operation in
    /// Selection mode automatically creates a canvas from the shape.
    /// Reduces friction for the common workflow.
    /// </summary>
    public bool AutoCreateCanvas { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the "Promote → Custom Shape" action clears the
    /// selection after capturing it. Prevents accidental re-promotion.
    /// </summary>
    public bool ClearSelectionOnPromote { get; set; } = true;

    /// <summary>The shape configuration (shape type, fill mode, outline thickness).</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    // ── Three-Layer Overlay Colors (all configurable) ─────────────────
    // Layer 1 (bottom): Outside — darkens area outside the canvas
    // Layer 2 (middle): Canvas — shows the canvas working area
    // Layer 3 (top):    TileSelection — highlights selected tiles within the canvas

    /// <summary>
    /// Layer 1: Semi-transparent fill drawn over tiles OUTSIDE the canvas.
    /// Creates a "spotlight" dimming effect so the canvas area stands out.
    /// </summary>
    public Color OutsideColor { get; set; } = new Color(0, 0, 0, 51);           // Black, 20% alpha

    /// <summary>
    /// Layer 2: Fill drawn over canvas tiles (the working area).
    /// Visible in Selection mode to show the constraint region.
    /// </summary>
    public Color CanvasColor { get; set; } = new Color(255, 255, 255, 102);     // White, 40% alpha

    /// <summary>
    /// Layer 3: Fill drawn over selected tiles within the canvas.
    /// </summary>
    public Color TileSelectionColor { get; set; } = new Color(128, 128, 0, 102); // Olive, 40% alpha

    /// <summary>Canvas border color — gold edge segments around the canvas boundary.</summary>
    public static readonly Color CanvasBorderColor = new Color(255, 215, 0);

    /// <summary>
    /// Accent RGB for Canvas Edit mode fill. Alpha is NOT embedded here — the
    /// overlay applies the user's <c>CanvasFillAlpha</c> config slider via
    /// <c>Color * alpha</c> (premultiplied blend). This replaces the old
    /// <c>CanvasEditFillColor</c> which embedded alpha=40 and bypassed the slider.
    /// </summary>
    public static readonly Color CanvasEditAccentColor = new Color(255, 215, 0, 255);

    /// <summary>Canvas border thickness in pixels.</summary>
    public const int CanvasBorderThickness = 2;

    /// <summary>
    /// Resets all settings to their defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        Operation = SelectionOperation.Add;
        Mode = DelimitationWandMode.Selection;
        AutoCreateCanvas = true;
        ClearSelectionOnPromote = true;
        Shape = ShapeInfo.Default;
    }
}
