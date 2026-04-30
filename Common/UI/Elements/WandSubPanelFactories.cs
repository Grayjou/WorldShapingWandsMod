using System;
using Terraria;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.UI.Elements.Builders;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S5 2026-04-29 — SubUI Architecture Phase B; renamed S7 2026-04-29 G-34
/// from <c>SubPanelFactories</c> for namespace clarity — the WSW prefix
/// matches <see cref="WandSubPanel"/> + <see cref="WandSubPanelHost"/>
/// + <see cref="WandUISystem"/>) The single registry of SubUI factory
/// entry-points. Per the C-2 ratification (Browser Claude verdict on
/// <c>LettersSent/WSW 2026-04-29 Letter #1.md</c>) WSW's house style for
/// SubUI construction is <b>factory-per-instance</b>: each SubUI is built
/// by a static factory method here, NOT by a dedicated subclass of
/// <see cref="WandSubPanel"/>. Subclassing is reserved for the (currently
/// empty) case where a SubUI needs per-instance private state interacting
/// across multiple methods, 3+ interacting protected overrides, or
/// host-level type-system branching — see
/// <c>dev_notes/architecture/SubUIArchitecture.md</c> §"When to escape
/// into a subclass".
///
/// <para><b>Companion-builder rule</b>: if a SubUI's child-element
/// wiring exceeds ~80 lines, it lives in a static
/// <c>XBuilder.BuildBody(...)</c> class under
/// <c>Common/UI/Elements/Builders/</c>. The factory method here then
/// collapses to:
/// (a) call the builder for the body (or accept it from the caller for
///     popout-flavour entries that wrap an externally-owned body);
/// (b) wrap in a <see cref="WandSubPanel"/>;
/// (c) declare the architecture-prescribed lifecycle metadata
///     (<see cref="WandSubPanel.Type"/>, <see cref="WandSubPanel.OwnerFamilies"/>,
///     <see cref="WandSubPanel.LockBehaviourDecl"/>,
///     <see cref="WandSubPanel.OnChoice"/>,
///     <see cref="WandSubPanel.OnParentClose"/>);
/// (d) wire any per-consumer escape-hatch lambdas (e.g. the β-model
///     <see cref="WandSubPanel.OwnerVisibilityCheck"/> for Color Replace
///     and the PaintColor popout).
/// All currently-shipped SubUIs (Stencil Picker ~100 LOC, Color Replace
/// ~250 LOC of body wiring) qualified for the builder split; PaintColor
/// did NOT — its body is the swatch grid built by
/// <c>CoatingPaintColorGrid.Build</c> elsewhere and merely re-parented
/// into the popout chrome (zero builder LOC needed).</para>
///
/// <para>Consumers open the returned panel via
/// <see cref="WandUISystem.OpenWandSubPanel"/> +
/// <see cref="WandSubPanel.AnchorToHost"/>.</para>
/// </summary>
public static class WandSubPanelFactories
{
    /// <summary>
    /// Builds (but does NOT open) a fresh Stencil Slot picker SubPanel
    /// anchored to the given host element (typically the Mold cell of
    /// stencil-wand settings panels).
    ///
    /// <para>Lifecycle metadata: <c>Type=Panel</c>,
    /// <c>OwnerFamilies=Molding|Delimitation</c>
    /// (the two stencil-wand families per
    /// <c>MultipleStencilsPlan.md</c> §0.2),
    /// <c>LockBehaviourDecl=DefaultUnlocked</c>
    /// (single row + no aux toggle → "open → pick → close" flow),
    /// <c>OnChoice=ClosesIfUnlocked</c>,
    /// <c>OnParentClose=StaysUpIfLocked</c>.</para>
    /// </summary>
    public static WandSubPanel CreateStencilPicker(UIElement host)
    {
        // Build first, then wrap: the builder returns the body + a re-sync
        // action; the panel reference is captured once constructed so the
        // builder's onSlotPicked callback can route through NotifySelection.
        WandSubPanel panelRef = null;
        var body = StencilPickerBuilder.BuildBody(
            onSlotPicked: () => panelRef?.NotifySelection(),
            out var resyncToggled);

        var panel = new WandSubPanel(
            body: body,
            titleKey: StencilPickerBuilder.TitleKey,
            defaultLocked: false, // ≤1 row + no aux → OFF (WSWSubUIPrimitivePlan §0)
            host: host,
            identityKey: StencilPickerBuilder.IdentityKey)
        {
            Type              = SubPanelType.Panel,
            OwnerFamilies     = WandFamilyMask.Molding | WandFamilyMask.Delimitation,
            LockBehaviourDecl = LockBehaviour.DefaultUnlocked,
            OnChoice          = ChoiceBehaviour.ClosesIfUnlocked,
            OnParentClose     = ParentCloseBehaviour.StaysUpIfLocked,
        };
        panelRef = panel;

        // Sync the toggled visual to the player's current ActiveStencilSlot.
        resyncToggled();

        return panel;
    }

