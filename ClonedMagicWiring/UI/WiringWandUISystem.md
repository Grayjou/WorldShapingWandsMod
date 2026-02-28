using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using MagicWiring.Content.Items;

namespace MagicWiring.UI;

[Autoload(Side = ModSide.Client)]
public class WiringWandUISystem : ModSystem
{
    private UserInterface _wiringWandInterface;
    private WiringWandState _wiringWandState;

    public static WiringWandUISystem Instance => ModContent.GetInstance<WiringWandUISystem>();

    public bool IsVisible => _wiringWandInterface?.CurrentState != null;

    public override void Load()
    {
        if (!Main.dedServ)
        {
            _wiringWandState = new WiringWandState();
            _wiringWandState.Activate();
            _wiringWandInterface = new UserInterface();
            // Do NOT set state here — UI should start hidden
            // _wiringWandInterface.SetState(_wiringWandState);  <-- was the bug
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        // Auto-close the UI when the player is no longer holding the wand
        if (IsVisible)
        {
            Player player = Main.LocalPlayer;
            bool holdingWand = player.HeldItem?.ModItem is WiringWandItem;

            if (!holdingWand)
            {
                _wiringWandInterface.SetState(null);
                return;
            }
        }

        _wiringWandInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (mouseTextIndex != -1)
        {
            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "MagicWiring: Wiring Wand UI",
                delegate
                {
                    if (IsVisible)
                    {
                        _wiringWandInterface.Draw(Main.spriteBatch, new GameTime());
                    }
                    return true;
                },
                InterfaceScaleType.UI)
            );
        }
    }

    public void ToggleUI()
    {
        if (IsVisible)
            _wiringWandInterface.SetState(null);
        else
            _wiringWandInterface.SetState(_wiringWandState);
    }

    public void CloseUI()
    {
        _wiringWandInterface?.SetState(null);
    }
}