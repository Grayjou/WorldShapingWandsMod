using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI;

[Autoload(Side = ModSide.Client)]
public class WandUISystem : ModSystem
{
    // Individual UI panels
    internal BuildingSettingsPanel BuildingUI;
    internal DismantlingSettingsPanel DismantlingUI;
    internal ReplacementSettingsPanel ReplacementUI;
    internal WiringSettingsPanel WiringUI;
    internal SafekeepingSettingsPanel SafekeepingUI;
    internal CoatingSettingsPanel CoatingUI;
    
    private UserInterface _userInterface;

    public bool IsAnyUIOpen => 
        (BuildingUI?.IsVisible ?? false) ||
        (DismantlingUI?.IsVisible ?? false) ||
        (ReplacementUI?.IsVisible ?? false) ||
        (WiringUI?.IsVisible ?? false) ||
        (SafekeepingUI?.IsVisible ?? false) ||
        (CoatingUI?.IsVisible ?? false);

    /// <summary>
    /// Returns true if a wand UI panel is visible AND the cursor is currently
    /// over that panel's actual draggable sub-element (not the full UIState bounds,
    /// which covers the entire screen and would always return true).
    /// Uses PanelElement.ContainsPoint(Main.MouseScreen) for a real-time check.
    /// </summary>
    public bool IsCursorOverPanel()
    {
        if (!IsAnyUIOpen) return false;

        var mousePos = Main.MouseScreen;

        // IMPORTANT: Use PanelElement (the inner UIDraggablePanel), NOT the UIState itself.
        // UIState.ContainsPoint() covers the full screen (UIState fills screen when active),
        // so it would always return true — causing IsMouseOverUI() to block all instant wand
        // interactions whenever any panel is open.
        if (BuildingUI?.IsVisible == true && (BuildingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (DismantlingUI?.IsVisible == true && (DismantlingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (ReplacementUI?.IsVisible == true && (ReplacementUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (WiringUI?.IsVisible == true && (WiringUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (SafekeepingUI?.IsVisible == true && (SafekeepingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;
        if (CoatingUI?.IsVisible == true && (CoatingUI.PanelElement?.ContainsPoint(mousePos) ?? false)) return true;

        return false;
    }

    public override void Load()
    {
        _userInterface = new UserInterface();
    }

    public override void PostSetupContent()
    {
        // Panels must be created AFTER localization is loaded (PostSetupContent or later).
        // Creating them in Load() causes Language.GetTextValue() to return raw keys
        // because localization entries are not registered until SetupContent.
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
    }

    public override void Unload()
    {
        BuildingUI = null;
        DismantlingUI = null;
        ReplacementUI = null;
        WiringUI = null;
        SafekeepingUI = null;
        CoatingUI = null;
        _userInterface = null;
    }

    /// <summary>
    /// Opens the appropriate UI based on currently held wand
    /// </summary>
    public void OpenUIForCurrentWand()
    {
        CloseAllUI();

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
    }

    public void ToggleUIForCurrentWand()
    {
        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;

        // Check if the current wand's UI is already open
        bool currentIsOpen = heldItem switch
        {
            WandOfBuildingBase => BuildingUI?.IsVisible ?? false,
            WandOfDismantlingBase => DismantlingUI?.IsVisible ?? false,
            WandOfReplacementBase => ReplacementUI?.IsVisible ?? false,
            WandOfWiringBase => WiringUI?.IsVisible ?? false,
            WandOfSafekeepingBase => SafekeepingUI?.IsVisible ?? false,
            WandOfCoatingBase => CoatingUI?.IsVisible ?? false,
            _ => false
        };

        if (currentIsOpen)
            CloseAllUI();
        else
            OpenUIForCurrentWand();
    }

    public void CloseAllUI()
    {
        if (BuildingUI != null) BuildingUI.IsVisible = false;
        if (DismantlingUI != null) DismantlingUI.IsVisible = false;
        if (ReplacementUI != null) ReplacementUI.IsVisible = false;
        if (WiringUI != null) WiringUI.IsVisible = false;
        if (SafekeepingUI != null) SafekeepingUI.IsVisible = false;
        if (CoatingUI != null) CoatingUI.IsVisible = false;
        _userInterface?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (IsAnyUIOpen)
        {
            // Only run the UserInterface update (which sets mouseInterface) when the
            // cursor is actually over the panel. When cursor is on tiles, we skip the
            // update so mouseInterface stays clean — this allows instant wand HoldItem
            // to check mouseInterface as a reliable "something else is blocking" signal
            // (minimap drag, NPC shop, etc.) without our own panel poisoning the flag.
            // Non-interactive panel state (visibility, layout) is preserved because
            // WandSettingsState handles display independently of the update cycle.
            if (IsCursorOverPanel())
            {
                _userInterface?.Update(gameTime);
            }
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
                    // Use _userInterface.Draw instead of panel.Draw directly.
                    // UserInterface.Draw handles click propagation, hover states,
                    // and focus management that UI elements need to receive proper
                    // mouse interaction. Calling panel.Draw directly bypasses this,
                    // which was the root cause of wiring UI not responding to clicks.
                    if (IsAnyUIOpen)
                    {
                        _userInterface?.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                    }
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }
}