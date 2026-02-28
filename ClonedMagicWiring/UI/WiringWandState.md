using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using MagicWiring.Common;
using MagicWiring.UI.Elements;

namespace MagicWiring.UI;

public class WiringWandState : UIState
{
    private UIDraggablePanel _mainPanel;

    // Wire selection (toggles — multiple can be active)
    private UIToggleButton _redWireButton;
    private UIToggleButton _greenWireButton;
    private UIToggleButton _blueWireButton;
    private UIToggleButton _yellowWireButton;
    private UIToggleButton _actuatorButton;

    // Mode selection (radio — exactly one active)
    private UIToggleButton _placeModeButton;
    private UIToggleButton _removeModeButton;

    // Shape selection (radio — exactly one active)
    private UIToggleButton _wireKiteButton;
    private UIToggleButton _filledRectButton;
    private UIToggleButton _hollowRectButton;
    private UIToggleButton _filledDiamondButton;
    private UIToggleButton _hollowDiamondButton;
    private UIToggleButton _filledTriangleButton;
    private UIToggleButton _hollowTriangleButton;

    // Interaction mode (radio — exactly one active)
    private UIToggleButton _holdModeButton;
    private UIToggleButton _toggleModeButton;

    // Layout constants
    private const float PanelWidth = 300f;
    private const float PanelHeight = 560f;  // Taller for new sections
    private const float Padding = 10f;
    private const float ButtonWidth = 130f;
    private const float ButtonHeight = 30f;
    private const float Col1X = Padding;
    private const float Col2X = PanelWidth - Padding - ButtonWidth - 12f; // Account for panel internal padding

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();  // Changed from UIPanel
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.Height.Set(PanelHeight, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(44, 57, 105, 200);
        _mainPanel.BorderColor = new Color(20, 20, 60);
        Append(_mainPanel);

        float currentY = 0f;

        // === TITLE BAR (also serves as drag handle) ===
        var titleLabel = new UISectionTitle("Wand of Wiring");
        titleLabel.Width.Set(0f, 1f);
        titleLabel.Height.Set(28f, 0f);
        titleLabel.Top.Set(currentY, 0f);
        _mainPanel.Append(titleLabel);
        currentY += 34f;

        // === WIRE & ACTUATOR SECTION ===
        var wireSection = new UISectionTitle("Wires & Actuators");
        wireSection.Width.Set(0f, 1f);
        wireSection.Height.Set(24f, 0f);
        wireSection.Top.Set(currentY, 0f);
        _mainPanel.Append(wireSection);
        currentY += 30f;

        _redWireButton = MakeToggle("Red Wire", WiringSettings.WireRed, Col1X, currentY);
        _redWireButton.TintColor = new Color(200, 50, 50);
        _redWireButton.OnToggled += (_, _) => WiringSettings.WireRed = _redWireButton.Toggled;
        _mainPanel.Append(_redWireButton);

        _greenWireButton = MakeToggle("Green Wire", WiringSettings.WireGreen, Col2X, currentY);
        _greenWireButton.TintColor = new Color(50, 200, 50);
        _greenWireButton.OnToggled += (_, _) => WiringSettings.WireGreen = _greenWireButton.Toggled;
        _mainPanel.Append(_greenWireButton);
        currentY += 36f;

        _blueWireButton = MakeToggle("Blue Wire", WiringSettings.WireBlue, Col1X, currentY);
        _blueWireButton.TintColor = new Color(50, 100, 220);
        _blueWireButton.OnToggled += (_, _) => WiringSettings.WireBlue = _blueWireButton.Toggled;
        _mainPanel.Append(_blueWireButton);

        _yellowWireButton = MakeToggle("Yellow Wire", WiringSettings.WireYellow, Col2X, currentY);
        _yellowWireButton.TintColor = new Color(220, 200, 50);
        _yellowWireButton.OnToggled += (_, _) => WiringSettings.WireYellow = _yellowWireButton.Toggled;
        _mainPanel.Append(_yellowWireButton);
        currentY += 36f;

        _actuatorButton = MakeToggle("Actuator", WiringSettings.Actuator, Col1X, currentY);
        _actuatorButton.TintColor = new Color(150, 150, 150);
        _actuatorButton.OnToggled += (_, _) => WiringSettings.Actuator = _actuatorButton.Toggled;
        _mainPanel.Append(_actuatorButton);
        currentY += 46f;

        // === MODE SECTION ===
        var modeSection = new UISectionTitle("Mode");
        modeSection.Width.Set(0f, 1f);
        modeSection.Height.Set(24f, 0f);
        modeSection.Top.Set(currentY, 0f);
        _mainPanel.Append(modeSection);
        currentY += 30f;

        _placeModeButton = MakeRadio("Place", WiringSettings.Mode == WiringMode.Place, Col1X, currentY);
        _placeModeButton.TintColor = new Color(80, 200, 80);
        _placeModeButton.OnToggled += (_, _) => { WiringSettings.Mode = WiringMode.Place; UpdateModeButtons(); };
        _mainPanel.Append(_placeModeButton);

        _removeModeButton = MakeRadio("Remove", WiringSettings.Mode == WiringMode.Remove, Col2X, currentY);
        _removeModeButton.TintColor = new Color(200, 80, 80);
        _removeModeButton.OnToggled += (_, _) => { WiringSettings.Mode = WiringMode.Remove; UpdateModeButtons(); };
        _mainPanel.Append(_removeModeButton);
        currentY += 46f;

        // === SHAPE SECTION ===
        var shapeSection = new UISectionTitle("Shape");
        shapeSection.Width.Set(0f, 1f);
        shapeSection.Height.Set(24f, 0f);
        shapeSection.Top.Set(currentY, 0f);
        _mainPanel.Append(shapeSection);
        currentY += 30f;

        // WireKite at top (the "classic" option)
        _wireKiteButton = MakeRadio("Wire (90°)", WiringSettings.Shape == WiringShape.WireKite, Col1X, currentY, new Color(180, 180, 80));
        _wireKiteButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.WireKite; UpdateShapeButtons(); };
        _mainPanel.Append(_wireKiteButton);
        currentY += 36f;

