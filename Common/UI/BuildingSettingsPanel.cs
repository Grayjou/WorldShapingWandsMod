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

public class BuildingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Object type buttons
    private UIIconButton _solidBtn, _platformBtn, _ropeBtn, _railBtn, _grassSeedBtn, _planterBtn, _wallBtn, _noneBtn;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;
    private UIIconButton _magicWandReadBtn;

    private UIText _thicknessValue;

    // Slope buttons
    private UIIconButton _slopeDefaultBtn, _slopeHalfBtn;
    private UIIconButton _slopeBottomRightBtn, _slopeBottomLeftBtn;
    private UIIconButton _slopeTopRightBtn, _slopeTopLeftBtn;

    private UIToggleButton _overwriteSlopeBtn;

    // Options
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _flipHalfOrientationBtn, _paintSprayerBtn, _actuationBtn;
    // S9: third button in the Building.Options row that toggles the InventoryView panel.
    // Makes the InventoryView keybind optional per user S9 directive #3 (panel discoverability).
    // Stateful toggle: button.Toggled mirrors WandUISystem.InventoryViewUI.IsVisible (synced in Update).
    private UIIconButton _openInventoryViewBtn;
    private UISliceGrid _sliceGrid;

    // Paint Sprayer source toggle textures (Off / Inventory / CoatingSettings).
    // See Common/Settings/PaintSprayerSource.cs and DesignDoc_PaintSprayerSourceToggle.md.
    private Asset<Texture2D> _texPaintSprayerOff;
    private Asset<Texture2D> _texPaintSprayerInventory;
    private Asset<Texture2D> _texPaintSprayerCoating;

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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.BuildingBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.BuildingBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Building.Title");

        // === OBJECT TYPE ===
        var texSolid     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjSolid", AssetRequestMode.ImmediateLoad);
        var texPlatform  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjPlatform", AssetRequestMode.ImmediateLoad);
        var texRope      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjRope", AssetRequestMode.ImmediateLoad);
        var texRail      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjRail", AssetRequestMode.ImmediateLoad);
        var texGrassSeed = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjGrassSeed", AssetRequestMode.ImmediateLoad);
        var texPlanter   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjPlanter", AssetRequestMode.ImmediateLoad);
        var texWall      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjWall", AssetRequestMode.ImmediateLoad);
        var texNone      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Misc/WhiteX", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Building.ObjectType");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texSolid,     "Building.SolidBlock"),
            new(texPlatform,  "Building.Platform"),
            new(texWall,      "Building.Wall"),
            new(texRope,      "Building.Rope"),
            new(texRail,      "Building.Rail"),
            new(texGrassSeed, "Building.GrassSeed"),
            new(texPlanter,   "Building.PlanterBox"),
            new(texNone,      "Building.NoObject"),
        }, iconsPerRow: 4, out var objBtns);
        _solidBtn     = objBtns[0];
        _platformBtn  = objBtns[1];
        _wallBtn      = objBtns[2];
        _ropeBtn      = objBtns[3];
        _railBtn      = objBtns[4];
        _grassSeedBtn = objBtns[5];
        _planterBtn   = objBtns[6];
        _noneBtn      = objBtns[7];

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;     _rectHollowBtn    = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled;  _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled;  _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;
        _magicWandReadBtn = shapes.MagicWandRead;

        // (S4 2026-05-01 � StencilMagicWandSelectionPlan.md �4.1) Right-click on
        // the Magic Wand Read shape cell opens the Read configuration SubUI.
        // The SubUI's underlying state (MagicWandReadConfig) is a player-scoped
        // preference shared across every wand, so the wiring is centralised in
        // MagicWandReadCellWiring (mirrors the MoldCellWiring singleton model).
        Common.UI.Elements.MagicWandReadCellWiring.WireConfigSubUI(_magicWandReadBtn);
        // (S11 2026-04-29 — Bug 3 fix; StencilEditVsActOn.md §3)
        // Wire ACT-ON Stencil Picker on Mold cell (right-click) + hover-icon swap.
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === SLOPE ===
        var texDefault     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeDefault", AssetRequestMode.ImmediateLoad);
        var texHalf        = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeHalf", AssetRequestMode.ImmediateLoad);
        var texBottomRight = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeBottomRight", AssetRequestMode.ImmediateLoad);
        var texBottomLeft  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeBottomLeft", AssetRequestMode.ImmediateLoad);
        var texTopRight    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeTopRight", AssetRequestMode.ImmediateLoad);
        var texTopLeft     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Slopes/SlopeTopLeft", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Building.Slope");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texDefault,     "Building.SlopeDefault"),
            new(texHalf,        "Building.SlopeHalf"),
            new(texBottomRight, "Building.SlopeBottomRight"),
            new(texBottomLeft,  "Building.SlopeBottomLeft"),
            new(texTopRight,    "Building.SlopeTopRight"),
            new(texTopLeft,     "Building.SlopeTopLeft"),
        }, iconsPerRow: 6, out var slopeBtns);
        _slopeDefaultBtn     = slopeBtns[0];
        _slopeHalfBtn        = slopeBtns[1];
        _slopeBottomRightBtn = slopeBtns[2];
        _slopeBottomLeftBtn  = slopeBtns[3];
        _slopeTopRightBtn    = slopeBtns[4];
        _slopeTopLeftBtn     = slopeBtns[5];

        // === OVERWRITE SLOPE ===
        _builder.AddCenteredToggle("Building.OverwriteSlope", true, out _overwriteSlopeBtn);

        // === OPTIONS ===
        var texEqualDim     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        // (S2 2026-04-30 — DesignDoc_HalfShapeOrientationFlipToggle.md #IOP)
        // Placeholder reuses ToggleInvertSel art. TODO: dedicated 'flip half'
        // arrow-swap icon (tracked in dev_notes/dev_tasks/pending_assets.md).
        // TODO: pending ToggleFlipHalfOrientation dedicated asset (placeholder = ToggleInvertSel byte-copy; tracked in dev_notes/dev_tasks/pending_assets.md §3b)
        var texFlipHalf     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", AssetRequestMode.ImmediateLoad);
        var texPaintSprayer = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayer", AssetRequestMode.ImmediateLoad);
        var texActuation    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleActuation", AssetRequestMode.ImmediateLoad);
        // S9 placeholder icon for InventoryView toggle — reuses the generic tile-cube glyph.
        // TODO: replace with a dedicated "open block-picker" icon once art is drafted.
        var texInventoryView = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInventoryView", AssetRequestMode.ImmediateLoad);
        _texPaintSprayerOff       = texPaintSprayer;
        _texPaintSprayerInventory = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayerInventory", AssetRequestMode.ImmediateLoad);
        _texPaintSprayerCoating   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayerCoating", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,     "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam,  "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,    "Common.InvertSelection",      isToggle: true),
            new(texFlipHalf,     "Common.FlipHalfOrientation",  isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];

        // === BUILDING OPTIONS (Paint Sprayer + Actuation + InventoryView toggle) ===
        // S9: third button opens/closes the InventoryView panel for the held wand.
        // Makes the InventoryView keybind optional — discoverable from the panel itself.
        _builder.AddIconToggleRow("Building.Options", new WandPanelBuilder.IconDef[]
        {
            new(texPaintSprayer,  "Common.PaintSprayer",      isToggle: true),
            new(texActuation,     "Common.ActuationIgnore",   isToggle: true),
            new(texInventoryView, "Common.OpenInventoryView", isToggle: true),
        }, out var buildOptBtns);
        _paintSprayerBtn    = buildOptBtns[0];
        _paintSprayerBtn.IsRadio = false;
        _paintSprayerBtn.AllowDeselect = true;
        _paintSprayerBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;
        _actuationBtn       = buildOptBtns[1];
        _actuationBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;
        _openInventoryViewBtn = buildOptBtns[2];
        _openInventoryViewBtn.IsRadio = false;
        _openInventoryViewBtn.AllowDeselect = true;
        _openInventoryViewBtn.ActiveColor = WandPanelTheme.Colors.ActiveBlue;
        _openInventoryViewBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===
        _solidBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Solid);
        _platformBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Platform);
        _wallBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Wall);
        _ropeBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Rope);
        _railBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Rail);
        _grassSeedBtn.OnToggled += (_, _) => SetObjectType(PlaceType.GrassSeed);
        _planterBtn.OnToggled += (_, _) => SetObjectType(PlaceType.PlantPot);
        _noneBtn.OnToggled += (_, _) => SetObjectType(PlaceType.None);

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
        _magicWandReadBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandRead, ShapeMode.Filled);

        _slopeDefaultBtn.OnToggled += (_, _) => SetSlope(SlopeType.Default);
        _slopeHalfBtn.OnToggled += (_, _) => SetSlope(SlopeType.VerticalHalf);
        _slopeBottomRightBtn.OnToggled += (_, _) => SetSlope(SlopeType.BottomRight);
        _slopeBottomLeftBtn.OnToggled += (_, _) => SetSlope(SlopeType.BottomLeft);
        _slopeTopRightBtn.OnToggled += (_, _) => SetSlope(SlopeType.TopRight);
        _slopeTopLeftBtn.OnToggled += (_, _) => SetSlope(SlopeType.TopLeft);

        _overwriteSlopeBtn.OnToggled += (_, _) => ToggleOverwriteSlope();
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
        _paintSprayerBtn.OnToggled += (_, _) => CyclePaintSprayer();
        _actuationBtn.OnToggled += (_, _) => CycleActuation();
        _openInventoryViewBtn.OnToggled += (_, _) => ToggleInventoryViewPanel();
    }

    /// <summary>
    /// S9: toggles the InventoryView panel via <see cref="WandUISystem.ToggleInventoryView"/>.
    /// The button's visual <c>Toggled</c> state is re-synced from the panel's actual
    /// <c>IsVisible</c> in <see cref="UpdateInventoryViewButton"/> (single source of truth on
    /// the WandUISystem side, so other open paths — keybind, Escape close, CloseAllUI — stay
    /// reflected in the button without desync).
    /// </summary>
    private void ToggleInventoryViewPanel()
    {
        ModContent.GetInstance<WandUISystem>()?.ToggleInventoryView();
    }

    private WandOfBuildingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.BuildingSettings;

    private void SetObjectType(PlaceType type) { var s = GetSettings(); if (s == null) return; s.Object = type; UpdateObjectButtons(); }

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter, settings.Shape.InvertSelection, settings.Shape.InvertHalfOrientation);
        UpdateShapeButtons();
    }

    private void SetSlope(SlopeType slope) { var s = GetSettings(); if (s == null) return; s.Slope = slope; UpdateSlopeButtons(); }
    private void ToggleOverwriteSlope() { var s = GetSettings(); if (s == null) return; s.OverwriteSlope = _overwriteSlopeBtn.Toggled; }
    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void ToggleFlipHalfOrientation() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertHalfOrientation = _flipHalfOrientationBtn.Toggled; s.Shape = sh; }
    private void TogglePaintSprayer()
    {
        // Legacy entry point retained for any external caller; delegates to the cycler.
        CyclePaintSprayer();
    }

    private void CyclePaintSprayer()
    {
        var s = GetSettings();
        if (s == null) return;
        s.PaintSprayer = s.PaintSprayer.Next();
        UpdatePaintSprayerButton();
    }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

    private void CycleActuation()
    {
        var settings = GetSettings();
        if (settings == null) return;

        if (settings.Actuation == null)
            settings.Actuation = true;
        else if (settings.Actuation == true)
            settings.Actuation = false;
        else
            settings.Actuation = null;

        UpdateActuationButton();
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

    private void UpdateObjectButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _solidBtn.Toggled = settings.Object == PlaceType.Solid;
        _platformBtn.Toggled = settings.Object == PlaceType.Platform;
        _wallBtn.Toggled = settings.Object == PlaceType.Wall;
        _ropeBtn.Toggled = settings.Object == PlaceType.Rope;
        _railBtn.Toggled = settings.Object == PlaceType.Rail;
        _grassSeedBtn.Toggled = settings.Object == PlaceType.GrassSeed;
        _planterBtn.Toggled = settings.Object == PlaceType.PlantPot;
        _noneBtn.Toggled = settings.Object == PlaceType.None;
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
        _magicWandReadBtn.Toggled = shape.Shape == ShapeType.MagicWandRead;
    }

    private void UpdateThicknessDisplay() { _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1"); }

    private void UpdateSlopeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _slopeDefaultBtn.Toggled     = settings.Slope == SlopeType.Default;
        _slopeHalfBtn.Toggled        = settings.Slope == SlopeType.VerticalHalf;
        _slopeBottomRightBtn.Toggled = settings.Slope == SlopeType.BottomRight;
        _slopeBottomLeftBtn.Toggled  = settings.Slope == SlopeType.BottomLeft;
        _slopeTopRightBtn.Toggled    = settings.Slope == SlopeType.TopRight;
        _slopeTopLeftBtn.Toggled     = settings.Slope == SlopeType.TopLeft;
    }

    private void UpdateOverwriteSlopeButton() { var s = GetSettings(); if (s == null || _overwriteSlopeBtn == null) return; _overwriteSlopeBtn.Toggled = s.OverwriteSlope; }
    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateSliceGrid() { var s = GetSettings(); if (s == null || _sliceGrid == null) return; _sliceGrid.SetValue(s.Shape.Slice); }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateFlipHalfOrientationButton() { var s = GetSettings(); if (s == null || _flipHalfOrientationBtn == null) return; _flipHalfOrientationBtn.Toggled = s.Shape.InvertHalfOrientation; _flipHalfOrientationBtn.Disabled = s.Shape.Slice == SliceMode.Full; }
    private void UpdatePaintSprayerButton()
    {
        var s = GetSettings();
        if (s == null || _paintSprayerBtn == null) return;

        // Tri-state visual: Off (grey, base icon), Inventory (saddle-brown tint, backpack icon),
        // Coating (teal tint, coating-wand-tip icon). See PaintSprayerSource.cs.
        switch (s.PaintSprayer)
        {
            case PaintSprayerSource.Off:
                _paintSprayerBtn.Toggled = false;
                _paintSprayerBtn.SetTexture(_texPaintSprayerOff);
                _paintSprayerBtn.HoverText = L("Common.PaintSprayer.Off");
                break;
            case PaintSprayerSource.Inventory:
                _paintSprayerBtn.Toggled = true;
                _paintSprayerBtn.ActiveColor = WandPanelTheme.Colors.PaintSourceBrown;
                _paintSprayerBtn.SetTexture(_texPaintSprayerInventory);
                _paintSprayerBtn.HoverText = L("Common.PaintSprayer.Inventory");
                break;
            case PaintSprayerSource.CoatingSettings:
                _paintSprayerBtn.Toggled = true;
                _paintSprayerBtn.ActiveColor = WandPanelTheme.Colors.PaintCoatingTeal;
                _paintSprayerBtn.SetTexture(_texPaintSprayerCoating);
                _paintSprayerBtn.HoverText = L("Common.PaintSprayer.Coating");
                break;
        }
    }

    private void UpdateActuationButton()
    {
        var settings = GetSettings();
        if (settings == null || _actuationBtn == null) return;

        if (settings.Actuation == null)
        {
            _actuationBtn.Toggled = false;
            _actuationBtn.ActiveColor = WandPanelTheme.Colors.ActiveGreen;
            _actuationBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;
        }
        else if (settings.Actuation == true)
        {
            _actuationBtn.Toggled = true;
            _actuationBtn.ActiveColor = WandPanelTheme.Colors.ActiveGreen;
        }
        else
        {
            _actuationBtn.Toggled = true;
            _actuationBtn.ActiveColor = WandPanelTheme.Colors.ActiveRed;
        }

        string stateKey = settings.Actuation switch
        {
            null => "Common.ActuationIgnore",
            true => "Common.ActuationOn",
            false => "Common.ActuationOff",
        };
        _actuationBtn.HoverText = L(stateKey);
    }

    private void UpdateInventoryViewButton()
    {
        if (_openInventoryViewBtn == null) return;
        // Mirror the actual panel visibility — the WandUISystem may close the panel via
        // Escape, CloseAllUI, or off-family hold; the button must reflect that.
        var sys = ModContent.GetInstance<WandUISystem>();
        bool open = sys?.InventoryViewUI?.IsVisible ?? false;
        _openInventoryViewBtn.Toggled = open;
    }

    private void SyncFromSettings()
    {
        UpdateObjectButtons();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateSlopeButtons();
        UpdateOverwriteSlopeButton();
        UpdateEqualDimensionsButton();
        UpdateSliceGrid();
        UpdateConnectDiameterButton();
        UpdateInvertSelectionButton();
        UpdateFlipHalfOrientationButton();
        UpdatePaintSprayerButton();
        UpdateActuationButton();
        UpdateInventoryViewButton();
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