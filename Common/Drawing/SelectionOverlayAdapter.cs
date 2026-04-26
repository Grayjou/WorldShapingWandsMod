using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Adapter that wraps the existing <see cref="SelectionOverlay"/> as an
/// <see cref="IComposableOverlay"/> for the composable overlay pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Uses the End/Delegate/Re-Begin pattern: ends the manager's sprite batch,
/// delegates to <see cref="SelectionOverlay.DrawAll()"/> (which manages its
/// own Begin/End cycles internally), then re-begins the batch for subsequent
/// overlays in the pipeline.
/// </para>
/// <para>
/// When this adapter is registered, it sets
/// <see cref="SelectionOverlay._managedByOverlaySystem"/> to true, which causes
/// the original <see cref="SelectionOverlay.PostDrawTiles"/> to early-return.
/// This prevents double-drawing.
/// </para>
/// </remarks>
[Autoload(Side = ModSide.Client)]
internal sealed class SelectionOverlayAdapter : IComposableOverlay
{
    /// <summary>Selection overlay draws first (behind everything else).</summary>
    public int ZOrder => 0;

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <summary>Always redraw — SelectionOverlay manages its own caching.</summary>
    public bool NeedsRedraw => true;

    private OverlayManager _manager;

    public void Initialize(OverlayManager manager)
    {
        _manager = manager;
    }

    public void OnRegister()
    {
        // Tell the original SelectionOverlay to stop drawing from PostDrawTiles
        var overlay = ModContent.GetInstance<SelectionOverlay>();
        if (overlay != null)
            overlay._managedByOverlaySystem = true;
    }

    public void OnUnregister()
    {
        // Re-enable the original PostDrawTiles path
        var overlay = ModContent.GetInstance<SelectionOverlay>();
        if (overlay != null)
            overlay._managedByOverlaySystem = false;
    }

    public void Update(OverlayContext context)
    {
        // No-op: SelectionOverlay manages its own cache internally
    }

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        // End the manager's batch — SelectionOverlay manages its own Begin/End
        spriteBatch.End();

        // Delegate to the existing overlay's extracted drawing logic
        var overlay = ModContent.GetInstance<SelectionOverlay>();
        overlay?.DrawAll();

        // Re-begin for subsequent overlays in the pipeline
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null,
            Main.GameViewMatrix.TransformationMatrix);
    }
}
