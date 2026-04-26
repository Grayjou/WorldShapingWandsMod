using System;
using Microsoft.Xna.Framework;
using Terraria;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

#nullable enable

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Read-only snapshot of the current game state, passed to overlays every frame.
/// Overlays use this to decide what to draw and whether cached state is still valid.
/// </summary>
public readonly struct OverlayContext : IEquatable<OverlayContext>
{
    /// <summary>Current selection state (start/end tiles, active, clamped, etc.).</summary>
    public SelectionState Selection { get; init; }

    /// <summary>Shape configuration for the currently held wand.</summary>
    public ShapeInfo ShapeInfo { get; init; }

    /// <summary>Geometry context derived from selection + shape info.</summary>
    public ShapeContext ShapeContext { get; init; }

    /// <summary>The local player reference.</summary>
    public Player Player { get; init; }

    /// <summary>The local player's WandPlayer mod-player instance.</summary>
    public WandPlayer WandPlayer { get; init; }

    /// <summary>Current game time (from <c>Main._drawInterfaceGameTime</c>).</summary>
    public GameTime GameTime { get; init; }

    /// <summary>
    /// Screen-visible tile bounds for culling off-screen overlay elements.
    /// Computed from camera position and screen dimensions.
    /// </summary>
    public Rectangle ScreenTileBounds { get; init; }

    /// <summary>Item type ID of the player's currently held item.</summary>
    public int HeldItemType { get; init; }

    /// <summary>True when the player is holding any recognized wand item.</summary>
    public bool IsHoldingWand { get; init; }

    /// <summary>
    /// The most recent cancelled selection, or null if none.
    /// Used by overlays that render the cancel-fade effect.
    /// </summary>
    public CancelledSelectionState? CancelledSelection { get; init; }

    public bool Equals(OverlayContext other)
    {
        return HeldItemType == other.HeldItemType
            && IsHoldingWand == other.IsHoldingWand
            && ShapeInfo.Shape == other.ShapeInfo.Shape
            && ShapeInfo.FillMode == other.ShapeInfo.FillMode
            && ScreenTileBounds == other.ScreenTileBounds
            && ReferenceEquals(Selection, other.Selection);
    }

    public override bool Equals(object? obj) => obj is OverlayContext other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Selection?.GetHashCode() ?? 0,
            ShapeInfo.Shape,
            ShapeInfo.FillMode,
            HeldItemType,
            IsHoldingWand,
            ScreenTileBounds.GetHashCode());
    }

    public static bool operator ==(OverlayContext left, OverlayContext right) => left.Equals(right);
    public static bool operator !=(OverlayContext left, OverlayContext right) => !left.Equals(right);
}
