using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Settings;
#if DEBUG
using WorldShapingWandsMod.Common.Debug;
#endif

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
        var cfg = WandConfigs.Overlay;
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

    /// <summary>Selection wand cancellation overlay color (gold).</summary>
    public static readonly Color CancelSelection = new Color(255, 215, 0);

    // â”€â”€ Chat messages â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Success / placement messages.
    /// In DEBUG builds, live-tunable via <c>/dev set Color.MsgSuccess</c>.</summary>
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

    /// <summary>Contextual hint messages — gentle guidance when an operation has no effect.
    /// In DEBUG builds, live-tunable via <c>/dev set Color.MsgHint</c>.</summary>
    public static readonly Color MsgHint = new Color(180, 200, 255); // Soft periwinkle

    /// <summary>Selection prompts (e.g. "click again to…").</summary>
    public static readonly Color MsgPrompt = Color.Cyan;

    /// <summary>Confirm prompts (e.g. "click to confirm, or right-click to cancel").</summary>
    public static readonly Color MsgConfirm = Color.Yellow;

    // ── Torch tiling overlay (magenta anchor + green reference point) ──
    // Centralized so they stay easy to tweak. The `*Opacity` constants are
    // applied at draw time via `WandColors.Foo * WandColors.FooOpacity`.

    /// <summary>Amber-ish marker drawn on every tiling candidate tile (non-anchor).</summary>
    public static readonly Color TilingMarker = new Color(255, 180, 60);
    public static readonly float TilingMarkerOpacity = 0.45f;

    /// <summary>Anchor cell — the tile the placement grid actually expands from
    /// (after any snap-to-existing-torch override). Magenta, drawn on top.</summary>
    public static readonly Color TilingAnchor = new Color(255, 64, 220);
    public static readonly float TilingAnchorOpacity = 0.85f;

    /// <summary>Reference cell — the raw seed point implied by the active
    /// <c>TorchReferenceMode</c> BEFORE any snap-to-existing-torch override.
    /// Drawn in green so it's distinguishable from the magenta anchor.</summary>
    public static readonly Color TilingReference = new Color(80, 255, 120);
    public static readonly float TilingReferenceOpacity = 0.85f;

    /// <summary>White outline framing the anchor + reference cells.</summary>
    public static readonly Color TilingCellOutline = Color.White;
    public static readonly float TilingCellOutlineOpacity = 0.95f;

    // ── Mode colors (per-wand-variant) ─────────────────────────────────
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
        public static readonly Color Instant = new Color(0, 220, 200);    // Teal — Instant
        public static readonly Color Select  = new Color(100, 230, 220);  // Light teal — Select
        public static readonly Color Confirm = new Color(180, 255, 240);  // Pale teal — Confirm
        public static readonly Color Stamp   = new Color(220, 255, 250);  // Very pale teal — Stamp
    }

    /// <summary>Fluids wand mode colors (blue water theme).</summary>
    public static class Fluids
    {
        public static readonly Color Instant = new Color(50, 130, 255);   // Vivid blue — Instant
        public static readonly Color Select  = new Color(80, 170, 255);   // Medium blue — Select
        public static readonly Color Confirm = new Color(130, 200, 255);  // Light blue — Confirm
        public static readonly Color Stamp   = new Color(180, 220, 255);  // Pale blue — Stamp
    }

    /// <summary>Torches wand mode colors (warm orange/amber theme).</summary>
    public static class Torches
    {
        public static readonly Color Instant = new Color(255, 160, 50);   // Vivid amber — Instant
        public static readonly Color Select  = new Color(255, 190, 80);   // Medium amber — Select
        public static readonly Color Confirm = new Color(255, 210, 130);  // Light amber — Confirm
        public static readonly Color Stamp   = new Color(255, 230, 180);  // Pale amber — Stamp
    }

    /// <summary>Fluids wand cancellation overlay color.</summary>
    public static readonly Color CancelFluids = new Color(50, 130, 255);

    /// <summary>Fluids wand chat message color.</summary>
    public static readonly Color MsgFluids = new Color(50, 160, 255);

    /// <summary>Torches wand cancellation overlay color.</summary>
    public static readonly Color CancelTorches = new Color(255, 160, 50);

    /// <summary>Torches wand chat message color.</summary>
    public static readonly Color MsgTorches = new Color(255, 180, 60);

    /// <summary>Molding wand mode colors (teal/cyan sculpting theme).</summary>
    public static class Molding
    {
        public static readonly Color Instant = new Color(0, 220, 220);    // Bright teal — Instant
        public static readonly Color Select  = new Color(50, 235, 235);   // Lighter teal — Select
        public static readonly Color Confirm = new Color(0, 180, 180);    // Dark teal — Confirm
        public static readonly Color Stamp   = new Color(0, 140, 140);    // Deepest teal — Stamp
    }

    /// <summary>Molding wand cancellation overlay color.</summary>
    public static readonly Color CancelMolding = new Color(0, 200, 200);

    /// <summary>Molding wand chat message color.</summary>
    public static readonly Color MsgMolding = new Color(0, 220, 220);

    // ── Per-Family Overlay Colors ─────────────────────────────────────
    // Each wand family gets a distinct hue for the selection overlay.
    // Colors chosen to align with each family's mode color theme.

    /// <summary>
    /// Per-family overlay base colors at full brightness. Dimming is applied
    /// separately via <see cref="GetStepBrightness"/>.
    /// </summary>
    private static readonly Dictionary<WandFamily, Color> FamilyOverlayColors = new()
    {
        { WandFamily.Building,     new Color(80, 220, 80)    },  // Green (120°)
        { WandFamily.Dismantling,  new Color(220, 70, 70)    },  // Red (0°)
        { WandFamily.Replacement,  new Color(170, 90, 230)   },  // Purple/Magenta (280°)
        { WandFamily.Wiring,       new Color(230, 210, 60)   },  // Yellow (55°)
        { WandFamily.Safekeeping,  new Color(230, 150, 60)   },  // Orange (30°)
        { WandFamily.Coating,      new Color(0, 200, 185)    },  // Teal (170°)
        { WandFamily.Fluids,       new Color(50, 130, 255)   },  // Blue (215°)
        { WandFamily.Torches,      new Color(255, 170, 50)   },  // Amber/Orange (35°)
        { WandFamily.Delimitation, new Color(230, 195, 50)   },  // Gold (45°)
        { WandFamily.Molding,      new Color(0, 210, 210)    },  // Cyan (180°)
    };

    // ── Temperature-Based Cancel Colors ───────────────────────────────
    // Three cancel colors based on hue temperature bands:
    //   Warm (270°–60° via 0°) → Blue   — max contrast with reds/oranges/magentas
    //   Yellow-Teal (60°–150°) → Purple — complements yellow-green hues
    //   Cool (150°–270°)       → Red    — contrasts with greens/blues/cyans

    /// <summary>Cancel color for Warm hue band families (Red, Orange, Magenta).</summary>
    public static readonly Color CancelBlue = new Color(60, 120, 255);

    /// <summary>Cancel color for Yellow-Teal hue band families (Yellow, Green, Gold).</summary>
    public static readonly Color CancelPurple = new Color(170, 80, 230);

    /// <summary>Cancel color for Cool hue band families (Teal, Blue, Cyan).</summary>
    public static readonly Color CancelRed = new Color(240, 70, 70);

    /// <summary>
    /// Gets the overlay base color for a specific wand family.
    /// Falls back to the configured single color for Unknown family or
    /// when per-family colors are disabled in config.
    /// </summary>
    public static Color GetOverlayBaseForFamily(WandFamily family)
    {
        var cfg = WandConfigs.Overlay;
        if (cfg != null && !cfg.UsePerFamilyOverlayColor)
            return cfg.SelectionOverlayColor; // user opted out → legacy single color

        if (family != WandFamily.Unknown && FamilyOverlayColors.TryGetValue(family, out var color))
            return color;

        return GetOverlayBase(); // fallback
    }

    /// <summary>
    /// Gets the cancel overlay color for a specific wand family based on
    /// the hue-temperature rule (Warm→Blue, Yellow-Teal→Purple, Cool→Red).
    /// </summary>
    public static Color GetCancelColorForFamily(WandFamily family)
    {
        return family switch
        {
            WandFamily.Dismantling  => CancelBlue,      // 0°   → Warm → Blue
            WandFamily.Safekeeping  => CancelBlue,      // 30°  → Warm → Blue
            WandFamily.Torches      => CancelBlue,      // 35°  → Warm → Blue
            WandFamily.Delimitation => CancelPurple,    // 45°  → Yellow-Teal → Purple
            WandFamily.Wiring       => CancelPurple,    // 55°  → Yellow-Teal → Purple
            WandFamily.Building     => CancelPurple,    // 120° → Yellow-Teal → Purple
            WandFamily.Coating      => CancelRed,       // 170° → Cool → Red
            WandFamily.Molding      => CancelRed,       // 180° → Cool → Red
            WandFamily.Fluids       => CancelRed,       // 215° → Cool → Red
            WandFamily.Replacement  => CancelBlue,      // 280° → Warm → Blue
            _                       => Color.Orange      // fallback (legacy behavior)
        };
    }

    // ── Fluid-Specific Overlay Colors ────────────────────────────────
    // Per-liquid overlay hues for the Fluids wand family.
    // Active when UsePerFamilyOverlayColor is enabled; resolved via
    // GetOverlayBaseForFluids() which inspects the live settings.

    /// <summary>Water overlay color — matches the standard Fluids family blue.</summary>
    public static readonly Color FluidWater = new Color(59, 107, 249);

    /// <summary>Lava overlay color — warm orange matching lava visuals.</summary>
    public static readonly Color FluidLava = new Color(255, 100, 30);

    /// <summary>Honey overlay color — amber/gold matching honey visuals.</summary>
    public static readonly Color FluidHoney = new Color(255, 200, 50);

    /// <summary>Shimmer overlay color — lavender matching shimmer visuals.</summary>
    public static readonly Color FluidShimmer = new Color(229, 201, 255);

    /// <summary>Bubble overlay color — minty teal matching Bubble block aesthetics.</summary>
    public static readonly Color FluidBubble = new Color(139, 255, 220);

    /// <summary>Drain overlay color — desaturated gray-blue signaling "removal."</summary>
    public static readonly Color FluidDrain = new Color(130, 130, 150);

    // ── Fluid-Specific Cancel Colors ──────────────────────────────────
    // Complement the fluid overlay hues for maximum contrast during cancel.

    /// <summary>Cancel color for Water — red (complementary to blue).</summary>
    public static readonly Color CancelFluidWater = new Color(205, 80, 80);

    /// <summary>Cancel color for Lava — blue (complementary to orange).</summary>
    public static readonly Color CancelFluidLava = new Color(2, 128, 252);

    /// <summary>Cancel color for Honey — purple (complementary to amber).</summary>
    public static readonly Color CancelFluidHoney = new Color(123, 84, 218);

    /// <summary>Cancel color for Shimmer — green (complementary to purple).</summary>
    public static readonly Color CancelFluidShimmer = new Color(65, 148, 19);

    /// <summary>Cancel color for Bubble — rose (complementary to minty teal).</summary>
    public static readonly Color CancelFluidBubble = new Color(204, 17, 60);

    /// <summary>Cancel color for Drain — pinkish (complementary to gray-blue).</summary>
    public static readonly Color CancelFluidDrain = new Color(255, 148, 172);

    /// <summary>
    /// Gets the overlay base color for a Fluids wand based on current settings.
    /// Bubble coating during Fill → Bubble color; Drain → gray; else → liquid type color.
    /// Falls back to the user's single overlay color when per-family colors are disabled.
    /// </summary>
    public static Color GetOverlayBaseForFluids(WandOfFluidsSettings settings)
    {
        var cfg = WandConfigs.Overlay;
        if (cfg != null && !cfg.UsePerFamilyOverlayColor)
            return cfg.SelectionOverlayColor; // user opted out → legacy single color

        if (settings == null)
            return FluidWater; // safety fallback

        if (settings.CoatInBubble && settings.Operation == FluidOperation.Fill)
            return FluidBubble;
        if (settings.PlaceBubble && settings.Operation == FluidOperation.Fill)
            return FluidBubble;
        if (settings.Operation == FluidOperation.Drain)
            return FluidDrain;

        return settings.LiquidType switch
        {
            LiquidTypeSelection.Water   => FluidWater,
            LiquidTypeSelection.Lava    => FluidLava,
            LiquidTypeSelection.Honey   => FluidHoney,
            LiquidTypeSelection.Shimmer => FluidShimmer,
            _                           => FluidWater
        };
    }

    /// <summary>
    /// Gets the cancel color for a Fluids wand based on current settings.
    /// Each fluid type gets a complementary cancel hue for maximum visual contrast.
    /// </summary>
    public static Color GetCancelColorForFluids(WandOfFluidsSettings settings)
    {
        if (settings == null)
            return CancelRed; // safety fallback (Cool band default for Fluids)

        if (settings.CoatInBubble && settings.Operation == FluidOperation.Fill)
            return CancelFluidBubble;
        if (settings.PlaceBubble && settings.Operation == FluidOperation.Fill)
            return CancelFluidBubble;
        if (settings.Operation == FluidOperation.Drain)
            return CancelFluidDrain;

        return settings.LiquidType switch
        {
            LiquidTypeSelection.Water   => CancelFluidWater,
            LiquidTypeSelection.Lava    => CancelFluidLava,
            LiquidTypeSelection.Honey   => CancelFluidHoney,
            LiquidTypeSelection.Shimmer => CancelFluidShimmer,
            _                           => CancelRed
        };
    }

    /// <summary>
    /// Computes the brightness multiplier based on selection step progress.
    /// Returns <paramref name="bMax"/> for OneClick wands (single step = always max).
    /// Returns a lerp between BrightnessMin and BrightnessMax for multi-step wands.
    /// </summary>
    public static float GetStepBrightness(int currentStep, int maxSteps)
    {
        var cfg = WandConfigs.Overlay;
        float bMin = cfg?.OverlayBrightnessMin ?? 0.6f;
        float bMax = cfg?.OverlayBrightnessMax ?? 1.0f;

        if (maxSteps <= 1)
            return bMax; // OneClick wands always at max

        float progress = (float)(currentStep - 1) / (maxSteps - 1);
        return MathHelper.Lerp(bMin, bMax, MathHelper.Clamp(progress, 0f, 1f));
    }
}