        _filledRectButton = MakeRadio("Filled Rect", WiringSettings.Shape == WiringShape.FilledRectangle, Col1X, currentY);
        _filledRectButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.FilledRectangle; UpdateShapeButtons(); };
        _mainPanel.Append(_filledRectButton);

        _hollowRectButton = MakeRadio("Hollow Rect", WiringSettings.Shape == WiringShape.HollowRectangle, Col2X, currentY);
        _hollowRectButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.HollowRectangle; UpdateShapeButtons(); };
        _mainPanel.Append(_hollowRectButton);
        currentY += 36f;

        _filledDiamondButton = MakeRadio("Filled Diamond", WiringSettings.Shape == WiringShape.FilledDiamond, Col1X, currentY);
        _filledDiamondButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.FilledDiamond; UpdateShapeButtons(); };
        _mainPanel.Append(_filledDiamondButton);

        _hollowDiamondButton = MakeRadio("Hollow Diamond", WiringSettings.Shape == WiringShape.HollowDiamond, Col2X, currentY);
        _hollowDiamondButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.HollowDiamond; UpdateShapeButtons(); };
        _mainPanel.Append(_hollowDiamondButton);
        currentY += 36f;

        _filledTriangleButton = MakeRadio("Filled Triangle", WiringSettings.Shape == WiringShape.FilledTriangle, Col1X, currentY);
        _filledTriangleButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.FilledTriangle; UpdateShapeButtons(); };
        _mainPanel.Append(_filledTriangleButton);

        _hollowTriangleButton = MakeRadio("Hollow Triangle", WiringSettings.Shape == WiringShape.HollowTriangle, Col2X, currentY);
        _hollowTriangleButton.OnToggled += (_, _) => { WiringSettings.Shape = WiringShape.HollowTriangle; UpdateShapeButtons(); };
        _mainPanel.Append(_hollowTriangleButton);
        currentY += 46f;

        // Interaction Mode
        var interactSection = new UISectionTitle("Interaction");
        interactSection.Width.Set(0f, 1f);
        interactSection.Height.Set(24f, 0f);
        interactSection.Top.Set(currentY, 0f);
        _mainPanel.Append(interactSection);
        currentY += 30f;

        _holdModeButton = MakeRadio("Hold & Drag", WiringSettings.Interaction == InteractionMode.Hold, Col1X, currentY, new Color(100, 150, 200));
        _holdModeButton.OnToggled += (_, _) =>
        {
            WiringSettings.Interaction = InteractionMode.Hold;
            Main.LocalPlayer.GetModPlayer<WiringWandPlayer>().ClearPending();
            UpdateInteractionButtons();
        };
        _mainPanel.Append(_holdModeButton);

        _toggleModeButton = MakeRadio("Click Mode", WiringSettings.Interaction == InteractionMode.Toggle, Col2X, currentY, new Color(200, 150, 100));
        _toggleModeButton.OnToggled += (_, _) =>
        {
            WiringSettings.Interaction = InteractionMode.Toggle;
            UpdateInteractionButtons();
            Main.NewText("Click to set start, click again to confirm. Right-click to cancel.", Color.Yellow);
        };
        _mainPanel.Append(_toggleModeButton);
    }

    /// <summary>Creates a standard toggle button (for wire/actuator selection).</summary>
    private UIToggleButton MakeToggle(string text, bool initialState, float left, float top, Color? tint = null)
    {
        var btn = new UIToggleButton(text, initialState);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = false;
        if (tint.HasValue) btn.TintColor = tint.Value;
        return btn;
    }

    /// <summary>Creates a radio button (for mode/shape selection — exactly one active).</summary>
    private UIToggleButton MakeRadio(string text, bool initialState, float left, float top, Color? tint = null)
    {
        var btn = new UIToggleButton(text, initialState);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = true;
        if (tint.HasValue) btn.TintColor = tint.Value;
        return btn;
    }

    private void UpdateModeButtons()
    {
        _placeModeButton.Toggled = WiringSettings.Mode == WiringMode.Place;
        _removeModeButton.Toggled = WiringSettings.Mode == WiringMode.Remove;
    }

    private void UpdateShapeButtons()
    {
        _wireKiteButton.Toggled = WiringSettings.Shape == WiringShape.WireKite;
        _filledRectButton.Toggled = WiringSettings.Shape == WiringShape.FilledRectangle;
        _hollowRectButton.Toggled = WiringSettings.Shape == WiringShape.HollowRectangle;
        _filledDiamondButton.Toggled = WiringSettings.Shape == WiringShape.FilledDiamond;
        _hollowDiamondButton.Toggled = WiringSettings.Shape == WiringShape.HollowDiamond;
        _filledTriangleButton.Toggled = WiringSettings.Shape == WiringShape.FilledTriangle;
        _hollowTriangleButton.Toggled = WiringSettings.Shape == WiringShape.HollowTriangle;
    }

    private void UpdateInteractionButtons()
    {
        _holdModeButton.Toggled = WiringSettings.Interaction == InteractionMode.Hold;
        _toggleModeButton.Toggled = WiringSettings.Interaction == InteractionMode.Toggle;
    }

    // mouseInterface is now handled inside UIDraggablePanel.Update(),
    // so we don't need it here anymore.
}