using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S1 2026-04-29 — SubUI Architecture Phase A; sole substrate after Phase C
/// 2026-04-29) The architecture-prescribed in-panel section primitive
/// (<c>dev_notes/architecture/SubUIArchitecture.md</c> §"Primitive 2: UISection").
/// A section inside a WandPanel with two orthogonal lifecycle axes:
/// <see cref="Foldable"/> (collapse-in-place) and <see cref="CanBePoppedOut"/>
/// (detach into a floating <see cref="WandSubPanel"/>).
///
/// <para>
/// <b>History</b>: this primitive replaced the legacy <c>CollapsibleSection</c>
/// + <c>CollapsedPopoutHost</c> substrate in Phase C 2026-04-29. The legacy
/// types are deleted; the only live consumer (<see cref="PaintColorSection"/>)
/// is a popout-flavour <see cref="UISection"/> subclass.
/// </para>
///
/// <para>
/// <b>State persistence</b>: fold state is round-tripped through
/// <see cref="Players.WandPlayer.CollapsedSections"/> keyed on
/// <see cref="PreferenceKey"/>. PoppedOut state is NOT persisted (matching
/// the v1.1 2026-04-25 S2 invariant I-3 baked into the legacy popout
/// substrate: the popout open/closed state defaults to closed on every world
/// enter; only the popout POSITION survives via
/// <see cref="Players.WandPlayer.PopoutPositions"/>).
/// </para>
/// </summary>
public abstract class UISection : UIElement
{
    /// <summary>Header bar height in pixels. 22f matches the legacy
    /// <c>CollapsibleSection</c> visual (deleted Phase C 2026-04-29) so
    /// pre-existing layout maths in consumer panels needs no recomputation.</summary>
    public const float HeaderHeightConst = 22f;
    private const float HeaderHeight = HeaderHeightConst;
    private const float IconSize     = 16f;

    // ── Lifecycle Configuration (Override in subclass) ──────────────────

    /// <summary>Whether this section can be folded (collapsed in place).
    /// When true, a ▾/▸ chevron button is rendered in the header;
    /// clicking it toggles <see cref="IsFolded"/>.</summary>
    public abstract bool Foldable { get; }

    /// <summary>Whether this section can be popped out into a
    /// <see cref="WandSubPanel"/>. When true, a ⧧ button is rendered in the
    /// header; clicking it summons the popout via
    /// <see cref="CreatePopoutWandSubPanel"/>.</summary>
    public abstract bool CanBePoppedOut { get; }

    /// <summary>Localisation key for the header title text.
    /// Resolved via <see cref="Language.GetTextValue(string)"/>.</summary>
    public abstract string TitleKey { get; }

    /// <summary>Stable identifier for the per-section persisted fold bit.
    /// Form: <c>"PanelName.SectionName"</c>.</summary>
    public abstract string PreferenceKey { get; }

    // ── Runtime State ───────────────────────────────────────────────────

    /// <summary>Current fold state. Use <see cref="Fold"/> / <see cref="Unfold"/>
    /// to mutate. Only relevant when <see cref="Foldable"/> is true.</summary>
    public bool IsFolded { get; private set; }

    /// <summary>True when the section's body is currently living in a
    /// floating <see cref="WandSubPanel"/> rather than this section. Use
    /// <see cref="PopOut"/> / <see cref="HandlePopoutCollapsed"/> to
    /// transition. Only relevant when <see cref="CanBePoppedOut"/> is true.</summary>
    public bool IsPoppedOut { get; private set; }

    /// <summary>Reference to the active popout <see cref="WandSubPanel"/>
    /// when <see cref="IsPoppedOut"/> is true; null otherwise.</summary>
    public WandSubPanel ActivePopout { get; private set; }

    // ── Internal chrome ────────────────────────────────────────────────

    private UIElement _headerBar;
    private UIText _titleText;
    private UIText _foldIcon;     // ▾/▸ — rendered iff Foldable
    private UIText _popoutIcon;   // ⧓   — rendered iff CanBePoppedOut

    /// <summary>
    /// (Phase C 2026-04-29) The internal wrapper element that holds whatever
    /// <see cref="BuildContent"/> appended. Exposed as <c>protected</c> so
    /// popout-flavour subclasses (e.g. <c>PaintColorSection</c>) can re-parent
    /// their actual content elements out of and back into this container as
    /// the section transitions Normal ⇄ PoppedOut. Subclasses MUST NOT
    /// destroy / replace this reference — only manipulate its children.
    /// </summary>
    protected UIElement _bodyContainer;

    /// <summary>Raised after a fold-state change. Subscribers typically
    /// call <c>UIList.Recalculate()</c> on the parent.</summary>
    public event Action<UISection> OnStateChanged;

