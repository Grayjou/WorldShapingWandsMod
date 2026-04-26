using Microsoft.Xna.Framework;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Centralized visual constants for all wand-related UI panels and elements.
///
/// <para>
/// History — created 2026-04-23 Session 2 (Letter #11) per Cavendish's
/// <c>DesignDoc_UIThemeCentralization.md</c> Phase A. The class kills off the
/// "magic-number colour and dimension literals scattered across panels" that
/// GrayJou flagged in Letter #10 §10. Phase A's scope is *extraction only*:
/// every value below was lifted verbatim from a previously inline literal in
/// <c>Common/UI/**/*.cs</c>, so adopting the theme is a no-visual-change refactor.
/// </para>
///
/// <para>
/// Conventions:
/// <list type="bullet">
///   <item><description>Panel chrome (per-family BG/Border) lives under <see cref="PanelChrome"/>.</description></item>
///   <item><description>Reusable button states (active/inactive/disabled) live under <see cref="Colors"/>.</description></item>
///   <item><description>Font scales and layout pixel values live under <see cref="Fonts"/> and <see cref="Layout"/>.</description></item>
///   <item><description>Timing constants live under <see cref="Timing"/>.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Future phases (B-D from Cavendish's design doc):
/// <list type="bullet">
///   <item><description>Phase B — sweep <c>WandColors</c>'s panel-related entries into here, leave overlay colours there.</description></item>
///   <item><description>Phase C — audit for leftover inline literals; tag exceptions <c>// theme-exempt: &lt;reason&gt;</c>.</description></item>
///   <item><description>Phase D — deferred. (Per GrayJou: grep-guard in deploy script — green-light required.)</description></item>
/// </list>
/// </para>
/// </summary>
public static class WandPanelTheme
{
    // ── Reusable button / state colours ───────────────────────────────
    public static class Colors
    {
        /// <summary>Standard "ON / active / accept" green. Replaces inline <c>new Color(80, 200, 80)</c>.</summary>
        public static readonly Color ActiveGreen = new(80, 200, 80);

        /// <summary>Standard "OFF / refuse / destructive" red. Replaces inline <c>new Color(200, 80, 80)</c>.</summary>
        public static readonly Color ActiveRed = new(200, 80, 80);

        /// <summary>Stronger "danger / clear all" red. Replaces inline <c>new Color(180, 60, 60)</c> / <c>new Color(220, 40, 40)</c>.</summary>
        public static readonly Color DangerRed = new(180, 60, 60);

        /// <summary>InventoryView-button active blue (cool accent). Replaces <c>new Color(120, 180, 255)</c>.</summary>
        public static readonly Color ActiveBlue = new(120, 180, 255);

        /// <summary>Paint-source brown (PaintSprayer = Inventory). Replaces <c>new Color(139, 90, 43)</c>.</summary>
        public static readonly Color PaintSourceBrown = new(139, 90, 43);

        /// <summary>Paint-coating teal (PaintSprayer = CoatingSettings). Replaces <c>new Color(46, 196, 182)</c>.</summary>
        public static readonly Color PaintCoatingTeal = new(46, 196, 182);

        /// <summary>Default per-button "off / inactive" background. Replaces <c>new Color(50, 50, 70)</c>.</summary>
        public static readonly Color ButtonInactive = new(50, 50, 70);

        /// <summary>Element-default inactive (UIIconButton/UIToggleButton/UITriStateButton). Replaces <c>new Color(60, 60, 60)</c>.</summary>
        public static readonly Color ElementInactive = new(60, 60, 60);

        /// <summary>Disabled background (greyed out). Replaces <c>new Color(40, 40, 40)</c>.</summary>
        public static readonly Color Disabled = new(40, 40, 40);

        /// <summary>Tri-state "ignore" mid-grey. Replaces <c>new Color(120, 120, 140)</c>.</summary>
        public static readonly Color TriStateIgnore = new(120, 120, 140);

        /// <summary>Tri-state "remove" muted red. Replaces <c>new Color(180, 70, 70)</c>.</summary>
        public static readonly Color TriStateRemove = new(180, 70, 70);

