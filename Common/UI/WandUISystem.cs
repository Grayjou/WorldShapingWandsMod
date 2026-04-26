using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
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

    internal CollapsedPopoutHost ActivePopoutHost;
    private UserInterface _popoutInterface;

    [System.Obsolete("v1.3 hide/show model makes this irrelevant; will be removed in v1.4. See DesignDoc_PopoutFrameworkV1_3 §B.2.")]
    public static int HotbarCycleGracePeriod = 10;

    public static bool AllowFoldStyle = false;

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
        _popoutInterface = new UserInterface();
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

        ActivePopoutHost = new CollapsedPopoutHost();
        ActivePopoutHost.Activate();
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
        ActivePopoutHost = null;
        _userInterface = null;
        _inventoryViewInterface = null;
        _popoutInterface = null;
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
    }

    public override void UpdateUI(GameTime gameTime)
    {
        UpdateInventoryViewVisibility();

        if (ActivePopoutHost != null && ActivePopoutHost.IsActive)
        {
            var owner = ActivePopoutHost.ActiveSection;
            bool ownerAvailable = owner?.OwnerVisibilityCheck?.Invoke() ?? true;
            ActivePopoutHost.SetVisibilityFromPredicate(ownerAvailable);
        }

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

        // (S6 §3 fix) Popout needs Update ticks even while fading out/in,
        // otherwise the alpha easing in its Update never runs and a fully-
        // faded popout can never revive. Use IsFadingOrVisible, not
        // IsCurrentlyVisible, for the Update gate.
        if (ActivePopoutHost != null && ActivePopoutHost.IsActive
            && ActivePopoutHost.IsFadingOrVisible)
        {
            _popoutInterface?.Update(gameTime);
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

                    // (S6 §3) Draw during fade — IsFadingOrVisible keeps it
                    // drawing while fading out. ApplyFadeAlpha sets DrawAlpha
                    // on the panel; the panel's Draw override skips at α=0.
                    if (ActivePopoutHost != null && ActivePopoutHost.IsActive
                        && ActivePopoutHost.IsFadingOrVisible)
                    {
                        ActivePopoutHost.ApplyFadeAlpha();
                        _popoutInterface?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    }
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }

    public void OpenPopout(Elements.CollapsibleSection section, UIElement body)
    {
        if (ActivePopoutHost == null || section == null || body == null) return;

        if (ActivePopoutHost.IsActive)
            ClosePopout();

        ActivePopoutHost.OpenWith(section, body);
        _popoutInterface?.SetState(ActivePopoutHost);
        ActivePopoutHost.SetVisibilityFromPredicate(true);
    }

    public void ClosePopout()
    {
        if (ActivePopoutHost == null || !ActivePopoutHost.IsActive) return;
        var section = ActivePopoutHost.ActiveSection;
        UIElement body = ActivePopoutHost.ReleaseBody();
        _popoutInterface?.SetState(null);
        section?.NotifyPopoutClosed();
    }
}