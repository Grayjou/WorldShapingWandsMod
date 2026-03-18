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

public class DismantlingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    /// <summary>Exposes the inner draggable panel for accurate ContainsPoint checks in WandUISystem.</summary>
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Dismantling toggles
    private UIToggleButton _destroyTilesBtn, _destroyWallsBtn, _destroyContainersBtn;

    // Shape buttons (icon-based)
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn;
    private UIIconButton _cardinalBtn;
    private UIIconButton _straightLineBtn;

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
    private const float PanelHeight = 600f;
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
        _mainPanel.BackgroundColor = new Color(79, 44, 44, 220);  // Reddish tint
        _mainPanel.BorderColor = new Color(60, 20, 20);
        Append(_mainPanel);

        float y = 8f;
        float col1 = Padding;
        float col2 = PanelWidth - Padding - ButtonWidth - 12f;

        // Title
        var title = new UISectionTitle(L("Dismantling.Title"));
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // === DESTRUCTION OPTIONS ===
        var optionsSection = new UISectionTitle(L("Dismantling.Options"));
        optionsSection.Width.Set(0f, 1f);
        optionsSection.Height.Set(22f, 0f);
        optionsSection.Top.Set(y, 0f);
        _mainPanel.Append(optionsSection);
        y += 28f;

        _destroyTilesBtn = MakeToggle(L("Dismantling.DestroyTiles"), col1, y, new Color(200, 80, 80));
        _destroyWallsBtn = MakeToggle(L("Dismantling.DestroyWalls"), col2, y, new Color(150, 100, 80));
        _mainPanel.Append(_destroyTilesBtn);
        _mainPanel.Append(_destroyWallsBtn);
        y += 36f;

        _destroyContainersBtn = MakeToggle(L("Dismantling.DestroyContainers"), col1, y, new Color(180, 130, 60));
        _destroyContainersBtn.HoverText = L("Dismantling.DestroyContainersTooltip");
        _mainPanel.Append(_destroyContainersBtn);
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
        var texRectHollow     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectHollow", AssetRequestMode.ImmediateLoad);
        var texEllipseFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseFilled", AssetRequestMode.ImmediateLoad);
        var texEllipseHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseHollow", AssetRequestMode.ImmediateLoad);
        var texDiamondFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondFilled", AssetRequestMode.ImmediateLoad);
        var texDiamondHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondHollow", AssetRequestMode.ImmediateLoad);
        var texTriangleFilled = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad);
        var texTriangleHollow = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleHollow", AssetRequestMode.ImmediateLoad);
        var texElbow           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeElbow", AssetRequestMode.ImmediateLoad);
        var texCardinal       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeCardinal", AssetRequestMode.ImmediateLoad);
        var texStraightLine   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraightLine", AssetRequestMode.ImmediateLoad);

        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

        _rectFilledBtn    = MakeIconBtn(texRectFilled,     L("Common.ShapeRectFilled"),    shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _rectHollowBtn    = MakeIconBtn(texRectHollow,     L("Common.ShapeRectHollow"),    shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _ellipseFilledBtn = MakeIconBtn(texEllipseFilled,  L("Common.ShapeEllipseFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _ellipseHollowBtn = MakeIconBtn(texEllipseHollow,  L("Common.ShapeEllipseHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _edgeBtn          = MakeIconBtn(texElbow,           L("Common.ShapeElbow"),          shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_rectHollowBtn);
        _mainPanel.Append(_ellipseFilledBtn);
        _mainPanel.Append(_ellipseHollowBtn);
        _mainPanel.Append(_edgeBtn);
        y += IconBtnSize + IconGap;

        _diamondFilledBtn  = MakeIconBtn(texDiamondFilled,  L("Common.ShapeDiamondFilled"),  shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _diamondHollowBtn  = MakeIconBtn(texDiamondHollow,  L("Common.ShapeDiamondHollow"),  shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _triangleFilledBtn = MakeIconBtn(texTriangleFilled, L("Common.ShapeTriangleFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _triangleHollowBtn = MakeIconBtn(texTriangleHollow, L("Common.ShapeTriangleHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _cardinalBtn       = MakeIconBtn(texCardinal,       L("Common.ShapeCardinal"),       shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_diamondHollowBtn);
        _mainPanel.Append(_triangleFilledBtn);
        _mainPanel.Append(_triangleHollowBtn);
        _mainPanel.Append(_cardinalBtn);
        y += IconBtnSize + IconGap;

        // Row 3: additional line shapes
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
        minusBtn.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
        _mainPanel.Append(minusBtn);

        _thicknessValue = new UIText("1", 0.9f);
        _thicknessValue.Left.Set(col1 + 170f, 0f);
        _thicknessValue.Top.Set(y, 0f);
        _thicknessValue.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
        _mainPanel.Append(_thicknessValue);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(y - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => AdjustThickness(1);
        plusBtn.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
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
        _destroyTilesBtn.OnToggled += (_, _) => GetSettings().DestroyTiles = _destroyTilesBtn.Toggled;
        _destroyWallsBtn.OnToggled += (_, _) => GetSettings().DestroyWalls = _destroyWallsBtn.Toggled;
        _destroyContainersBtn.OnToggled += (_, _) => GetSettings().DestroyContainers = _destroyContainersBtn.Toggled;

        _rectFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _rectHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Hollow);
        _edgeBtn.OnToggled += (_, _) => SetShape(ShapeType.Elbow, ShapeMode.Filled);
        _cardinalBtn.OnToggled += (_, _) => SetShape(ShapeType.CardinalLine, ShapeMode.Filled);
        _straightLineBtn.OnToggled += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
        _ellipseFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Filled);
        _ellipseHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Hollow);
        _diamondFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _diamondHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Hollow);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _triangleHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Hollow);
    }

    private WandOfDismantlingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.DismantlingSettings;

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

    private void UpdateOptionsButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _destroyTilesBtn.Toggled = settings.DestroyTiles;
        _destroyWallsBtn.Toggled = settings.DestroyWalls;
        _destroyContainersBtn.Toggled = settings.DestroyContainers;
    }

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;

        _rectFilledBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _rectHollowBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Hollow;
        _edgeBtn.Toggled = shape.Shape == ShapeType.Elbow;
        _cardinalBtn.Toggled = shape.Shape == ShapeType.CardinalLine;
        _straightLineBtn.Toggled = shape.Shape == ShapeType.StraightLine;
        _ellipseFilledBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Filled;
        _ellipseHollowBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Hollow;
        _diamondFilledBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _diamondHollowBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Hollow;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
        _triangleHollowBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Hollow;
    }

    private void UpdateThicknessDisplay()
    {
        var settings = GetSettings();
        _thicknessValue?.SetText(settings?.Shape.Thickness.ToString() ?? "1");
    }

    private void SyncFromSettings()
    {
        UpdateOptionsButtons();
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

    private void UpdateEqualDimensionsButton()
    {
        var settings = GetSettings();
        if (settings == null || _equalDimensionsBtn == null) return;
        _equalDimensionsBtn.Toggled = settings.Shape.EqualDimensions;
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