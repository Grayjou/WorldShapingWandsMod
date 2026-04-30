using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Items;
using WorldShapingWandsMod.Common.UI.Elements;
using WorldShapingWandsMod.Common.UI.InventoryView;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI;

[Autoload(Side = ModSide.Client)]
public class WandUISystem : ModSystem
{
    internal BuildingSettingsPanel BuildingUI;
    internal DismantlingSettingsPanel DismantlingUI;
    internal ReplacementSettingsPanel ReplacementUI;
    internal WiringSettingsPanel WiringUI;
    internal SafekeepingSettingsPanel SafekeepingUI;
    internal CoatingSettingsPanel CoatingUI;
    internal FluidsSettingsPanel FluidsUI;
    internal TorchesSettingsPanel TorchesUI;
    internal SelectionSettingsPanel SelectionUI;
    internal MoldingSettingsPanel MoldingUI;

    internal InventoryViewPanel InventoryViewUI;

    private UserInterface _userInterface;
    private UserInterface _inventoryViewInterface;

    // (S4 2026-04-28; renamed S6 2026-04-29 Phase D) WandSubPanel primitive infrastructure.
    // See `WSWSubUIPrimitivePlan.md`. The host owns a stack of currently-open
    // WandSubPanels (top-level + nested children).
    internal Elements.WandSubPanelHost WandSubPanelHost;
    private UserInterface _wandSubPanelInterface;
    private bool _escWasDown;

    /// <summary>
    /// (S1 2026-04-29 — SubUI Architecture Phase A) Cached held-wand family
    /// recomputed once per <see cref="UpdateUI"/> tick. Read by SubPanels
    /// whose <see cref="Elements.WandSubPanel.OwnerFamilies"/> mask drives a
    /// synthesised visibility lambda (see
    /// <see cref="Elements.WandSubPanel.UpdateOwnerFamilyVisibility"/>).
    /// Cached so each SubPanel doesn't re-derive it from
    /// <c>BaseCyclingWand.GetHeldFamily</c> independently. Reads
    /// <see cref="WandFamily.Unknown"/> when no wand is held — every
    /// non-empty <see cref="WandFamilyMask"/> naturally fails the predicate
    /// in that case (Unknown maps to <see cref="WandFamilyMask.None"/>).
    /// </summary>
    public WandFamily LastSeenHeldFamily { get; private set; } = WandFamily.Unknown;

    [System.Obsolete("v1.3 hide/show model makes this irrelevant; will be removed in v1.4. See DesignDoc_PopoutFrameworkV1_3 §B.2.")]
    public static int HotbarCycleGracePeriod = 10;

    public bool IsAnyUIOpen =>
        (BuildingUI?.IsVisible ?? false) ||
        (DismantlingUI?.IsVisible ?? false) ||
        (ReplacementUI?.IsVisible ?? false) ||
        (WiringUI?.IsVisible ?? false) ||
        (SafekeepingUI?.IsVisible ?? false) ||
        (CoatingUI?.IsVisible ?? false) ||
        (FluidsUI?.IsVisible ?? false) ||
        (TorchesUI?.IsVisible ?? false) ||
        (SelectionUI?.IsVisible ?? false) ||
        (MoldingUI?.IsVisible ?? false) ||
        (InventoryViewUI?.IsVisible ?? false);

