using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

/// <summary>
/// Settings for the Wand of Fluids.
/// Stores which liquid type to operate with, which fill mode to use,
/// whether to fill or drain, and the shape/selection configuration.
/// </summary>
public class WandOfFluidsSettings
{
    /// <summary>The type of liquid to place (Water, Lava, Honey, Shimmer).</summary>
    public LiquidTypeSelection LiquidType { get; set; } = LiquidTypeSelection.Water;

    /// <summary>
    /// When true, the wand places Bubble Blocks (TileID.Bubble) instead of liquids.
    /// Bubble mode is exclusive: you place bubbles OR liquids, not both.
    /// Only works with FullLiquid fill mode — other modes silently default to FullLiquid.
    /// </summary>
    public bool PlaceBubble { get; set; }

    /// <summary>The fill algorithm to use (FullLiquid, RainFill, PocketFill).</summary>
    public FluidFillMode FillMode { get; set; } = FluidFillMode.FullLiquid;

    /// <summary>Whether to fill or drain liquid.</summary>
    public FluidOperation Operation { get; set; } = FluidOperation.Fill;

    /// <summary>
    /// When true and Operation is Drain, only drains the liquid type
    /// selected by <see cref="LiquidType"/>. When false, drains all liquids.
    /// </summary>
    public bool SelectiveDrain { get; set; }

    /// <summary>
    /// When true, an additional overlay displays bubble block positions around
    /// the liquid selection. The wand places Bubble tiles as a containment shell
    /// before filling with liquid. Default off.
    /// </summary>
    public bool CoatInBubble { get; set; }

    /// <summary>
    /// When true and FillMode is FullLiquid, tiles that already contain a different
    /// liquid are converted into the vanilla mixing result tile instead of being overwritten:
    ///   honey + water → Honey Block,
    ///   lava  + water → Obsidian,
    ///   lava  + honey → Crispy Honey Block,
    ///   shimmer + any → Aetherium Block.
    /// Default off. Incompatible with non-FullLiquid fill modes and Drain.
    /// </summary>
    public bool MixLiquids { get; set; }

    /// <summary>
    /// When true and FillMode is FullLiquid (and MixLiquids is OFF), the wand
    /// destructively replaces existing different-typed liquid in target tiles
    /// (e.g. pouring water onto lava clears the lava and fills with water).
    /// Default off — see S5 2026-04-23 (W-2) ship for the bug-fix-plus-toggle
    /// rationale. Mutually exclusive with MixLiquids in semantics; when both
    /// are set MixLiquids wins (the dispatch in WandOfFluidsBase.cs only enters
    /// ExecuteFullLiquid when MixLiquids is OFF, so this flag is only consulted
    /// inside ExecuteFullLiquid).
    /// </summary>
    public bool OverwriteLiquids { get; set; }

    /// <summary>Shape and selection parameters (shape type, slice mode, etc.).</summary>
    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    /// <summary>Start point of the current selection.</summary>
    public Point StartPoint { get; set; }

    /// <summary>End point of the current selection.</summary>
    public Point EndPoint { get; set; }

    /// <summary>Creates a shallow copy of these settings.</summary>
    public WandOfFluidsSettings Clone()
    {
        return new WandOfFluidsSettings
        {
            LiquidType = LiquidType,
            PlaceBubble = PlaceBubble,
            FillMode = FillMode,
            Operation = Operation,
            SelectiveDrain = SelectiveDrain,
            CoatInBubble = CoatInBubble,
            MixLiquids = MixLiquids,
            OverwriteLiquids = OverwriteLiquids,
            Shape = Shape,
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    /// <summary>Resets all settings to their defaults.</summary>
    public void ResetToDefaults()
    {
        LiquidType = LiquidTypeSelection.Water;
        PlaceBubble = false;
        FillMode = FluidFillMode.RainFill;
        Operation = FluidOperation.Fill;
        SelectiveDrain = false;
        CoatInBubble = false;
        MixLiquids = false;
        OverwriteLiquids = false;
        Shape = ShapeInfo.Default;
        StartPoint = Point.Zero;
        EndPoint = Point.Zero;
    }

    /// <summary>Returns a user-facing summary of the current settings.</summary>
    public string GetDescription()
    {
        string placeType = PlaceBubble ? "Bubble" : LiquidType.ToString();
        string op = Operation == FluidOperation.Drain ? "Drain" : FillMode.ToString();
        string extras = "";
        if (CoatInBubble) extras += " +Coat";
        if (MixLiquids) extras += " +Mix";
        return $"{placeType} / {op} / {Shape.Shape}{extras}";
    }
}
