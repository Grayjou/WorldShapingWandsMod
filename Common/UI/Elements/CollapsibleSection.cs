using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Three collapse styles a <see cref="CollapsibleSection"/> can declare.
/// (Mirrors Cavendish <c>DesignDoc_CollapsablePanelSystem.md</c> §2.3, 2026-04-21 S1.)
/// </summary>
public enum CollapseStyle
{
    /// <summary>Section content removed from layout; parent shrinks (most sections).</summary>
    Hide,
    /// <summary>Section header stays, content hidden in place (no parent reflow).</summary>
    Compact,
    /// <summary>Section detaches into a floating <see cref="CollapsedPopoutHost"/> mini-panel.</summary>
    Popout,
    /// <summary>
    /// (v1.3 §E, 2026-04-25 S4 — DesignDoc_PopoutFrameworkV1_3 §E,
    /// Notes_FoldableReConfirmation.md.) Section header stays with a
    /// chevron/triangle that animates a fold-down body reveal in place
    /// (similar to Compact but with explicit fold semantics rather than
    /// "compacted"). Currently temp-disabled at the framework level via
    /// <see cref="WandUISystem.AllowFoldStyle"/>; sections constructed with
    /// this style are silently downgraded to <see cref="Hide"/> until the
    /// fold UX is finished.
    /// </summary>
    Folded,
}

