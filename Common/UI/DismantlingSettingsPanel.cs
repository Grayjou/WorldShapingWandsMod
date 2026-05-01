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

    // Destroy toggles (icon buttons)
    private UIIconButton _destroyTilesBtn, _destroyWallsBtn, _destroyContainersBtn;

    // Void Everything icon button (Carefree Mode only)
    private UIIconButton _voidEverythingBtn;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;

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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.DismantlingBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.DismantlingBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Dismantling.Title");

        // === DESTROY OPTIONS (icon buttons) ===
        var texDestroyTiles = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Destroy/DestroyTiles", AssetRequestMode.ImmediateLoad);
        var texDestroyWalls = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Destroy/DestroyWalls", AssetRequestMode.ImmediateLoad);
        var texDestroyContainers = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Destroy/DestroyContainers", AssetRequestMode.ImmediateLoad);
        var texVoidEverything = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Misc/VoidEverything", AssetRequestMode.ImmediateLoad);

        _builder.AddIconToggleRow("Dismantling.Options", new WandPanelBuilder.IconDef[]
        {
            new(texDestroyTiles,      "Dismantling.DestroyTiles",      isToggle: true, initialState: true),
            new(texDestroyWalls,      "Dismantling.DestroyWalls",      isToggle: true),
            new(texDestroyContainers, "Dismantling.DestroyContainers", isToggle: true),
            new(texVoidEverything,    "Dismantling.VoidEverythingTooltip", isToggle: true),
        }, out var destroyBtns);
        _destroyTilesBtn      = destroyBtns[0];
        _destroyWallsBtn      = destroyBtns[1];
        _destroyContainersBtn = destroyBtns[2];
        _voidEverythingBtn    = destroyBtns[3];

        // Tint the destroy buttons to match their semantic colors
        _destroyTilesBtn.ActiveColor      = WandPanelTheme.DestroyCategories.Tiles;
        _destroyWallsBtn.ActiveColor      = WandPanelTheme.DestroyCategories.Walls;
        _destroyContainersBtn.ActiveColor = WandPanelTheme.DestroyCategories.Containers;
        _voidEverythingBtn.ActiveColor    = WandPanelTheme.DestroyCategories.Void;

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;
        _rectHollowBtn    = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled;
        _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled;
        _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled;
        _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn          = shapes.Elbow;
        _cardinalBtn      = shapes.Cardinal;
        _straightLineBtn  = shapes.StraightLine;
        _moldBtn          = shapes.Mold;
        // (S11 2026-04-29 — Bug 3 fix; StencilEditVsActOn.md §3)
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);

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
        _destroyTilesBtn.OnToggled += (_, _) => GetSettings().DestroyTiles = _destroyTilesBtn.Toggled;
        _destroyWallsBtn.OnToggled += (_, _) => GetSettings().DestroyWalls = _destroyWallsBtn.Toggled;
        _destroyContainersBtn.OnToggled += (_, _) => GetSettings().DestroyContainers = _destroyContainersBtn.Toggled;

        // Void Everything: true toggle — controls whether next dismantling voids
        _voidEverythingBtn.OnToggled += (_, _) =>
        {
            var s = GetSettings();
            if (s != null) s.VoidEverything = _voidEverythingBtn.Toggled;
        };

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
        _moldBtn.OnToggled += (_, _) => SetShape(ShapeType.Mold, ShapeMode.Filled);

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();

    }

    private WandOfDismantlingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.DismantlingSettings;

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

    private void UpdateOptionsButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _destroyTilesBtn.Toggled = settings.DestroyTiles;
        _destroyWallsBtn.Toggled = settings.DestroyWalls;
        _destroyContainersBtn.Toggled = settings.DestroyContainers;
        _voidEverythingBtn.Toggled = settings.VoidEverything;
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
        _moldBtn.Toggled = shape.Shape == ShapeType.Mold;
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
        UpdateInvertSelectionButton();
        UpdateFlipHalfOrientationButton();
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

        // Void Everything: always visible, but Disabled when Carefree Mode is off
        var serverConfig = WandConfigs.Carefree;
        bool carefreeEnabled = serverConfig?.EnableCarefreeMode == true;
        if (_voidEverythingBtn != null)
        {
            _voidEverythingBtn.Disabled = !carefreeEnabled;
            _voidEverythingBtn.HoverText = carefreeEnabled
                ? L("Dismantling.VoidEverythingTooltip")
                : L("Dismantling.VoidEverythingDisabled");

            // Auto-untoggle if Carefree Mode gets disabled while VoidEverything is on
            if (!carefreeEnabled && _voidEverythingBtn.Toggled)
            {
                _voidEverythingBtn.Toggled = false;
                var s = GetSettings();
                if (s != null) s.VoidEverything = false;
            }
        }

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