    /// <summary>
    /// (S11 2026-04-29 \u2014 Bug 3 fix; <c>StencilEditVsActOn.md</c> \u00a73)
    /// Builds (but does NOT open) the ACT-ON Stencil Slot picker SubPanel
    /// anchored to a host element (the Mold cell of any wand panel \u2014 every
    /// WSW wand gets this, not just Wand of Molding).
    ///
    /// <para><b>Distinct from <see cref="CreateStencilPicker"/></b>: that
    /// factory writes <see cref="MoldingWandPlayer.ActiveStencilSlot"/> (=
    /// Wand-of-Molding's EDIT cursor); this one writes
    /// <see cref="MoldingWandPlayer.ActOnStencilSlot"/> (= every wand's
    /// Mold-Shape source). They are independent indices on the same player
    /// per <c>StencilEditVsActOn.md</c> \u00a71.</para>
    ///
    /// <para>Lifecycle metadata: <c>Type=Panel</c>,
    /// <c>OwnerFamilies=None</c> (every WSW family can summon this picker
    /// because every WSW wand has a Mold cell), with
    /// <c>OwnerVisibilityCheck = held item is BaseCyclingWand</c> escape-
    /// hatch so the locked picker hides while the player holds a non-WSW
    /// item and resurfaces on swap-back, state intact (the user-requested
    /// "hides when item is not a World Shaping Wand" semantic).
    /// <c>LockBehaviourDecl=DefaultLocked</c> because cross-wand persistence
    /// is the whole point \u2014 the bear stays selected as the player swaps
    /// from Dismantling to Building to Wiring.
    /// <c>OnChoice=ClosesIfUnlocked</c>,
    /// <c>OnParentClose=StaysUpIfLocked</c>.</para>
    /// </summary>
    public static WandSubPanel CreateStencilActOnPicker(UIElement host)
    {
        WandSubPanel panelRef = null;
        var body = StencilPickerBuilder.BuildBody(
            kind: StencilPickerBuilder.PickerKind.ActOn,
            onSlotPicked: () => panelRef?.NotifySelection(),
            out var resyncToggled);

        var panel = new WandSubPanel(
            body: body,
            titleKey: StencilPickerBuilder.ActOnTitleKey,
            defaultLocked: true, // ON \u2014 cross-wand persistence is the goal
            host: host,
            identityKey: StencilPickerBuilder.ActOnIdentityKey)
        {
            Type              = SubPanelType.Panel,
            // OwnerFamilies stays None: every WSW wand can host this picker;
            // visibility is gated by the held-item lambda below instead.
            OwnerFamilies     = WandFamilyMask.None,
            LockBehaviourDecl = LockBehaviour.DefaultLocked,
            OnChoice          = ChoiceBehaviour.ClosesIfUnlocked,
            OnParentClose     = ParentCloseBehaviour.StaysUpIfLocked,
            // (S14 2026-04-29; per GrayJou S14 verbatim: *"the stencil
            // selector is 12 pixels too thin and 22 pixels too short"*)
            // Add 12 px of horizontal breathing room and 22 px below the
            // body. Lives here on the ACT-ON factory only \u2014 the EDIT
            // picker (`CreateStencilPicker`) keeps its tight existing
            // footprint because GrayJou's complaint is specifically about
            // the cross-wand selector users see most often.
            ExtraWidth        = 12f,
            ExtraHeight       = 22f,
            OwnerVisibilityCheck = () =>
                Main.LocalPlayer?.HeldItem?.ModItem is BaseCyclingWand,
        };
        panelRef = panel;

        resyncToggled();
        return panel;
    }