/// <summary>
/// A header-bar wrapper around an arbitrary body <see cref="UIElement"/> that
/// can be collapsed via a corner icon. Collapsed/expanded state is keyed by
/// <see cref="PreferenceKey"/> and round-tripped through <see cref="WandPlayer"/>.
///
/// <para>
/// History — created 2026-04-23 Session 4 (Cavendish
/// <c>DesignDoc_CollapsablePanelSystem.md</c> 2026-04-21 S1, executed under the
/// Convergence S+1 plan from <c>SessionPlan_WSW_Next3Sessions.md</c>). Framework
/// only — no consumer migrations land in S+1; that's S+2.
/// </para>
///
/// <para>
/// <b>Adaptation</b>: the design doc names a <c>SettingsPlayer</c> for persistence;
/// no such class exists in WSW. State lives on <see cref="WandPlayer"/>'s
/// <c>CollapsedSections</c>/<c>PopoutPositions</c> dicts, round-tripped in the
/// existing <c>SaveData</c>/<c>LoadData</c> at L783/L792.
/// </para>
///
/// <para>
/// <b>API contract</b>:
/// <list type="bullet">
///   <item><description><c>SetBody</c> attaches the content element. Call once after construction.</description></item>
///   <item><description><c>SetCollapsed(bool)</c> flips visibility, raises <see cref="OnStateChanged"/>, and persists to player dict.</description></item>
///   <item><description>For <see cref="CollapseStyle.Popout"/>, collapsing detaches the body and routes it to <see cref="WandUISystem.OpenPopout"/>; restoring re-parents.</description></item>
///   <item><description>Height self-updates from body's <c>Height.Pixels</c> (consumers SHOULD use absolute pixel heights for the body root).</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CollapsibleSection : UIElement
{
    /// <summary>
    /// Header bar height in pixels. Public for cross-element re-attach math
    /// (e.g. <see cref="CollapsedPopoutHost.ReleaseBody"/> resets the body's
    /// <c>Top.Pixels</c> back to this value when re-parenting into a section).
    /// (Made public 2026-04-25 S2 v1.1 — DesignDoc §3.3.c.)
    /// </summary>
    public const float HeaderHeightConst = 22f;
    private const float HeaderHeight = HeaderHeightConst;
    private const float IconSize     = 16f;
    private const float IconPadding  = 4f;

    /// <summary>Localisation key resolved via <see cref="Language.GetTextValue(string)"/>.</summary>
    public string TitleKey { get; }

    /// <summary>Stable identifier for the per-section persisted collapsed bit. Form: "PanelName.SectionName".</summary>
    public string PreferenceKey { get; }

    /// <summary>Behaviour when collapsed — Hide / Compact / Popout. Defaults to Hide per design doc §2.3.</summary>
    public CollapseStyle Style { get; }

    /// <summary>Current collapsed state. Use <see cref="SetCollapsed"/> to mutate.</summary>
    public bool IsCollapsed { get; private set; }

    /// <summary>Raised after a collapsed-state change. Subscribers typically call <c>UIList.Recalculate()</c> on the parent.</summary>
    public event Action<CollapsibleSection> OnStateChanged;

    /// <summary>
    /// True while the body lives in a <see cref="CollapsedPopoutHost"/> rather than this section.
    /// <para>v1.1 (2026-04-25 S2, DesignDoc §3.2.a / Invariant I-2): this is now a
    /// PURE DERIVED VIEW of the popout host's <c>ActiveSection</c>. There is no
    /// independent backing field — eliminating an entire class of two-tracker
    /// drift bugs (Diagnosis Bug D).</para>
    /// </summary>
    public bool IsPoppedOut =>
        Style == CollapseStyle.Popout
        && ModContent_WandUISystem()?.ActivePopoutHost?.ActiveSection == this;

    /// <summary>
    /// Optional predicate evaluated each <c>UpdateUI</c> tick by
    /// <see cref="WandUISystem"/>: while it returns <c>true</c> the popout is
    /// shown; while it returns <c>false</c> the popout is HIDDEN (parked) — body
    /// stays attached, position stays remembered, only Draw/Update are gated.
    /// Defaults to <c>null</c> ⇒ "always visible" (popout shows until the user
    /// explicitly clicks ✕).
    /// <para>v1.3 §B (2026-04-25 S4 — DesignDoc_PopoutFrameworkV1_3 §B / Invariant
    /// I-11): the predicate now drives a *visibility* lifecycle (show/hide), not
    /// an *attachment* lifecycle (open/close). Replaces v1.1/v1.2's grace-then-
    /// close path which destroyed configured state on brief hotbar excursions.
    /// Only ✕ closes for real.</para>
    /// <para>For the Coating PaintColor section the predicate is an OR over
    /// every wand family that consumes the selected paint (Coating + Building
    /// + Replacement) so the popout stays VISIBLE across the natural "build with
    /// paint → swap to replacement → swap back" flow. (DesignDoc §2 I-5 / §3.5,
    /// 2026-04-25 S2.)</para>
    /// </summary>
    public Func<bool> OwnerVisibilityCheck { get; set; }

    private UIElement _body;
    private readonly UIElement _headerBar;
    private readonly UIText _titleText;
    private readonly UIText _toggleIcon;
    private readonly UIText _popoutIcon; // null for non-Popout styles

    public CollapsibleSection(string titleKey, string preferenceKey, CollapseStyle style = CollapseStyle.Hide)
    {
        // (v1.3 §E, 2026-04-25 S4 — DesignDoc_PopoutFrameworkV1_3 §E,
        //  Notes_FoldableReConfirmation.md.) Single-source-of-truth gate for
        //  the unfinished fold UX. While AllowFoldStyle is false, any caller
        //  asking for Compact or Folded silently gets Hide instead. No compile
        //  error and no runtime exception — graceful degradation by design.
        //  Will flip to true (or this gate will be removed) when §E.4 lands.
        if (!WandUISystem.AllowFoldStyle
            && (style == CollapseStyle.Compact || style == CollapseStyle.Folded))
        {
            style = CollapseStyle.Hide;
        }

        TitleKey = titleKey;
        PreferenceKey = preferenceKey;
        Style = style;

        Width.Set(0f, 1f);
        Height.Set(HeaderHeight, 0f);

        _headerBar = new UIElement();
        _headerBar.Width.Set(0f, 1f);
        _headerBar.Height.Set(HeaderHeight, 0f);
        _headerBar.OnLeftClick += HeaderBarClicked;
        Append(_headerBar);

        _titleText = new UIText(Language.GetTextValue(titleKey), WandPanelTheme.Fonts.SectionTitleScale);
        _titleText.HAlign = 0f;
        _titleText.VAlign = 0.5f;
        _titleText.Left.Set(2f, 0f);
        _titleText.IgnoresMouseInteraction = true;
        _headerBar.Append(_titleText);

        // Right-aligned icon set.
        // (S5 2026-04-25 — GrayJou Letter #4 §1: leftover-triangle fix.)
        // For Popout-style sections we render ONLY the ⧓ pop-out icon. The
        // ▾/▸ collapse-toggle was a vestige of the Compact/Folded UX which
        // is master-gated off (WandUISystem.AllowFoldStyle == false until
        // v1.3 §E.4 lands). With Compact downgraded to Hide, clicking the
        // ▾ on a Popout section did a redundant in-place hide that was
        // visually confusing next to the dedicated ⧓ button — GrayJou's
        // playtest report flagged exactly this artifact ("little triangle
        // right next to the Popout button"). Hide-style sections still keep
        // their ▾ (the only collapse affordance they have). When Foldable
        // ships, this branch grows back symmetrically.
        if (style == CollapseStyle.Popout)
        {
            // ⧓ pop-out icon, right-anchored on its own (no ▾ neighbour).
            _popoutIcon = new UIText("⧓", WandPanelTheme.Fonts.SectionTitleScale);
            _popoutIcon.HAlign = 1f;
            _popoutIcon.VAlign = 0.5f;
            _popoutIcon.Width.Set(IconSize, 0f);
            _popoutIcon.IgnoresMouseInteraction = true;
            _headerBar.Append(_popoutIcon);
        }
        else
        {
            _toggleIcon = new UIText("▾", WandPanelTheme.Fonts.SectionTitleScale);
            _toggleIcon.HAlign = 1f;
            _toggleIcon.VAlign = 0.5f;
            _toggleIcon.Width.Set(IconSize, 0f);
            _toggleIcon.IgnoresMouseInteraction = true;
            _headerBar.Append(_toggleIcon);
        }
    }

    /// <summary>
    /// Attaches the body element below the header. Should be called once after
    /// construction (typically in panel <c>OnInitialize</c>). The body is expected
    /// to declare an absolute pixel <c>Height</c> — section height is derived from it.
    /// </summary>
    public void SetBody(UIElement body)
    {
        if (_body != null && _body.Parent == this)
            RemoveChild(_body);

        _body = body;
        if (_body != null)
        {
            _body.Top.Set(HeaderHeight, 0f);
            _body.Width.Set(0f, 1f);
            if (!IsCollapsed && !IsPoppedOut)
                Append(_body);
        }
        UpdateLayout();
    }

    /// <summary>
    /// Reads persisted collapsed state from the local player's
    /// <see cref="WandPlayer.CollapsedSections"/> dict and applies it WITHOUT
    /// firing <see cref="OnStateChanged"/>. Call once after <see cref="SetBody"/>
    /// in panel <c>OnInitialize</c>.
    /// </summary>
    public void RestoreFromPreferences()
    {
        // v1.1 (2026-04-25 S2, DesignDoc §3.2.c / Invariant I-3): Popout-style sections
        // never restore their collapsed bit. The bit was overloaded with "popped out at
        // save time" and re-applying it on world enter orphaned the body (Bug C —
        // Diagnosis_CoatingPopoutMismatch §4). The popout panel POSITION is still
        // remembered via WandPlayer.PopoutPositions; just the open/closed state defaults
        // to "expanded in-panel" on every world enter.
        if (Style == CollapseStyle.Popout) return;

        var wp = TryGetWandPlayer();
        if (wp == null) return;
        if (wp.CollapsedSections != null && wp.CollapsedSections.TryGetValue(PreferenceKey, out bool stored))
        {
            ApplyCollapsedState(stored, fireEvent: false, openPopout: false);
        }
    }

    /// <summary>
    /// Programmatically collapse / expand. Persists to player dict and raises
    /// <see cref="OnStateChanged"/>.
    /// <para>v1.1 (2026-04-25 S2, DesignDoc §3.2.f): for Popout-style sections this
    /// method is now ONLY used for the Hide/Compact toggle path (header click that
    /// is NOT on the ⬓ icon). Popout open/close is routed through
    /// <see cref="WandUISystem.OpenPopout"/> / <see cref="WandUISystem.ClosePopout"/>
    /// in <see cref="HeaderBarClicked"/> — single source of truth (Invariant I-1)
    /// for the body parentage state machine.</para>
    /// </summary>
    public void SetCollapsed(bool collapsed)
    {
        if (IsCollapsed == collapsed) return;
        ApplyCollapsedState(collapsed, fireEvent: true, openPopout: false);
        Persist();
    }

    /// <summary>
    /// Called by <see cref="CollapsedPopoutHost"/> (via <see cref="WandUISystem.ClosePopout"/>)
    /// when the popout is being torn down — re-attach the body to this section
    /// and reflect the expanded-in-panel state.
    /// <para>v1.1 (2026-04-25 S2, DesignDoc §3.2.e): IDEMPOTENT. The early-return
    /// guard (<c>if (!IsPoppedOut) return</c>) was removed because <c>IsPoppedOut</c>
    /// is now derived from the host (Invariant I-2) and by the time this notify
    /// fires the host has already cleared its <c>_owningSection</c>, so the guard
    /// would always trip and orphan the body (Bug D). Persist call also dropped —
    /// Popout-style sections don't persist <c>IsCollapsed</c> per Invariant I-3.</para>
    /// </summary>
    internal void NotifyPopoutClosed()
    {
        // Body should be re-parented to this section; re-attach defensively in case
        // the caller didn't already do it (e.g. desync recovery).
        if (_body != null && _body.Parent != this)
            Append(_body);
        IsCollapsed = false;
        UpdateIcon();
        UpdateLayout();
        OnStateChanged?.Invoke(this);
    }

    private void HeaderBarClicked(UIMouseEvent evt, UIElement listening)
    {
        // v1.1 (2026-04-25 S2, DesignDoc §3.2.f): the ⬓ icon zone now routes through
        // WandUISystem explicitly. The section never mutates host._body directly —
        // single entry point per direction (Invariant I-1) means OpenPopout /
        // ClosePopout are the only paths through which body parentage changes.
        if (Style == CollapseStyle.Popout && _popoutIcon != null)
        {
            CalculatedStyle popDims = _popoutIcon.GetDimensions();
            if (popDims.ToRectangle().Contains((int)evt.MousePosition.X, (int)evt.MousePosition.Y))
            {
                var sys = ModContent_WandUISystem();
                if (IsPoppedOut)
                    sys?.ClosePopout();
                else if (_body != null)
                    sys?.OpenPopout(this, _body);
                return;
            }
        }
        // Default: toggle Hide/Compact collapse (or non-popout collapse for Popout style)
        SetCollapsed(!IsCollapsed);
    }

    private void ApplyCollapsedState(bool collapsed, bool fireEvent, bool openPopout)
    {
        IsCollapsed = collapsed;

        // v1.1 (2026-04-25 S2, DesignDoc §3.2): Popout flow no longer routes through
        // here — IsPoppedOut is now derived from WandUISystem (Invariant I-2) and the
        // body is attached/detached exclusively via OpenPopout/ClosePopout. This
        // method only handles Hide / Compact-style collapsing now (and the Hide-side
        // visual collapse for a Popout section that's NOT currently popped out, in
        // which case the body just hides in place like Hide).
        _ = openPopout; // legacy parameter kept for ABI; no longer drives state.

        if (_body != null)
        {
            // Skip body re-parenting entirely if the body is currently floating in the
            // popout host — touching it here would yank it out from under the host
            // (Bug D regression vector).
            bool bodyIsInPopout = IsPoppedOut;
            if (!bodyIsInPopout)
            {
                if (collapsed && _body.Parent == this) RemoveChild(_body);
                else if (!collapsed && _body.Parent != this) Append(_body);
            }
        }

        UpdateIcon();
        UpdateLayout();
        if (fireEvent) OnStateChanged?.Invoke(this);
    }

    private void UpdateIcon()
    {
        if (_toggleIcon == null) return;
        // ▾ expanded, ▸ collapsed; popout icon is static ⬓.
        _toggleIcon.SetText(IsCollapsed ? "▸" : "▾");
    }

    private void UpdateLayout()
    {
        float bodyH = 0f;
        if (!IsCollapsed && !IsPoppedOut && _body != null && _body.Parent == this)
            bodyH = _body.Height.Pixels;
        Height.Set(HeaderHeight + bodyH, 0f);
        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        // Subtle 1-px underline beneath header (matches UISectionTitle convention).
        CalculatedStyle dims = GetDimensions();
        var lineRect = new Rectangle((int)dims.X, (int)(dims.Y + HeaderHeight - 1), (int)dims.Width, 1);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineRect, Color.White * 0.25f);

        // Hover-tooltips for the toggle / popout icons.
        // Icons have IgnoresMouseInteraction=true so we hit-test by point-in-rect against
        // their CalculatedStyle while the header bar reports IsMouseHovering. Localised
        // strings live under UI.CollapsibleSection.{Expand,Collapse,Popout}Tooltip — which
        // is also why those keys are referenced HERE (Tier 3 orphan-lint enforcement).
        if (_headerBar != null && _headerBar.IsMouseHovering)
        {
            int mx = Main.mouseX;
            int my = Main.mouseY;
            if (_popoutIcon != null && _popoutIcon.GetDimensions().ToRectangle().Contains(mx, my))
            {
                Main.hoverItemName = Language.GetTextValue("Mods.WorldShapingWandsMod.UI.CollapsibleSection.PopoutTooltip");
            }
            else if (_toggleIcon != null && _toggleIcon.GetDimensions().ToRectangle().Contains(mx, my))
            {
                // Two separate Language.GetTextValue calls (rather than a ternary on the
                // key string) so the locale lint's regex picks up BOTH key references.
                Main.hoverItemName = IsCollapsed
                    ? Language.GetTextValue("Mods.WorldShapingWandsMod.UI.CollapsibleSection.ExpandTooltip")
                    : Language.GetTextValue("Mods.WorldShapingWandsMod.UI.CollapsibleSection.CollapseTooltip");
            }
        }
    }

    private void Persist()
    {
        // v1.1 (2026-04-25 S2, DesignDoc §3.2.d / Invariant I-3): Popout-style
        // sections never persist IsCollapsed — see RestoreFromPreferences() for
        // the read-side counterpart. Old `true` entries from pre-v1.1 saves are
        // silently overwritten with no entry (and ignored on read regardless).
        if (Style == CollapseStyle.Popout) return;

        var wp = TryGetWandPlayer();
        if (wp == null) return;
        wp.CollapsedSections ??= new System.Collections.Generic.Dictionary<string, bool>();
        wp.CollapsedSections[PreferenceKey] = IsCollapsed;
    }

    private static WandPlayer TryGetWandPlayer()
    {
        var p = Main.LocalPlayer;
        if (p == null || !p.active) return null;
        return p.GetModPlayer<WandPlayer>();
    }

    private static WandUISystem ModContent_WandUISystem()
    {
        return Terraria.ModLoader.ModContent.GetInstance<WandUISystem>();
    }
}
