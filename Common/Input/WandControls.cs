using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI;

namespace WorldShapingWandsMod.Common.Input;

public class WandControls : ModSystem
{
    public static ModKeybind IncreaseThickness { get; private set; }
    public static ModKeybind DecreaseThickness { get; private set; }
    public static ModKeybind OpenWandUI { get; private set; }
    public static ModKeybind UndoStep { get; private set; }

    public override void Load()
    {
        IncreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Increase Thickness", Keys.OemCloseBrackets);
        DecreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Decrease Thickness", Keys.OemOpenBrackets);
        OpenWandUI = KeybindLoader.RegisterKeybind(Mod, "Open Wand Settings", Keys.OemPeriod);
        UndoStep = KeybindLoader.RegisterKeybind(Mod, "Undo Selection Step", Keys.Back);
    }

    public override void Unload()
    {
        IncreaseThickness = null;
        DecreaseThickness = null;
        OpenWandUI = null;
        UndoStep = null;
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
            Main.NewText($"Thickness: {settings.Thickness} — {settings.GetDescription()}", Color.Cyan);
        }

        if (DecreaseThickness?.JustPressed == true)
        {
            settings.Thickness = Math.Max(settings.Thickness - 1, 0);
            settings.Validate();
            Main.NewText($"Thickness: {settings.Thickness} — {settings.GetDescription()}", Color.Cyan);
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
                Main.NewText("Stamp unlocked. Click to set new anchor.", Color.Yellow);
            }
            else if (wandPlayer.Selection.IsLocked)
            {
                wandPlayer.UnlockSelection();
                Main.NewText("Selection unlocked. Move to adjust end point.", Color.Yellow);
            }
            else
            {
                wandPlayer.ClearSelection();
                Main.NewText("Selection cleared.", Color.Yellow);
            }
        }
    }
}