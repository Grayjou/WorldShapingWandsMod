using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Central manager for composable overlays. Handles registration, z-order sorting,
/// and coordinated update/draw passes.
/// </summary>
/// <remarks>
/// <para>Singleton instance — null on dedicated servers (overlays are client-only).</para>
/// <para>Step 1 = compile-passing stubs. Step 2 adds real logic.</para>
/// </remarks>
public sealed class OverlayManager
{
    // ================================================================
    //  Singleton
    // ================================================================

    private static OverlayManager _instance;

    /// <summary>
    /// Gets the singleton instance. Returns null on dedicated servers.
    /// Created by <see cref="OverlayManagerSystem.Load"/>.
    /// </summary>
    public static OverlayManager Instance => _instance;

    /// <summary>Creates the singleton. Call from <see cref="OverlayManagerSystem.Load"/>.</summary>
    internal static void Create() => _instance = new OverlayManager();

    /// <summary>Destroys the singleton. Call from <see cref="OverlayManagerSystem.Unload"/>.</summary>
    internal static void Destroy()
    {
        _instance?.UnregisterAll();
        _instance = null;
    }

    private OverlayManager() { }

    // ================================================================
    //  State
    // ================================================================

    private readonly List<IComposableOverlay> _overlays = new();
    private bool _sortDirty;

    private int _lastSelectionHash;
    private OverlayContext? _currentContext;

    // ================================================================
    //  Events
    // ================================================================

    /// <summary>Raised when a selection change is detected during UpdateAll.</summary>
    public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

    /// <summary>Raised after an overlay is registered.</summary>
    public event EventHandler<OverlayEventArgs> OverlayRegistered;

    /// <summary>Raised after an overlay is unregistered.</summary>
    public event EventHandler<OverlayEventArgs> OverlayUnregistered;

    // ================================================================
    //  Properties
    // ================================================================

    /// <summary>Number of registered overlays.</summary>
    public int Count => _overlays.Count;

    /// <summary>Read-only view of registered overlays (in z-order after sorting).</summary>
    public IReadOnlyList<IComposableOverlay> Overlays => _overlays;

    // ================================================================
    //  Registration
    // ================================================================

    /// <summary>
    /// Registers an overlay with the manager. Calls Initialize and OnRegister.
    /// </summary>
    public void Register(IComposableOverlay overlay)
    {
        if (overlay == null)
            throw new ArgumentNullException(nameof(overlay));

        if (_overlays.Contains(overlay))
            throw new InvalidOperationException(
                $"Overlay {overlay.GetType().Name} is already registered.");

        overlay.Initialize(this);
        _overlays.Add(overlay);
        overlay.OnRegister();
        _sortDirty = true;

        OverlayRegistered?.Invoke(this, new OverlayEventArgs(overlay));
    }

    /// <summary>
    /// Removes an overlay from the manager. Calls OnUnregister.
    /// </summary>
    public bool Unregister(IComposableOverlay overlay)
    {
        if (overlay == null || !_overlays.Contains(overlay))
            return false;

        overlay.OnUnregister();
        _overlays.Remove(overlay);

        OverlayUnregistered?.Invoke(this, new OverlayEventArgs(overlay));
        return true;
    }

    /// <summary>
    /// Removes all registered overlays. Called during Unload.
    /// </summary>
    public void UnregisterAll()
    {
        // Iterate in reverse so removals don't shift indices
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var overlay = _overlays[i];
            try { overlay.OnUnregister(); }
            catch (Exception ex)
            {
                WorldShapingWandsMod.Instance?.Logger.Error(
                    $"Overlay OnUnregister ({overlay.GetType().Name}): {ex}");
            }
        }

        _overlays.Clear();
        _sortDirty = false;
    }

    // ================================================================
    //  Queries
    // ================================================================

    /// <summary>
    /// Finds the first registered overlay of type T, or null if not found.
    /// </summary>
    public T Get<T>() where T : class, IComposableOverlay
    {
        foreach (var overlay in _overlays)
        {
            if (overlay is T typed)
                return typed;
        }

        return null;
    }

    /// <summary>
    /// Tries to find a registered overlay of type T.
    /// </summary>
    public bool TryGet<T>(out T overlay) where T : class, IComposableOverlay
    {
        overlay = Get<T>();
        return overlay != null;
    }

    // ================================================================
    //  Update / Draw
    // ================================================================

    /// <summary>
    /// Updates all visible overlays with the current context.
    /// Called by OverlayManagerSystem before DrawAll.
    /// </summary>
    public void UpdateAll(OverlayContext context)
    {
        _currentContext = context;
        EnsureSorted();

        foreach (var overlay in _overlays)
        {
            if (!overlay.Visible) continue;

            try
            {
                overlay.Update(context);
            }
            catch (Exception ex)
            {
                WorldShapingWandsMod.Instance?.Logger.Error(
                    $"Overlay Update ({overlay.GetType().Name}): {ex}");
            }
        }
    }

    /// <summary>
    /// Draws all visible overlays. Manages the top-level sprite batch.
    /// Called by OverlayManagerSystem.PostDrawTiles.
    /// </summary>
    /// <remarks>
    /// The manager begins the sprite batch with TransformationMatrix (matching
    /// SelectionOverlay's existing behavior). Individual overlays may End/Begin
    /// their own batch if they need different blend states.
    /// </remarks>
    public void DrawAll(SpriteBatch spriteBatch)
    {
        if (_currentContext == null) return;
        var context = _currentContext.Value;

        EnsureSorted();

        // Begin with TransformationMatrix to match existing SelectionOverlay behavior.
        // Note: The design doc incorrectly uses ZoomMatrix — we use TransformationMatrix.
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null,
            Main.GameViewMatrix.TransformationMatrix);

        try
        {
            foreach (var overlay in _overlays)
            {
                if (!overlay.Visible) continue;

                try
                {
                    overlay.Draw(spriteBatch, context);
                }
                catch (Exception ex)
                {
                    WorldShapingWandsMod.Instance?.Logger.Error(
                        $"Overlay Draw ({overlay.GetType().Name}): {ex}");
                }
            }
        }
        finally
        {
            spriteBatch.End();
        }
    }

    // ================================================================
    //  Visibility / Invalidation
    // ================================================================

    /// <summary>
    /// Sets Visible on all registered overlays.
    /// </summary>
    public void SetAllVisible(bool visible)
    {
        foreach (var overlay in _overlays)
            overlay.Visible = visible;
    }

    /// <summary>
    /// Forces all overlays to recompute their cached state on next Update.
    /// </summary>
    public void InvalidateAll()
    {
        // Stub — overlays manage their own invalidation via NeedsRedraw
    }

    // ================================================================
    //  Private Helpers
    // ================================================================

    private void EnsureSorted()
    {
        if (!_sortDirty) return;
        _overlays.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
        _sortDirty = false;
    }
}
