using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;

namespace WorldShapingWandsMod.Common.Drawing;

/// <summary>
/// Centralized color palette for all wand visual feedback.
/// Edit these values to restyle the entire mod in one place.
/// </summary>
public static class WandColors
{
    // â”€â”€ Overlay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Default selection overlay base color (fallback when config unavailable).</summary>
    public static readonly Color OverlayBase = Color.LimeGreen;

    /// <summary>
    /// Returns the configured overlay base color from client config,
    /// falling back to <see cref="OverlayBase"/> if the config is unavailable.
    /// </summary>
    public static Color GetOverlayBase()
    {
        var cfg = ModContent.GetInstance<WandClientConfig>();
        return cfg?.SelectionOverlayColor ?? OverlayBase;
    }

    /// <summary>Selection overlay color when the selection was clamped to max distance.</summary>
    public static readonly Color OverlayClamped = Color.Orange;

    /// <summary>Fill opacity for selected tiles in the overlay (0â€“1).</summary>
    public const float OverlayFillOpacity = 0.18f;

    /// <summary>Outline opacity for exterior edges in the overlay (0â€“1).</summary>
    public const float OverlayOutlineOpacity = 0.55f;

    /// <summary>Outline width in pixels for exterior edges.</summary>
    public const int OverlayOutlineWidth = 2;

    // â”€â”€ Large shape debounce â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Opacity for the lightweight bounding-rect outline shown while a large shape is being resized.</summary>
    public const float DebounceBoundingRectOpacity = 0.45f;

    /// <summary>Duration (in frames) of the fade-in after the full shape is rasterized.</summary>
    public const int DebounceFadeInFrames = 10; // ~0.17s at 60fps

    // â”€â”€ Start/End position markers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Color for the Start tile marker during active selection.</summary>
    public static readonly Color StartMarker = Color.Cyan;

    /// <summary>Color for the End tile marker during active selection.</summary>
    public static readonly Color EndMarker = Color.Cyan;

    /// <summary>Outline opacity for start/end position markers (0â€“1).</summary>
    public const float MarkerOutlineOpacity = 0.85f;

    /// <summary>Outline width in pixels for start/end position markers.</summary>
    public const int MarkerOutlineWidth = 2;

    /// <summary>Dimension label opacity (0â€“1).</summary>
    public const float DimensionLabelOpacity = 0.8f;

    // â”€â”€ Cancellation overlay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Duration in ticks (60 = 1 second) that the cancelled overlay stays visible.</summary>
    public const int CancelOverlayDurationTicks = 45;

    /// <summary>Duration in ticks that the "Cancelled" text is visible (fades over this).</summary>
    public const int CancelTextDurationTicks = 42;

    /// <summary>Building wand cancellation overlay color (green â†’ orange).</summary>
    public static readonly Color CancelBuilding = Color.Orange;

    /// <summary>Dismantling wand cancellation overlay color (red â†’ yellow).</summary>
    public static readonly Color CancelDismantling = Color.Yellow;

    /// <summary>Replacement wand cancellation overlay color (green â†’ orange).</summary>
    public static readonly Color CancelReplacement = Color.Orange;

    /// <summary>Wiring wand cancellation overlay color.</summary>
    public static readonly Color CancelWiring = Color.Orange;

    /// <summary>Safekeeping wand cancellation overlay color (purple).</summary>
    public static readonly Color CancelSafekeeping = new Color(180, 100, 255);

    /// <summary>Coating wand cancellation overlay color (teal/cyan).</summary>
    public static readonly Color CancelCoating = new Color(0, 200, 180);

    // â”€â”€ Chat messages â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

    /// <summary>Coating (paint/scrape) result messages.</summary>
    public static readonly Color MsgCoating = new Color(0, 220, 200); // teal

    /// <summary>Error / missing-item messages.</summary>
    public static readonly Color MsgError = Color.Red;

    /// <summary>Warning / cancel messages.</summary>
    public static readonly Color MsgWarning = Color.Yellow;

    /// <summary>Informational messages (e.g. "no tiles changed").</summary>
    public static readonly Color MsgInfo = Color.Gray;

    /// <summary>Selection prompts (e.g. "click again toâ€¦").</summary>
    public static readonly Color MsgPrompt = Color.Cyan;

    /// <summary>Confirm prompts (e.g. "click to confirm, or right-click to cancel").</summary>
    public static readonly Color MsgConfirm = Color.Yellow;

    // â”€â”€ Mode colors (per-wand-variant) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

    /// <summary>Coating wand mode colors.</summary>
    public static class Coating
    {
        public static readonly Color Instant = new Color(0, 220, 200);    // Teal â€” Instant
        public static readonly Color Select  = new Color(100, 230, 220);  // Light teal â€” Select
        public static readonly Color Confirm = new Color(180, 255, 240);  // Pale teal â€” Confirm
        public static readonly Color Stamp   = new Color(220, 255, 250);  // Very pale teal â€” Stamp
    }
}
