using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

public class WiringSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    private UIDraggablePanel _mainPanel;

    // Wire toggles
    private UIToggleButton _redWireBtn, _greenWireBtn, _blueWireBtn, _yellowWireBtn, _actuatorBtn;

    // Mode buttons
    private UIToggleButton _placeModeBtn, _removeModeBtn;

    // Shape buttons (icon-based)
    private UIIconButton _rectFilledBtn, _edgeBtn, _straightBtn;
    private UIIconButton _diamondFilledBtn, _triangleFilledBtn;
    private UIIconButton _halfEllipseHFilledBtn, _halfEllipseVFilledBtn;

    private UIText _thicknessValue;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float PanelHeight = 425f;
    private const float Padding = 10f;
    private const float ButtonWidth = 140f;
    private const float ButtonHeight = 28f;
    private const float IconBtnSize = 36f;
    private const float IconGap = 6f;

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.Height.Set(PanelHeight, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(79, 79, 44, 220);  // Yellowish tint
        _mainPanel.BorderColor = new Color(60, 60, 20);
        Append(_mainPanel);

        float y = 8f;
        float col1 = Padding;
        float col2 = PanelWidth - Padding - ButtonWidth - 12f;

        // Title
        var title = new UISectionTitle(L("Wiring.Title"));
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // === WIRE TYPES ===
        var wiresSection = new UISectionTitle(L("Wiring.WireTypes"));
        wiresSection.Width.Set(0f, 1f);
        wiresSection.Height.Set(22f, 0f);
        wiresSection.Top.Set(y, 0f);
        _mainPanel.Append(wiresSection);
        y += 28f;

        _redWireBtn = MakeToggle(L("Wiring.RedWire"), col1, y, new Color(200, 80, 80));
        _greenWireBtn = MakeToggle(L("Wiring.GreenWire"), col2, y, new Color(80, 200, 80));
        _mainPanel.Append(_redWireBtn);
        _mainPanel.Append(_greenWireBtn);
        y += 34f;

        _blueWireBtn = MakeToggle(L("Wiring.BlueWire"), col1, y, new Color(80, 80, 200));
        _yellowWireBtn = MakeToggle(L("Wiring.YellowWire"), col2, y, new Color(200, 200, 80));
        _mainPanel.Append(_blueWireBtn);
        _mainPanel.Append(_yellowWireBtn);
        y += 34f;

        _actuatorBtn = MakeToggle(L("Wiring.Actuator"), col1, y, new Color(150, 150, 150));
        _mainPanel.Append(_actuatorBtn);
        y += 42f;

        // === MODE SECTION ===
        var modeSection = new UISectionTitle(L("Wiring.Mode"));
        modeSection.Width.Set(0f, 1f);
        modeSection.Height.Set(22f, 0f);
        modeSection.Top.Set(y, 0f);
        _mainPanel.Append(modeSection);
        y += 28f;

        _placeModeBtn = MakeRadio(L("Wiring.Place"), col1, y, new Color(100, 200, 100));
        _removeModeBtn = MakeRadio(L("Wiring.Remove"), col2, y, new Color(200, 100, 100));
        _mainPanel.Append(_placeModeBtn);
        _mainPanel.Append(_removeModeBtn);
        y += 42f;

        // === SHAPE SECTION ===
        var shapeSection = new UISectionTitle(L("Common.Shape"));
        shapeSection.Width.Set(0f, 1f);
        shapeSection.Height.Set(22f, 0f);
        shapeSection.Top.Set(y, 0f);
        _mainPanel.Append(shapeSection);
        y += 28f;

        // Load shape icon textures
        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var texRectFilled     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectFilled", AssetRequestMode.ImmediateLoad);
        var texEdge           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEdge", AssetRequestMode.ImmediateLoad);
        var texStraight       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraight", AssetRequestMode.ImmediateLoad);
        var texDiamondFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondFilled", AssetRequestMode.ImmediateLoad);
        var texTriangleFilled = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEHFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseHFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEVFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseVFilled", AssetRequestMode.ImmediateLoad);

        // Shape row 1: 5 icons
        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

        _rectFilledBtn    = MakeIconBtn(texRectFilled,     L("Common.ShapeRectFilled"),    shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _edgeBtn          = MakeIconBtn(texEdge,           L("Common.ShapeEdge"),          shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _straightBtn      = MakeIconBtn(texStraight,       L("Common.ShapeStraight"),      shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _diamondFilledBtn = MakeIconBtn(texDiamondFilled,  L("Common.ShapeDiamondFilled"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _triangleFilledBtn = MakeIconBtn(texTriangleFilled, L("Common.ShapeTriangleFilled"), shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_edgeBtn);
        _mainPanel.Append(_straightBtn);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_triangleFilledBtn);
        y += IconBtnSize + IconGap;

        // Shape row 2: Half-Ellipses (filled only for wiring)
        float totalRow2Width = IconBtnSize * 2 + IconGap * 1;
        float row2StartX = (PanelWidth - totalRow2Width) / 2f - Padding;

        _halfEllipseHFilledBtn = MakeIconBtn(texHalfEHFilled, L("Common.ShapeHalfEllipseHFilled"), row2StartX + (IconBtnSize + IconGap) * 0, y);
        _halfEllipseVFilledBtn = MakeIconBtn(texHalfEVFilled, L("Common.ShapeHalfEllipseVFilled"), row2StartX + (IconBtnSize + IconGap) * 1, y);
        _mainPanel.Append(_halfEllipseHFilledBtn);
        _mainPanel.Append(_halfEllipseVFilledBtn);
        y += IconBtnSize + 12f;

        // Thickness
        var thicknessLabel = new UIText(L("Common.OutlineThickness"), 0.85f);
        thicknessLabel.Left.Set(col1, 0f);
        thicknessLabel.Top.Set(y, 0f);
        _mainPanel.Append(thicknessLabel);

        var minusBtn = new UITextPanel<string>("-", 0.8f, false);
        minusBtn.Width.Set(30f, 0f);
        minusBtn.Height.Set(26f, 0f);
        minusBtn.Left.Set(col1 + 130f, 0f);
        minusBtn.Top.Set(y - 2f, 0f);
        minusBtn.OnLeftClick += (_, _) => AdjustThickness(-1);
        _mainPanel.Append(minusBtn);

        _thicknessValue = new UIText("1", 0.9f);
        _thicknessValue.Left.Set(col1 + 170f, 0f);
        _thicknessValue.Top.Set(y, 0f);
        _mainPanel.Append(_thicknessValue);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(y - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => AdjustThickness(1);
        _mainPanel.Append(plusBtn);
        y += 42f;

        // Close button
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => IsVisible = false;
        _mainPanel.Append(closeBtn);

        // Wire up events
        _redWireBtn.OnToggled += (_, _) => GetSettings().WireRed = _redWireBtn.Toggled;
        _greenWireBtn.OnToggled += (_, _) => GetSettings().WireGreen = _greenWireBtn.Toggled;
        _blueWireBtn.OnToggled += (_, _) => GetSettings().WireBlue = _blueWireBtn.Toggled;
        _yellowWireBtn.OnToggled += (_, _) => GetSettings().WireYellow = _yellowWireBtn.Toggled;
        _actuatorBtn.OnToggled += (_, _) => GetSettings().Actuator = _actuatorBtn.Toggled;

        _placeModeBtn.OnToggled += (_, _) => GetSettings().Mode = WiringMode.Place;
        _removeModeBtn.OnToggled += (_, _) => GetSettings().Mode = WiringMode.Remove;

        _rectFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _edgeBtn.OnToggled += (_, _) => SetShape(ShapeType.Edge, ShapeMode.Filled);
        _straightBtn.OnToggled += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
        _diamondFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _halfEllipseHFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseH, ShapeMode.Filled);
        _halfEllipseVFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseV, ShapeMode.Filled);
    }

    private WandOfWiringSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.WiringSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, 50);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private UIToggleButton MakeToggle(string text, float left, float top, Color? tint = null)
    {
        var btn = new UIToggleButton(text, false);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = false;
        if (tint.HasValue) btn.TintColor = tint.Value;
        return btn;
    }

    private UIToggleButton MakeRadio(string text, float left, float top, Color? tint = null)
    {
        var btn = new UIToggleButton(text, false);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = true;
        if (tint.HasValue) btn.TintColor = tint.Value;
        return btn;
    }

    private UIIconButton MakeIconBtn(Asset<Texture2D> texture, string hoverText, float left, float top)
    {
        var btn = new UIIconButton(texture, hoverText);
        btn.Width.Set(IconBtnSize, 0f);
        btn.Height.Set(IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = true;
        return btn;
    }

    private void UpdateWireButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _redWireBtn.Toggled = settings.WireRed;
        _greenWireBtn.Toggled = settings.WireGreen;
        _blueWireBtn.Toggled = settings.WireBlue;
        _yellowWireBtn.Toggled = settings.WireYellow;
        _actuatorBtn.Toggled = settings.Actuator;
    }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _placeModeBtn.Toggled = settings.Mode == WiringMode.Place;
        _removeModeBtn.Toggled = settings.Mode == WiringMode.Remove;
    }

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;

        _rectFilledBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _edgeBtn.Toggled = shape.Shape == ShapeType.Edge;
        _straightBtn.Toggled = shape.Shape == ShapeType.StraightLine;
        _diamondFilledBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
        _halfEllipseHFilledBtn.Toggled = shape.Shape == ShapeType.HalfEllipseH && shape.FillMode == ShapeMode.Filled;
        _halfEllipseVFilledBtn.Toggled = shape.Shape == ShapeType.HalfEllipseV && shape.FillMode == ShapeMode.Filled;
    }

    private void UpdateThicknessDisplay()
    {
        var settings = GetSettings();
        _thicknessValue?.SetText(settings?.Shape.Thickness.ToString() ?? "1");
    }

    private void SyncFromSettings()
    {
        UpdateWireButtons();
        UpdateModeButtons();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SyncFromSettings();

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            IsVisible = false;
    }

    public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
    }
}