        /// <summary>Ghost-choice toast (warm gold). Replaces <c>new Color(220, 200, 110)</c>.</summary>
        public static readonly Color GhostChoiceToast = new(220, 200, 110);

        /// <summary>Subdued info text (e.g. "N candidates" label). Replaces <c>new Color(180, 180, 180)</c>.</summary>
        public static readonly Color SubduedText = new(180, 180, 180);

        /// <summary>Section-header soft-white text. Replaces inline <c>new Color(220, 220, 220)</c>.</summary>
        public static readonly Color SectionHeaderText = new(220, 220, 220);

        // ── Close-button / draggable header ──────────────────────────
        /// <summary>Wand-panel close button base colour. Replaces <c>new Color(63, 82, 151)</c>.</summary>
        public static readonly Color CloseButton = new(63, 82, 151);

        /// <summary>Wand-panel close button hover colour. Replaces <c>new Color(73, 94, 171)</c>.</summary>
        public static readonly Color CloseButtonHover = new(73, 94, 171);

        /// <summary>Header-strip border accent. Replaces <c>new Color(89, 116, 213)</c>.</summary>
        public static readonly Color HeaderStripBorder = new(89, 116, 213);

        /// <summary>Universal Wand panel chrome BG (parent state container). Replaces <c>new Color(33, 43, 79)</c>.</summary>
        public static readonly Color UniversalPanelBg = new(33, 43, 79);

        // ── Phase B additions (2026-04-23 S2) ─────────────────────────
        /// <summary>Neutral hover for chrome-tint buttons (e.g. InventoryView auxiliary action buttons). Replaces <c>new Color(80, 80, 100)</c>.</summary>
        public static readonly Color NeutralHover = new(80, 80, 100);

        /// <summary>UISliceGrid per-cell inactive background. Slightly different green channel than <see cref="ButtonInactive"/> by design (kept distinct so a future tweak doesn't drag along every panel toggle). Replaces <c>new Color(50, 55, 70)</c>.</summary>
        public static readonly Color SliceGridInactive = new(50, 55, 70);

        // ── Phase S4 additions (2026-04-23, Convergence S+1 framework) ──
        /// <summary>CollapsibleSection header-bar background tint. Currently unused by the Hide/Compact path (header lives transparently atop parent panel) but reserved for the styled-header variant landing in S+2.</summary>
        public static readonly Color SectionBg     = new(40, 50, 75, 180);

        /// <summary>CollapsibleSection 1-px underline / border accent.</summary>
        public static readonly Color SectionBorder = new(60, 80, 130);
    }

    // ── Per-family panel chrome (BackgroundColor + BorderColor) ─────
    public static class PanelChrome
    {
        // Each family settings panel uses a unique colour pair; they ARE the
        // family's identity colour. Centralizing them here means a future
        // re-skin (or accessibility palette) lands in one file.

        public static readonly Color BuildingBg     = new(44, 57, 105, 220);
        public static readonly Color BuildingBorder = new(20, 20, 60);

        public static readonly Color CoatingBg      = new(30, 50, 55, 220);
        public static readonly Color CoatingBorder  = new(0, 100, 90);

        public static readonly Color DismantlingBg     = new(79, 44, 44, 220);
        public static readonly Color DismantlingBorder = new(60, 20, 20);

        public static readonly Color FluidsBg     = new(30, 45, 70, 220);
        public static readonly Color FluidsBorder = new(40, 80, 160);

        public static readonly Color MoldingBg     = new(25, 50, 55, 220);
        public static readonly Color MoldingBorder = new(0, 200, 200);

        public static readonly Color ReplacementBg     = new(44, 79, 44, 220);
        public static readonly Color ReplacementBorder = new(20, 60, 20);

        public static readonly Color SafekeepingBg     = new(54, 44, 79, 220);
        public static readonly Color SafekeepingBorder = new(30, 20, 60);

        public static readonly Color SelectionBg     = new(30, 45, 55, 220);
        public static readonly Color SelectionBorder = new(180, 145, 10);

        public static readonly Color TorchesBg     = new(30, 45, 70, 220);
        public static readonly Color TorchesBorder = new(40, 80, 160);

