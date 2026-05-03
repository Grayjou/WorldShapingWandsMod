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
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Settings panel for the Wand of Torches.
/// Lets the player choose mode (Place/Replace/Remove/Convert), tiling style,
/// spacing, reference mode, flip tiling, options, shape, and common options.
/// </summary>
public class TorchesSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Mode buttons (radio: Place / Replace / Remove / Convert)
    private UIIconButton _modePlaceBtn, _modeReplaceBtn, _modeRemoveBtn, _modeConvertBtn;

    // Tiling style buttons (radio)
    private UIIconButton _manhattanBtn, _gridBtn;

    // Spacing controls
    private UIText _spacingXValue, _spacingYValue;

    // Reference mode buttons (radio)
    private UIIconButton _refFirstValidBtn, _refBboxTopLeftBtn, _refBboxTopRightBtn;
    private UIIconButton _refBboxBottomLeftBtn, _refBboxBottomRightBtn, _refFirstClickBtn, _refMousePosBtn;

    // Flip tiling toggle
    private UIIconButton _flipTilingBtn;

    // Options (toggle icon buttons)
    private UIIconButton _biomeTorchBtn, _overwriteBtn, _alignToExistingBtn;

    // Echo Coat (tri-state icon button)
    private UIIconButton _echoCoatBtn;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;
    private UIIconButton _magicWandReadBtn, _magicWandApplyBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _flipHalfOrientationBtn;
    private UISliceGrid _sliceGrid;

    private WandPanelBuilder _builder;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float Padding = 10f;

    /// <summary>Maximum allowed torch spacing value.</summary>
    private const int MaxSpacing = 50;

    /// <summary>Minimum allowed torch spacing value.</summary>
    private const int MinSpacing = 1;

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.TorchesBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.TorchesBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Torches.Title");

        // === MODE (Place / Replace / Remove / Convert) ===
        var texModPlace    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchModPlace",    AssetRequestMode.ImmediateLoad);
        var texModReplace  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchModReplace",  AssetRequestMode.ImmediateLoad);
        var texModRemove   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchModRemove",   AssetRequestMode.ImmediateLoad);
        var texModConvert  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchModConvert",  AssetRequestMode.ImmediateLoad);

        // Tiling-style icons
        var texManhattan = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchManhattan", AssetRequestMode.ImmediateLoad);
        var texGrid      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchGrid",      AssetRequestMode.ImmediateLoad);

        // Reference-mode icons
        var texRefFirstValid  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefFirstValid",  AssetRequestMode.ImmediateLoad);
        var texRefBboxTL      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefBboxTL",      AssetRequestMode.ImmediateLoad);
        var texRefBboxTR      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefBboxTR",      AssetRequestMode.ImmediateLoad);
        var texRefBboxBL      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefBboxBL",      AssetRequestMode.ImmediateLoad);
        var texRefBboxBR      = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefBboxBR",      AssetRequestMode.ImmediateLoad);
        var texRefFirstClick  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefFirstClick",  AssetRequestMode.ImmediateLoad);
        var texRefMousePos    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchRefMousePos",    AssetRequestMode.ImmediateLoad);

        // Option icons
        var texBiome     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchBiome",     AssetRequestMode.ImmediateLoad);
        var texOverwrite = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchOverwrite", AssetRequestMode.ImmediateLoad);
        var texFlipTiling = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchFlipTiling", AssetRequestMode.ImmediateLoad);
        var texEchoCoat   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchEchoCoat",   AssetRequestMode.ImmediateLoad);
        var texAlignExisting = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Torches/TorchAlignExisting", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Torches.ModeHeader");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texModPlace,   "Torches.ModPlace"),
            new(texModReplace, "Torches.ModReplace"),
            new(texModRemove,  "Torches.ModRemove"),
            new(texModConvert, "Torches.ModConvert"),
        }, iconsPerRow: 5, out var modeBtns);
        _modePlaceBtn   = modeBtns[0];
        _modeReplaceBtn = modeBtns[1];
        _modeRemoveBtn  = modeBtns[2];
        _modeConvertBtn = modeBtns[3];

        // === TILING STYLE (Manhattan / Grid) ===
        _builder.AddSectionHeader("Torches.TilingStyle");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texManhattan, "Torches.Manhattan"),
            new(texGrid,      "Torches.Grid"),
        }, iconsPerRow: 5, out var tileBtns);
        _manhattanBtn = tileBtns[0];
        _gridBtn      = tileBtns[1];

        // === SPACING X / Y (+/- steppers) ===
        AddSpacingControl("Torches.SpacingX", out _spacingXValue, delta => AdjustSpacingX(delta));
        AddSpacingControl("Torches.SpacingY", out _spacingYValue, delta => AdjustSpacingY(delta));

        // === REFERENCE MODE (7 options: FirstValid / BboxCorners / FirstClick / Mouse) ===
        _builder.AddSectionHeader("Torches.ReferenceHeader");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texRefFirstValid, "Torches.RefFirstValid"),
            new(texRefBboxTL,     "Torches.RefBboxTopLeft"),
            new(texRefBboxTR,     "Torches.RefBboxTopRight"),
            new(texRefBboxBL,     "Torches.RefBboxBottomLeft"),
            new(texRefBboxBR,     "Torches.RefBboxBottomRight"),
            new(texRefFirstClick, "Torches.RefFirstClick"),
            new(texRefMousePos,   "Torches.RefMousePos"),
        }, iconsPerRow: 5, out var refBtns);
        _refFirstValidBtn    = refBtns[0];
        _refBboxTopLeftBtn   = refBtns[1];
        _refBboxTopRightBtn  = refBtns[2];
        _refBboxBottomLeftBtn  = refBtns[3];
        _refBboxBottomRightBtn = refBtns[4];
        _refFirstClickBtn    = refBtns[5];
        _refMousePosBtn      = refBtns[6];

        // === OPTIONS (FlipTiling, BiomeTorch, Overwrite, EchoCoat, AlignToExisting — icon toggles) ===
        _builder.AddIconToggleRow("Torches.TorchOptions", new WandPanelBuilder.IconDef[]
        {
            new(texFlipTiling, "Torches.FlipTiling",            isToggle: true),
            new(texBiome,      "Torches.BiomeTorch",            isToggle: true),
            new(texOverwrite,  "Torches.OverwriteTorches",      isToggle: true),
            new(texEchoCoat,   "Torches.EchoCoatIgnore",        isToggle: true),
            new(texAlignExisting, "Torches.AlignToExisting",    isToggle: true, initialState: true),
        }, out var optToggleBtns);
        _flipTilingBtn      = optToggleBtns[0];
        _biomeTorchBtn      = optToggleBtns[1];
        _overwriteBtn       = optToggleBtns[2];
        _echoCoatBtn        = optToggleBtns[3];
        _alignToExistingBtn = optToggleBtns[4];
        _echoCoatBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn     = shapes.RectFilled;     _rectHollowBtn     = shapes.RectHollow;
        _ellipseFilledBtn  = shapes.EllipseFilled;  _ellipseHollowBtn  = shapes.EllipseHollow;
        _diamondFilledBtn  = shapes.DiamondFilled;  _diamondHollowBtn  = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled;  _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;
        _magicWandReadBtn = shapes.MagicWandRead;
        _magicWandApplyBtn = shapes.MagicWandApply;

        // (S4 2026-05-01 � StencilMagicWandSelectionPlan.md �4.1) Right-click on
        // the Magic Wand Read shape cell opens the Read configuration SubUI.
        // The SubUI's underlying state (MagicWandReadConfig) is a player-scoped
        // preference shared across every wand, so the wiring is centralised in
        // MagicWandReadCellWiring (mirrors the MoldCellWiring singleton model).
        Common.UI.Elements.MagicWandReadCellWiring.WireConfigSubUI(_magicWandReadBtn);
        // (S11 2026-04-29 — Bug 3 fix; StencilEditVsActOn.md §3)
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);

        // === SLICE ===
        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // === THICKNESS ===
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // === COMMON OPTIONS (EqualDim, ConnectDiam, InvertSel) ===
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim",    AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel",   AssetRequestMode.ImmediateLoad);
        // (S2 2026-04-30 — InvertHalfOrientation #IOP) placeholder reuses ToggleInvertSel.
        // TODO: pending ToggleFlipHalfOrientation dedicated asset (placeholder = ToggleInvertSel byte-copy; tracked in dev_notes/dev_tasks/pending_assets.md §3b)
        var texFlipHalf    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,    "Common.EqualDimensions",        isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,   "Common.InvertSelection",        isToggle: true),
            new(texFlipHalf,    "Common.FlipHalfOrientation",    isToggle: true),
        }, out var commonOptBtns);
        _equalDimensionsBtn = commonOptBtns[0];
        _connectDiameterBtn = commonOptBtns[1];
        _invertSelectionBtn = commonOptBtns[2];
        _flipHalfOrientationBtn = commonOptBtns[3];

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===

        // Mode: radio behavior
        _modePlaceBtn.OnToggled   += (_, _) => SetMode(TorchMode.Place);
        _modeReplaceBtn.OnToggled += (_, _) => SetMode(TorchMode.Replace);
        _modeRemoveBtn.OnToggled  += (_, _) => SetMode(TorchMode.Remove);
        _modeConvertBtn.OnToggled += (_, _) => SetMode(TorchMode.Convert);

        // Tiling style: radio behavior
        _manhattanBtn.OnToggled += (_, _) => SetTilingStyle(TilingStyle.Manhattan);
        _gridBtn.OnToggled      += (_, _) => SetTilingStyle(TilingStyle.Grid);

        // Reference mode: radio behavior
        _refFirstValidBtn.OnToggled      += (_, _) => SetReferenceMode(TorchReferenceMode.FirstValidTile);
        _refBboxTopLeftBtn.OnToggled     += (_, _) => SetReferenceMode(TorchReferenceMode.BboxTopLeft);
        _refBboxTopRightBtn.OnToggled    += (_, _) => SetReferenceMode(TorchReferenceMode.BboxTopRight);
        _refBboxBottomLeftBtn.OnToggled  += (_, _) => SetReferenceMode(TorchReferenceMode.BboxBottomLeft);
        _refBboxBottomRightBtn.OnToggled += (_, _) => SetReferenceMode(TorchReferenceMode.BboxBottomRight);
        _refFirstClickBtn.OnToggled      += (_, _) => SetReferenceMode(TorchReferenceMode.FirstBboxClick);
        _refMousePosBtn.OnToggled        += (_, _) => SetReferenceMode(TorchReferenceMode.MousePosition);

        // Flip tiling toggle
        _flipTilingBtn.OnToggled += (_, _) => ToggleFlipTiling();

        // Options
        _biomeTorchBtn.OnToggled += (_, _) => ToggleBiomeTorch();
        _overwriteBtn.OnToggled  += (_, _) => ToggleOverwrite();
        _alignToExistingBtn.OnToggled += (_, _) => ToggleAlignToExisting();

        // Echo Coat (tri-state cycling)
        _echoCoatBtn.OnToggled += (_, _) => CycleEchoCoat();

        // Shape buttons
        _rectFilledBtn.OnToggled     += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _rectHollowBtn.OnToggled     += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Hollow);
        _edgeBtn.OnToggled           += (_, _) => SetShape(ShapeType.Elbow, ShapeMode.Filled);
        _cardinalBtn.OnToggled       += (_, _) => SetShape(ShapeType.CardinalLine, ShapeMode.Filled);
        _straightLineBtn.OnToggled   += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
        _ellipseFilledBtn.OnToggled  += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Filled);
        _ellipseHollowBtn.OnToggled  += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Hollow);
        _diamondFilledBtn.OnToggled  += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _diamondHollowBtn.OnToggled  += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Hollow);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _triangleHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Hollow);
        _moldBtn.OnToggled           += (_, _) => SetShape(ShapeType.Mold, ShapeMode.Filled);
        _magicWandReadBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandRead, ShapeMode.Filled);
        _magicWandApplyBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandApply, ShapeMode.Filled);

        // Common options
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
    }

    // ================================================================
    //  Custom spacing stepper (reuses thickness-style layout)
    // ================================================================

    private void AddSpacingControl(string locKey, out UIText valueText, System.Action<int> onAdjust)
    {
        float col1 = Padding;

        var label = new UIText(L(locKey), 0.85f);
        label.Left.Set(col1, 0f);
        label.Top.Set(_builder.CurrentY, 0f);
        _mainPanel.Append(label);

        // (S2 2026-04-30 #ROI Fix B) Consume scroll delta to prevent hotbar leakage.
        // See WandPanelBuilder.AddThicknessSection remarks for full rationale.
        void ScrollAdjust(Terraria.UI.UIScrollWheelEvent evt)
        {
            int delta = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI;
            if (delta == 0)
                delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
            if (delta == 0)
                return;

            onAdjust(delta > 0 ? 1 : -1);
            if (Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;
            Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
            Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
        }

        var minusBtn = new UITextPanel<string>("-", 0.8f, false);
        minusBtn.Width.Set(30f, 0f);
        minusBtn.Height.Set(26f, 0f);
        minusBtn.Left.Set(col1 + 130f, 0f);
        minusBtn.Top.Set(_builder.CurrentY - 2f, 0f);
        minusBtn.OnLeftClick += (_, _) => onAdjust(-1);
        minusBtn.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _mainPanel.Append(minusBtn);

        valueText = new UIText("5", 0.9f);
        valueText.Left.Set(col1 + 170f, 0f);
        valueText.Top.Set(_builder.CurrentY, 0f);
        valueText.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _mainPanel.Append(valueText);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(_builder.CurrentY - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => onAdjust(1);
        plusBtn.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _mainPanel.Append(plusBtn);

        _builder.AdvanceY(WandPanelBuilder.AfterThicknessSpacing);
    }

    // ================================================================
    //  Settings access
    // ================================================================

    private WandTorchSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.TorchSettings;

    // ================================================================
    //  Mode
    // ================================================================

    private void SetMode(TorchMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Mode = mode;
        UpdateModeButtons();
    }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _modePlaceBtn.Toggled   = settings.Mode == TorchMode.Place;
        _modeReplaceBtn.Toggled = settings.Mode == TorchMode.Replace;
        _modeRemoveBtn.Toggled  = settings.Mode == TorchMode.Remove;
        _modeConvertBtn.Toggled = settings.Mode == TorchMode.Convert;
    }

    // ================================================================
    //  Tiling Style
    // ================================================================

    private void SetTilingStyle(TilingStyle style)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.TilingStyle = style;
        UpdateTilingStyleButtons();
    }

    private void UpdateTilingStyleButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _manhattanBtn.Toggled = settings.TilingStyle == TilingStyle.Manhattan;
        _gridBtn.Toggled      = settings.TilingStyle == TilingStyle.Grid;
    }

    // ================================================================
    //  Spacing
    // ================================================================

    private void AdjustSpacingX(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.SpacingX = System.Math.Clamp(settings.SpacingX + delta, MinSpacing, MaxSpacing);
        UpdateSpacingDisplay();
    }

    private void AdjustSpacingY(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.SpacingY = System.Math.Clamp(settings.SpacingY + delta, MinSpacing, MaxSpacing);
        UpdateSpacingDisplay();
    }

    private void UpdateSpacingDisplay()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _spacingXValue?.SetText(settings.SpacingX.ToString());
        _spacingYValue?.SetText(settings.SpacingY.ToString());
    }

    // ================================================================
    //  Reference Mode
    // ================================================================

    private void SetReferenceMode(TorchReferenceMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.ReferenceMode = mode;
        UpdateReferenceModeButtons();
    }

    private void UpdateReferenceModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _refFirstValidBtn.Toggled      = settings.ReferenceMode == TorchReferenceMode.FirstValidTile;
        _refBboxTopLeftBtn.Toggled     = settings.ReferenceMode == TorchReferenceMode.BboxTopLeft;
        _refBboxTopRightBtn.Toggled    = settings.ReferenceMode == TorchReferenceMode.BboxTopRight;
        _refBboxBottomLeftBtn.Toggled  = settings.ReferenceMode == TorchReferenceMode.BboxBottomLeft;
        _refBboxBottomRightBtn.Toggled = settings.ReferenceMode == TorchReferenceMode.BboxBottomRight;
        _refFirstClickBtn.Toggled      = settings.ReferenceMode == TorchReferenceMode.FirstBboxClick;
        _refMousePosBtn.Toggled        = settings.ReferenceMode == TorchReferenceMode.MousePosition;
    }

    // ================================================================
    //  Flip Tiling
    // ================================================================

    private void ToggleFlipTiling()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.FlipTiling = _flipTilingBtn.Toggled;
    }

    private void UpdateFlipTilingButton()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _flipTilingBtn.Toggled = settings.FlipTiling;
    }

    // ================================================================
    //  Options (BiomeTorch, Overwrite)
    // ================================================================

    private void ToggleBiomeTorch()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.BiomeTorch = _biomeTorchBtn.Toggled;
    }

    private void ToggleOverwrite()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.OverwriteTorches = _overwriteBtn.Toggled;
    }

    private void ToggleAlignToExisting()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.AlignToExistingTorches = _alignToExistingBtn.Toggled;
    }

    private void UpdateOptionButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _biomeTorchBtn.Toggled = settings.BiomeTorch;
        _overwriteBtn.Toggled  = settings.OverwriteTorches;
        if (_alignToExistingBtn != null)
            _alignToExistingBtn.Toggled = settings.AlignToExistingTorches;
    }

    // ================================================================
    //  Echo Coat (tri-state icon button: Ignore → Apply → Remove)
    // ================================================================

    private void CycleEchoCoat()
    {
        var settings = GetSettings();
        if (settings == null) return;

        settings.EchoCoat = settings.EchoCoat.Next();
        UpdateEchoCoatButton();
    }

    private void UpdateEchoCoatButton()
    {
        var settings = GetSettings();
        if (settings == null || _echoCoatBtn == null) return;

        switch (settings.EchoCoat)
        {
            case TriStateValue.Ignore:
                _echoCoatBtn.Toggled = false;
                _echoCoatBtn.ActiveColor = WandPanelTheme.Colors.ActiveGreen;
                _echoCoatBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;
                break;
            case TriStateValue.Apply:
                _echoCoatBtn.Toggled = true;
                _echoCoatBtn.ActiveColor = WandPanelTheme.Colors.ActiveGreen;
                break;
            case TriStateValue.Remove:
                _echoCoatBtn.Toggled = true;
                _echoCoatBtn.ActiveColor = WandPanelTheme.Colors.ActiveRed;
                break;
        }

        string stateKey = settings.EchoCoat switch
        {
            TriStateValue.Ignore => "Torches.EchoCoatIgnore",
            TriStateValue.Apply  => "Torches.EchoCoatApply",
            TriStateValue.Remove => "Torches.EchoCoatRemove",
            _ => "Torches.EchoCoatIgnore",
        };
        _echoCoatBtn.HoverText = L(stateKey);
    }

    // ================================================================
    //  Shape
    // ================================================================

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness,
            settings.Shape.EqualDimensions, settings.Shape.Slice,
            settings.Shape.ConnectDiameter, settings.Shape.InvertSelection, settings.Shape.InvertHalfOrientation);
        UpdateShapeButtons();
    }

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        _rectFilledBtn.Toggled     = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _rectHollowBtn.Toggled     = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Hollow;
        _edgeBtn.Toggled           = shape.Shape == ShapeType.Elbow;
        _cardinalBtn.Toggled       = shape.Shape == ShapeType.CardinalLine;
        _straightLineBtn.Toggled   = shape.Shape == ShapeType.StraightLine;
        _ellipseFilledBtn.Toggled  = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Filled;
        _ellipseHollowBtn.Toggled  = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Hollow;
        _diamondFilledBtn.Toggled  = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _diamondHollowBtn.Toggled  = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Hollow;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
        _triangleHollowBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Hollow;
        _moldBtn.Toggled = shape.Shape == ShapeType.Mold;
        _magicWandReadBtn.Toggled = shape.Shape == ShapeType.MagicWandRead;
        _magicWandApplyBtn.Toggled = shape.Shape == ShapeType.MagicWandApply;
    }

    // ================================================================
    //  Thickness / Options / Slice
    // ================================================================

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        var config = Configs.WandConfigs.Limits;
        int max = config?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void ToggleFlipHalfOrientation() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertHalfOrientation = _flipHalfOrientationBtn.Toggled; s.Shape = sh; }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

    private void UpdateThicknessDisplay() { _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1"); }
    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateFlipHalfOrientationButton() { var s = GetSettings(); if (s == null || _flipHalfOrientationBtn == null) return; _flipHalfOrientationBtn.Toggled = s.Shape.InvertHalfOrientation; _flipHalfOrientationBtn.Disabled = s.Shape.Slice == SliceMode.Full; }
    private void UpdateSliceGrid() { var s = GetSettings(); if (s == null || _sliceGrid == null) return; _sliceGrid.SetValue(s.Shape.Slice); }

    // ================================================================
    //  Sync all
    // ================================================================

    private void SyncFromSettings()
    {
        UpdateModeButtons();
        UpdateTilingStyleButtons();
        UpdateSpacingDisplay();
        UpdateReferenceModeButtons();
        UpdateFlipTilingButton();
        UpdateOptionButtons();
        UpdateEchoCoatButton();
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