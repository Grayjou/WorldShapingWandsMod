namespace WorldShapingWandsMod.Common.Settings;

public struct PlanificationRenderConfig
{
    public bool ShowOutline;
    public bool ShowGrid;
    public bool ShowFill;

    public static PlanificationRenderConfig Default => new()
    {
        ShowOutline = true,
        ShowGrid = false,
        ShowFill = false,
    };
}