        public static readonly Color WiringBg     = new(79, 79, 44, 220);
        // Wiring DOES have a border — the S1-2026-04-23 sweep mistakenly skipped this slot
        // (the inline literal sat one line below `BackgroundColor =`). Kept at the original
        // dark-olive that visually echoes WiringBg, NOT switched to HeaderStripBorder, so
        // that adopting the theme remains a no-visual-change refactor as Phase A promised.
        // Per Cavendish 2026-04-23 Letter #1 §8(2): symmetry is achieved by *having* the slot;
        // the family identity colour stays.
        public static readonly Color WiringBorder = new(60, 60, 20);

        public static readonly Color InventoryViewBg     = new(28, 36, 56, 230);
        public static readonly Color InventoryViewBorder = new(70, 100, 160);

        // ── Popout host (2026-04-23 S4 framework) ──
        // Aliased to the InventoryView pair to start — the popout host is
        // visually a sibling of the IV: a small cool-toned floating mini-panel.
        // Future re-skin can fork these without touching the IV.
        public static readonly Color PopoutBg     = new(28, 36, 56, 230);
        public static readonly Color PopoutBorder = new(70, 100, 160);
    }

    // ── Per-button-set family colours (Phase B, 2026-04-23 S2) ──────
    // These were inline in WiringSettingsPanel / DismantlingSettingsPanel /
    // SafekeepingSettingsPanel as the per-toggle ActiveColor parameters. Pulled
    // here so a future re-skin / accessibility palette / colour-blind mode can
    // override them in one place. Each constant maps 1:1 to its previous inline
    // literal — Phase B is still extraction-only, no visual change.

    /// <summary>Wiring family per-wire colours + actuator + place/remove mode toggles.</summary>
    public static class Wires
    {
        public static readonly Color Red    = new(200, 80, 80);
        public static readonly Color Green  = new(80, 200, 80);
        public static readonly Color Blue   = new(80, 80, 200);
        public static readonly Color Yellow = new(200, 200, 80);
        public static readonly Color Actuator = new(150, 150, 150);
        public static readonly Color PlaceMode  = new(100, 200, 100);
        public static readonly Color RemoveMode = new(200, 100, 100);
    }

    /// <summary>Dismantling family per-category destroy toggles.</summary>
    public static class DestroyCategories
    {
        public static readonly Color Tiles      = new(200, 100, 100);
        public static readonly Color Walls      = new(100, 100, 200);
        public static readonly Color Containers = new(200, 200, 100);
        // VoidEverything reuses the strongest danger red.
        public static readonly Color Void       = new(220, 40, 40);
    }

    /// <summary>Safekeeping family protect/unprotect/per-layer toggles.</summary>
    public static class ProtectModes
    {
        public static readonly Color Protect   = new(80, 180, 80);
        public static readonly Color Unprotect = new(200, 80, 80);
        public static readonly Color Tiles     = new(100, 140, 200);
        public static readonly Color Walls     = new(150, 100, 180);
    }

    // ── Typography / scale ───────────────────────────────────────────
    public static class Fonts
    {
        public const float PanelTitleScale     = 1.0f;
        public const float SectionTitleScale   = 0.9f;
        public const float ButtonLabelScale    = 0.85f;
        public const float TooltipScale        = 0.8f;
        public const float DimensionLabelScale = 0.9f;
    }

    // ── Layout / sizing pixel values ─────────────────────────────────
    public static class Layout
    {
        public const int PanelPadding          = 8;
        public const int SectionSpacing        = 6;
        public const int RowSpacing            = 4;
        public const int ButtonSize            = 28;
        public const int IconButtonSize        = 24;
        public const int InventoryViewSlotSize = 40;
        public const int DraggableHeaderHeight = 26;
    }

    // ── Animation / timing (ticks unless noted) ──────────────────────
    public static class Timing
    {
        /// <summary>GhostChoiceToast throttle (ticks; 60 = 1 second).</summary>
        public const int GhostToastThrottleTicks   = 120;

        /// <summary>Frames between dimension-label area recalcs (debounce).</summary>
        public const int AreaDebounceFrames        = 8;

        /// <summary>Frames between narrow-stock cache refreshes.</summary>
        public const int NarrowStockRefreshFrames  = 30;
    }
}
