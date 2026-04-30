using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (Phase C 2026-04-29 — SubUI Migration Plan §Phase C) Concrete
/// <see cref="UISection"/> for the Coating panel's PaintColor swatch grid.
/// Replaces the legacy <c>CollapsibleSection { Style = Popout }</c> +
/// <c>CollapsedPopoutHost</c> substrate that drove this section pre-Phase-C.
///
/// <para><b>Lifecycle axes</b>: <c>Foldable=false</c>, <c>CanBePoppedOut=true</c>.
/// The header bar shows only the ⧓ popout button (no fold chevron). The body
/// is the <see cref="UIElement"/> built by <c>CoatingPaintColorGrid.Build</c>
/// — a swatch grid sized externally, owned externally, and merely re-parented
/// by this section between its in-panel <c>_bodyContainer</c> and the popout
/// <see cref="WandSubPanel"/>.</para>
///
/// <para><b>Persistence model</b> (S4 C-11 simplification, ratified S6 by GrayJou):
/// the section opts into legacy-key persistence by reading/writing
/// <see cref="WandPlayer.PopoutPositions"/> with the EXACT pre-migration key
/// <c>"Coating.PaintColor"</c>. No <see cref="WandPlayer"/> edits required —
/// the existing dict round-trips for free. The popout OPEN/CLOSED state is
/// NOT persisted (Invariant I-3): every world enter starts with the popout
/// stowed back in the panel; only the dragged POSITION survives.</para>
///
/// <para><b>Owner-visibility (β model, S4 v1.3 §B)</b>: the popout
/// SubPanel — built by <see cref="WandSubPanelFactories.CreatePaintColorPopout"/>
/// — declares <c>OwnerFamilies = Coating ∪ Building ∪ Replacement</c>
/// (the three paint-consuming wand families) AND wires the legacy lambda
/// <c>HeldItem is one of those three</c> for forward-compat with per-wand
/// PaintSource opt-outs. While the predicate returns false the popout PARKS
/// (body + position + lock state preserved); on swap-back it resurfaces in
/// place. Only ✕ closes for real.</para>
/// </summary>
public sealed class PaintColorSection : UISection
{
    public override bool Foldable      => false;
    public override bool CanBePoppedOut => true;
    public override string TitleKey      => "Mods.WorldShapingWandsMod.UI.Coating.PaintColor";
    /// <summary>EXACT legacy key — must NOT be retyped (preserves
    /// pre-Phase-C save data via <see cref="WandPlayer.PopoutPositions"/>).</summary>
    public override string PreferenceKey => "Coating.PaintColor";

    private readonly UIElement _swatchGrid;
    private readonly Func<bool> _ownerVisibility;

    /// <param name="swatchGrid">The PaintColor swatch grid built by
    /// <c>CoatingPaintColorGrid.Build</c>. Must have non-zero pixel
    /// Width/Height (used to size both the in-panel body container and the
    /// popout chrome).</param>
    /// <param name="ownerVisibility">Predicate evaluated each frame by the
    /// popout SubPanel — when false, the popout parks (hides without
    /// closing). Typical: <c>() => HeldItem is WandOfCoatingBase ||
    /// WandOfBuildingBase || WandOfReplacementBase</c>.</param>
    public PaintColorSection(UIElement swatchGrid, Func<bool> ownerVisibility)
    {
        _swatchGrid      = swatchGrid ?? throw new ArgumentNullException(nameof(swatchGrid));
        _ownerVisibility = ownerVisibility ?? (static () => true);
    }

    protected override void BuildContent(UIElement container)
    {
        container.Height.Set(_swatchGrid.Height.Pixels, 0f);
        ResetSwatchGridLayout();
        container.Append(_swatchGrid);
    }

    /// <summary>
    /// Restore the swatch grid's authored layout (HAlign=0.5, top-left zero)
    /// after a popout round-trip. <see cref="CoatingPaintColorGrid.Build"/>
    /// ships the container with <c>HAlign=0.5f</c> precisely so it self-
    /// centres in either the in-panel body container (300f-wide section)
    /// OR the popout chrome body slot (~204f-wide), per the v1.1 2026-04-25
    /// S2 popout-framework Bug A fix. Wiping HAlign to 0 here was the S8
    /// regression that pushed the swatch strip flush against the section's
    /// left edge inside the Coating panel.
    /// </summary>
    private void ResetSwatchGridLayout()
    {
        _swatchGrid.HAlign = 0.5f;
        _swatchGrid.VAlign = 0f;
        _swatchGrid.Left.Set(0f, 0f);
        _swatchGrid.Top.Set(0f, 0f);
    }

