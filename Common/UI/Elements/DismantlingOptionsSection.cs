using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (Session 2 2026-05-03 — Dismantling Options Popout Implementation)
/// Concrete <see cref="UISection"/> for the Dismantling panel's destroy options.
/// Replaces the legacy direct button management with a popout-capable section.
///
/// <para><b>Lifecycle axes</b>: <c>Foldable=false</c>, <c>CanBePoppedOut=true</c>.
/// The header bar shows only the ⧓ popout button (no fold chevron). The body
/// is a row of four icon toggle buttons (DestroyTiles, DestroyWalls,
/// DestroyContainers, VoidEverything).</para>
///
/// <para><b>Persistence model</b>: the section opts into position persistence
/// by reading/writing <see cref="WandPlayer.PopoutPositions"/> with the key
/// <c>"Dismantling.Options"</c>. The popout OPEN/CLOSED state is NOT persisted
/// (Invariant I-3): every world enter starts with the popout stowed back in
/// the panel; only the dragged POSITION survives.</para>
///
/// <para><b>Owner-visibility (wand-scoped)</b>: the popout SubPanel — built by
/// <see cref="WandSubPanelFactories.CreateDismantlingOptionsPopout"/> — declares
/// <c>OwnerFamilies = Dismantling</c> (ONLY the Dismantling wand family, unlike
/// the PaintColor popout which serves three families). While the predicate
/// returns false the popout PARKS (body + position + lock state preserved); on
/// swap-back it resurfaces in place. Only ✕ closes for real.</para>
/// </summary>
public sealed class DismantlingOptionsSection : UISection
{
    public override bool Foldable      => false;
    public override bool CanBePoppedOut => true;
    public override string TitleKey      => "Mods.WorldShapingWandsMod.UI.Dismantling.Options";
    /// <summary>Preference key for popout position persistence.</summary>
    public override string PreferenceKey => "Dismantling.Options";

    private readonly UIElement _optionsContainer;
    private readonly Func<bool> _ownerVisibility;

    /// <param name="optionsContainer">The container element holding the four
    /// destroy option icon buttons. Must have non-zero pixel Width/Height (used
    /// to size both the in-panel body container and the popout chrome).</param>
    /// <param name="ownerVisibility">Predicate evaluated each frame by the
    /// popout SubPanel — when false, the popout parks (hides without
    /// closing). Typical: <c>() => HeldItem is WandOfDismantlingBase</c>.</param>
    public DismantlingOptionsSection(UIElement optionsContainer, Func<bool> ownerVisibility)
    {
        _optionsContainer  = optionsContainer ?? throw new ArgumentNullException(nameof(optionsContainer));
        _ownerVisibility   = ownerVisibility ?? (static () => true);
    }

    protected override void BuildContent(UIElement container)
    {
        container.Height.Set(_optionsContainer.Height.Pixels, 0f);
        ResetOptionsContainerLayout();
        container.Append(_optionsContainer);
    }

    /// <summary>
    /// Restore the options container's authored layout after a popout round-trip.
    /// Ensures consistent alignment in both the in-panel body container and
    /// the popout chrome.
    /// </summary>
    private void ResetOptionsContainerLayout()
    {
        _optionsContainer.HAlign = 0.5f;
        _optionsContainer.VAlign = 0f;
        _optionsContainer.Left.Set(0f, 0f);
        _optionsContainer.Top.Set(0f, 0f);
    }

    protected override WandSubPanel CreatePopoutWandSubPanel()
    {
        // Detach the options container from its current parent (the in-panel
        // _bodyContainer); the WandSubPanel constructor takes ownership and
        // re-parents into its own chrome.
        _optionsContainer.Remove();

        var popout = WandSubPanelFactories.CreateDismantlingOptionsPopout(this, _optionsContainer, _ownerVisibility);

        // Position: restore from saved PopoutPositions[PreferenceKey] OR
        // safe-spawn next to the parent panel.
        var (x, y) = ResolveSpawnPosition(popout);
        popout.HAlign = 0f;
        popout.VAlign = 0f;
        popout.Left.Set(x, 0f);
        popout.Top.Set(y, 0f);

        // ✕ / Esc / click-outside routes through OnClose. For popout-flavour
        // SubPanels, ✕ semantics = "collapse back into panel" (NOT real
        // dismissal — state lives on in the section). Persist final position
        // before re-parenting.
        popout.OnClose += () =>
        {
            PersistPopoutPosition(popout);
            _optionsContainer.Remove();
            ResetOptionsContainerLayout();
            // Re-attach into the in-panel body container so HandlePopoutCollapsed
            // (which re-Appends _bodyContainer to this section) brings the
            // container back into view.
            _bodyContainer?.Append(_optionsContainer);
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
