using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Centralized color palette for all wand visual feedback.
/// Edit these values to restyle the entire mod in one place.
/// </summary>
public static class WandColors
{
    // ── Overlay ──────────────────────────────────────────────
    /// <summary>Default selection overlay base color.</summary>
    public static readonly Color OverlayBase = Color.LimeGreen;

    /// <summary>Selection overlay color when the selection was clamped to max distance.</summary>
    public static readonly Color OverlayClamped = Color.Orange;

    /// <summary>Fill opacity for selected tiles in the overlay (0–1).</summary>
    public const float OverlayFillOpacity = 0.18f;

    /// <summary>Outline opacity for exterior edges in the overlay (0–1).</summary>
    public const float OverlayOutlineOpacity = 0.55f;

    /// <summary>Outline width in pixels for exterior edges.</summary>
    public const int OverlayOutlineWidth = 2;

    /// <summary>Dimension label opacity (0–1).</summary>
    public const float DimensionLabelOpacity = 0.8f;

    // ── Cancellation overlay ──────────────────────────────────
    /// <summary>Duration in ticks (60 = 1 second) that the cancelled overlay stays visible.</summary>
    public const int CancelOverlayDurationTicks = 45;

    /// <summary>Duration in ticks that the "Cancelled" text is visible (fades over this).</summary>
    public const int CancelTextDurationTicks = 42;

    /// <summary>Building wand cancellation overlay color (green → orange).</summary>
    public static readonly Color CancelBuilding = Color.Orange;

    /// <summary>Dismantling wand cancellation overlay color (red → yellow).</summary>
    public static readonly Color CancelDismantling = Color.Yellow;

    /// <summary>Replacement wand cancellation overlay color (green → orange).</summary>
    public static readonly Color CancelReplacement = Color.Orange;

    /// <summary>Wiring wand cancellation overlay color.</summary>
    public static readonly Color CancelWiring = Color.Orange;

    /// <summary>Safekeeping wand cancellation overlay color (purple).</summary>
    public static readonly Color CancelSafekeeping = new Color(180, 100, 255);

    // ── Chat messages ────────────────────────────────────────
    /// <summary>Success / placement messages.</summary>
    public static readonly Color MsgSuccess = Color.Cyan;

    /// <summary>Dismantling result messages.</summary>
    public static readonly Color MsgDismantling = Color.OrangeRed;

    /// <summary>Replacement result messages.</summary>
    public static readonly Color MsgReplacement = Color.MediumPurple;

    /// <summary>Wiring placement messages.</summary>
    public static readonly Color MsgWiring = Color.LimeGreen;

    /// <summary>Safekeeping status messages.</summary>
    public static readonly Color MsgSafekeeping = Color.MediumPurple;

    /// <summary>Error / missing-item messages.</summary>
    public static readonly Color MsgError = Color.Red;

    /// <summary>Warning / cancel messages.</summary>
    public static readonly Color MsgWarning = Color.Yellow;

    /// <summary>Informational messages (e.g. "no tiles changed").</summary>
    public static readonly Color MsgInfo = Color.Gray;

    /// <summary>Selection prompts (e.g. "click again to…").</summary>
    public static readonly Color MsgPrompt = Color.Cyan;

    /// <summary>Confirm prompts (e.g. "click to confirm, or right-click to cancel").</summary>
    public static readonly Color MsgConfirm = Color.Yellow;

    // ── Mode colors (per-wand-variant) ───────────────────────
    // These are referenced by each wand's ModeColor property.
    // Kept here for easy cross-referencing but each wand still
    // exposes its own ModeColor override.

    /// <summary>Dismantling wand mode colors.</summary>
    public static class Dismantling
    {
        public static readonly Color Instant = new Color(255, 100, 100);
        public static readonly Color Select  = new Color(255, 200, 100);
        public static readonly Color Confirm = new Color(100, 255, 100);
    }

    /// <summary>Building wand mode colors.</summary>
    public static class Building
    {
        public static readonly Color Instant = new Color(100, 255, 100);
        public static readonly Color Select  = new Color(100, 200, 255);
        public static readonly Color Confirm = new Color(255, 255, 100);
    }

    /// <summary>Replacement wand mode colors.</summary>
    public static class Replacement
    {
        public static readonly Color Instant = new Color(180, 100, 255);
        public static readonly Color Select  = new Color(200, 150, 255);
        public static readonly Color Confirm = new Color(255, 180, 220);
    }

    /// <summary>Wiring wand mode colors.</summary>
    public static class Wiring
    {
        public static readonly Color Instant = new Color(255, 200, 50);
        public static readonly Color Select  = new Color(255, 220, 100);
        public static readonly Color Confirm = new Color(200, 255, 150);
    }

    /// <summary>Safekeeping wand mode colors.</summary>
    public static class Safekeeping
    {
        public static readonly Color Instant = new Color(255, 80, 80);
        public static readonly Color Select  = new Color(255, 255, 80);
        public static readonly Color Confirm = new Color(80, 255, 80);
        public static readonly Color Stamp   = new Color(100, 200, 255);
    }
}
