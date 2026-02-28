using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.Input;

public class ThicknessControls : ModSystem
{
    public static ModKeybind IncreaseThickness { get; private set; }
    public static ModKeybind DecreaseThickness { get; private set; }

    public override void Load()
    {
        IncreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Increase Thickness", Keys.OemCloseBrackets);
        DecreaseThickness = KeybindLoader.RegisterKeybind(Mod, "Decrease Thickness", Keys.OemOpenBrackets);
    }

    public override void Unload()
    {
        IncreaseThickness = null;
        DecreaseThickness = null;
    }

    public override void PostUpdateInput()
    {
        if (Main.gameMenu) return;

        var player = Main.LocalPlayer;
        if (player?.active != true) return;

        var settings = player.GetModPlayer<WandPlayer>().Settings;

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
    }
}