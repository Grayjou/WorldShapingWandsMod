#if DEBUG
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.Debug;

/// <summary>
/// Immutable snapshot of selection/overlay state for a single frame.
/// Used by the edge-triggered debug emitter in <see cref="SelectionOverlay"/>
/// and by the <c>/wsw overlay</c> on-demand chat command.
///
/// Design: C-S4 2026-05-03 (DesignDoc_OverlayDebugSnapshot_OnDemand.md §2).
/// Supersedes: the 30-tick periodic print from C-S3.
/// </summary>
internal readonly struct OverlaySnapshot : IEquatable<OverlaySnapshot>
{
    public readonly bool SelectionActive;
    public readonly int  ClickStep;
    public readonly int  LastMagicWandTileCount; // 0 if null
    public readonly bool CancelledPresent;
    public readonly int  CachedTilesCount;       // SelectionOverlay._cachedTiles?.Count ?? 0
    public readonly bool ManagedByOverlaySystem;
    public readonly bool MouseInterface;

    public OverlaySnapshot(
        bool selectionActive,
        int  clickStep,
        int  lastMagicWandTileCount,
        bool cancelledPresent,
        int  cachedTilesCount,
        bool managedByOverlaySystem,
        bool mouseInterface)
    {
        SelectionActive         = selectionActive;
        ClickStep               = clickStep;
        LastMagicWandTileCount  = lastMagicWandTileCount;
        CancelledPresent        = cancelledPresent;
        CachedTilesCount        = cachedTilesCount;
        ManagedByOverlaySystem  = managedByOverlaySystem;
        MouseInterface          = mouseInterface;
    }

    /// <summary>
    /// Captures the current frame's overlay state.
    /// </summary>
    public static OverlaySnapshot Capture(Player player, WandPlayer wp, SelectionOverlay overlay)
    {
        bool cancelled = wp.CancelledSelection != null && !wp.CancelledSelection.IsExpired;
        return new OverlaySnapshot(
            selectionActive:        wp.Selection.IsActive,
            clickStep:              wp.SelectionClickStep,
            lastMagicWandTileCount: wp.LastMagicWandShape?.Tiles?.Count ?? 0,
            cancelledPresent:       cancelled,
            cachedTilesCount:       overlay.DebugCachedTilesCount,
            managedByOverlaySystem: overlay._managedByOverlaySystem,
            mouseInterface:         player.mouseInterface
        );
    }

    /// <summary>
    /// One-line chat representation. Magenta-ready; caller supplies the color.
    /// </summary>
    public string ToChatLine() =>
        $"[OVL] sel={SelectionActive} step={ClickStep} mw={LastMagicWandTileCount} " +
        $"cnc={CancelledPresent} cache={CachedTilesCount} " +
        $"mgr={ManagedByOverlaySystem} mi={MouseInterface}";

    public bool Equals(OverlaySnapshot other) =>
        SelectionActive        == other.SelectionActive        &&
        ClickStep              == other.ClickStep              &&
        LastMagicWandTileCount == other.LastMagicWandTileCount &&
        CancelledPresent       == other.CancelledPresent       &&
        CachedTilesCount       == other.CachedTilesCount       &&
        ManagedByOverlaySystem == other.ManagedByOverlaySystem &&
        MouseInterface         == other.MouseInterface;

    public override bool Equals(object obj) => obj is OverlaySnapshot o && Equals(o);

    public override int GetHashCode() => HashCode.Combine(
        SelectionActive, ClickStep, LastMagicWandTileCount,
        CancelledPresent, CachedTilesCount, ManagedByOverlaySystem, MouseInterface);
}
#endif
