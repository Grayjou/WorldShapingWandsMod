using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
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
/// Settings panel for the Wand of Fluids.
/// Lets the player choose liquid type (Water/Lava/Honey/Shimmer/Bubble),
/// fill mode (FullLiquid/RainFill/PocketFill), operation (Fill/Drain),
/// fluid options (Coat in Bubble, Mix Liquids),
/// shape, slice, thickness, and options.
/// </summary>
public class FluidsSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Place type buttons (liquid types + bubble)
    private UIIconButton _waterBtn, _lavaBtn, _honeyBtn, _shimmerBtn, _bubbleBtn;

    // Fill mode buttons
    private UIIconButton _fullLiquidBtn, _rainFillBtn, _pocketFillBtn;

    // Operation buttons
    private UIIconButton _fillBtn, _drainBtn;

    // Fluid option toggles
    private UIIconButton _coatInBubbleBtn, _mixLiquidsBtn, _overwriteLiquidsBtn, _selectiveDrainBtn;

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

    /// <summary>
    /// Tracks whether Bubble mode is active (place bubble blocks instead of liquid).
    /// Stored as local UI state and synced back to settings on change.
    /// </summary>
    private bool _isBubbleMode;

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.FluidsBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.FluidsBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Fluids.Title");

        // === PLACE TYPE (liquid types + bubble) ===
        var texWater   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidWater",   AssetRequestMode.ImmediateLoad);
        var texLava    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidLava",    AssetRequestMode.ImmediateLoad);
        var texHoney   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidHoney",   AssetRequestMode.ImmediateLoad);
        var texShimmer = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidShimmer", AssetRequestMode.ImmediateLoad);
        var texBubble  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidBubble",  AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Fluids.PlaceType");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texWater,   "Fluids.Water"),
            new(texLava,    "Fluids.Lava"),
            new(texHoney,   "Fluids.Honey"),
            new(texShimmer, "Fluids.Shimmer"),
            new(texBubble,  "Fluids.Bubble"),
        }, iconsPerRow: 5, out var placeTypeBtns);
        _waterBtn   = placeTypeBtns[0];
        _lavaBtn    = placeTypeBtns[1];
        _honeyBtn   = placeTypeBtns[2];
        _shimmerBtn = placeTypeBtns[3];
        _bubbleBtn  = placeTypeBtns[4];

        // === FILL MODE ===
        var texFull   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidModeFull",   AssetRequestMode.ImmediateLoad);
        var texRain   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidModeRain",   AssetRequestMode.ImmediateLoad);
        var texPocket = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidModePocket", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Fluids.FillMode");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texFull,   "Fluids.FullLiquid"),
            new(texRain,   "Fluids.RainFill"),
            new(texPocket, "Fluids.PocketFill"),
        }, iconsPerRow: 5, out var modeBtns);
        _fullLiquidBtn = modeBtns[0];
        _rainFillBtn   = modeBtns[1];
        _pocketFillBtn = modeBtns[2];

        // === OPERATION ===
        var texFill  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidFill",  AssetRequestMode.ImmediateLoad);
        var texDrain = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidDrain", AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Fluids.Operation");
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texFill,  "Fluids.Fill"),
            new(texDrain, "Fluids.Drain"),
        }, iconsPerRow: 5, out var opBtns);
        _fillBtn  = opBtns[0];
        _drainBtn = opBtns[1];

        // === FLUID OPTIONS (Coat in Bubble / Mix Liquids / Overwrite Liquids / Selective Drain toggles) ===
        var texCoatBubble = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidCoatBubble", AssetRequestMode.ImmediateLoad);
        var texMixLiquids = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidMixLiquids", AssetRequestMode.ImmediateLoad);
        // S5 2026-04-23 (W-2): OverwriteLiquids reuses Toggles/ToggleInvertSel as a
        // documented placeholder — "invert/swap" reads close to "overwrite" semantically.
        // Replace with a dedicated icon (ideas: liquid droplet over X-mark) when assets are next iterated.
        var texOverwriteLiquids = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/FluidOverwriteLiquids", AssetRequestMode.ImmediateLoad);
        var texSelectiveDrain = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Fluids/SelectiveDrain", AssetRequestMode.ImmediateLoad);

        _builder.AddIconToggleRow("Fluids.FluidOptions", new WandPanelBuilder.IconDef[]
        {
            new(texCoatBubble, "Fluids.CoatInBubble", isToggle: true),
            new(texMixLiquids, "Fluids.MixLiquids",   isToggle: true),
            new(texOverwriteLiquids, "Fluids.OverwriteLiquids", isToggle: true),
            new(texSelectiveDrain, "Fluids.SelectiveDrain", isToggle: true),
        }, out var fluidOptBtns);
        _coatInBubbleBtn = fluidOptBtns[0];
        _mixLiquidsBtn   = fluidOptBtns[1];
        _overwriteLiquidsBtn = fluidOptBtns[2];
        _selectiveDrainBtn = fluidOptBtns[3];

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

        // === OPTIONS ===
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
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];

        // === CLOSE ===
        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // === WIRE UP EVENTS ===

        // Place type: radio behavior — clicking one deselects others
        _waterBtn.OnToggled   += (_, _) => SetPlaceType(LiquidTypeSelection.Water);
        _lavaBtn.OnToggled    += (_, _) => SetPlaceType(LiquidTypeSelection.Lava);
        _honeyBtn.OnToggled   += (_, _) => SetPlaceType(LiquidTypeSelection.Honey);
        _shimmerBtn.OnToggled += (_, _) => SetPlaceType(LiquidTypeSelection.Shimmer);
        _bubbleBtn.OnToggled  += (_, _) => SetBubbleMode();

        // Fill mode: radio behavior
        _fullLiquidBtn.OnToggled += (_, _) => SetFillMode(FluidFillMode.FullLiquid);
        _rainFillBtn.OnToggled   += (_, _) => SetFillMode(FluidFillMode.RainFill);
        _pocketFillBtn.OnToggled += (_, _) => SetFillMode(FluidFillMode.PocketFill);

        // Operation: radio behavior
        _fillBtn.OnToggled  += (_, _) => SetOperation(FluidOperation.Fill);
        _drainBtn.OnToggled += (_, _) => SetOperation(FluidOperation.Drain);

        // Fluid options: independent toggles
        _coatInBubbleBtn.OnToggled += (_, _) => ToggleCoatInBubble();
        _mixLiquidsBtn.OnToggled   += (_, _) => ToggleMixLiquids();
        _overwriteLiquidsBtn.OnToggled += (_, _) => ToggleOverwriteLiquids();
        _selectiveDrainBtn.OnToggled += (_, _) => ToggleSelectiveDrain();

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
        _triangleHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _moldBtn.OnToggled           += (_, _) => SetShape(ShapeType.Mold, ShapeMode.Filled);
        _magicWandReadBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandRead, ShapeMode.Filled);
        _magicWandApplyBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandApply, ShapeMode.Filled);

        // Options
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
    }

    // ════════════════════════════════════════════════════════════════
    //  Settings access
    // ════════════════════════════════════════════════════════════════

    private WandOfFluidsSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.FluidsSettings;

    // ════════════════════════════════════════════════════════════════
    //  Place type
    // ════════════════════════════════════════════════════════════════

    private void SetPlaceType(LiquidTypeSelection liquid)
    {
        var settings = GetSettings();
        if (settings == null) return;
        _isBubbleMode = false;
        settings.PlaceBubble = false;
        settings.LiquidType = liquid;
        UpdatePlaceTypeButtons();
    }

    private void SetBubbleMode()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _isBubbleMode = true;
        settings.PlaceBubble = true;
        UpdatePlaceTypeButtons();
    }

    private void UpdatePlaceTypeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        // Sync _isBubbleMode from settings for bidirectional consistency
        _isBubbleMode = settings.PlaceBubble;

        _waterBtn.Toggled   = !_isBubbleMode && settings.LiquidType == LiquidTypeSelection.Water;
        _lavaBtn.Toggled    = !_isBubbleMode && settings.LiquidType == LiquidTypeSelection.Lava;
        _honeyBtn.Toggled   = !_isBubbleMode && settings.LiquidType == LiquidTypeSelection.Honey;
        _shimmerBtn.Toggled = !_isBubbleMode && settings.LiquidType == LiquidTypeSelection.Shimmer;
        _bubbleBtn.Toggled  = _isBubbleMode;
    }

    // ════════════════════════════════════════════════════════════════
    //  Fill mode
    // ════════════════════════════════════════════════════════════════

    private void SetFillMode(FluidFillMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.FillMode = mode;
        UpdateFillModeButtons();
    }

    private void UpdateFillModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _fullLiquidBtn.Toggled = settings.FillMode == FluidFillMode.FullLiquid;
        _rainFillBtn.Toggled   = settings.FillMode == FluidFillMode.RainFill;
        _pocketFillBtn.Toggled = settings.FillMode == FluidFillMode.PocketFill;
    }

    // ════════════════════════════════════════════════════════════════
    //  Operation
    // ════════════════════════════════════════════════════════════════

    private void SetOperation(FluidOperation op)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Operation = op;
        UpdateOperationButtons();
    }

    private void UpdateOperationButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _fillBtn.Toggled  = settings.Operation == FluidOperation.Fill;
        _drainBtn.Toggled = settings.Operation == FluidOperation.Drain;
    }

    // ════════════════════════════════════════════════════════════════
    //  Fluid options (Coat in Bubble / Mix Liquids)
    // ════════════════════════════════════════════════════════════════

    private void ToggleCoatInBubble()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.CoatInBubble = _coatInBubbleBtn.Toggled;
    }

    private void ToggleMixLiquids()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.MixLiquids = _mixLiquidsBtn.Toggled;
    }

    private void ToggleOverwriteLiquids()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.OverwriteLiquids = _overwriteLiquidsBtn.Toggled;
    }

    private void ToggleSelectiveDrain()
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.SelectiveDrain = _selectiveDrainBtn.Toggled;
    }

    private void UpdateFluidOptionsButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _coatInBubbleBtn.Toggled = settings.CoatInBubble;
        _mixLiquidsBtn.Toggled = settings.MixLiquids;
        _overwriteLiquidsBtn.Toggled = settings.OverwriteLiquids;
        _selectiveDrainBtn.Toggled = settings.SelectiveDrain;

        // Coat in Bubble is only relevant for FullLiquid fill (not RainFill, not Drain)
        bool canCoat = settings.Operation == FluidOperation.Fill
                       && settings.FillMode == FluidFillMode.FullLiquid;
        _coatInBubbleBtn.Disabled = !canCoat;

        // Mix Liquids is only compatible with FullLiquid mode + Fill operation, not Bubble mode
        bool canMix = !settings.PlaceBubble
                      && settings.FillMode == FluidFillMode.FullLiquid
                      && settings.Operation == FluidOperation.Fill;
        _mixLiquidsBtn.Disabled = !canMix;

        // Overwrite Liquids: same eligibility envelope as Mix, AND Mix must be off
        // (Mix takes priority — the dispatch in WandOfFluidsBase only enters
        // ExecuteFullLiquid when MixLiquids is OFF, so Overwrite is dormant otherwise).
        _overwriteLiquidsBtn.Disabled = !canMix || settings.MixLiquids;

        // Selective Drain is only relevant in Drain mode
        _selectiveDrainBtn.Disabled = settings.Operation != FluidOperation.Drain;
    }

    // ════════════════════════════════════════════════════════════════
    //  Shape
    // ════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════
    //  Thickness / Options / Slice
    // ════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════
    //  Sync all
    // ════════════════════════════════════════════════════════════════

    private void SyncFromSettings()
    {
        UpdatePlaceTypeButtons();
        UpdateFillModeButtons();
        UpdateOperationButtons();
        UpdateFluidOptionsButtons();
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
