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
    
    private UserInterface _userInterface;

    public bool IsAnyUIOpen => 
        (BuildingUI?.IsVisible ?? false) ||
        (DismantlingUI?.IsVisible ?? false) ||
        (ReplacementUI?.IsVisible ?? false) ||
        (WiringUI?.IsVisible ?? false) ||
        (SafekeepingUI?.IsVisible ?? false);

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
    }

    public override void Unload()
    {
        BuildingUI = null;
        DismantlingUI = null;
        ReplacementUI = null;
        WiringUI = null;
        SafekeepingUI = null;
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
        _userInterface?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (IsAnyUIOpen)
        {
            _userInterface?.Update(gameTime);
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