using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S4 2026-04-28; renamed S6 2026-04-29 Phase D from <c>UISubPanel</c>) The
/// unified WSW SubUI primitive. One class — collapses the previous Inline ↔
/// Persistent flavour split into a single `WandSubPanel` with a runtime-
/// toggleable Lock chrome button (per `WSWSubUIPrimitivePlan.md` §0).
/// Chrome is fixed: drag-handle (left), title (centre), lock toggle +
/// dismiss button (right). Body is caller-supplied and owns its own layout.
///
/// <para>
/// Lifecycle is owned by <see cref="WandUISystem"/> via
/// <c>OpenWandSubPanel</c> / <c>CloseWandSubPanel</c>. Multiple panels can be open at
/// once when nested (one panel hosting another) — the primitive supports
/// arbitrary nesting depth though current consumers stop at depth 2 (Magic
/// Wand Read SubUI → Paint child SubUI per
/// <see cref="StencilMagicWandSelectionPlan.md"/> §0.3).
/// </para>
///
/// <para>
/// <b>Lock semantics</b>: when LOCKED, the panel persists across selection
/// clicks (the consumer's <c>onSelect</c> updates state but does NOT auto-
/// close). When UNLOCKED, the first selection auto-closes the panel. Esc and
/// the dismiss button always close regardless of lock. Click-outside closes
/// only when UNLOCKED — locked panels can be dismissed only explicitly.
/// </para>
/// </summary>
public sealed class WandSubPanel : UIDraggablePanel
{
    // ── Chrome dimensions ───────────────────────────────────
    public const float ChromeHeight    = 22f;
    public const float ChromeButtonSize = 18f;
    public const float ChromePadding   = 4f;

    /// <summary>
    /// (Phase C polish, S8 2026-04-29) Body inset on all four sides between
    /// the chrome and the user-supplied body element. Defaults to
    /// <see cref="ChromePadding"/> (4f) so Panel-flavour SubPanels (Stencil
    /// Picker, Color Replace) keep their existing tight chrome. Popout-
    /// flavour factories (e.g. <see cref="WandSubPanelFactories.CreatePaintColorPopout"/>)
    /// bump this to 6f to restore the breathing room the legacy
    /// <c>CollapsedPopoutHost.HorizontalPadding</c> /
    /// <c>VerticalPadding</c> calibration shipped at 2026-04-25 S2; without
    /// the bump the swatch grid sits cramped against the chrome edge and
    /// reads visually smaller than the legacy popout did.
    /// </summary>
    public float BodyPadding { get; init; } = ChromePadding;

    /// <summary>
    /// (S14 2026-04-29; per GrayJou S14 verbatim: *"the stencil selector
    /// is 12 pixels too thin and 22 pixels too short"*) Optional extra
    /// pixels added to the SubPanel's outer Width on top of the body's
    /// natural width + <see cref="BodyPadding"/> on each side. Defaults
    /// to 0 so existing SubPanels (Color Replace, EDIT-stencil picker,
    /// PaintColor popout) keep their byte-for-byte previous footprint.
    /// Set per-consumer in the matching <see cref="WandSubPanelFactories"/>
    /// entry when a SubPanel needs extra breathing room around its body —
    /// e.g. the cross-wand ACT-ON Stencil picker, where the 5×22 cell
    /// strip otherwise reads cramped against the chrome.
    /// </summary>
    public float ExtraWidth { get; init; }

    /// <summary>
    /// (S14 2026-04-29) Companion to <see cref="ExtraWidth"/>: extra pixels
    /// added to the SubPanel's outer Height on top of
    /// <c>ChromeHeight + bodyH + BodyPadding * 2</c>. Defaults to 0. The
    /// extra space lives BELOW the body (the body keeps its natural top
    /// inset of <c>ChromeHeight + BodyPadding</c>) so the chrome bar
    /// position is invariant w.r.t. this knob — only the bottom margin
    /// grows.
    /// </summary>
    public float ExtraHeight { get; init; }

    // ── Identity / configuration ────────────────────────────────────────

    /// <summary>Stable per-(player, SubUI-identity) key for lock-state persistence.
    /// e.g. <c>"WandOfMolding.StencilSlotPicker"</c>. Caller-supplied.</summary>
    public string IdentityKey { get; init; } = "";

    /// <summary>Localisation key for the chrome title text. Pass null/empty to suppress the title.</summary>
    public string TitleKey { get; init; }