    /// <summary>
    /// Builds (but does NOT open) a fresh Color Replace configuration
    /// SubPanel anchored to the given host element (typically the
    /// <c>UIColorReplaceButton</c>).
    ///
    /// <para>Lifecycle metadata: <c>Type=Panel</c>,
    /// <c>OwnerFamilies=Coating</c>
    /// (ReplaceColor is Coating-exclusive — no other family in the mod
    /// can fire a colour-replace operation),
    /// <c>LockBehaviourDecl=DefaultLocked</c>
    /// (channel + source + target ⇒ ≥2 sections → ON per
    /// <c>WSWSubUIPrimitivePlan.md</c> §0),
    /// <c>OnChoice=NeverCloses</c>
    /// (the player picks source AND target before the SubPanel earns its
    /// dismiss — auto-closing on first pick would force re-right-clicking),
    /// <c>OnParentClose=StaysUpIfLocked</c>.</para>
    ///
    /// <para><b>Why the lambda stays</b> (S4 C-7 + C-10 β ratification):
    /// the <c>OwnerFamilies=Coating</c> mask is necessary but not
    /// sufficient. Under the β model the panel hosts a single shared
    /// ColorReplace surface; the visibility gate for a Coating wand
    /// depends on the per-wand <c>PaintSource</c> config (which a static
    /// mask cannot express). Currently every Coating wand qualifies, so
    /// the lambda reads <c>HeldItem is WandOfCoatingBase</c>; if a future
    /// Coating wand opts out of ColorReplace, the lambda is the
    /// composable place to encode the per-wand predicate. The mask is the
    /// declarative discoverability promise; the lambda is the expressivity
    /// escape valve. Both exist by design (see arch doc §"Predicate
    /// Discipline" + §"Section Instance Ownership").</para>
    /// </summary>
    /// <param name="host">The action button this picker anchors to.</param>
    /// <param name="onChanged">Callback fired after each swatch click /
    /// channel change so the host can refresh its baked icon + tooltip.
    /// Receives the (sourcePaint, targetPaint) tuple. Never null — host
    /// must subscribe.</param>
    public static WandSubPanel CreateColorReplaceConfig(
        UIElement host,
        Action<byte, byte> onChanged)
    {
        if (onChanged is null) throw new ArgumentNullException(nameof(onChanged));

        var body = ColorReplaceConfigBuilder.BuildBody(onChanged);

        var panel = new WandSubPanel(
            body: body,
            titleKey: ColorReplaceConfigBuilder.TitleKey,
            defaultLocked: true, // ≥2 sections → ON
            host: host,
            identityKey: ColorReplaceConfigBuilder.IdentityKey)
        {
            Type              = SubPanelType.Panel,
            OwnerFamilies     = WandFamilyMask.Coating,
            LockBehaviourDecl = LockBehaviour.DefaultLocked,
            OnChoice          = ChoiceBehaviour.NeverCloses,
            OnParentClose     = ParentCloseBehaviour.StaysUpIfLocked,
        };

        // β-model visibility gate. The mask above declares "Coating wands
        // only"; this lambda is the composable hook for any future
        // per-wand opt-out (currently every Coating wand qualifies, so the
        // lambda is structurally identical to the mask check, but it stays
        // in place per the C-7 + C-10 β discipline so the gate remains
        // expressible when WandOfCoating subclasses diverge on PaintSource).
        panel.OwnerVisibilityCheck = static () =>
            Main.LocalPlayer?.HeldItem?.ModItem is WandOfCoatingBase;

        return panel;
    }

