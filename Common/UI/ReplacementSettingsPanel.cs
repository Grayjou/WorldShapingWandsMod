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
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

public class ReplacementSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _paintSprayerBtn;
    private UISliceGrid _sliceGrid;

    // Source (OldObject) type buttons
    private UIIconButton _srcTileBtn, _srcPlatformBtn, _srcRopeBtn, _srcPlanterBtn, _srcWallBtn;

    // Target (NewObject) type buttons
    private UIIconButton _tgtSameBtn, _tgtTileBtn, _tgtPlatformBtn, _tgtRopeBtn, _tgtPlanterBtn, _tgtAirBtn;

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
        _mainPanel.BackgroundColor = new Color(44, 79, 44, 220);
        _mainPanel.BorderColor = new Color(20, 60, 20);
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        // Load object type icon textures
        var texTile      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjTile", AssetRequestMode.ImmediateLoad);
        var texPlatform  = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlatform", AssetRequestMode.ImmediateLoad);
        var texRope      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjRope", AssetRequestMode.ImmediateLoad);
        var texPlanter   = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlanter", AssetRequestMode.ImmediateLoad);
        var texAir       = mod.Assets.Request<Texture2D>("Assets/Icons/ObjAir", AssetRequestMode.ImmediateLoad);
        var texWall      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjWall", AssetRequestMode.ImmediateLoad);
        var texSameType  = mod.Assets.Request<Texture2D>("Assets/Icons/ObjSameType", AssetRequestMode.ImmediateLoad);

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Replacement.Title");

        // === SOURCE TYPE (OldObject - what to find) ===
        _builder.AddSectionHeader("Replacement.SourceType");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texTile,     "Replacement.Tile"),
            new(texPlatform, "Replacement.Platform"),
            new(texRope,     "Replacement.Rope"),
            new(texPlanter,  "Replacement.Planter"),
            new(texWall,     "Replacement.Wall"),
        }, iconsPerRow: 5, out var srcBtns);
        _srcTileBtn     = srcBtns[0];
        _srcPlatformBtn = srcBtns[1];
        _srcRopeBtn     = srcBtns[2];
        _srcPlanterBtn  = srcBtns[3];
        _srcWallBtn     = srcBtns[4];

        // === TARGET TYPE (NewObject - what to replace with) ===
        _builder.AddSectionHeader("Replacement.TargetType");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texSameType, "Replacement.SameType"),
            new(texTile,     "Replacement.Tile"),
            new(texPlatform, "Replacement.Platform"),
            new(texRope,     "Replacement.Rope"),
            new(texPlanter,  "Replacement.Planter"),
            new(texAir,      "Replacement.Air"),
        }, iconsPerRow: 6, out var tgtBtns);
        _tgtSameBtn     = tgtBtns[0];
        _tgtTileBtn     = tgtBtns[1];
        _tgtPlatformBtn = tgtBtns[2];
        _tgtRopeBtn     = tgtBtns[3];
        _tgtPlanterBtn  = tgtBtns[4];
        _tgtAirBtn      = tgtBtns[5];

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
        var texEqualDim     = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam  = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel    = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        var texPaintSprayer = mod.Assets.Request<Texture2D>("Assets/Icons/TogglePaintSprayer", AssetRequestMode.ImmediateLoad);

        _builder.AddOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,     "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam,  "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,    "Common.InvertSelection",      isToggle: true),
            new(texPaintSprayer, "Common.PaintSprayer",          isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _paintSprayerBtn    = optBtns[3];

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===
        _srcTileBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Tile);
        _srcPlatformBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Platform);
        _srcRopeBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Rope);
        _srcPlanterBtn.OnToggled += (_, _) => SetSourceType(ObjectType.PlanterBox);
        _srcWallBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Wall);

        _tgtSameBtn.OnToggled += (_, _) => SetTargetSameType();
        _tgtTileBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Tile);
        _tgtPlatformBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Platform);
        _tgtRopeBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Rope);
        _tgtPlanterBtn.OnToggled += (_, _) => SetTargetType(ObjectType.PlanterBox);
        _tgtAirBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Air);

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
        _paintSprayerBtn.OnToggled += (_, _) => TogglePaintSprayer();
    }

    private WandOfReplacementSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.ReplacementSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter, settings.Shape.InvertSelection);
        UpdateShapeButtons();
    }

    private void SetSourceType(ObjectType type)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.OldObject = type;
        UpdateSourceButtons();

        if (settings.SameTypeMode)
        {
            settings.NewObject = type;
            UpdateTargetButtons();
            return;
        }

        if (type == ObjectType.Wall)
        {
            if (settings.NewObject != ObjectType.Air)
            {
                settings.NewObject = ObjectType.Wall;
                UpdateTargetButtons();
            }
        }
        else if (settings.NewObject == ObjectType.Wall)
        {
            settings.NewObject = ObjectType.Tile;
            UpdateTargetButtons();
        }
    }

    private void SetTargetType(ObjectType type)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.SameTypeMode = false;
        settings.NewObject = type;
        UpdateTargetButtons();
    }

    private void SetTargetSameType()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.SameTypeMode = true;
        settings.NewObject = settings.OldObject;
        UpdateTargetButtons();
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
    private void TogglePaintSprayer() { var s = GetSettings(); if (s == null) return; s.PaintSprayer = _paintSprayerBtn.Toggled; }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

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

    private void UpdateSourceButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var src = settings.OldObject;
        _srcTileBtn.Toggled = src == ObjectType.Tile;
        _srcPlatformBtn.Toggled = src == ObjectType.Platform;
        _srcRopeBtn.Toggled = src == ObjectType.Rope;
        _srcPlanterBtn.Toggled = src == ObjectType.PlanterBox;
        _srcWallBtn.Toggled = src == ObjectType.Wall;
    }

    private void UpdateTargetButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var tgt = settings.NewObject;
        bool isSame = settings.SameTypeMode;
        _tgtSameBtn.Toggled = isSame;
        _tgtTileBtn.Toggled = tgt == ObjectType.Tile && !isSame;
        _tgtPlatformBtn.Toggled = tgt == ObjectType.Platform && !isSame;
        _tgtRopeBtn.Toggled = tgt == ObjectType.Rope && !isSame;
        _tgtPlanterBtn.Toggled = tgt == ObjectType.PlanterBox && !isSame;
        _tgtAirBtn.Toggled = tgt == ObjectType.Air;
    }

    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateSliceGrid() { var s = GetSettings(); if (s == null || _sliceGrid == null) return; _sliceGrid.SetValue(s.Shape.Slice); }
    private void UpdatePaintSprayerButton() { var s = GetSettings(); if (s == null || _paintSprayerBtn == null) return; _paintSprayerBtn.Toggled = s.PaintSprayer; }

    private void SyncFromSettings()
    {
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateSourceButtons();
        UpdateTargetButtons();
        UpdateEqualDimensionsButton();
        UpdateSliceGrid();
        UpdateConnectDiameterButton();
        UpdateInvertSelectionButton();
        UpdatePaintSprayerButton();
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

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
        _builder?.DrawDebugLines(spriteBatch, _mainPanel.GetDimensions());
    }
}