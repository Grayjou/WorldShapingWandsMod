using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Torches.
/// Stores tiling style, spacing, biome torch toggle, overwrite toggle,
/// echo coat tri-state, operation mode, reference mode, flip tiling,
/// and the shape/selection configuration.
/// </summary>
public class WandTorchSettings
{
    /// <summary>Horizontal spacing between torches (>=1).</summary>
    public int SpacingX { get; set; } = 5;

    /// <summary>Vertical spacing between torches (>=1).</summary>
    public int SpacingY { get; set; } = 5;

    /// <summary>Tiling pattern: Manhattan (diamond lattice) or Grid (rectangular).</summary>
    public TilingStyle TilingStyle { get; set; } = TilingStyle.Manhattan;

    /// <summary>
    /// When true, replaces regular torches with biome-appropriate torches.
    /// Requires Torch God's Favor (unless overridden by server config).
    /// </summary>
    public bool BiomeTorch { get; set; }

    /// <summary>
    /// When true, existing torches in the selection can be replaced.
    /// When false, existing torch positions are skipped.
    /// </summary>
    public bool OverwriteTorches { get; set; }

    /// <summary>
    /// Echo Coat tri-state for torch placement.
    /// <list type="bullet">
    /// <item><term>Apply</term><description>Always place/replace with Echo-coated torches (invisible, emits light).</description></item>
    /// <item><term>Remove</term><description>Always place/replace without Echo coating (visible torch).</description></item>
    /// <item><term>Ignore</term><description>New torches have no coating; replaced torches keep their existing Echo state.</description></item>
    /// </list>
    /// </summary>
    public TriStateValue EchoCoat { get; set; } = TriStateValue.Ignore;

    /// <summary>
    /// Operation mode for the torch wand.
    /// Place: lay new torches. Replace: swap existing. Remove: delete existing. Convert: biome-convert existing.
    /// </summary>
    public TorchMode Mode { get; set; } = TorchMode.Place;

    /// <summary>
    /// Reference point mode for tiling seed selection.
    /// FirstValidTile: scan for first valid position (current behavior).
    /// BboxTopLeft/TopRight/BottomLeft/BottomRight: use a corner of bounding box.
    /// FirstBboxClick: use the initial click point.
    /// MousePosition: use cursor position at execution time.
    /// </summary>
    public TorchReferenceMode ReferenceMode { get; set; } = TorchReferenceMode.FirstValidTile;

    /// <summary>
    /// When true, uses the flipped Manhattan tiling pattern (diagonally upwards).
    /// When false, uses the normal Manhattan tiling pattern (diagonally downwards).
    /// Only affects Manhattan tiling style; Grid is symmetric and unaffected.
    /// </summary>
    public bool FlipTiling { get; set; }

    /// <summary>
    /// When true, after the <see cref="ReferenceMode"/> seed is computed, the wand
    /// scans the selection bounds for any existing torch and snaps the seed onto it,
    /// so newly-placed torches stay aligned with what's already there.
    /// When false, the seed from <see cref="ReferenceMode"/> is used as-is
    /// (the magenta cell in the preview), ignoring nearby existing torches.
    /// Default: true.
    /// </summary>
    public bool AlignToExistingTorches { get; set; } = true;

    /// <summary>
    /// chosen source-item type for torch placement (InventoryView v1 framework).
    /// When non-null, the wand prefers the user's explicit choice over the
    /// hotbar-scan fallback. Cleared (null) by default.
    /// </summary>
    public int? ChosenTorchItemType { get; set; }

    /// <summary>Shape and selection parameters (shape type, slice mode, etc.).</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>Start point of the current selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>End point of the current selection.</summary>
    public Point EndPoint { get; set; }

    /// <summary>Creates a shallow copy of these settings.</summary>
    public WandTorchSettings Clone()
    {
        return new WandTorchSettings
        {
            SpacingX = SpacingX,
            SpacingY = SpacingY,
            TilingStyle = TilingStyle,
            BiomeTorch = BiomeTorch,
            OverwriteTorches = OverwriteTorches,
            EchoCoat = EchoCoat,
            Mode = Mode,
            ReferenceMode = ReferenceMode,
            FlipTiling = FlipTiling,
            AlignToExistingTorches = AlignToExistingTorches,
            ChosenTorchItemType = ChosenTorchItemType,
            Shape = Shape,
            StartPoint = StartPoint,
            EndPoint = EndPoint,
        };
    }

    /// <summary>Resets all settings to their defaults.</summary>
    public void ResetToDefaults()
    {
        SpacingX = 5;
        SpacingY = 5;
        TilingStyle = TilingStyle.Manhattan;
        BiomeTorch = false;
        OverwriteTorches = false;
        EchoCoat = TriStateValue.Ignore;
        Mode = TorchMode.Place;
        ReferenceMode = TorchReferenceMode.FirstValidTile;
        FlipTiling = false;
        AlignToExistingTorches = true;
        ChosenTorchItemType = null;
        Shape = ShapeInfo.Default;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>Returns a user-facing summary of the current settings.</summary>
    public string GetDescription()
    {
        string desc = $"{TilingStyle} ({SpacingX}x{SpacingY})";
        if (Mode != TorchMode.Place) desc += $" [{Mode}]";
        if (ReferenceMode != TorchReferenceMode.FirstValidTile) desc += $" [{ReferenceMode}]";
        if (FlipTiling) desc += " [Flipped]";
        if (!AlignToExistingTorches) desc += " [No Snap]";
        if (BiomeTorch) desc += " [Biome]";
        if (OverwriteTorches) desc += " [Overwrite]";
        if (EchoCoat == TriStateValue.Apply) desc += " [Echo On]";
        else if (EchoCoat == TriStateValue.Remove) desc += " [Echo Off]";
        return desc;
    }
}