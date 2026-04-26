using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Settings panel for the Wand of Coating.
/// Lets the player choose paint mode (PaintTile/PaintWall/ScrapeMoss/HarvestMoss),
/// pick a paint color from the 30 vanilla colors, select a shape, and adjust thickness.
/// </summary>
public class CoatingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Mode toggles
    private UIIconButton _paintTileBtn, _paintWallBtn, _scrapeMossBtn, _harvestMossBtn;

    // Coating toggles (Illuminant / Echo tri-state)
    private UITriStateButton _illuminantBtn, _echoBtn;

    // Paint color picker
    private UIPaintColorButton[] _colorButtons;
    private UIElement _colorPickerContainer;
    private CollapsibleSection _paintColorSection;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _repaintBtn;
    private UISliceGrid _sliceGrid;

    private WandPanelBuilder _builder;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float Padding = 10f;

    // Paint swatch layout
    private const float SwatchSize = 22f;
    private const float SwatchGap  = 4f;
    private const int   SwatchCols = 8;
    private const int   SwatchRows = 4;
    private const int   SwatchCount = 32;

    // S6 2026-04-24 (W-S6-3): the local PaintColors/PaintColorNames aliases
    // were inlined into Common/UI/Elements/CoatingPaintColorGrid.Build(...).
    // Both arrays are still sourced from PaintPalette, just one indirection
    // shorter at the consumption site.

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.CoatingBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.CoatingBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Coating.Title");

        // === MODE (non-radio icon buttons) ===
        var texPaintTile   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintTile",   AssetRequestMode.ImmediateLoad);
        var texPaintWall   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintWall",   AssetRequestMode.ImmediateLoad);
        var texScrapeMoss  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModeScrapeMoss",  AssetRequestMode.ImmediateLoad);
        var texHarvestMoss = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModeHarvestMoss", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Coating.Mode");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texPaintTile,   "Coating.PaintTile",   isToggle: true),
            new(texPaintWall,   "Coating.PaintWall",   isToggle: true),
            new(texScrapeMoss,  "Coating.ScrapeMoss",  isToggle: true),
            new(texHarvestMoss, "Coating.HarvestMoss", isToggle: true),
        }, iconsPerRow: 4, out var modeBtns);
        _paintTileBtn   = modeBtns[0]; _paintTileBtn.IsRadio = false;
        _paintWallBtn   = modeBtns[1]; _paintWallBtn.IsRadio = false;
        _scrapeMossBtn  = modeBtns[2]; _scrapeMossBtn.IsRadio = false;
        _harvestMossBtn = modeBtns[3]; _harvestMossBtn.IsRadio = false;

        // === COATING TYPE (Illuminant / Echo tri-state) ===
        _builder.AddSectionHeader("Coating.CoatingType");
        _builder.AddTriStateRow(
            L("Coating.Illuminant"), out _illuminantBtn,
            L("Coating.Echo"), out _echoBtn,
            spacing: WandPanelBuilder.AfterToggleGroupSpacing);

        // === PAINT COLOR PICKER (CollapsibleSection { Style = Popout }) ===
        // S+2 2026-04-25 (S1): wrap the swatch grid in a CollapsibleSection per
        // SessionPlan_WSW_Next3Sessions §S+2 Tasks 2-3 + Cavendish S9 §6
        // ("the Coating panel is the canary; we want it loud if anything cracks").
        //
        // Layout note (intentional, deferred for v1.2.0): when the user pops out
        // this section, the in-panel block shrinks from
        //   (HeaderHeight 22f + gridHeight 104f) = 126f
        // down to just HeaderHeight 22f, leaving a ~104f visual gap above the
        // Shape section below. This is the *honest* UX (the body really is
        // floating elsewhere now), and it matches SessionPlan §S+2 step 4(a)
        // verbatim ("full PaintColor section disappears from in-panel"). A
        // re-flow pass that shifts the below-sections up on collapse is a
        // proper UIList migration and is queued in DeferredForNextSession.md
        // as the v1.2.0 polish item — out of scope for the canary ship.
        //
        // PreferenceKey = "Coating.PaintColor" — namespaced per panel so the
        // same WandPlayer.CollapsedSections dict can serve future migrations
        // without key collisions.

        float paintSectionTop = _builder.CurrentY;
        var paintColorSection = new CollapsibleSection(
            titleKey: $"{UIPrefix}.Coating.PaintColor",
            preferenceKey: "Coating.PaintColor",
            style: CollapseStyle.Popout);
        paintColorSection.Top.Set(paintSectionTop, 0f);
        _mainPanel.Append(paintColorSection);

        _colorPickerContainer = CoatingPaintColorGrid.Build(
            columns: SwatchCols,
            containerInnerWidth: PanelWidth - 2 * Padding,
            onSelect: SetPaintColor,
            out _colorButtons,
            swatchSize: SwatchSize,
            swatchGap: SwatchGap);
        paintColorSection.SetBody(_colorPickerContainer);
        paintColorSection.RestoreFromPreferences();
        _paintColorSection = paintColorSection;

        // (v1.1, 2026-04-25 S2 — DesignDoc_PopoutFrameworkV1_1 §3.5 / Invariant I-5;
        //  semantics revised v1.3 §B, 2026-04-25 S4 — DesignDoc_PopoutFrameworkV1_3 §B
        //  / Invariant I-11.)
        // Multi-family ownership: PaintColor is consumed by the Coating wand
        // family AND by Wand of Building / Wand of Replacement (both expose a
        // PaintSprayer toggle that applies the selected paint while building /
        // replacing). The popout therefore stays VISIBLE across the natural
        // build-with-paint → swap-to-replacement → swap-back flow.
        //
        // v1.3 §B reframed the predicate from "auto-close gate" to "visibility
        // gate" — when this returns false the popout PARKS (body + position
        // preserved), and resurfaces on the next tick the predicate returns
        // true. Only ✕ closes for real, so configured state survives any number
        // of brief hotbar excursions.
        //
        // To make this fully modeless (popout always shown until explicit ✕),
        // flip the predicate to `() => true` — single-line change, reversible
        // at any time, no schema impact.
        paintColorSection.OwnerVisibilityCheck = () =>
        {
            var heldMod = Main.LocalPlayer?.HeldItem?.ModItem;
            return heldMod is WandOfCoatingBase
                || heldMod is WandOfBuildingBase
                || heldMod is WandOfReplacementBase;
        };

        // The section auto-derives its own Height from header + body (or just
        // header when collapsed/popped-out). Read it back so the next section
        // sits flush below.
        float colorPickerHeight = SwatchRows * (SwatchSize + SwatchGap);
        _builder.AdvanceY(WandPanelBuilder.SectionHeaderSpacing - 22f /* CollapsibleSection.HeaderHeight */
                          + colorPickerHeight + WandPanelBuilder.AfterIconGridSpacing);

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;     _rectHollowBtn    = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled;  _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled;  _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === OPTIONS ===
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        var texRepaint     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleRepaint", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,    "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,   "Common.InvertSelection",      isToggle: true),
            new(texRepaint,     "Coating.Repaint",              isToggle: true, initialState: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _repaintBtn         = optBtns[3];

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===
        _paintTileBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.PaintTile;   UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _paintWallBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.PaintWall;   UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _scrapeMossBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.ScrapeMoss;  UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _harvestMossBtn.OnToggled += (_, _) => { GetSettings().Mode = CoatingMode.HarvestMoss; UpdateModeButtons(); UpdateColorPickerVisibility(); };

        _illuminantBtn.OnStateChanged += state => { var s = GetSettings(); if (s != null) s.Illuminant = state; };
        _echoBtn.OnStateChanged       += state => { var s = GetSettings(); if (s != null) s.Echo = state; };

        _rectFilledBtn.OnToggled    += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _rectHollowBtn.OnToggled    += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Hollow);
        _edgeBtn.OnToggled          += (_, _) => SetShape(ShapeType.Elbow, ShapeMode.Filled);
        _cardinalBtn.OnToggled      += (_, _) => SetShape(ShapeType.CardinalLine, ShapeMode.Filled);
        _straightLineBtn.OnToggled  += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
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
        _repaintBtn.OnToggled += (_, _) => ToggleRepaint();
    }

    private WandOfCoatingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.CoatingSettings;

    private void SetPaintColor(byte colorIndex)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.PaintColor = colorIndex;
        UpdateColorButtons();
    }


    private void UpdateCoatingButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        if (_illuminantBtn.State != settings.Illuminant)
        {
            _illuminantBtn.State = settings.Illuminant;
            _illuminantBtn.RefreshDisplay();
        }
        if (_echoBtn.State != settings.Echo)
        {
            _echoBtn.State = settings.Echo;
            _echoBtn.RefreshDisplay();
        }
    }

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
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void ToggleRepaint() { var s = GetSettings(); if (s == null) return; s.Repaint = _repaintBtn.Toggled; }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _paintTileBtn.Toggled  = settings.Mode == CoatingMode.PaintTile;
        _paintWallBtn.Toggled  = settings.Mode == CoatingMode.PaintWall;
        _scrapeMossBtn.Toggled = settings.Mode == CoatingMode.ScrapeMoss;
        _harvestMossBtn.Toggled = settings.Mode == CoatingMode.HarvestMoss;
    }

    private void UpdateColorPickerVisibility()
    {
        var settings = GetSettings();
        if (settings == null || _colorPickerContainer == null) return;
        bool showPicker = settings.Mode == CoatingMode.PaintTile || settings.Mode == CoatingMode.PaintWall;
        _colorPickerContainer.MarginBottom = showPicker ? 0f : 0f;
    }

    private void UpdateColorButtons()
    {
        var settings = GetSettings();
        if (settings == null || _colorButtons == null) return;
        for (int i = 0; i < SwatchCount; i++)
        {
            if (_colorButtons[i] == null) continue;
            byte paintByte = (byte)(i < 31 ? i : WandOfCoatingBase.IgnorePaintColor);
            _colorButtons[i].IsSelected = (settings.PaintColor == paintByte);
        }
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

    private void UpdateThicknessDisplay() { _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1"); }
    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateRepaintButton() { var s = GetSettings(); if (s == null || _repaintBtn == null) return; _repaintBtn.Toggled = s.Repaint; }
    private void UpdateSliceGrid() { var s = GetSettings(); if (s == null || _sliceGrid == null) return; _sliceGrid.SetValue(s.Shape.Slice); }

    private void SyncFromSettings()
    {
        UpdateModeButtons();
        UpdateCoatingButtons();
        UpdateColorButtons();
        UpdateColorPickerVisibility();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateEqualDimensionsButton();
        UpdateSliceGrid();
        UpdateConnectDiameterButton();
        UpdateInvertSelectionButton();
        UpdateRepaintButton();
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

    // -- UIPaintColorButton was promoted to Common/UI/Elements/UIPaintColorButton.cs
    //    in S6 2026-04-24 (W-S6-3, S+2 prep) so CoatingPaintColorGrid.Build can
    //    construct it from outside this class. Behaviour is byte-identical.
}