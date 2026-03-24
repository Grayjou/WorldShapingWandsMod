using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.Input;

public class WandControls : ModSystem
{
    public static ModKeybind IncreaseThickness { get; private set; }
    public static ModKeybind DecreaseThickness { get; private set; }
    public static ModKeybind OpenWandUI { get; private set; }
    public static ModKeybind UndoStep { get; private set; }
    public static ModKeybind ToggleSuppressDrops { get; private set; }

    public override void Load()
    {
        IncreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Increase Thickness", Keys.OemCloseBrackets);
        DecreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Decrease Thickness", Keys.OemOpenBrackets);
        OpenWandUI = KeybindLoader.RegisterKeybind(Mod, "Open Wand Settings", Keys.OemPeriod);
        UndoStep = KeybindLoader.RegisterKeybind(Mod, "Undo Selection Step", Keys.Back);
        ToggleSuppressDrops = KeybindLoader.RegisterKeybind(Mod, "Toggle Suppress Drops", Keys.OemSemicolon);
    }

    public override void Unload()
    {
        IncreaseThickness = null;
        DecreaseThickness = null;
        OpenWandUI = null;
        UndoStep = null;
        ToggleSuppressDrops = null;
    }

    public override void PostUpdateInput()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var wandPlayer = player.GetModPlayer<WandPlayer>();
        var settings = wandPlayer.Settings;

        if (IncreaseThickness?.JustPressed == true)
        {
            settings.Thickness = Math.Min(settings.Thickness + 1, 50);
            settings.Validate();
            Main.NewText(Get("ThicknessChanged", settings.Thickness, settings.GetDescription()), Color.Cyan);
        }

        if (DecreaseThickness?.JustPressed == true)
        {
            settings.Thickness = Math.Max(settings.Thickness - 1, 0);
            settings.Validate();
            Main.NewText(Get("ThicknessChanged", settings.Thickness, settings.GetDescription()), Color.Cyan);
        }

        if (OpenWandUI?.JustPressed == true)
        {
            var uiSystem = ModContent.GetInstance<WandUISystem>();
            uiSystem?.ToggleUIForCurrentWand();
        }

        // Undo-step: go back one click in multi-click selection modes.
        // Stamp locked → unlock stamp (go back to "click to lock anchor")
        // Selection locked → unlock selection (go back to "click to set end")
        // Selection active (unlocked) → clear selection (go back to "click to start")
        if (UndoStep?.JustPressed == true && wandPlayer.Selection.IsActive)
        {
            if (wandPlayer.IsStampLocked)
            {
                wandPlayer.UnlockStamp();
                Main.NewText(Get("StampUnlocked"), Color.Yellow);
            }
            else if (wandPlayer.Selection.IsLocked)
            {
                wandPlayer.UnlockSelection();
                Main.NewText(Get("SelectionUnlocked"), Color.Yellow);
            }
            else
            {
                wandPlayer.ClearSelection();
                Main.NewText(Get("SelectionCleared"), Color.Yellow);
            }
        }

        // Toggle Suppress Drops: flips the SuppressDrops setting in WandServerConfig.
        // In single-player this takes effect immediately. In multiplayer, only the host
        // can change server-side config, so this keybind is a no-op for clients.
        if (ToggleSuppressDrops?.JustPressed == true)
        {
            var serverConfig = ModContent.GetInstance<WandServerConfig>();
            if (serverConfig != null)
            {
                serverConfig.SuppressDrops = !serverConfig.SuppressDrops;
                string state = serverConfig.SuppressDrops
                    ? Get("SuppressDropsOn")
                    : Get("SuppressDropsOff");
                Main.NewText(state, serverConfig.SuppressDrops ? Color.Orange : Color.Green);
            }
        }
    }
}
