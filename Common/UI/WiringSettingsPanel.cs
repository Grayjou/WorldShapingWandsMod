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
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

public class WiringSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    /// <summary>Exposes the inner draggable panel for accurate ContainsPoint checks in WandUISystem.</summary>
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Wire toggles
    private UIToggleButton _redWireBtn, _greenWireBtn, _blueWireBtn, _yellowWireBtn, _actuatorBtn;

    // Mode buttons
    private UIToggleButton _placeModeBtn, _removeModeBtn;

    // Shape buttons (icon-based)
    private UIIconButton _rectFilledBtn, _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _diamondFilledBtn, _triangleFilledBtn;

    private UIText _thicknessValue;

    // Equal Dimensions toggle
    private UIToggleButton _equalDimensionsBtn;

    // Slice grid
    private UISliceGrid _sliceGrid;

    // Connect diameter toggle
    private UIToggleButton _connectDiameterBtn;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float PanelHeight = 630f;
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
        var texElbow           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeElbow", AssetRequestMode.ImmediateLoad);
        var texCardinal       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeCardinal", AssetRequestMode.ImmediateLoad);
        var texStraightLine   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraightLine", AssetRequestMode.ImmediateLoad);
        var texDiamondFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondFilled", AssetRequestMode.ImmediateLoad);
        var texTriangleFilled = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad);

        // Shape row 1: 5 icons
        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

        _rectFilledBtn    = MakeIconBtn(texRectFilled,     L("Common.ShapeRectFilled"),    shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _edgeBtn          = MakeIconBtn(texElbow,           L("Common.ShapeElbow"),          shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _cardinalBtn      = MakeIconBtn(texCardinal,       L("Common.ShapeCardinal"),      shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _diamondFilledBtn = MakeIconBtn(texDiamondFilled,  L("Common.ShapeDiamondFilled"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _triangleFilledBtn = MakeIconBtn(texTriangleFilled, L("Common.ShapeTriangleFilled"), shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_edgeBtn);
        _mainPanel.Append(_cardinalBtn);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_triangleFilledBtn);
        y += IconBtnSize + IconGap;

        // Row 2: additional line shapes
        _straightLineBtn   = MakeIconBtn(texStraightLine,   L("Common.ShapeStraightLine"),   shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _mainPanel.Append(_straightLineBtn);
        y += IconBtnSize + 12f;

        // === SLICE SECTION ===
        var sliceSection = new UISectionTitle(L("Common.Slice"));
        sliceSection.Width.Set(0f, 1f);
        sliceSection.Height.Set(22f, 0f);
        sliceSection.Top.Set(y, 0f);
        _mainPanel.Append(sliceSection);
        y += 28f;

        _sliceGrid = new UISliceGrid();
        _sliceGrid.HAlign = 0.5f;
        _sliceGrid.Top.Set(y, 0f);
        _sliceGrid.OnChanged += OnSliceChanged;
        _mainPanel.Append(_sliceGrid);
        y += _sliceGrid.Height.Pixels + 12f;

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

        // === EQUAL DIMENSIONS TOGGLE ===
        _equalDimensionsBtn = new UIToggleButton(L("Common.EqualDimensions"), false);
        _equalDimensionsBtn.Width.Set(200f, 0f);
        _equalDimensionsBtn.Height.Set(28f, 0f);
        _equalDimensionsBtn.HAlign = 0.5f;
        _equalDimensionsBtn.Top.Set(y, 0f);
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _mainPanel.Append(_equalDimensionsBtn);
        y += 38f;

        // === CONNECT DIAMETER TOGGLE ===
        _connectDiameterBtn = new UIToggleButton(L("Common.ConnectDiameter"), true);
        _connectDiameterBtn.Width.Set(200f, 0f);
        _connectDiameterBtn.Height.Set(28f, 0f);
        _connectDiameterBtn.HAlign = 0.5f;
        _connectDiameterBtn.Top.Set(y, 0f);
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _mainPanel.Append(_connectDiameterBtn);
        y += 38f;

        // Close button
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => ModContent.GetInstance<WandUISystem>().CloseAllUI();
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
        _edgeBtn.OnToggled += (_, _) => SetShape(ShapeType.Elbow, ShapeMode.Filled);
        _cardinalBtn.OnToggled += (_, _) => SetShape(ShapeType.CardinalLine, ShapeMode.Filled);
        _straightLineBtn.OnToggled += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
        _diamondFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
    }

    private WandOfWiringSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.WiringSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        int max = ModContent.GetInstance<Configs.WandConfig>()?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
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
        _edgeBtn.Toggled = shape.Shape == ShapeType.Elbow;
        _cardinalBtn.Toggled = shape.Shape == ShapeType.CardinalLine;
        _straightLineBtn.Toggled = shape.Shape == ShapeType.StraightLine;
        _diamondFilledBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
    }

    private void ToggleEqualDimensions()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.EqualDimensions = _equalDimensionsBtn.Toggled;
        settings.Shape = shape;
    }

    private void ToggleConnectDiameter()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.ConnectDiameter = _connectDiameterBtn.Toggled;
        settings.Shape = shape;
    }

    private void OnSliceChanged(SliceMode slice)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.Slice = slice;
        settings.Shape = shape;
    }

    private void UpdateEqualDimensionsButton()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _equalDimensionsBtn.Toggled = settings.Shape.EqualDimensions;
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
        UpdateEqualDimensionsButton();
        UpdateSliceGrid();
        UpdateConnectDiameterButton();
    }

    private void UpdateConnectDiameterButton()
    {
        var settings = GetSettings();
        if (settings == null || _connectDiameterBtn == null) return;
        _connectDiameterBtn.Toggled = settings.Shape.ConnectDiameter;
    }

    private void UpdateSliceGrid()
    {
        var settings = GetSettings();
        if (settings == null || _sliceGrid == null) return;
        _sliceGrid.SetValue(settings.Shape.Slice);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SyncFromSettings();

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            ModContent.GetInstance<WandUISystem>().CloseAllUI();
    }

    public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
    }
}