    /// <summary>
    /// (Phase C 2026-04-29) Builds (but does NOT open) the PaintColor
    /// popout — the floating chrome that hosts the swatch grid when the
    /// <see cref="PaintColorSection"/> in the Coating panel is popped
    /// out via the ⧓ icon. Replaces the legacy
    /// <c>CollapsedPopoutHost</c> substrate.
    ///
    /// <para>Lifecycle metadata: <c>Type=Popout</c>,
    /// <c>OwnerFamilies=Coating|Building|Replacement</c>
    /// (the three paint-consuming wand families per popout-framework v1.3 §B),
    /// <c>LockBehaviourDecl=Implicit</c>
    /// (no chrome lock toggle; popout-flavour SubPanels are structurally
    /// always-locked — only ✕ dismisses, never click-outside / Esc),
    /// <c>OnChoice=NeverCloses</c>
    /// (selecting a swatch updates state but never closes — matches the
    /// legacy popout's "swatch click ≠ dismissal" semantics so the player
    /// can A/B-test colours freely),
    /// <c>OnParentClose=StaysUp</c>
    /// (closing the parent CoatingPanel — Esc, X-button, click-outside,
    /// wand swap — leaves the popout floating; matches the legacy
    /// CollapsedPopoutHost survival contract).</para>
    ///
    /// <para><b>The lambda</b> (S4 v1.3 §B): mirrors the legacy
    /// <c>OwnerVisibilityCheck</c> on the deleted CollapsibleSection —
    /// when the player swaps to a wand outside the three paint-consuming
    /// families, the popout PARKS (body + position + lock state preserved,
    /// only Draw + hit-testing gated). On swap-back the popout resurfaces
    /// in place. Only ✕ closes for real (and ✕ on a popout-flavour
    /// SubPanel routes through the section's HandlePopoutCollapsed —
    /// "collapse back into panel", NOT real dismissal).</para>
    ///
    /// <para>Position persistence + close-collapses-back wiring lives on
    /// <see cref="PaintColorSection.CreatePopoutWandSubPanel"/> (the
    /// caller of this factory) — the factory itself is a thin metadata
    /// declaration so the architecture-prescribed lifecycle properties
    /// stay co-located with the other SubUI factories.</para>
    /// </summary>
    /// <param name="section">The owning <see cref="PaintColorSection"/>.
    /// Used as the <c>Host</c> for click-outside detection (no real host
    /// element exists for popouts; the section's chrome bar is the
    /// nearest semantic anchor).</param>
    /// <param name="body">The swatch grid built by
    /// <c>CoatingPaintColorGrid.Build</c>. Caller MUST detach it from
    /// any prior parent before calling — the WandSubPanel constructor
    /// re-parents it into its own chrome.</param>
    /// <param name="ownerVisibility">Per-frame visibility predicate
    /// (typically <c>HeldItem is WandOfCoatingBase or
    /// WandOfBuildingBase or WandOfReplacementBase</c>). Wired into
    /// <see cref="WandSubPanel.OwnerVisibilityCheck"/>.</param>
    public static WandSubPanel CreatePaintColorPopout(
        PaintColorSection section,
        UIElement body,
        Func<bool> ownerVisibility)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));
        if (body is null) throw new ArgumentNullException(nameof(body));

        var panel = new WandSubPanel(
            body: body,
            titleKey: section.TitleKey,
            defaultLocked: true, // Implicit lock ≡ structurally always-locked
            host: section,
            identityKey: "Coating.PaintColor.Popout")
        {
            Type              = SubPanelType.Popout,
            OwnerFamilies     = WandFamilyMask.PaintConsumers,
            LockBehaviourDecl = LockBehaviour.Implicit,
            OnChoice          = ChoiceBehaviour.NeverCloses,
            OnParentClose     = ParentCloseBehaviour.StaysUp,
            // (S8 2026-04-29 polish) Restore legacy CollapsedPopoutHost
            // breathing room (6f, not the 4f Panel-flavour default) so the
            // swatch grid doesn't sit cramped against the chrome edge.
            BodyPadding       = 6f,
        };

        panel.OwnerVisibilityCheck = ownerVisibility;
        return panel;
    }
}