    protected override WandSubPanel CreatePopoutWandSubPanel()
    {
        // Detach the swatch grid from its current parent (the in-panel
        // _bodyContainer); the WandSubPanel constructor takes ownership and
        // re-parents into its own chrome.
        _swatchGrid.Remove();

        var popout = WandSubPanelFactories.CreatePaintColorPopout(this, _swatchGrid, _ownerVisibility);

        // Position: restore from saved PopoutPositions[PreferenceKey] OR
        // safe-spawn next to the parent panel (mirrors the deleted
        // CollapsedPopoutHost.OpenWith / ComputeSafeSpawn logic).
        var (x, y) = ResolveSpawnPosition(popout);
        popout.HAlign = 0f;
        popout.VAlign = 0f;
        popout.Left.Set(x, 0f);
        popout.Top.Set(y, 0f);

        // ✕ / Esc / click-outside (only ✕ for Implicit-locked Popout flavour)
        // routes through OnClose. For popout-flavour SubPanels, ✕ semantics =
        // "collapse back into panel" (NOT real dismissal — state lives on in
        // the section). Persist final position before re-parenting.
        popout.OnClose += () =>
        {
            PersistPopoutPosition(popout);
            _swatchGrid.Remove();
            ResetSwatchGridLayout();
            // Re-attach into the in-panel body container so HandlePopoutCollapsed
            // (which re-Appends _bodyContainer to this section) brings the
            // grid back into view.
            _bodyContainer?.Append(_swatchGrid);
            HandlePopoutCollapsed();
        };

        return popout;
    }

    private (float x, float y) ResolveSpawnPosition(WandSubPanel popout)
    {
        var wp = TryGetWandPlayer();
        if (wp?.PopoutPositions != null
            && wp.PopoutPositions.TryGetValue(PreferenceKey, out Vector2 saved))
        {
            return (saved.X, saved.Y);
        }
        // Safe-spawn: try right of the parent panel, then left, then centred.
        // Ported from CollapsedPopoutHost.ComputeSafeSpawn (deleted Phase C).
        float panelW = popout.Width.Pixels;
        float panelH = popout.Height.Pixels;
        float screenW = Main.screenWidth;
        float screenH = Main.screenHeight;
        const float margin = 16f;

        var parentDims = FindAncestorPanelDims(this);
        if (parentDims is { } p)
        {
            float xRight = p.X + p.Width + margin;
            if (xRight + panelW <= screenW - margin)
                return (xRight, MathHelper.Clamp(p.Y, margin, screenH - panelH - margin));

            float xLeft = p.X - margin - panelW;
            if (xLeft >= margin)
                return (xLeft, MathHelper.Clamp(p.Y, margin, screenH - panelH - margin));
        }
        return ((screenW - panelW) * 0.5f + 40f,
                MathHelper.Clamp((screenH - panelH) * 0.5f, margin, screenH - panelH - margin));
    }

    private static CalculatedStyle? FindAncestorPanelDims(UIElement node)
    {
        for (UIElement n = node; n != null; n = n.Parent)
            if (n is UIDraggablePanel) return n.GetDimensions();
        return null;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        // Per-frame position persistence while popped out — matches the
        // legacy CollapsedPopoutHost.PersistPosition cadence so a crash
        // mid-drag still preserves the most recent position.
        if (IsPoppedOut && ActivePopout != null)
            PersistPopoutPosition(ActivePopout);
    }

    private void PersistPopoutPosition(WandSubPanel popout)
    {
        var wp = TryGetWandPlayer();
        if (wp == null) return;
        wp.PopoutPositions ??= new System.Collections.Generic.Dictionary<string, Vector2>();
        // Only persist when the panel is in absolute-pixel mode (HAlign/VAlign 0).
        if (popout.HAlign == 0f && popout.VAlign == 0f)
            wp.PopoutPositions[PreferenceKey] = new Vector2(popout.Left.Pixels, popout.Top.Pixels);
    }

    private static WandPlayer TryGetWandPlayer()
    {
        var p = Main.LocalPlayer;
        if (p == null || !p.active) return null;
        return p.GetModPlayer<WandPlayer>();
    }
}
