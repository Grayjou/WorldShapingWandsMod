using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Players;

/// <summary>
/// Per-player state for the Wand of Molding system.
/// Holds an independent canvas, tile selection, and the output <see cref="CustomShape"/>.
/// </summary>
/// <remarks>
/// <para>
/// The Molding wand's canvas/selection system is completely independent from the
/// Delimitation wand's (<see cref="DelimitationWandPlayer"/>). This means the user
/// can maintain active selections on both wands simultaneously without interference.
/// </para>
/// <para>
/// The key difference from Delimitation: the Molding wand's "execute" path
/// (<see cref="PromoteMoldToCustomShape"/>) creates a <see cref="CustomShape"/> that is
/// immediately available to all Stamp wands via <see cref="MoldedShape"/>. The user's
/// workflow is: build a shape → it becomes the custom shape → stamp it with any wand.
/// No separate "Promote" step is needed — the selection IS the mold.
/// </para>
/// </remarks>
public class MoldingWandPlayer : ModPlayer
{
    /// <summary>
    /// The canvas defines the boundary region that constrains all mold selection operations.
    /// </summary>
    public SelectionCanvas Canvas { get; private set; } = new();

    /// <summary>
    /// The tile selection — a subset of the canvas representing the mold being sculpted.
    /// </summary>
    public TileSelection Selection { get; private set; } = new();

    /// <summary>
    /// The output custom shape — created from the mold selection. When non-null,
    /// Stamp wands can use this shape instead of parametric shapes.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="DelimitationWandPlayer.ActiveCustomShape"/> which requires an
    /// explicit "Promote" step, this property is automatically updated whenever the
    /// mold selection changes (if <see cref="AutoPromote"/> is enabled) or when the user
    /// manually promotes via the UI.
    /// </remarks>
    public CustomShape MoldedShape { get; set; }

    /// <summary>Per-player Molding Wand settings (operation, mode, visual preferences).</summary>
    public MoldingWandSettings Settings { get; private set; } = new();

    /// <summary>
    /// When <c>true</c>, the mold selection is automatically promoted to a
    /// <see cref="CustomShape"/> after every execute operation. This gives the
    /// user's desired "immediate availability" workflow.
    /// </summary>
    public bool AutoPromote { get; set; } = true;

    /// <summary>
    /// Promotes the current mold <see cref="Selection"/> to a <see cref="CustomShape"/>.
    /// The selection is NOT cleared — the user can continue adding/removing from the mold.
    /// </summary>
    /// <returns><c>true</c> if a custom shape was successfully created.</returns>
    public bool PromoteMoldToCustomShape()
    {
        var shape = CustomShape.FromSelection(Selection);
        if (shape == null)
            return false;

        MoldedShape = shape;

        // Also make it available as the "active custom shape" on the Delimitation player,
        // so existing Stamp wand infrastructure can access it without modification.
        var delimPlayer = Player.GetModPlayer<DelimitationWandPlayer>();
        delimPlayer.ActiveCustomShape = shape;

        return true;
    }

    /// <summary>
    /// Clears the molded shape output.
    /// </summary>
    public void ClearMoldedShape()
    {
        MoldedShape = null;
    }

    /// <summary>
    /// Clears all Molding Wand state: canvas, selection, and molded shape.
    /// </summary>
    public void ClearAll()
    {
        Canvas.Clear();
        Selection.Clear();
        MoldedShape = null;
    }

    public override void OnRespawn()
    {
        // Keep canvas and molded shape on respawn — only clear the active selection
        Selection.Clear();
    }

    public override void OnEnterWorld()
    {
        ClearAll();
        Settings.ResetToDefaults();
        LastHintTick = 0;
    }

    /// <summary>
    /// Game tick at which the last molding contextual hint was shown.
    /// Used by <c>WandOfMoldingBase.ShowMoldingHint</c> for rate-limiting.
    /// Reset on world entry so hints are fresh each session.
    /// </summary>
    internal int LastHintTick { get; set; }
}
