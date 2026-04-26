using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Adapter that wraps the existing <see cref="SafekeepingOverlay"/> as an
/// <see cref="IComposableOverlay"/> for the composable overlay pipeline.
/// </summary>
/// <remarks>
/// Same End/Delegate/Re-Begin pattern as <see cref="SelectionOverlayAdapter"/>.
/// ZOrder = 50 (draws on top of the selection overlay).
/// </remarks>
[Autoload(Side = ModSide.Client)]
internal sealed class SafekeepingOverlayAdapter : IComposableOverlay
{
    /// <summary>Safekeeping overlay draws on top of selection (ZOrder 50 &gt; 0).</summary>
    public int ZOrder => 50;

    /// <inheritdoc />
    public bool Visible { get; set; } = true;

    /// <summary>Always redraw — SafekeepingOverlay manages its own state.</summary>
    public bool NeedsRedraw => true;

    private OverlayManager _manager;

    public void Initialize(OverlayManager manager)
    {
        _manager = manager;
    }

    public void OnRegister()
    {
        var overlay = ModContent.GetInstance<SafekeepingOverlay>();
        if (overlay != null)
            overlay._managedByOverlaySystem = true;
    }

    public void OnUnregister()
    {
        var overlay = ModContent.GetInstance<SafekeepingOverlay>();
        if (overlay != null)
            overlay._managedByOverlaySystem = false;
    }

    public void Update(OverlayContext context)
    {
        // No-op: SafekeepingOverlay manages its own state
    }

    public void Draw(SpriteBatch spriteBatch, OverlayContext context)
    {
        // End the manager's batch — SafekeepingOverlay manages its own Begin/End
        spriteBatch.End();

        // Delegate to the existing overlay's extracted drawing logic
        var overlay = ModContent.GetInstance<SafekeepingOverlay>();
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
