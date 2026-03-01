using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Consolidated settings for wand configuration and shape generation.
/// </summary>
public class WandSettings
{
    // Shape Configuration
    public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;
    public ShapeMode ShapeMode { get; set; } = ShapeMode.Filled;
    public int Thickness { get; set; } = 1;

    // Bias/Anchoring
    public HorizontalBias HorizontalBias { get; set; } = HorizontalBias.None;
    public VerticalBias VerticalBias { get; set; } = VerticalBias.None;

    // Shape-specific settings
    public bool VerticalFirst { get; set; } = false;

    // Display settings
    public PreviewMode PreviewMode { get; set; } = PreviewMode.Default;
    public bool ShowDimensions { get; set; } = true;

    /// <summary>
    /// The effective thickness passed to outline/hollow generation.
    /// Returns 0 when not in outline or hollow mode (irrelevant but safe).
    /// </summary>
    public int EffectiveThickness => (ShapeMode == ShapeMode.Hollow) ? Thickness : 0;

    public ShapeContext ToShapeContext(Point start, Point end)
    {
        return new ShapeContext(start, end, ShapeMode, EffectiveThickness,
            HorizontalBias, VerticalBias, VerticalFirst);
    }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public WandSettings Clone()
    {
        return new WandSettings
        {
            ShapeType = ShapeType,
            ShapeMode = ShapeMode,
            Thickness = Thickness,
            HorizontalBias = HorizontalBias,
            VerticalBias = VerticalBias,
            VerticalFirst = VerticalFirst,
            PreviewMode = PreviewMode,
            ShowDimensions = ShowDimensions
        };
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        ShapeType = ShapeType.Rectangle;
        ShapeMode = ShapeMode.Filled;
        Thickness = 1;
        HorizontalBias = HorizontalBias.None;
        VerticalBias = VerticalBias.None;
        VerticalFirst = false;
        PreviewMode = PreviewMode.Default;
        ShowDimensions = true;
    }

    /// <summary>
    /// Returns a human-readable description of the current settings.
    /// </summary>
    public string GetDescription()
    {
        return ShapeMode switch
        {
            ShapeMode.Filled => $"{ShapeType} - Filled",
            ShapeMode.Hollow => Thickness switch
            {
                0 => $"{ShapeType} - Hollow (Slim)",
                1 => $"{ShapeType} - Hollow (Standard)",
                _ => $"{ShapeType} - Hollow ({Thickness})"
            },
            _ => $"{ShapeType} - Unknown"
        };
    }

    /// <summary>
    /// Clamps values to valid ranges. Does NOT reset thickness on mode change.
    /// </summary>
    public void Validate()
    {
        Thickness = (int)MathHelper.Clamp(Thickness, 0, 50);
    }

    /// <summary>
    /// Determines if the preview should be shown based on the current mode and item context.
    /// </summary>
    /// <param name="isHoldingWand">Whether the player is currently holding a wand item.</param>
    /// <returns>True if the preview should be displayed.</returns>
    public bool ShouldShowPreview(bool isHoldingWand)
    {
        return PreviewMode switch
        {
            PreviewMode.Forced => true,
            PreviewMode.Default => isHoldingWand,
            _ => false
        };
    }

    #region Static Presets

    /// <summary>
    /// Default settings for new users.
    /// </summary>
    public static WandSettings Default => new WandSettings();

    /// <summary>
    /// Settings optimized for building large structures.
    /// </summary>
    public static WandSettings BuilderPreset => new WandSettings
    {
        ShapeType = ShapeType.Rectangle,
        ShapeMode = ShapeMode.Filled,
        PreviewMode = PreviewMode.Forced,
        ShowDimensions = true
    };

    /// <summary>
    /// Settings optimized for precise editing.
    /// </summary>
    public static WandSettings PrecisionPreset => new WandSettings
    {
        ShapeType = ShapeType.Rectangle,
        ShapeMode = ShapeMode.Hollow,
        Thickness = 1,
        PreviewMode = PreviewMode.Forced,
        ShowDimensions = true
    };

    /// <summary>
    /// Settings for creating outlines.
    /// </summary>
    public static WandSettings OutlinePreset => new WandSettings
    {
        ShapeType = ShapeType.Rectangle,
        ShapeMode = ShapeMode.Hollow,
        Thickness = 2,
        PreviewMode = PreviewMode.Forced,
        ShowDimensions = true
    };

    #endregion
}