    public bool IsCursorOverPanel()
    {
        if (!IsAnyUIOpen) return false;

        var mousePos = Main.MouseScreen;

        if (BuildingUI?.IsVisible == true && (BuildingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (DismantlingUI?.IsVisible == true && (DismantlingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (ReplacementUI?.IsVisible == true && (ReplacementUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (WiringUI?.IsVisible == true && (WiringUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (SafekeepingUI?.IsVisible == true && (SafekeepingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (CoatingUI?.IsVisible == true && (CoatingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (FluidsUI?.IsVisible == true && (FluidsUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (TorchesUI?.IsVisible == true && (TorchesUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (SelectionUI?.IsVisible == true && (SelectionUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (MoldingUI?.IsVisible == true && (MoldingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (InventoryViewUI?.IsVisible == true && (InventoryViewUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;

        return false;
    }

    public override void Load()
    {
        _userInterface = new UserInterface();
        _inventoryViewInterface = new UserInterface();
        _wandSubPanelInterface = new UserInterface();
    }

    public override void PostSetupContent()
    {
        BuildingUI = new BuildingSettingsPanel();
        BuildingUI.Activate();

        DismantlingUI = new DismantlingSettingsPanel();
        DismantlingUI.Activate();

        ReplacementUI = new ReplacementSettingsPanel();
        ReplacementUI.Activate();

        WiringUI = new WiringSettingsPanel();
        WiringUI.Activate();

        SafekeepingUI = new SafekeepingSettingsPanel();
        SafekeepingUI.Activate();

        CoatingUI = new CoatingSettingsPanel();
        CoatingUI.Activate();

        FluidsUI = new FluidsSettingsPanel();
        FluidsUI.Activate();

        TorchesUI = new TorchesSettingsPanel();
        TorchesUI.Activate();

        SelectionUI = new SelectionSettingsPanel();
        SelectionUI.Activate();

        MoldingUI = new MoldingSettingsPanel();
        MoldingUI.Activate();

        InventoryViewUI = new InventoryViewPanel();
        InventoryViewUI.Activate();

        WandSubPanelHost = new Elements.WandSubPanelHost();
        WandSubPanelHost.Activate();
    }

    public override void Unload()
    {
        BuildingUI = null;
        DismantlingUI = null;
        ReplacementUI = null;
        WiringUI = null;
        SafekeepingUI = null;
        CoatingUI = null;
        FluidsUI = null;
        TorchesUI = null;
        SelectionUI = null;
        MoldingUI = null;
        InventoryViewUI = null;
        WandSubPanelHost = null;
        _userInterface = null;
        _inventoryViewInterface = null;
        _wandSubPanelInterface = null;
    }

    public void OpenUIForCurrentWand()
    {
        CloseAllPanels();

        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;

        if (heldItem is WandOfBuildingBase)
        {
            BuildingUI.IsVisible = true;
            _userInterface.SetState(BuildingUI);
        }
        else if (heldItem is WandOfDismantlingBase)
        {
            DismantlingUI.IsVisible = true;
            _userInterface.SetState(DismantlingUI);
        }
        else if (heldItem is WandOfReplacementBase)
        {
            ReplacementUI.IsVisible = true;
            _userInterface.SetState(ReplacementUI);
        }
        else if (heldItem is WandOfWiringBase)
        {
            WiringUI.IsVisible = true;
            _userInterface.SetState(WiringUI);
        }
        else if (heldItem is WandOfSafekeepingBase)
        {
            SafekeepingUI.IsVisible = true;
            _userInterface.SetState(SafekeepingUI);
        }
        else if (heldItem is WandOfCoatingBase)
        {
            CoatingUI.IsVisible = true;
            _userInterface.SetState(CoatingUI);
        }
        else if (heldItem is WandOfFluidsBase)
        {
            FluidsUI.IsVisible = true;
            _userInterface.SetState(FluidsUI);
        }
        else if (heldItem is WandOfTorchesBase)
        {
            TorchesUI.IsVisible = true;
            _userInterface.SetState(TorchesUI);
        }
        else if (heldItem is WandOfDelimitationBase)
        {
            SelectionUI.IsVisible = true;
            _userInterface.SetState(SelectionUI);
        }
        else if (heldItem is WandOfMoldingBase)
        {
            MoldingUI.IsVisible = true;
            _userInterface.SetState(MoldingUI);
        }
    }

    public void ToggleUIForCurrentWand()
    {
        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;

        bool currentIsOpen = heldItem switch
        {
            WandOfBuildingBase => BuildingUI?.IsVisible ?? false,
            WandOfDismantlingBase => DismantlingUI?.IsVisible ?? false,
            WandOfReplacementBase => ReplacementUI?.IsVisible ?? false,
            WandOfWiringBase => WiringUI?.IsVisible ?? false,
            WandOfSafekeepingBase => SafekeepingUI?.IsVisible ?? false,
            WandOfCoatingBase => CoatingUI?.IsVisible ?? false,
            WandOfFluidsBase => FluidsUI?.IsVisible ?? false,
            WandOfTorchesBase => TorchesUI?.IsVisible ?? false,
            WandOfDelimitationBase => SelectionUI?.IsVisible ?? false,
            WandOfMoldingBase => MoldingUI?.IsVisible ?? false,
            _ => false
        };

        if (currentIsOpen)
            CloseAllPanels();
        else
            OpenUIForCurrentWand();
    }

    public bool ToggleInventoryView()
    {
        if (InventoryViewUI == null) return false;

        if (InventoryViewUI.IsUserOpenIntent)
        {
            InventoryViewUI.IsUserOpenIntent = false;
            UpdateInventoryViewVisibility();
            return false;
        }

        InventoryViewUI.IsUserOpenIntent = true;
        UpdateInventoryViewVisibility();
        return InventoryViewUI.IsVisible;
    }

    private void UpdateInventoryViewVisibility()
    {
        if (InventoryViewUI == null) return;

        bool hasProvider = false;
        var player = Main.LocalPlayer;
        if (player != null && player.active)
        {
            var provider = InventoryView.InventoryViewRegistry.GetProvider(player);
            hasProvider = provider != null;
        }

        bool shouldRender = InventoryViewUI.IsUserOpenIntent && hasProvider;

        if (shouldRender != InventoryViewUI.IsVisible)
        {
            InventoryViewUI.IsVisible = shouldRender;
            if (shouldRender)
                _inventoryViewInterface?.SetState(InventoryViewUI);
            else
                _inventoryViewInterface?.SetState(null);
        }
    }

    public void CloseAllUI()
    {
        CloseAllPanels();
        if (InventoryViewUI != null)
        {
            InventoryViewUI.IsUserOpenIntent = false;
            InventoryViewUI.IsVisible = false;
        }
        _inventoryViewInterface?.SetState(null);
    }

    public void CloseAllPanels()
    {
        if (BuildingUI != null) BuildingUI.IsVisible = false;
        if (DismantlingUI != null) DismantlingUI.IsVisible = false;
        if (ReplacementUI != null) ReplacementUI.IsVisible = false;
        if (WiringUI != null) WiringUI.IsVisible = false;
        if (SafekeepingUI != null) SafekeepingUI.IsVisible = false;
        if (CoatingUI != null) CoatingUI.IsVisible = false;
        if (FluidsUI != null) FluidsUI.IsVisible = false;
        if (TorchesUI != null) TorchesUI.IsVisible = false;
        if (SelectionUI != null) SelectionUI.IsVisible = false;
        if (MoldingUI != null) MoldingUI.IsVisible = false;
        _userInterface?.SetState(null);
        CloseAllWandSubPanels();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        UpdateInventoryViewVisibility();

        // (S6 §2/§3 fix) Always update active interfaces when any UI is open.
        // Previous version only updated when cursor was over panel, which
        // broke fast-drag (cursor leaves panel → Update stops → drag freezes).
        // Scroll wheel consumption is still gated on cursor position.
        if (IsAnyUIOpen)
        {
            _userInterface?.Update(gameTime);
            _inventoryViewInterface?.Update(gameTime);

            // Consume scroll wheel only when cursor is over a panel.
            if (IsCursorOverPanel() && PlayerInput.ScrollWheelDelta != 0)
                PlayerInput.ScrollWheelDelta = 0;
        }

        // (S4 2026-04-28) Drive the SubPanel layer + handle Esc / click-outside.
        if (IsAnyWandSubPanelOpen)
        {
            _wandSubPanelInterface?.Update(gameTime);

            // (S1 2026-04-29 — SubUI Architecture Phase A) Per-frame
            // OwnerFamilies visibility poll. Recompute the held-family once
            // per tick and let every SubPanel that declared an
            // OwnerFamilies mask synthesise / re-evaluate its visibility
            // predicate. SubPanels that did NOT declare a mask
            // (OwnerFamilies == None) are unaffected — their legacy
            // OwnerVisibilityCheck lambda (if any) keeps driving visibility
            // exactly as before.
            LastSeenHeldFamily = BaseCyclingWand.GetCurrentFamily(Main.LocalPlayer);
            foreach (var p in WandSubPanelHost.Panels)
                p.UpdateOwnerFamilyVisibility(LastSeenHeldFamily);

            // Esc → close topmost ONLY when topmost is unlocked.
            // (S14 2026-04-28; GrayJou worried-client review.) S4 ship
            // closed locked panels on Esc “as an emergency exit”, but the
            // lock chrome's promise to the player is *“only the X button
            // dismisses me”* — anything else surprises them. Parent panel
            // Esc handlers route through CloseAllPanels → CloseAllWandSubPanels
            // (respectLock=true) so the parent still closes cleanly while
            // the locked subpanel survives. Net behaviour: pressing Esc
            // with a locked Color Replace open + parent CoatingPanel open
            // closes the parent and leaves the locked picker floating;
            // pressing Esc again is a no-op (locked picker has no parent
            // panel listening for Esc, only the X button dismisses).
            bool escDown = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            if (escDown && !_escWasDown)
            {
                var top = WandSubPanelHost.Topmost;
                if (top != null && !top.IsLocked) CloseWandSubPanel(top);
            }
            _escWasDown = escDown;

            // Click-outside → close topmost only when topmost is UNLOCKED.
            // (Locked panels can be dismissed only via the close button or Esc.)
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                var pos = Main.MouseScreen;
                var top = WandSubPanelHost.Topmost;
                if (top != null && !top.IsLocked
                    && !top.ContainsScreenPoint(pos)
                    && !top.HostContainsScreenPoint(pos))
                {
                    CloseWandSubPanel(top);
                }
            }

            // Suppress scroll-wheel when over a subpanel.
            if (IsCursorOverWandSubPanel() && PlayerInput.ScrollWheelDelta != 0)
                PlayerInput.ScrollWheelDelta = 0;
        }
        else
        {
            _escWasDown = false;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

        if (mouseTextIndex != -1)
        {
            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "WorldShapingWandsMod: Wand Settings",
                delegate
                {
                    if (IsAnyUIOpen)
                    {
                        _userInterface?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                        if (InventoryViewUI?.IsVisible == true)
                            _inventoryViewInterface?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    }

                    // (S4 2026-04-28) Draw the SubPanel stack on top of everything else.
                    if (IsAnyWandSubPanelOpen)
                        _wandSubPanelInterface?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    // ── WandSubPanel primitive lifecycle (S4 2026-04-28; renamed S6 2026-04-29 Phase D) ──

    /// <summary>
    /// Opens a top-level <see cref="Elements.WandSubPanel"/> (or, if
    /// <paramref name="parent"/> is non-null, a nested child of an already-open
    /// SubPanel). The host attaches the panel, anchors it to its host element,
    /// and starts driving it through the WandSubPanel UserInterface layer.
    /// </summary>
    public void OpenWandSubPanel(Elements.WandSubPanel panel, Elements.WandSubPanel parent = null)
    {
        if (WandSubPanelHost == null || panel == null) return;
        if (WandSubPanelHost.Count == 0)
            _wandSubPanelInterface?.SetState(WandSubPanelHost);
        WandSubPanelHost.Push(panel, parent);
    }

    /// <summary>
    /// Closes a SubPanel (and every descendant child of it). Fires
    /// <c>OnClose</c> on each removed panel, deepest-first.
    /// </summary>
    public void CloseWandSubPanel(Elements.WandSubPanel panel)
    {
        if (WandSubPanelHost == null || panel == null) return;
        var removed = WandSubPanelHost.Pop(panel);
        foreach (var p in removed)
            p.RaiseClose();
        if (WandSubPanelHost.Count == 0)
            _wandSubPanelInterface?.SetState(null);
    }

    /// <summary>
    /// Closes every SubPanel (used when the host wand panel closes / wand swap).
    /// (S12 2026-04-28; GrayJou worried-client review) When
    /// <paramref name="respectLock"/> is true (the default for the
    /// parent-panel-close path), <see cref="Elements.WandSubPanel.IsLocked"/>
    /// panels SURVIVE the close — the whole point of the lock chrome is to
    /// keep the SubUI visible across other actions. Per S12 verbatim:
    /// *“it always closes when CoatingPanel does”*. Pass <c>false</c> to
    /// force-close every SubPanel (used by hard wand swap / mod unload).
    ///
    /// (S15 2026-04-28 audit; P 2*3) Caller survey:
    ///   • <see cref="CloseAllPanels"/> (line ≈298) — the ONLY in-tree caller.
    ///     Routes through the default soft path (<c>respectLock=true</c>),
    ///     which is the right contract for every parent-panel-close trigger
    ///     (Esc, X-button, click-outside, wand swap to a different wand).
    ///   • <c>respectLock=false</c> overload — currently has ZERO callers.
    ///     Preserved for hypothetical hard-teardown paths (mod unload,
    ///     world transition, future "panic-clear" debug command). Do NOT
    ///     wire it into any user-driven dismiss path; locked panels promise
    ///     the player *“only the X button dismisses me.”*
    /// </summary>
    public void CloseAllWandSubPanels(bool respectLock = true)
    {
        if (WandSubPanelHost == null) return;
        if (!respectLock)
        {
            var allRemoved = WandSubPanelHost.Clear();
            foreach (var p in allRemoved)
                p.RaiseClose();
            _wandSubPanelInterface?.SetState(null);
            return;
        }

        // Soft variant — close only the unlocked panels. Snapshot the live set
        // first because CloseWandSubPanel mutates WandSubPanelHost.
        var snapshot = new List<Elements.WandSubPanel>();
        foreach (var p in WandSubPanelHost.Panels)
        {
            if (!p.IsLocked) snapshot.Add(p);
        }
        foreach (var p in snapshot)
            CloseWandSubPanel(p);
    }

    /// <summary>True iff at least one SubPanel is currently open.</summary>
    public bool IsAnyWandSubPanelOpen => WandSubPanelHost != null && WandSubPanelHost.Count > 0;

    /// <summary>True iff the cursor is over any open SubPanel.</summary>
    public bool IsCursorOverWandSubPanel()
    {
        return WandSubPanelHost != null && WandSubPanelHost.ContainsScreenPoint(Main.MouseScreen);
    }
}