    protected UISection()
    {
        Width.Set(0f, 1f);
        Height.Set(HeaderHeight, 0f);
    }

    /// <summary>
    /// Architecture stub <c>Build()</c> entry point. Idempotent — call once
    /// after construction (typically from the parent panel's
    /// <c>OnInitialize</c>). Sets up the header, the body container, and
    /// renders <see cref="BuildContent"/> + the optional fold/popout
    /// buttons per the architecture doc's §"Build Logic" pseudocode.
    /// </summary>
    public void Build()
    {
        BuildHeader();

        _bodyContainer = new UIElement();
        _bodyContainer.Top.Set(HeaderHeight, 0f);
        _bodyContainer.Width.Set(0f, 1f);

        if (IsPoppedOut)
        {
            // Body lives in the popout SubPanel; nothing to do here. The
            // section reduces to its header bar (which still hosts the
            // popout-button so the player can dismiss the popout back into
            // place).
        }
        else if (IsFolded)
        {
            // Folded: header-only render. UpdateLayout collapses the body
            // container to zero height.
        }
        else
        {
            BuildContent(_bodyContainer);
            Append(_bodyContainer);
        }

        UpdateLayout();
    }

    /// <summary>
    /// Build the actual section content into the supplied container. Called
    /// when the section is in its Normal (non-folded, non-popped-out) state.
    /// Subclasses populate <paramref name="container"/> with the body
    /// elements and SHOULD set <c>container.Height.Pixels</c> so the
    /// section can size itself correctly.
    /// </summary>
    protected abstract void BuildContent(UIElement container);

    /// <summary>
    /// Factory for the popout <see cref="WandSubPanel"/> when the user
    /// clicks the ⧧ button. Required iff <see cref="CanBePoppedOut"/> is
    /// true; the default implementation throws to surface mis-declarations
    /// at runtime.
    /// </summary>
    protected virtual WandSubPanel CreatePopoutWandSubPanel()
        => throw new InvalidOperationException(
            $"{GetType().Name} declares CanBePoppedOut=true but does not override CreatePopoutWandSubPanel().");

    private void BuildHeader()
    {
        if (_headerBar != null) return;

        _headerBar = new UIElement();
        _headerBar.Width.Set(0f, 1f);
        _headerBar.Height.Set(HeaderHeight, 0f);
        Append(_headerBar);

        _titleText = new UIText(Language.GetTextValue(TitleKey), WandPanelTheme.Fonts.SectionTitleScale)
        {
            HAlign = 0f,
            VAlign = 0.5f,
            IgnoresMouseInteraction = true,
        };
        _titleText.Left.Set(2f, 0f);
        _headerBar.Append(_titleText);

        // Right-aligned icon cluster — popout icon outermost (right edge),
        // fold icon left of it, mirroring CollapsibleSection's layout
        // convention so the visual delta during Phase C migration is zero.
        float rightOffset = 0f;
        if (CanBePoppedOut)
        {
            _popoutIcon = new UIText("⧓", WandPanelTheme.Fonts.SectionTitleScale)
            {
                HAlign = 1f,
                VAlign = 0.5f,
                IgnoresMouseInteraction = true,
            };
            _popoutIcon.Width.Set(IconSize, 0f);
            _popoutIcon.Left.Set(-rightOffset, 0f);
            _headerBar.Append(_popoutIcon);
            rightOffset += IconSize + 2f;
        }
        if (Foldable)
        {
            _foldIcon = new UIText(IsFolded ? "▸" : "▾", WandPanelTheme.Fonts.SectionTitleScale)
            {
                HAlign = 1f,
                VAlign = 0.5f,
                IgnoresMouseInteraction = true,
            };
            _foldIcon.Width.Set(IconSize, 0f);
            _foldIcon.Left.Set(-rightOffset, 0f);
            _headerBar.Append(_foldIcon);
        }

        _headerBar.OnLeftClick += HeaderClicked;
    }

    private void HeaderClicked(UIMouseEvent evt, UIElement listening)
    {
        // Hit-test corner icons explicitly because they declare
        // IgnoresMouseInteraction=true (so the header bar gets the click).
        int mx = (int)evt.MousePosition.X;
        int my = (int)evt.MousePosition.Y;

        if (CanBePoppedOut && _popoutIcon != null
            && _popoutIcon.GetDimensions().ToRectangle().Contains(mx, my))
        {
            if (IsPoppedOut)
            {
                if (ActivePopout != null)
                {
                    var sys = ModContent.GetInstance<WandUISystem>();
                    sys?.CloseWandSubPanel(ActivePopout);
                }
                else
                {
                    HandlePopoutCollapsed();
                }
            }
            else PopOut();
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }

        if (Foldable && _foldIcon != null
            && _foldIcon.GetDimensions().ToRectangle().Contains(mx, my))
        {
            if (IsFolded) Unfold();
            else Fold();
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }
    }

