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
        _userInterface = new UserInterface();
    }

    public override void PostSetupContent()
    {
        // Panels must be created AFTER localization is loaded (PostSetupContent or later).
        // Creating them in Load() causes Language.GetTextValue() to return raw keys
        // because localization entries are not registered until SetupContent.
        BuildingUI = new BuildingSettingsPanel();
        BuildingUI.Activate();

        DestructionUI = new DestructionSettingsPanel();
        DestructionUI.Activate();

        ReplacementUI = new ReplacementSettingsPanel();
        ReplacementUI.Activate();

        WiringUI = new WiringSettingsPanel();
        WiringUI.Activate();
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
        Main.NewText($"[DEBUG] OpenUIForCurrentWand called", Color.Cyan);
        CloseAllUI();

        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;
        Main.NewText($"[DEBUG] Opening UI for item type: {heldItem?.GetType().Name ?? "NULL"}", Color.Cyan);

        if (heldItem is WandOfBuildingBase)
        {
            Main.NewText($"[DEBUG] Setting BuildingUI visible", Color.Cyan);
            BuildingUI.IsVisible = true;
            _userInterface.SetState(BuildingUI);
            Main.NewText($"[DEBUG] BuildingUI.IsVisible = {BuildingUI.IsVisible}", Color.Cyan);
        }
        else if (heldItem is WandOfDestructionBase)
        {
            Main.NewText($"[DEBUG] Setting DestructionUI visible", Color.Cyan);
            DestructionUI.IsVisible = true;
            _userInterface.SetState(DestructionUI);
            Main.NewText($"[DEBUG] DestructionUI.IsVisible = {DestructionUI.IsVisible}", Color.Cyan);
        }
        else if (heldItem is WandOfReplacementBase)
        {
            Main.NewText($"[DEBUG] Setting ReplacementUI visible", Color.Cyan);
            ReplacementUI.IsVisible = true;
            _userInterface.SetState(ReplacementUI);
            Main.NewText($"[DEBUG] ReplacementUI.IsVisible = {ReplacementUI.IsVisible}", Color.Cyan);
        }
        else if (heldItem is WandOfWiringBase)
        {
            Main.NewText($"[DEBUG] Setting WiringUI visible", Color.Cyan);
            WiringUI.IsVisible = true;
            _userInterface.SetState(WiringUI);
            Main.NewText($"[DEBUG] WiringUI.IsVisible = {WiringUI.IsVisible}", Color.Cyan);
        }
        else
        {
            Main.NewText($"[DEBUG] Held item is not a recognized wand type!", Color.Red);
        }
    }

    public void ToggleUIForCurrentWand()
    {
        var heldItem = Main.LocalPlayer?.HeldItem?.ModItem;
        Main.NewText($"[DEBUG] ToggleUIForCurrentWand called. HeldItem: {heldItem?.GetType().Name ?? "NULL"}", Color.Cyan);

        // Check if the current wand's UI is already open
        bool currentIsOpen = heldItem switch
        {
            WandOfBuildingBase => BuildingUI?.IsVisible ?? false,
            WandOfDestructionBase => DestructionUI?.IsVisible ?? false,
            WandOfReplacementBase => ReplacementUI?.IsVisible ?? false,
            WandOfWiringBase => WiringUI?.IsVisible ?? false,
            _ => false
        };

        Main.NewText($"[DEBUG] Current UI open: {currentIsOpen}", Color.Cyan);

        if (currentIsOpen)
        {
            Main.NewText($"[DEBUG] Closing all UI", Color.Yellow);
            CloseAllUI();
        }
        else
        {
            Main.NewText($"[DEBUG] Opening UI for wand", Color.Yellow);
            OpenUIForCurrentWand();
        }
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