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
    private UIIconButton _moldBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _paintSprayerBtn, _preservePaintBtn;
    // S9 (GrayJou Letter #9): IV-button parity with BuildingSettingsPanel.
    // Stateful toggle mirroring WandUISystem.InventoryViewUI.IsVisible (synced in Update).
    private UIIconButton _openInventoryViewBtn;
    private UISliceGrid _sliceGrid;

    // Paint Sprayer source toggle textures (Off / Inventory / CoatingSettings).
    // See Common/Settings/PaintSprayerSource.cs and DesignDoc_PaintSprayerSourceToggle.md.
    private Asset<Texture2D> _texPaintSprayerOff;
    private Asset<Texture2D> _texPaintSprayerInventory;
    private Asset<Texture2D> _texPaintSprayerCoating;

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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.ReplacementBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.ReplacementBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        // Load object type icon textures
        var texTile      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjTile", AssetRequestMode.ImmediateLoad);
        var texPlatform  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjPlatform", AssetRequestMode.ImmediateLoad);
        var texRope      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjRope", AssetRequestMode.ImmediateLoad);
        var texPlanter   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjPlanter", AssetRequestMode.ImmediateLoad);
        var texAir       = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjAir", AssetRequestMode.ImmediateLoad);
        var texWall      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjWall", AssetRequestMode.ImmediateLoad);
        var texSameType  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Objects/ObjSameType", AssetRequestMode.ImmediateLoad);

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
        _moldBtn = shapes.Mold;
        // (S11 2026-04-29 — Bug 3 fix; StencilEditVsActOn.md §3)
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === SHAPE OPTIONS ===
        var texEqualDim     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,     "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam,  "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,    "Common.InvertSelection",      isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];

        // === REPLACEMENT OPTIONS (Paint Sprayer + Preserve Paint + InventoryView toggle) ===
        var texPaintSprayer   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayer", AssetRequestMode.ImmediateLoad);
        var texPreservePaint  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePreservePaint", AssetRequestMode.ImmediateLoad); // TODO: dedicated icon
        // S9 (GrayJou Letter #9): IV-button parity with BuildingSettingsPanel — use the
        // same ObjTile placeholder texture for now until a dedicated InventoryView icon ships.
        var texInventoryView  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInventoryView", AssetRequestMode.ImmediateLoad);
        _texPaintSprayerOff       = texPaintSprayer;
        _texPaintSprayerInventory = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayerInventory", AssetRequestMode.ImmediateLoad);
        _texPaintSprayerCoating   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/TogglePaintSprayerCoating", AssetRequestMode.ImmediateLoad);

        _builder.AddIconToggleRow("Replacement.Options", new WandPanelBuilder.IconDef[]
        {
            new(texPaintSprayer,  "Common.PaintSprayer",      isToggle: true),
            new(texPreservePaint, "Common.PreservePaint",     isToggle: true),
            new(texInventoryView, "Common.OpenInventoryView", isToggle: true),
        }, out var replOptBtns);
        _paintSprayerBtn    = replOptBtns[0];
        _paintSprayerBtn.IsRadio = false;
        _paintSprayerBtn.AllowDeselect = true;
        _paintSprayerBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;
        _preservePaintBtn   = replOptBtns[1];
        _openInventoryViewBtn = replOptBtns[2];
        _openInventoryViewBtn.IsRadio = false;
        _openInventoryViewBtn.AllowDeselect = true;
        _openInventoryViewBtn.ActiveColor = WandPanelTheme.Colors.ActiveBlue;
        _openInventoryViewBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;

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
        _moldBtn.OnToggled += (_, _) => SetShape(ShapeType.Mold, ShapeMode.Filled);

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _paintSprayerBtn.OnToggled += (_, _) => CyclePaintSprayer();
        _preservePaintBtn.OnToggled += (_, _) => TogglePreservePaint();
        _openInventoryViewBtn.OnToggled += (_, _) => ToggleInventoryViewPanel();
    }

    /// <summary>
    /// S9 (GrayJou Letter #9): mirror of <c>BuildingSettingsPanel.ToggleInventoryViewPanel</c>.
    /// Single source of truth for visibility lives on <c>WandUISystem</c>; the button's
    /// <c>Toggled</c> state is re-synced from the panel's actual <c>IsVisible</c> via
    /// <see cref="UpdateInventoryViewButton"/> on every <c>SyncFromSettings()</c> tick.
    /// </summary>
    private void ToggleInventoryViewPanel()
    {
        ModContent.GetInstance<WandUISystem>()?.ToggleInventoryView();
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
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void TogglePaintSprayer() { CyclePaintSprayer(); }
    private void CyclePaintSprayer()
    {
        var s = GetSettings();
        if (s == null) return;
        s.PaintSprayer = s.PaintSprayer.Next();
        // S9 (GrayJou Letter #9): Paint Sprayer ↔ Preserve Paint mutual exclusion.
        // Prior behavior: PreservePaint silently won at execution time — which is
        // the kind of hidden override that breaks UX trust. The two toggles are
        // now mutually exclusive at the UI layer: turning the Paint Sprayer ON
        // (any non-Off mode) forces Preserve Paint OFF; turning Preserve Paint
        // ON forces the Paint Sprayer to Off. The execution-side conflict is
        // therefore unreachable through the UI (the WandOfReplacementBase paint
        // logic that picks one over the other still works as defensive code,
        // but legitimate users won't hit it).
        if (s.PaintSprayer.IsActive() && s.PreservePaint)
        {
            s.PreservePaint = false;
        }
        UpdatePaintSprayerButton();
    }
    private void TogglePreservePaint()
    {
        var s = GetSettings();
        if (s == null) return;
        s.PreservePaint = _preservePaintBtn.Toggled;
        // S9 (GrayJou Letter #9): see CyclePaintSprayer mutual-exclusion comment.
        // Turning Preserve Paint ON forces Paint Sprayer back to Off.
        if (s.PreservePaint && s.PaintSprayer.IsActive())
        {
            s.PaintSprayer = PaintSprayerSource.Off;
            UpdatePaintSprayerButton();
        }
    }
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
        _moldBtn.Toggled = shape.Shape == ShapeType.Mold;
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
    private void UpdatePaintSprayerButton()
    {
        var s = GetSettings();
        if (s == null || _paintSprayerBtn == null) return;
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
    private void UpdatePreservePaintButton() { var s = GetSettings(); if (s == null || _preservePaintBtn == null) return; _preservePaintBtn.Toggled = s.PreservePaint; }

    private void UpdateInventoryViewButton()
    {
        if (_openInventoryViewBtn == null) return;
        // Mirror the actual panel visibility — same pattern as BuildingSettingsPanel.
        var sys = ModContent.GetInstance<WandUISystem>();
        bool open = sys?.InventoryViewUI?.IsVisible ?? false;
        _openInventoryViewBtn.Toggled = open;
    }

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
        UpdatePreservePaintButton();
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