using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Utilities;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player wand state.
/// </summary>
public class WandPlayer : ModPlayer
{
    public SelectionState Selection { get; private set; } = SelectionState.Empty;
    
    // Per-wand settings
    public WandOfBuildingSettings BuildingSettings { get; private set; } = new();
    public WandOfDestructionSettings DestructionSettings { get; private set; } = new();
    public WandOfReplacementSettings ReplacementSettings { get; private set; } = new();
    public WandOfWiringSettings WiringSettings { get; private set; } = new();

    // Keep global settings for backward compatibility with test commands
    public WandSettings Settings { get; private set; } = new WandSettings();

    public void StartSelection(Point start, bool verticalFirst)
    {
        Settings.VerticalFirst = verticalFirst;
        Selection = SelectionState.Create(start, start, verticalFirst);
    }

    public void UpdateSelection(Point end, bool wasClamped = false)
    {
        if (Selection.IsActive && !Selection.IsLocked)  // Check lock before updating
            Selection = Selection.WithEnd(end, wasClamped);
    }

    // NEW: Lock the selection to prevent endpoint updates
    public void LockSelection()
    {
        if (Selection.IsActive)
            Selection = Selection.WithLocked(true);
    }

    // NEW: Unlock if needed (e.g., user wants to adjust)
    public void UnlockSelection()
    {
        if (Selection.IsActive)
            Selection = Selection.WithLocked(false);
    }

    public SelectionState CompleteSelection()
    {
        var completed = Selection;
        Selection = SelectionState.Empty;
        return completed;
    }

    public void ClearSelection() => Selection = SelectionState.Empty;

    /// <summary>
    /// Builds a ShapeContext from the current selection + settings.
    /// </summary>
    public ShapeContext GetCurrentShapeContext()
    {
        return Settings.ToShapeContext(Selection.StartTile, Selection.EndTile);
    }

    public override void PostUpdate()
    {
        // Update selection preview in real-time when selection is active AND not locked
        if (Selection.IsActive && !Selection.IsLocked && IsHoldingWandItem())
        {
            Point mouseTile = GeometryHelper.WorldToTile(Main.MouseWorld);
            UpdateSelection(mouseTile);
        }
    }

    private bool IsHoldingWandItem()
    {
        return Player.HeldItem?.ModItem is WandOfDestructionBase
            || Player.HeldItem?.ModItem is WandOfBuildingBase
            || Player.HeldItem?.ModItem is WandOfReplacementBase
            || Player.HeldItem?.ModItem is WandOfWiringBase
            // Add other wand types here as you implement them
            // || Player.HeldItem?.ModItem is WandOfDesigner
            ;
    }

    public override void OnRespawn() => ClearSelection();
    
    public override void OnEnterWorld()
    {
        ClearSelection();
        Settings.ResetToDefaults();
        BuildingSettings.ResetToDefaults();
        DestructionSettings.ResetToDefaults();
        ReplacementSettings.ResetToDefaults();
        WiringSettings.ResetToDefaults();
    }
}