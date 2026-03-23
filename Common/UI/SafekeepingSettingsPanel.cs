using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.Systems;
using WorldShapingWandsMod.Common.UI.Elements;
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.UI;

public class SafekeepingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Mode toggle
    private UIToggleButton _protectBtn, _unprotectBtn;

    // Target toggles
    private UIToggleButton _protectTilesBtn, _protectWallsBtn;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn;
    private UISliceGrid _sliceGrid;

    // Clear All
    private bool _clearConfirmPending;
    private double _clearConfirmExpiry;
    private UITextPanel<string> _clearBtn;

    private WandPanelBuilder _builder;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float Padding = 10f;

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(54, 44, 79, 220);
        _mainPanel.BorderColor = new Color(30, 20, 60);
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Safekeeping.Title");

        // === MODE ===
        _builder.AddSectionHeader("Safekeeping.Mode");
        _builder.AddToggleRow(
            "Safekeeping.Protect", out _protectBtn, new Color(80, 180, 80),
            "Safekeeping.Unprotect", out _unprotectBtn, new Color(200, 80, 80));

        // === TARGETS ===
        _builder.AddToggleRow(
            "Safekeeping.ProtectTiles", out _protectTilesBtn, new Color(100, 140, 200),
            "Safekeeping.ProtectWalls", out _protectWallsBtn, new Color(150, 100, 180),
            spacing: WandPanelBuilder.AfterToggleGroupSpacing);

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;     _rectHollowBtn    = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled;  _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled;  _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === OPTIONS ===
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleInvertSel", AssetRequestMode.ImmediateLoad);

        _builder.AddOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,    "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,   "Common.InvertSelection",      isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];

        // === CLEAR ALL ===
        _builder.AddCenteredButton("Safekeeping.ClearAll", 120f, 30f, out _clearBtn);
        _clearBtn.OnLeftClick += (_, _) =>
        {
            int tiles = SafekeepingSystem.ProtectedTileCount;
            int walls = SafekeepingSystem.ProtectedWallCount;
            int totalProtected = tiles + walls;

            var config = ModContent.GetInstance<WandServerConfig>();
            int threshold = config?.SafekeepingClearThreshold ?? 50;

            if (threshold > 0 && totalProtected >= threshold && !_clearConfirmPending)
            {
                _clearConfirmPending = true;
                _clearConfirmExpiry = Main.GameUpdateCount + 180;
                _clearBtn.SetText(L("Safekeeping.ClearConfirm"));
                _clearBtn.BackgroundColor = new Color(180, 60, 60);
                Main.NewText(Get("ClearConfirmPrompt", tiles, walls), Color.Orange);
                return;
            }

            _clearConfirmPending = false;
            _clearBtn.SetText(L("Safekeeping.ClearAll"));
            _clearBtn.BackgroundColor = new Color(63, 82, 151) * 0.7f;
            SafekeepingSystem.ClearAll();
            Main.NewText(Get("ClearedProtection", tiles, walls), Color.LightCoral);
        };

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===
        _protectBtn.OnToggled += (_, _) => { GetSettings().Mode = SafekeepingMode.Protect; UpdateModeButtons(); };
        _unprotectBtn.OnToggled += (_, _) => { GetSettings().Mode = SafekeepingMode.Unprotect; UpdateModeButtons(); };

        _protectTilesBtn.OnToggled += (_, _) => GetSettings().ProtectTiles = _protectTilesBtn.Toggled;
        _protectWallsBtn.OnToggled += (_, _) => GetSettings().ProtectWalls = _protectWallsBtn.Toggled;

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

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
    }

    private WandOfSafekeepingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.SafekeepingSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter, settings.Shape.InvertSelection);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        int max = ModContent.GetInstance<WandServerConfig>()?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _protectBtn.Toggled = settings.Mode == SafekeepingMode.Protect;
        _unprotectBtn.Toggled = settings.Mode == SafekeepingMode.Unprotect;
    }

    private void UpdateTargetButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _protectTilesBtn.Toggled = settings.ProtectTiles;
        _protectWallsBtn.Toggled = settings.ProtectWalls;
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

    private void UpdateThicknessDisplay() { _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1"); }
    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateSliceGrid() { var s = GetSettings(); if (s == null || _sliceGrid == null) return; _sliceGrid.SetValue(s.Shape.Slice); }

    private void SyncFromSettings()
    {
        UpdateModeButtons();
        UpdateTargetButtons();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateEqualDimensionsButton();
        UpdateSliceGrid();
        UpdateConnectDiameterButton();
        UpdateInvertSelectionButton();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SyncFromSettings();

        if (_clearConfirmPending && Main.GameUpdateCount > _clearConfirmExpiry)
        {
            _clearConfirmPending = false;
            _clearBtn.SetText(L("Safekeeping.ClearAll"));
            _clearBtn.BackgroundColor = new Color(63, 82, 151) * 0.7f;
        }

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            ModContent.GetInstance<WandUISystem>().CloseAllUI();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
        _builder?.DrawDebugLines(spriteBatch, _mainPanel.GetDimensions());
    }
}