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
    internal DestructionSettingsPanel DestructionUI;
    internal ReplacementSettingsPanel ReplacementUI;
    internal WiringSettingsPanel WiringUI;
    
    private UserInterface _userInterface;

    public bool IsAnyUIOpen => 
        (BuildingUI?.IsVisible ?? false) ||
        (DestructionUI?.IsVisible ?? false) ||
        (ReplacementUI?.IsVisible ?? false) ||
        (WiringUI?.IsVisible ?? false);

    public override void Load()
    {
        BuildingUI = new BuildingSettingsPanel();
        BuildingUI.Activate();

        DestructionUI = new DestructionSettingsPanel();
        DestructionUI.Activate();

        ReplacementUI = new ReplacementSettingsPanel();
        ReplacementUI.Activate();

        WiringUI = new WiringSettingsPanel();
        WiringUI.Activate();

        _userInterface = new UserInterface();
    }

    public override void Unload()
    {
        BuildingUI = null;
        DestructionUI = null;
        ReplacementUI = null;
        WiringUI = null;
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
        else if (heldItem is WandOfDestructionBase)
        {
            DestructionUI.IsVisible = true;
            _userInterface.SetState(DestructionUI);
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
    }

    public void ToggleUIForCurrentWand()
    {
        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;

        // Check if the current wand's UI is already open
        bool currentIsOpen = heldItem switch
        {
            WandOfBuildingBase => BuildingUI?.IsVisible ?? false,
            WandOfDestructionBase => DestructionUI?.IsVisible ?? false,
            WandOfReplacementBase => ReplacementUI?.IsVisible ?? false,
            WandOfWiringBase => WiringUI?.IsVisible ?? false,
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
        if (DestructionUI != null) DestructionUI.IsVisible = false;
        if (ReplacementUI != null) ReplacementUI.IsVisible = false;
        if (WiringUI != null) WiringUI.IsVisible = false;
        _userInterface?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (IsAnyUIOpen)
            _userInterface?.Update(gameTime);
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
                        // Draw whichever is visible
                        if (BuildingUI?.IsVisible == true) BuildingUI.Draw(Main.spriteBatch);
                        else if (DestructionUI?.IsVisible == true) DestructionUI.Draw(Main.spriteBatch);
                        else if (ReplacementUI?.IsVisible == true) ReplacementUI.Draw(Main.spriteBatch);
                        else if (WiringUI?.IsVisible == true) WiringUI.Draw(Main.spriteBatch);
                    }
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }
}