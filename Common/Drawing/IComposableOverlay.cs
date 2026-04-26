using Microsoft.Xna.Framework.Graphics;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Interface for composable overlay layers managed by <see cref="OverlayManager"/>.
/// Each overlay handles its own drawing within the coordinated pipeline.
/// </summary>
public interface IComposableOverlay
{
    /// <summary>
    /// Called once when the overlay is first registered with the manager.
    /// Use for one-time setup that requires the manager reference.
    /// </summary>
    void Initialize(OverlayManager manager);

    /// <summary>
    /// Called immediately after <see cref="Initialize"/> during registration.
    /// Use for activation logic that should happen when the overlay becomes part of the pipeline.
    /// </summary>
    void OnRegister();

    /// <summary>
    /// Called when the overlay is removed from the manager.
    /// Use for cleanup and resource disposal.
    /// </summary>
    void OnUnregister();

    /// <summary>
    /// Called every frame before <see cref="Draw"/>. Update cached state,
    /// check for invalidation, etc.
    /// </summary>
    void Update(OverlayContext context);

    /// <summary>
    /// Called every frame to render the overlay. The sprite batch is already
    /// begun by the manager; overlays that need a different batch state
    /// should End/Begin their own.
    /// </summary>
    void Draw(SpriteBatch spriteBatch, OverlayContext context);

    /// <summary>
    /// Controls rendering order. Lower values draw first (behind).
    /// Selection overlay = 0, Safekeeping = 50, Tiling preview = 100.
    /// </summary>
    int ZOrder { get; }

    /// <summary>
    /// When false, <see cref="Update"/> and <see cref="Draw"/> are skipped.
    /// Can be toggled by config or UI.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Hint for the manager: if false, the overlay's cached output is still valid
    /// and a full redraw can be skipped. Overlays set this in <see cref="Update"/>.
    /// </summary>
    bool NeedsRedraw { get; }
}
