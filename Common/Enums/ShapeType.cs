namespace WorldShapingWandsMod.Common.Enums;

/// <summary>
/// Defines the geometric shape to be drawn or filled.
/// Half-shape variants (e.g. half-ellipse, half-diamond) are produced
/// by combining a base shape with <see cref="SliceMode"/>, not via
/// separate enum values.
/// </summary>
public enum ShapeType : byte
{
    Rectangle = 0,
    Ellipse = 1,
    Diamond = 2,
    Triangle = 3,
    Elbow = 4,
    CardinalLine = 5,
    StraightLine = 6,

    /// <summary>
    /// User-defined custom shape captured from the Wand of Molding.
    /// When selected, Stamp wands use the mold's tile set instead of
    /// a parametric shape. The mold shape ignores ShapeMode/Thickness/Slice
    /// — it is always exactly the captured tile pattern.
    /// </summary>
    Mold = 7,

    /// <summary>
    /// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0/§7)
    /// Magic Wand — Read variant. Stencil-wands only. Runs the configured
    /// flood-fill match (object-type taxonomy + contiguity) at the click
    /// point against the active stencil canvas; result is written to the
    /// canvas (Stencil Mode aware) AND captured to
    /// <c>WandPlayer.LastMagicWandShape</c> for later Apply replay.
    /// Right-click on the shape cell opens the Read configuration SubUI
    /// (SampleMode + Contiguity sections — UI scheduled for S11+ once art
    /// + multi-section PersistentSubUI primitive land).
    /// </summary>
    MagicWandRead = 8,

    /// <summary>
    /// (S10 2026-04-29; <c>StencilMagicWandSelectionPlan.md</c> §0/§7)
    /// Magic Wand — Apply variant. Available on every wand. Bare shape:
    /// no parameter row, no SubUI, no configuration. At click time it
    /// stamps <c>WandPlayer.LastMagicWandShape</c> at the cursor (Mold-
    /// style translation by stored origin) and runs the wand's per-tile
    /// action over the stamped set. Empty storage → no-op + chat warning
    /// (*"Magic Wand: no captured shape. Use Magic Wand Read on a stencil
    /// wand first."*). Strict sibling of <see cref="Mold"/> — both read
    /// from a player-scoped tile set and stamp at the cursor.
    /// </summary>
    MagicWandApply = 9,
}