    /// <summary>The element this SubPanel was opened from. Used by click-outside detection
    /// (clicks on the host are ignored — the host owns its own toggle behaviour).</summary>
    public UIElement Host { get; init; }

    /// <summary>The parent SubPanel when this is a nested child. Null for top-level subpanels.</summary>
    public WandSubPanel Parent { get; internal set; }

    /// <summary>The currently-open child SubPanel of this one, if any. At most one child at a time.</summary>
    public WandSubPanel Child { get; internal set; }

    // ── Lock state ──────────────────────────────────────────────────────

    private bool _isLocked;
    /// <summary>True when the panel persists across selections. Toggled by the chrome lock button.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked == value) return;
            _isLocked = value;
            UpdateLockTexture();
        }
    }

    // ── Body ────────────────────────────────────────────────────────────

    /// <summary>The caller-supplied body element. Fills the panel below the chrome.
    /// Caller is responsible for layout inside the body.</summary>
    public UIElement Body { get; private set; }

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Raised when the panel closes (any cause: dismiss, Esc, click-outside, unlocked-selection,
    /// parent-close cascade). Useful for the host to clear "subpanel-open" UI state.</summary>
    public event Action OnClose;

    /// <summary>
    /// (S16 2026-04-28; GrayJou Pin/Carefree clarification side-quest) Optional
    /// predicate evaluated each frame in <see cref="DrawSelf"/>: while it
    /// returns <c>true</c> the panel renders + accepts hits; while it returns
    /// <c>false</c> the panel is HIDDEN (parked) — body stays attached, lock
    /// state stays remembered, position stays remembered, only Draw +
    /// hit-testing are gated. Defaults to <c>null</c> ⇒ "always visible"
    /// (panel persists across wand swaps until the user explicitly clicks ✕
    /// or, in the unlocked case, makes a selection / clicks outside).
    /// <para>Mirrors the popout-framework v1.3 §B model originally hosted on
    /// the (now deleted, Phase C 2026-04-29) <c>CollapsibleSection.OwnerVisibilityCheck</c>:
    /// the predicate drives a *visibility* lifecycle (show/hide) NOT an
    /// *attachment* lifecycle (open/close). Configured state survives brief
    /// held-item excursions away from the owning wand family. Only ✕ / Esc
    /// / click-outside (unlocked) close for real.</para>
    /// <para>Canonical use: ColorReplaceConfigSubPanel sets the predicate to
    /// <c>HeldItem is WandOfCoatingBase</c> so the SubUI hides the moment the
    /// player swaps off Coating (ReplaceColor is Coating-only — every other
    /// wand family has nothing to do with it) and re-shows on swap-back, with
    /// every paint pick + lock state intact.</para>
    /// </summary>
    public Func<bool> OwnerVisibilityCheck { get; set; }

    /// <summary>True iff <see cref="OwnerVisibilityCheck"/> is set AND currently returns false.</summary>
    public bool IsHidden => OwnerVisibilityCheck != null && !OwnerVisibilityCheck();

    // ── Architecture-conformant lifecycle metadata (S1 2026-04-29 Phase A) ──

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Chrome style declared per
    /// the architecture's <see cref="SubPanelType"/> vocabulary
    /// (<c>dev_notes/architecture/SubUIArchitecture.md</c> §"Lifecycle Axes").
    /// Defaults to <see cref="SubPanelType.Panel"/> — the chrome flavour the
    /// S4-S16 primitive was originally built for (Color Replace Config,
    /// Stencil Picker). Set to <see cref="SubPanelType.Popout"/> for SubPanels
    /// summoned from a <see cref="UISection"/> (collapsed-popout flavour);
    /// such SubPanels should also use
    /// <see cref="LockBehaviour.Implicit"/> so the chrome lock toggle is
    /// hidden and dismissal routes through CollapseBack.
    /// </summary>
    public SubPanelType Type { get; init; } = SubPanelType.Panel;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Architecture-prescribed
    /// owner-families bit-mask. When set to a non-<see cref="WandFamilyMask.None"/>
    /// value, <see cref="WandUISystem.UpdateUI"/> drives
    /// <see cref="UpdateOwnerFamilyVisibility"/> each frame so the SubPanel
    /// fades / parks the moment the player swaps to a non-owning wand and
    /// resurfaces on swap-back (state intact). Defaults to
    /// <see cref="WandFamilyMask.None"/>, in which case the predicate is a
    /// no-op — the legacy <see cref="OwnerVisibilityCheck"/> lambda escape
    /// hatch is still consulted (and is currently the load-bearing mechanism
    /// for the Color Replace SubPanel until Phase B's per-consumer
    /// retrofit). When BOTH are set, BOTH must agree (intersection).
    /// </summary>
    public WandFamilyMask OwnerFamilies { get; init; } = WandFamilyMask.None;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Architecture-prescribed
    /// lock declaration. Defaults to a value derived from the existing
    /// constructor's <c>defaultLocked</c> bool (set in <c>OnOpen</c>-equivalent
    /// initialisation): <c>true</c> → <see cref="LockBehaviour.DefaultLocked"/>,
    /// <c>false</c> → <see cref="LockBehaviour.DefaultUnlocked"/>. Subclasses /
    /// init-block configurers can override with <see cref="LockBehaviour.Implicit"/>
    /// for Popout-flavoured SubPanels (which suppresses the chrome lock
    /// toggle entirely). Note: the runtime bool is still
    /// <see cref="IsLocked"/>; this property declares the *initial* state +
    /// chrome-toggle visibility intent.
    /// </summary>
    public LockBehaviour LockBehaviourDecl { get; init; } = LockBehaviour.DefaultUnlocked;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Architecture-prescribed
    /// selection-dismiss policy. Consumers pick via <see cref="NotifySelection"/>
    /// after updating their state; that method already routes through
    /// <see cref="IsLocked"/> (close iff unlocked = <see cref="ChoiceBehaviour.ClosesIfUnlocked"/>).
    /// Defaults to <see cref="ChoiceBehaviour.ClosesIfUnlocked"/> to match
    /// the existing <see cref="NotifySelection"/> contract. Set to
    /// <see cref="ChoiceBehaviour.NeverCloses"/> to disable the selection-
    /// dismiss path entirely (e.g. the Color Replace SubPanel where the
    /// player picks source AND target before the SubPanel earns its
    /// dismiss).
    /// </summary>
    public ChoiceBehaviour OnChoice { get; init; } = ChoiceBehaviour.ClosesIfUnlocked;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Architecture-prescribed
    /// parent-close policy. Defaults to <see cref="ParentCloseBehaviour.StaysUpIfLocked"/>
    /// which matches the S14 2026-04-28 Esc-respects-lock contract baked
    /// into <see cref="WandUISystem.CloseAllWandSubPanels"/>(respectLock: true).
    /// Popout-flavoured SubPanels should set this to
    /// <see cref="ParentCloseBehaviour.StaysUp"/> (lock is implicit so
    /// they always survive parent-close).
    /// </summary>
    public ParentCloseBehaviour OnParentClose { get; init; } = ParentCloseBehaviour.StaysUpIfLocked;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Composite visibility
    /// predicate combining the architecture-prescribed
    /// <see cref="OwnerFamilies"/> mask with the legacy
    /// <see cref="OwnerVisibilityCheck"/> lambda. Returns true iff the
    /// SubPanel should render and accept hits this frame. When BOTH are
    /// configured, BOTH must agree (intersection); when neither is set,
    /// the SubPanel is always visible (legacy default).
    ///
    /// <para>This method is the planned Phase B replacement for
    /// <see cref="IsHidden"/>. For now <see cref="IsHidden"/> still drives
    /// <see cref="Draw"/> and <see cref="ContainsScreenPoint"/>, and the
    /// <see cref="WandUISystem"/> per-frame poller updates the legacy
    /// lambda result via this method when an <see cref="OwnerFamilies"/>
    /// mask is supplied — see
    /// <see cref="UpdateOwnerFamilyVisibility"/>.</para>
    /// </summary>
    public bool ShouldBeVisible(WandFamily heldFamily)
    {
        bool maskOk = OwnerFamilies == WandFamilyMask.None
            || OwnerFamilies.Contains(heldFamily);
        bool lambdaOk = OwnerVisibilityCheck == null || OwnerVisibilityCheck();
        return maskOk && lambdaOk;
    }

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Per-frame hook called by
    /// <see cref="WandUISystem.UpdateUI"/>. When the SubPanel declares an
    /// <see cref="OwnerFamilies"/> mask, this drives the visibility
    /// predicate against the currently-held wand family — synthesising a
    /// lambda into <see cref="OwnerVisibilityCheck"/> if the consumer
    /// didn't already supply one. The synthesised lambda is idempotent: a
    /// consumer that already wired its own <see cref="OwnerVisibilityCheck"/>
    /// (e.g. Color Replace's <c>HeldItem is WandOfCoatingBase</c>) keeps
    /// its lambda; the mask is applied as a stricter intersection via
    /// <see cref="ShouldBeVisible"/>.
    /// </summary>
    internal void UpdateOwnerFamilyVisibility(WandFamily heldFamily)
    {
        // No-op when the SubPanel doesn't declare a mask. Legacy
        // OwnerVisibilityCheck lambda (if any) continues to drive
        // visibility as before.
        if (OwnerFamilies == WandFamilyMask.None) return;

        // If a consumer already wired its own lambda, leave it alone — the
        // intersection is handled by ShouldBeVisible at predicate-eval time.
        // Otherwise, synthesise one so the existing IsHidden + Draw path
        // sees the mask as the authoritative gate.
        if (OwnerVisibilityCheck == null)
        {
            var capturedMask = OwnerFamilies;
            OwnerVisibilityCheck = () =>
            {
                var sys = ModContent.GetInstance<WandUISystem>();
                var family = sys?.LastSeenHeldFamily ?? WandFamily.Unknown;
                return capturedMask.Contains(family);
            };
        }
    }


    // ── Internals ───────────────────────────────────────────────────────

    private UIIconButton _lockBtn;
    private UIText _closeBtn;
    private UIText _titleText;
    private UIDragHandle _dragHandle;

    private static Asset<Texture2D> _lockedTex;
    private static Asset<Texture2D> _unlockedTex;

    /// <summary>
    /// Constructs a SubPanel. The body is provided by the caller and will be
    /// appended inside the chrome. Width/height are taken from the body's
    /// pixel dimensions (chrome adds <see cref="ChromeHeight"/> to height).
    /// </summary>
    /// <param name="body">The body element. Must have non-zero pixel Width/Height.</param>
    /// <param name="titleKey">Localisation key for chrome title; null/empty suppresses title.</param>
    /// <param name="defaultLocked">Initial lock state (per WSWSubUIPrimitivePlan.md §0
    /// "≤1 row + no aux → OFF; ≥2 sections OR aux toggle → ON").</param>
    /// <param name="host">The element this SubPanel anchors to. Used for click-outside
    /// detection and for screen-clip flip logic.</param>
    /// <param name="identityKey">Stable key for lock-preference persistence. Optional.</param>
    public WandSubPanel(UIElement body, string titleKey, bool defaultLocked, UIElement host, string identityKey = "")
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        TitleKey = titleKey;
        Host = host;
        IdentityKey = identityKey ?? "";
        _isLocked = defaultLocked;

        // (S1 2026-04-29 Phase A) Seed the architecture-conformant lock
        // declaration from the constructor's defaultLocked bool. Object-
        // initializer consumers can override (incl. to LockBehaviour.Implicit
        // for Popout-flavoured SubPanels). init-setters fire AFTER the
        // constructor body so the consumer's choice always wins.
        LockBehaviourDecl = defaultLocked
            ? LockBehaviour.DefaultLocked
            : LockBehaviour.DefaultUnlocked;

        BackgroundColor = WandPanelTheme.PanelChrome.PopoutBg;
        BorderColor     = WandPanelTheme.PanelChrome.PopoutBorder;
        DragPolicy      = DragPolicy.HandleOnly;

        EnsureChromeAssets();
        BuildChrome();

        // Size: width = body width + small padding; height = chrome + body height.
        // (S14 2026-04-29) ExtraWidth / ExtraHeight expand the outer chrome
        // beyond the body's natural footprint so consumers can request
        // breathing room without resizing the body itself. Note: init-
        // setters fire AFTER the constructor body, BUT the Width.Set / Height.Set
        // calls below execute as part of the constructor — meaning we read
        // the still-default 0 values here. The lazy `EnsureChromeInsetsApplied`
        // pass already does a two-step recalculate for popout-flavour
        // panels; we piggy-back on the same pattern via
        // `EnsureExtraSizeApplied` for the Extra knobs (see Update()).
        float bodyW = body.Width.Pixels  > 0f ? body.Width.Pixels  : 120f;
        float bodyH = body.Height.Pixels > 0f ? body.Height.Pixels : 32f;
        Width.Set(bodyW + BodyPadding * 2f, 0f);
        Height.Set(ChromeHeight + bodyH + BodyPadding * 2f, 0f);

        body.Left.Set(0f, 0f);
        body.Top.Set(ChromeHeight + BodyPadding, 0f);
        body.HAlign = 0.5f;
        Append(body);
    }

    private static void EnsureChromeAssets()
    {
        if (_lockedTex != null && _unlockedTex != null) return;
        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        _lockedTex   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Chromes/PanelVisibilityLocked",   AssetRequestMode.ImmediateLoad);
        _unlockedTex = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Chromes/PanelVisibilityUnlocked", AssetRequestMode.ImmediateLoad);
    }

    private void BuildChrome()
    {
        // (S13 2026-04-29) Drag handle reverted to the top-left after the
        // S12 ACT-ON locale fix made the title obviously short enough that
        // the original left-edge handle no longer overlaps it. GrayJou S13
        // verbatim: *"please revert the change where the handle was moved
        // from top left to top right, now that the locale is fixed, the
        // space makes perfect sense and the top left looks better"*. Final
        // chrome layout reads left-to-right as `[≡ drag] … [title centred]
        // … [🔒 lock] [✕ close]`.
        _dragHandle = new UIDragHandle();
        _dragHandle.Width.Set(ChromeButtonSize, 0f);
        _dragHandle.Height.Set(ChromeButtonSize, 0f);
        _dragHandle.Left.Set(ChromePadding, 0f);
        _dragHandle.Top.Set((ChromeHeight - ChromeButtonSize) * 0.5f, 0f);
        Append(_dragHandle);

        // Title (centred — placed between drag handle and right cluster)
        if (!string.IsNullOrEmpty(TitleKey))
        {
            _titleText = new UIText(Language.GetTextValue(TitleKey), WandPanelTheme.Fonts.SectionTitleScale)
            {
                IgnoresMouseInteraction = true,
                HAlign = 0.5f,
                VAlign = 0f,
            };
            _titleText.Top.Set(2f, 0f);
            Append(_titleText);
        }

        // Right cluster: [lock] [close]
        // Close button (rightmost)
        _closeBtn = new UIText("✕", WandPanelTheme.Fonts.SectionTitleScale);
        _closeBtn.Width.Set(ChromeButtonSize, 0f);
        _closeBtn.Height.Set(ChromeButtonSize, 0f);
        _closeBtn.HAlign = 1f;
        // (S9 2026-04-29; GrayJou worried-client review of S8 popout chrome) The
        // close button is a UIText "✕" glyph; the drag handle (left) and lock
        // toggle (mid) are UIIconButton instances. UIText anchors text at the
        // top of its bounding box (baseline-top), while UIIconButton centres
        // its 18×18 icon geometrically. With Top=2f literal both elements have
        // the SAME pixel Top — but the visual centre of the "✕" glyph at
        // SectionTitleScale sits ~2px above the geometric centre of an icon
        // that occupies the same box. Result: the close button reads visibly
        // higher than the drag handle. Compensating offset of +2f shifts the
        // glyph centre down to match the icon centres at y≈11 within the 22f
        // chrome strip.
        _closeBtn.Top.Set(ChromePadding, 0f); // 4f — was 2f; +2f UIText baseline correction.
        _closeBtn.Left.Set(-ChromePadding, 0f);
        _closeBtn.OnLeftClick += (_, _) => RequestDismiss(DismissCause.CloseButton);
        Append(_closeBtn);

        // Lock toggle (left of close).
        // (S10 2026-04-28; GrayJou worried-client review) Originally the lock
        // was an `IsAction=true` UIIconButton that flipped IsLocked manually
        // and called SetTexture in the setter. Visually that meant the chrome
        // bg was *always* the inactive grey — the only state-distinguishing
        // bit was the icon swap, which at chrome scale (18px) reads almost
        // identically locked vs unlocked. GrayJou: "The Chrome for Locking
        // the visibility doesn't seem to work". S10 fix: drive the lock as a
        // real radio-style toggle (`AllowDeselect=true`, `IsRadio=true`) so
        // the bg flips ActiveGreen ↔ ElementInactive on every click in lock-
        // step with the icon swap. The visual delta is now unmistakable.
        //
        // (Phase C 2026-04-29) The button is built unconditionally here
        // because object-initializer `init`-setters (which set
        // <see cref="LockBehaviourDecl"/>) run AFTER the constructor body,
        // so we cannot read the final declaration from BuildChrome. The
        // button is suppressed lazily on first <see cref="Update"/> tick
        // when <see cref="LockBehaviourDecl"/> resolves to
        // <see cref="LockBehaviour.Implicit"/> — see
        // <see cref="EnsureLockBehaviourApplied"/>.
        _lockBtn = new UIIconButton(_isLocked ? _lockedTex : _unlockedTex,
            Language.GetTextValue(_isLocked
                ? "Mods.WorldShapingWandsMod.UI.SubPanel.UnlockTooltip"
                : "Mods.WorldShapingWandsMod.UI.SubPanel.LockTooltip"),
            initialState: _isLocked)
        {
            IsRadio = true,
            AllowDeselect = true,
        };
        _lockBtn.Width.Set(ChromeButtonSize, 0f);
        _lockBtn.Height.Set(ChromeButtonSize, 0f);
        _lockBtn.HAlign = 1f;
        _lockBtn.Top.Set((ChromeHeight - ChromeButtonSize) * 0.5f, 0f);
        _lockBtn.Left.Set(-(ChromePadding + ChromeButtonSize + 2f), 0f);
        _lockBtn.OnToggled += (_, _) => { IsLocked = _lockBtn.Toggled; };
        Append(_lockBtn);
    }

    private bool _lockBehaviourApplied;

    /// <summary>
    /// (Phase C 2026-04-29) Lazy one-shot: when <see cref="LockBehaviourDecl"/>
    /// resolves to <see cref="LockBehaviour.Implicit"/> (popout-flavour SubPanels
    /// whose lock state is structural — always-locked, only ✕ dismisses), remove
    /// the chrome lock toggle so the player isn't presented with a clickable
    /// control whose semantics they cannot actually toggle. Called once from
    /// <see cref="Update"/>; subsequent ticks short-circuit on the
    /// <c>_lockBehaviourApplied</c> latch.
    /// </summary>
    private void EnsureLockBehaviourApplied()
    {
        if (_lockBehaviourApplied) return;
        _lockBehaviourApplied = true;
        if (LockBehaviourDecl == LockBehaviour.Implicit && _lockBtn != null && _lockBtn.Parent == this)
        {
            RemoveChild(_lockBtn);
            _lockBtn = null;
        }
    }

    private bool _chromeInsetsApplied;

    /// <summary>
    /// (S9 2026-04-29; GrayJou worried-client review — popout dimension
    /// regression vs v1.0.0's 236×159) Lazy one-shot two-pass sizing for
    /// Popout-flavoured SubPanels. The constructor's first pass sets
    /// <c>Width = bodyW + BodyPadding*2</c> and <c>Height = ChromeHeight + bodyH + BodyPadding*2</c>
    /// — the OUTER box. But <see cref="UIPanel"/> reserves chrome (corner
    /// textures + edge strips + internal padding) inside that outer box,
    /// shrinking the INNER drawable area by ~10px on each side and ~12px
    /// top / 13px bottom. The body therefore gets clipped / squeezed and
    /// the panel reads visibly smaller than v1.0.0 (216×134 instead of
    /// 236×159 on the PaintColor popout's 204×100 swatch grid).
    ///
    /// <para>The legacy <c>CollapsedPopoutHost</c> (deleted Phase C 2026-04-29,
    /// recovered S8 via <c>git show HEAD:</c>) ran exactly this two-pass: set
    /// tentative outer, <c>Recalculate</c>, measure <c>GetDimensions</c> vs
    /// <c>GetInnerDimensions</c>, add the inset deltas back to outer, second
    /// <c>Recalculate</c>. Restored verbatim here, gated to
    /// <see cref="SubPanelType.Popout"/> so the existing Panel-flavour SubPanels
    /// (Stencil Picker, Color Replace) keep their tighter footprint unchanged.
    /// init-setters fire after the constructor body so we can't read
    /// <see cref="Type"/> from there — same lazy-on-first-Update pattern as
    /// <see cref="EnsureLockBehaviourApplied"/>.</para>
    /// </summary>
    private void EnsureChromeInsetsApplied()
    {
        if (_chromeInsetsApplied) return;
        _chromeInsetsApplied = true;
        if (Type != SubPanelType.Popout) return;

        // First Recalculate has been invoked by the parent UIElement tree by
        // the time Update fires. Read inner vs outer dims to discover the
        // chrome inset on each edge.
        Recalculate();
        var outer = GetDimensions();
        var inner = GetInnerDimensions();
        float insetL = inner.X - outer.X;
        float insetT = inner.Y - outer.Y;
        float insetR = (outer.X + outer.Width)  - (inner.X + inner.Width);
        float insetB = (outer.Y + outer.Height) - (inner.Y + inner.Height);

        // Defensive: if Terraria/tModLoader ever refactor UIPanel chrome away
        // (insets become zero) the existing tentative size is already correct.
        if (insetL + insetR + insetT + insetB <= 0.5f) return;

        Width.Set (Width.Pixels  + insetL + insetR, 0f);
        Height.Set(Height.Pixels + insetT + insetB, 0f);
        Recalculate();
    }

    private bool _extraSizeApplied;

    /// <summary>
    /// (S14 2026-04-29) Lazy one-shot mirror of
    /// <see cref="EnsureChromeInsetsApplied"/> for the
    /// <see cref="ExtraWidth"/> / <see cref="ExtraHeight"/> init knobs.
    /// init-setters fire after the constructor body, so the constructor's
    /// Width.Set / Height.Set calls always read the default 0 values. This
    /// hook runs on the first <see cref="Update"/> tick (idempotent
    /// thereafter) to add the requested extras to the outer chrome and
    /// re-anchor the panel so it stays positioned around its host element.
    /// </summary>
    private void EnsureExtraSizeApplied()
    {
        if (_extraSizeApplied) return;
        _extraSizeApplied = true;
        if (ExtraWidth <= 0.5f && ExtraHeight <= 0.5f) return;
        Width.Set(Width.Pixels + ExtraWidth, 0f);
        Height.Set(Height.Pixels + ExtraHeight, 0f);
        Recalculate();
        // Re-anchor so the new (larger) panel still sits flush with its
        // host element instead of leaving a gap at one edge.
        AnchorToHost();
    }

    public override void Update(GameTime gameTime)
    {
        EnsureLockBehaviourApplied();
        EnsureChromeInsetsApplied();
        EnsureExtraSizeApplied();
        TickVisibilityFade();
        base.Update(gameTime);
    }

    // ── Visibility fade (S8 2026-04-29 — ported from deleted CollapsedPopoutHost) ──

    /// <summary>Fade duration in frames. 12 @ 60fps = 200ms. Matches the
    /// legacy <c>CollapsedPopoutHost.FadeFrames</c> constant verbatim so the
    /// player-perceived ease is byte-for-byte identical to pre-Phase-C.</summary>
    public const int FadeFrames = 12;

    private float _visibilityAlpha = 1f;
    private float _visibilityTarget = 1f;

    /// <summary>Current per-frame fade alpha in <c>[0..1]</c>. Drawn into
    /// <see cref="UIDraggablePanel.DrawAlpha"/> in <see cref="Draw"/>.</summary>
    public float VisibilityAlpha => _visibilityAlpha;

    /// <summary>True iff the panel is currently visible OR easing toward visible.
    /// A fully-faded panel is still ticked one extra frame so it can ease back in.</summary>
    public bool IsFadingOrVisible => _visibilityAlpha > 0.001f || _visibilityTarget > 0.001f;

    private void TickVisibilityFade()
    {
        // Target derives from the visibility predicate: 0 when parked, 1 when live.
        _visibilityTarget = IsHidden ? 0f : 1f;

        if (System.Math.Abs(_visibilityAlpha - _visibilityTarget) < 0.001f)
        {
            _visibilityAlpha = _visibilityTarget;
            return;
        }

        float step = 1f / FadeFrames;
        if (_visibilityAlpha < _visibilityTarget)
            _visibilityAlpha = System.Math.Min(_visibilityTarget, _visibilityAlpha + step);
        else
            _visibilityAlpha = System.Math.Max(_visibilityTarget, _visibilityAlpha - step);
    }

    private void UpdateLockTexture()
    {
        if (_lockBtn == null) return;
        _lockBtn.SetTexture(_isLocked ? _lockedTex : _unlockedTex);
        // Keep the visible Toggled state in lock-step with IsLocked even when
        // IsLocked is set programmatically (not just by user click).
        if (_lockBtn.Toggled != _isLocked) _lockBtn.Toggled = _isLocked;
        // (S12 2026-04-28; GrayJou worried-client review of S10) The lock
        // tooltip now describes the CURRENT state — not the action you'd
        // perform by clicking. Per S12 verbatim: *“the Lock Chrome Icon
        // changes on click (toggles on and off), but text doesn't change”*.
        // Locked → “Locked open / click to unlock” via UnlockTooltip;
        // Unlocked → “Unlocked / click to lock open” via LockTooltip.
        _lockBtn.HoverText = Language.GetTextValue(_isLocked
            ? "Mods.WorldShapingWandsMod.UI.SubPanel.UnlockTooltip"
            : "Mods.WorldShapingWandsMod.UI.SubPanel.LockTooltip");
    }

    /// <summary>
    /// Anchors this SubPanel near its <see cref="Host"/>. Default placement is
    /// directly below the host, left-aligned. Flips upward / right-aligned if
    /// it would clip off-screen. Call after the panel has been activated and
    /// has stable Width/Height.
    /// </summary>
    public void AnchorToHost()
    {
        if (Host == null) return;
        var hostDims = Host.GetDimensions();
        float w = Width.Pixels;
        float h = Height.Pixels;

        float x = hostDims.X;
        float y = hostDims.Y + hostDims.Height + 4f;

        // Vertical flip: if it'd clip below, anchor above the host.
        if (y + h > Main.screenHeight - 8f)
            y = hostDims.Y - h - 4f;

        // Horizontal flip: if it'd clip right, right-align with host's right edge.
        if (x + w > Main.screenWidth - 8f)
            x = hostDims.X + hostDims.Width - w;

        // Clamp to screen with margin.
        x = MathHelper.Clamp(x, 8f, Math.Max(8f, Main.screenWidth  - w - 8f));
        y = MathHelper.Clamp(y, 8f, Math.Max(8f, Main.screenHeight - h - 8f));

        HAlign = 0f;
        VAlign = 0f;
        Left.Set(x, 0f);
        Top.Set(y, 0f);
        Recalculate();
    }

    /// <summary>True if the screen point lies inside this panel's drawn rectangle (post-anchor).</summary>
    public bool ContainsScreenPoint(Vector2 screen)
    {
        // (S16 OwnerVisibilityCheck) Hidden / fully-faded panels accept no hits — mouse
        // events fall through to whatever sits below. Mirrors the legacy
        // popout-host's hit-test gating for parked popouts. Partially-faded
        // panels (mid-ease) DO accept hits so the player can grab them mid-
        // fade-in without waiting 200ms.
        if (_visibilityAlpha <= 0.001f) return false;
        return ContainsPoint(screen);
    }

    /// <summary>True if the screen point lies inside this panel's <see cref="Host"/> element (or null host).</summary>
    public bool HostContainsScreenPoint(Vector2 screen)
    {
        return Host != null && Host.GetDimensions().ToRectangle().Contains((int)screen.X, (int)screen.Y);
    }

    // ── Selection / dismiss API used by consumers + the host ────────────

    /// <summary>
    /// Called by the consumer's per-cell click handler AFTER the consumer has
    /// updated its own state. Closes the panel iff <see cref="IsLocked"/> is
    /// false (the standard "pick → dismiss" semantic). Locked panels stay open
    /// so the player can A/B-test multiple picks.
    /// </summary>
    public void NotifySelection()
    {
        if (!IsLocked)
            RequestDismiss(DismissCause.Selection);
    }

    internal enum DismissCause { Selection, Escape, ClickOutside, CloseButton, ParentClose }

    internal void RequestDismiss(DismissCause cause)
    {
        var sys = ModContent.GetInstance<WandUISystem>();
        sys?.CloseWandSubPanel(this);
    }

    /// <summary>Internal — invoked by <see cref="WandUISystem.CloseWandSubPanel"/> after detachment.</summary>
    internal void RaiseClose()
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        OnClose?.Invoke();
    }

    /// <summary>
    /// (S16 OwnerVisibilityCheck; fade restored S8 2026-04-29) Skip the
    /// entire draw + child-draw pass when the popout has fully faded out.
    /// Body remains attached and lock state remains intact — re-rendering
    /// resumes the moment the predicate flips back to true and the alpha
    /// eases back up via <see cref="TickVisibilityFade"/>. Mid-ease frames
    /// render at <see cref="_visibilityAlpha"/> via
    /// <see cref="UIDraggablePanel.DrawAlpha"/> so the player sees a smooth
    /// 200ms fade rather than a hard cut (matches the legacy
    /// <c>CollapsedPopoutHost.ApplyFadeAlpha</c> contract verbatim).
    /// </summary>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_visibilityAlpha <= 0.001f) return;
        DrawAlpha = _visibilityAlpha;
        base.Draw(spriteBatch);
    }
}
