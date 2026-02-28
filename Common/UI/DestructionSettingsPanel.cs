using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

public class DestructionSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    private UIDraggablePanel _mainPanel;

    // Destruction toggles
    private UIToggleButton _destroyTilesBtn, _destroyWallsBtn, _suppressDropsBtn;

    // Shape buttons
    private UIToggleButton _rectFilledBtn, _rectHollowBtn, _rectOutlineBtn;
    private UIToggleButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIToggleButton _diamondFilledBtn, _diamondHollowBtn;
    private UIToggleButton _triangleFilledBtn, _triangleHollowBtn;
    private UIToggleButton _lineBtn;

    private UIText _thicknessValue;

    private const float PanelWidth = 320f;
    private const float PanelHeight = 420f;
    private const float Padding = 10f;
    private const float ButtonWidth = 140f;
    private const float ButtonHeight = 28f;

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
        var title = new UISectionTitle("Wand of Destruction");
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // === DESTRUCTION OPTIONS ===
        var optionsSection = new UISectionTitle("Options");
        optionsSection.Width.Set(0f, 1f);
        optionsSection.Height.Set(22f, 0f);
        optionsSection.Top.Set(y, 0f);
        _mainPanel.Append(optionsSection);
        y += 28f;

        _destroyTilesBtn = MakeToggle("Destroy Tiles", col1, y, new Color(200, 80, 80));
        _destroyWallsBtn = MakeToggle("Destroy Walls", col2, y, new Color(150, 100, 80));
        _mainPanel.Append(_destroyTilesBtn);
        _mainPanel.Append(_destroyWallsBtn);
        y += 34f;

        _suppressDropsBtn = MakeToggle("Suppress Drops", col1, y, new Color(100, 100, 100));
        _mainPanel.Append(_suppressDropsBtn);
        y += 42f;

        // === SHAPE SECTION ===
        var shapeSection = new UISectionTitle("Shape");
        shapeSection.Width.Set(0f, 1f);
        shapeSection.Height.Set(22f, 0f);
        shapeSection.Top.Set(y, 0f);
        _mainPanel.Append(shapeSection);
        y += 28f;

        _rectFilledBtn = MakeRadio("Rect Filled", col1, y);
        _rectHollowBtn = MakeRadio("Rect Hollow", col2, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_rectHollowBtn);
        y += 34f;

        _rectOutlineBtn = MakeRadio("Rect Outline", col1, y);
        _lineBtn = MakeRadio("Line", col2, y);
        _mainPanel.Append(_rectOutlineBtn);
        _mainPanel.Append(_lineBtn);
        y += 34f;

        _ellipseFilledBtn = MakeRadio("Ellipse Filled", col1, y);
        _ellipseHollowBtn = MakeRadio("Ellipse Hollow", col2, y);
        _mainPanel.Append(_ellipseFilledBtn);
        _mainPanel.Append(_ellipseHollowBtn);
        y += 34f;

        _diamondFilledBtn = MakeRadio("Diamond Filled", col1, y);
        _diamondHollowBtn = MakeRadio("Diamond Hollow", col2, y);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_diamondHollowBtn);
        y += 34f;

        _triangleFilledBtn = MakeRadio("Triangle Filled", col1, y);
        _triangleHollowBtn = MakeRadio("Triangle Hollow", col2, y);
        _mainPanel.Append(_triangleFilledBtn);
        _mainPanel.Append(_triangleHollowBtn);
        y += 42f;

        // Thickness
        var thicknessLabel = new UIText("Outline Thickness:", 0.85f);
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
        var closeBtn = new UITextPanel<string>("Close", 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => IsVisible = false;
        _mainPanel.Append(closeBtn);

        // Wire up events
        _destroyTilesBtn.OnToggled += (_, _) => GetSettings().DestroyTiles = _destroyTilesBtn.Toggled;
        _destroyWallsBtn.OnToggled += (_, _) => GetSettings().DestroyWalls = _destroyWallsBtn.Toggled;
        _suppressDropsBtn.OnToggled += (_, _) => GetSettings().SuppressDrops = _suppressDropsBtn.Toggled;

        _rectFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _rectHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Hollow);
        _rectOutlineBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Outline);
        _lineBtn.OnToggled += (_, _) => SetShape(ShapeType.Line, ShapeMode.Filled);
        _ellipseFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Filled);
        _ellipseHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Hollow);
        _diamondFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _diamondHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Hollow);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _triangleHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Hollow);
    }

    private WandOfDestructionSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.DestructionSettings;

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

    private void UpdateOptionsButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _destroyTilesBtn.Toggled = settings.DestroyTiles;
        _destroyWallsBtn.Toggled = settings.DestroyWalls;
        _suppressDropsBtn.Toggled = settings.SuppressDrops;
    }

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;

        _rectFilledBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _rectHollowBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Hollow;
        _rectOutlineBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Outline;
        _lineBtn.Toggled = shape.Shape == ShapeType.Line;
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