    // ── State Transitions ───────────────────────────────────────────────

    /// <summary>Fold the section (collapse in place). No-op if not
    /// <see cref="Foldable"/> or if currently <see cref="IsPoppedOut"/>.</summary>
    public virtual void Fold()
    {
        if (!Foldable || IsPoppedOut || IsFolded) return;
        IsFolded = true;
        if (_bodyContainer?.Parent == this) RemoveChild(_bodyContainer);
        UpdateFoldIcon();
        UpdateLayout();
        Persist();
        OnStateChanged?.Invoke(this);
    }

    /// <summary>Unfold the section (expand in place). No-op if not currently folded.</summary>
    public virtual void Unfold()
    {
        if (!IsFolded) return;
        IsFolded = false;
        if (_bodyContainer != null && _bodyContainer.Parent != this)
            Append(_bodyContainer);
        UpdateFoldIcon();
        UpdateLayout();
        Persist();
        OnStateChanged?.Invoke(this);
    }

    /// <summary>Pop out into a <see cref="WandSubPanel"/>. Allowed from
    /// Normal or Folded state. No-op if not <see cref="CanBePoppedOut"/>.</summary>
    public virtual void PopOut()
    {
        if (!CanBePoppedOut || IsPoppedOut) return;

        ActivePopout = CreatePopoutWandSubPanel();
        if (ActivePopout == null) return;
        IsPoppedOut = true;

        if (_bodyContainer?.Parent == this) RemoveChild(_bodyContainer);

        var sys = ModContent.GetInstance<WandUISystem>();
        sys?.OpenWandSubPanel(ActivePopout);

        UpdateLayout();
        OnStateChanged?.Invoke(this);
    }

    /// <summary>Called by the popout SubPanel when it dismisses via
    /// <see cref="CloseBehaviour.CollapseBack"/>. Returns the section to its
    /// Normal state (per the architecture's state-transitions table:
    /// "PoppedOut → Normal").</summary>
    public virtual void HandlePopoutCollapsed()
    {
        if (!IsPoppedOut) return;
        IsPoppedOut = false;
        ActivePopout = null;

        // Per architecture: PoppedOut → Normal (not Folded), even if was
        // folded before popout. The state-transitions table prohibits the
        // PoppedOut → Folded direct path explicitly.
        IsFolded = false;
        if (_bodyContainer != null && _bodyContainer.Parent != this)
            Append(_bodyContainer);
        UpdateFoldIcon();
        UpdateLayout();
        OnStateChanged?.Invoke(this);
    }

    // ── Persistence ─────────────────────────────────────────────────────

    /// <summary>Read persisted fold state from the local
    /// <see cref="Players.WandPlayer.CollapsedSections"/> dict. Call once
    /// after <see cref="Build"/> in the parent panel's
    /// <c>OnInitialize</c>.</summary>
    public void RestoreFromPreferences()
    {
        if (!Foldable) return;
        var wp = TryGetWandPlayer();
        if (wp?.CollapsedSections == null) return;
        if (wp.CollapsedSections.TryGetValue(PreferenceKey, out bool stored) && stored)
        {
            IsFolded = true;
            if (_bodyContainer?.Parent == this) RemoveChild(_bodyContainer);
            UpdateFoldIcon();
            UpdateLayout();
        }
    }

    private void Persist()
    {
        if (!Foldable) return;
        var wp = TryGetWandPlayer();
        if (wp == null) return;
        wp.CollapsedSections ??= new System.Collections.Generic.Dictionary<string, bool>();
        wp.CollapsedSections[PreferenceKey] = IsFolded;
    }

    private static Players.WandPlayer TryGetWandPlayer()
    {
        var p = Main.LocalPlayer;
        if (p == null || !p.active) return null;
        return p.GetModPlayer<Players.WandPlayer>();
    }

    // ── Layout ──────────────────────────────────────────────────────────

    private void UpdateFoldIcon()
    {
        if (_foldIcon == null) return;
        _foldIcon.SetText(IsFolded ? "▸" : "▾");
    }

    private void UpdateLayout()
    {
        float bodyH = 0f;
        if (!IsFolded && !IsPoppedOut && _bodyContainer != null && _bodyContainer.Parent == this)
            bodyH = _bodyContainer.Height.Pixels;
        Height.Set(HeaderHeight + bodyH, 0f);
        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        // Subtle 1-px underline beneath header (matches UISectionTitle and
        // CollapsibleSection convention).
        CalculatedStyle dims = GetDimensions();
        var lineRect = new Rectangle((int)dims.X, (int)(dims.Y + HeaderHeight - 1), (int)dims.Width, 1);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineRect, Color.White * 0.25f);
    }
}
