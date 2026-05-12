using Microsoft.Xna.Framework;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.Settings;

public class PlanificationWandSettings
{
    public SelectionOperation Operation { get; set; } = SelectionOperation.Add;

    public bool AutoCreateCanvas { get; set; } = true;

    public DelimitationWandMode Mode { get; set; } = DelimitationWandMode.Selection;

    public ShapeInfo Shape { get; set; } = ShapeInfo.Default;

    public bool TransformModeEnabled { get; set; }

    public TransformActionMode ActiveTransformAction { get; set; } = TransformActionMode.None;

    public Point? PendingTransformMoveStart { get; set; }

    public Point? PersistentPivot { get; set; }

    public Point? TemporaryPivot { get; set; }

    public void ResetToDefaults()
    {
        Operation = SelectionOperation.Add;
        AutoCreateCanvas = true;
        Mode = DelimitationWandMode.Selection;
        Shape = ShapeInfo.Default;
        TransformModeEnabled = false;
        ActiveTransformAction = TransformActionMode.None;
        PendingTransformMoveStart = null;
        PersistentPivot = null;
        TemporaryPivot = null;
    }
}
