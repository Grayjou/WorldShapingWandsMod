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
    private UIIconButton _magicWandReadBtn;

    private UIText _thicknessValue;

    // Toggle icon buttons
    private UIIconButton _equalDimensionsBtn;
    private UIIconButton _connectDiameterBtn;
    private UIIconButton _invertSelectionBtn;
    private UIIconButton _flipHalfOrientationBtn;

    // Slice grid
    private UISliceGrid _sliceGrid;

    // Builder (retained for debug drawing)
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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.WiringBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.WiringBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Wiring.Title");

        // === WIRE TYPES ===
        _builder.AddSectionHeader("Wiring.WireTypes");
        _builder.AddToggleRow(
            "Wiring.RedWire", out _redWireBtn, WandPanelTheme.Wires.Red,
            "Wiring.GreenWire", out _greenWireBtn, WandPanelTheme.Wires.Green);
        _builder.AddToggleRow(
            "Wiring.BlueWire", out _blueWireBtn, WandPanelTheme.Wires.Blue,
            "Wiring.YellowWire", out _yellowWireBtn, WandPanelTheme.Wires.Yellow);
        _builder.AddToggleSingle("Wiring.Actuator", out _actuatorBtn, WandPanelTheme.Wires.Actuator);

        // === MODE ===
        _builder.AddSectionHeader("Wiring.Mode");
        _builder.AddToggleRow(
            "Wiring.Place", out _placeModeBtn, WandPanelTheme.Wires.PlaceMode,
            "Wiring.Remove", out _removeModeBtn, WandPanelTheme.Wires.RemoveMode,
            spacing: WandPanelBuilder.AfterToggleGroupSpacing);
        _placeModeBtn.IsRadio = true;
        _removeModeBtn.IsRadio = true;

        // === SHAPE (reduced � wiring only) ===
        _builder.AddWiringShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;
        _edgeBtn          = shapes.Elbow;
        _cardinalBtn      = shapes.Cardinal;
        _diamondFilledBtn = shapes.DiamondFilled;
        _triangleFilledBtn = shapes.TriangleFilled;
        _straightLineBtn  = shapes.StraightLine;
        _magicWandReadBtn = shapes.MagicWandRead;

        // (S4 2026-05-01 � StencilMagicWandSelectionPlan.md �4.1) Right-click on
        // the Magic Wand Read shape cell opens the Read configuration SubUI.
        // The SubUI's underlying state (MagicWandReadConfig) is a player-scoped
        // preference shared across every wand, so the wiring is centralised in
        // MagicWandReadCellWiring (mirrors the MoldCellWiring singleton model).
        Common.UI.Elements.MagicWandReadCellWiring.WireConfigSubUI(_magicWandReadBtn);

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === OPTIONS ===
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        // (S2 2026-04-30 — InvertHalfOrientation #IOP) placeholder reuses ToggleInvertSel.
        // TODO: pending ToggleFlipHalfOrientation dedicated asset (placeholder = ToggleInvertSel byte-copy; tracked in dev_notes/dev_tasks/pending_assets.md §3b)
        var texFlipHalf    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,    "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,   "Common.InvertSelection",      isToggle: true),
            new(texFlipHalf,    "Common.FlipHalfOrientation",  isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===
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
        _magicWandReadBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandRead, ShapeMode.Filled);

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
    }

    private WandOfWiringSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.WiringSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter, settings.Shape.InvertSelection, settings.Shape.InvertHalfOrientation);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
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

    private void ToggleInvertSelection()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.InvertSelection = _invertSelectionBtn.Toggled;
        settings.Shape = shape;
    }

    private void ToggleFlipHalfOrientation()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.InvertHalfOrientation = _flipHalfOrientationBtn.Toggled;
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
        _magicWandReadBtn.Toggled = shape.Shape == ShapeType.MagicWandRead;
    }

    private void UpdateThicknessDisplay()
    {
        var settings = GetSettings();
        _thicknessValue?.SetText(settings?.Shape.Thickness.ToString() ?? "1");
    }

    private void UpdateEqualDimensionsButton()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _equalDimensionsBtn.Toggled = settings.Shape.EqualDimensions;
    }

    private void UpdateConnectDiameterButton()
    {
        var settings = GetSettings();
        if (settings == null || _connectDiameterBtn == null) return;
        _connectDiameterBtn.Toggled = settings.Shape.ConnectDiameter;
    }

    private void UpdateInvertSelectionButton()
    {
        var settings = GetSettings();
        if (settings == null || _invertSelectionBtn == null) return;
        _invertSelectionBtn.Toggled = settings.Shape.InvertSelection;
        _invertSelectionBtn.Disabled = !settings.Shape.SupportsInversion;
    }

    private void UpdateFlipHalfOrientationButton()
    {
        var settings = GetSettings();
        if (settings == null || _flipHalfOrientationBtn == null) return;
        _flipHalfOrientationBtn.Toggled = settings.Shape.InvertHalfOrientation;
        _flipHalfOrientationBtn.Disabled = settings.Shape.Slice == SliceMode.Full;
    }

    private void UpdateSliceGrid()
    {
        var settings = GetSettings();
        if (settings == null || _sliceGrid == null) return;
        _sliceGrid.SetValue(settings.Shape.Slice);
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
        UpdateInvertSelectionButton();
        UpdateFlipHalfOrientationButton();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SyncFromSettings();

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            // (S5 2026-04-25 — Letter #4 §2) Esc preserves IV intent; CloseAllPanels.
            ModContent.GetInstance<WandUISystem>().CloseAllPanels();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
        _builder?.DrawDebugLines(spriteBatch, _mainPanel.GetDimensions());
    }
}