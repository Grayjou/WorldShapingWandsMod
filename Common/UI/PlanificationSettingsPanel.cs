using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;
using WorldShapingWandsMod.Common.Utilities;

namespace WorldShapingWandsMod.Common.UI;

public class PlanificationSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;
    private WandPanelBuilder _builder;

    private UIIconButton _modeSelectionBtn;
    private UIIconButton _modeCanvasEditBtn;

    private UIIconButton _opAddBtn;
    private UIIconButton _opRemoveBtn;
    private UIIconButton _opIntersectBtn;
    private UIIconButton _opXorBtn;

    private UIIconButton[] _slotButtons;
    private UIIconButton _visibilityBtn;

    private UIIconButton _renderOutlineBtn;
    private UIIconButton _renderGridBtn;
    private UIIconButton _renderFillBtn;

    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn, _magicWandReadBtn;

    private UISliceGrid _sliceGrid;
    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _flipHalfOrientationBtn;
    private UIIconButton _drawFromCenterBtn;
    private Asset<Texture2D> _texDrawFromCenterOff, _texDrawFromCenterOdd, _texDrawFromCenterEven;

    private UIIconButton _autoCreateCanvasBtn, _flipHorizontalBtn, _flipVerticalBtn, _rotateCwBtn, _rotateCcwBtn;

    private UIIconButton _transformMoveBtn, _transformPivotPersistentBtn, _transformPivotTemporaryBtn;

    private const string TransformModeSubUITitleKey = "Mods.WorldShapingWandsMod.UI.TransformMode.SubUITitle";
    private const string TransformModeSubUIIdentityKey = "WandOfPlanification.TransformModeShell";

    private UIIconButton _clearCurrentBtn, _clearAllBtn, _teleportBtn;

    private const float PanelWidth = 320f;
    private const float Padding = 10f;
    private const string VisibilitySubUIIdentityKey = "WandOfPlanification.StencilVisibility";

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.SelectionBg;
        _mainPanel.BorderColor = new Color(255, 105, 180);
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Planification.Title");

        _builder.AddSectionHeader("Selection.Mode");
        var texModeSelection = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/ModeSelection", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texModeCanvasEdit = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/ModeCanvasEdit", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texModeSelection, "Selection.ModeSelection"),
            new(texModeCanvasEdit, "Selection.ModeCanvasEdit"),
        }, iconsPerRow: 5, out var modeBtns);
        _modeSelectionBtn = modeBtns[0];
        _modeCanvasEditBtn = modeBtns[1];

        _builder.AddSectionHeader("Selection.Operation");
        var texOpAdd = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/OpAdd", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texOpRemove = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/OpRemove", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texOpIntersect = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/OpIntersect", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texOpXor = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/OpXOR", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texOpAdd, "Selection.OpAdd"),
            new(texOpRemove, "Selection.OpRemove"),
            new(texOpIntersect, "Selection.OpIntersect"),
            new(texOpXor, "Selection.OpXOR"),
        }, iconsPerRow: 5, out var opBtns);
        _opAddBtn = opBtns[0];
        _opRemoveBtn = opBtns[1];
        _opIntersectBtn = opBtns[2];
        _opXorBtn = opBtns[3];

        _builder.AddSectionHeader("Stencil.PickerTitle");
        var slotDefs = new WandPanelBuilder.IconDef[6];
        for (int i = 0; i < 5; i++)
        {
            var tex = mod.Assets.Request<Texture2D>($"Assets_Build/Icons/Shapes/Stencil/StencilChoice{i + 1}", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            slotDefs[i] = WandPanelBuilder.IconDef.WithText(tex, $"Stencil {i + 1}");
        }
        var visTex = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/StencilVisibility", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        slotDefs[5] = WandPanelBuilder.IconDef.WithText(
            visTex,
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Planification.VisibleStencils"));

        _builder.AddSmallIconGrid(slotDefs, iconsPerRow: 6, out var slotBtns);
        _slotButtons = new UIIconButton[5];
        for (int i = 0; i < 5; i++)
            _slotButtons[i] = slotBtns[i];
        _visibilityBtn = slotBtns[5];
        _visibilityBtn.IsAction = true;
        _visibilityBtn.HasSubUIBadge = true;

        _builder.AddSectionHeader("Planification.RenderConfig");
        var outlineTex = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/OutlineVisibility", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var gridTex = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/GridVisibility", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var fillTex = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Stencil/FillVisibility", ReLogic.Content.AssetRequestMode.ImmediateLoad);

        _builder.AddSmallIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(outlineTex, Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Planification.Outline"), isToggle: true),
            new(gridTex, Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Planification.Grid"), isToggle: true),
            new(fillTex, Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Planification.Fill"), isToggle: true),
        }, iconsPerRow: 5, out var renderBtns);
        _renderOutlineBtn = renderBtns[0];
        _renderGridBtn = renderBtns[1];
        _renderFillBtn = renderBtns[2];

        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn = shapes.RectFilled; _rectHollowBtn = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled; _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled; _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold; _magicWandReadBtn = shapes.MagicWandRead;
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);
        Common.UI.Elements.MagicWandReadCellWiring.WireConfigSubUI(_magicWandReadBtn);

        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);
        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        var texEqualDim = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texInvertSel = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texFlipHalf = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _texDrawFromCenterOff  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleRadiusFromCenterOff",  ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _texDrawFromCenterOdd  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleRadiusFromCenterOdd",  ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _texDrawFromCenterEven = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleRadiusFromCenterEven", ReLogic.Content.AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim, "Common.EqualDimensions", isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel, "Common.InvertSelection", isToggle: true),
            new(texFlipHalf, "Common.FlipHalfOrientation", isToggle: true),
            new(_texDrawFromCenterOff, "Common.DrawFromCenter.Off", isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];
        _drawFromCenterBtn  = optBtns[4];
        _drawFromCenterBtn.IsRadio = false;
        _drawFromCenterBtn.AllowDeselect = true;
        _drawFromCenterBtn.InactiveColor = WandPanelTheme.Colors.ButtonInactive;

        StencilPanelRowHelpers.AddTransformOptionsRow(
            _builder,
            mod,
            "Planification.Options",
            "Selection.AutoCreateCanvas",
            "Flip Horizontal",
            "Flip Vertical",
            "Rotate CW",
            "Rotate CCW",
            out _autoCreateCanvasBtn,
            out _flipHorizontalBtn,
            out _flipVerticalBtn,
            out _rotateCwBtn,
            out _rotateCcwBtn);
        _rotateCwBtn.HasSubUIBadge = true;
        _rotateCcwBtn.HasSubUIBadge = true;

        _builder.AddSectionHeader("Selection.Actions");
        var texActionClearSel = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Actions/ActionClearSelection", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texActionClearAll = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Actions/ActionClearAll", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        var texActionTeleport = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Actions/ActionTeleportToPlayer", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            WandPanelBuilder.IconDef.WithText(texActionClearSel, "Clear Current"),
            WandPanelBuilder.IconDef.WithText(texActionClearAll, "Clear All"),
            WandPanelBuilder.IconDef.WithText(texActionTeleport, "Teleport Active To Player"),
        }, iconsPerRow: 5, out var actionBtns);
        _clearCurrentBtn = actionBtns[0];
        _clearAllBtn = actionBtns[1];
        _teleportBtn = actionBtns[2];

        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        WireEvents();
    }

    private void WireEvents()
    {
        _modeSelectionBtn.OnToggled += (_, _) => SetMode(DelimitationWandMode.Selection);
        _modeCanvasEditBtn.OnToggled += (_, _) => SetMode(DelimitationWandMode.CanvasEdit);

        _opAddBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Add);
        _opRemoveBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Remove);
        _opIntersectBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Intersect);
        _opXorBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.XOR);

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int slot = i;
            _slotButtons[i].OnToggled += (_, _) =>
            {
                var pwp = GetPlayerState();
                if (pwp == null) return;
                pwp.SetActiveEditSlot(slot);
                UpdateSlotButtons();
                UpdateRenderConfigButtons();
            };
        }

        // Left click shows hint if verbosity is enabled; right click opens subUI
        _visibilityBtn.OnLeftClick += (_, _) =>
        {
            Main.NewText(
                Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Planification.VisibleStencilsHint"),
                WandColors.MsgHint);
        };
        _visibilityBtn.OnRightClick += (_, _) => OpenVisibilitySubUI();

        _renderOutlineBtn.OnToggled += (_, _) => ApplyRenderConfig();
        _renderGridBtn.OnToggled += (_, _) => ApplyRenderConfig();
        _renderFillBtn.OnToggled += (_, _) => ApplyRenderConfig();

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

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
        _drawFromCenterBtn.OnToggled += (_, _) => CycleDrawFromCenter();

        _autoCreateCanvasBtn.OnToggled += (_, _) =>
        {
            var settings = GetSettings();
            if (settings == null) return;
            settings.AutoCreateCanvas = _autoCreateCanvasBtn.Toggled;
        };

        _flipHorizontalBtn.OnLeftClick += (_, _) => TransformActiveSlot(TileCoordTransforms.FlipHorizontal, "Flipped slot horizontally");
        _flipVerticalBtn.OnLeftClick += (_, _) => TransformActiveSlot(TileCoordTransforms.FlipVertical, "Flipped slot vertically");
        _rotateCwBtn.OnLeftClick += (_, _) => TransformActiveSlot(TileCoordTransforms.Rotate90CW, "Rotated slot CW");
        _rotateCcwBtn.OnLeftClick += (_, _) => TransformActiveSlot(TileCoordTransforms.Rotate90CCW, "Rotated slot CCW");
        _rotateCwBtn.OnRightClick += (_, _) => OpenTransformModeSubUI(_rotateCwBtn);
        _rotateCcwBtn.OnRightClick += (_, _) => OpenTransformModeSubUI(_rotateCcwBtn);

        _clearCurrentBtn.OnLeftClick += (_, _) =>
        {
            var pwp = GetPlayerState();
            if (pwp == null) return;
            pwp.ClearSlot(pwp.ActiveEditSlot);
            Main.NewText($"Cleared slot {pwp.ActiveEditSlot + 1}", WandColors.MsgInfo);
        };

        _clearAllBtn.OnLeftClick += (_, _) =>
        {
            var pwp = GetPlayerState();
            if (pwp == null) return;
            pwp.ClearAllSlots();
            Main.NewText("Cleared all planification slots", WandColors.MsgInfo);
        };

        _teleportBtn.OnLeftClick += (_, _) => TeleportActiveSlotToPlayer();
    }

    private void OpenVisibilitySubUI()
    {
        var sys = ModContent.GetInstance<WandUISystem>();
        if (sys?.WandSubPanelHost == null)
            return;

        foreach (var openPanel in sys.WandSubPanelHost.Panels)
        {
            if (openPanel.IdentityKey == VisibilitySubUIIdentityKey)
                return;
        }
        var panelShell = new WandSubPanel(
            body: BuildVisibilityBody(),
            titleKey: "Mods.WorldShapingWandsMod.UI.Planification.VisibleStencils",
            defaultLocked: true,
            host: _visibilityBtn,
            identityKey: VisibilitySubUIIdentityKey)
        {
            Type = SubPanelType.Panel,
            OwnerFamilies = WandFamilyMask.Planification,
            LockBehaviourDecl = LockBehaviour.DefaultLocked,
            OnChoice = ChoiceBehaviour.NeverCloses,
            OnParentClose = ParentCloseBehaviour.StaysUpIfLocked,
            ExtraWidth = 12f,
            ExtraHeight = 22f,
        };

        sys.OpenWandSubPanel(panelShell);
        panelShell.AnchorToHost();
    }

    private UIElement BuildVisibilityBody()
    {
        const float cellSize = WandPanelBuilder.SmallIconBtnSize;
        const float gap = WandPanelBuilder.SmallIconGap;
        const int count = 5;

        float width = (cellSize * count) + (gap * (count - 1));
        var body = new UIElement();
        body.Width.Set(width, 0f);
        body.Height.Set(cellSize, 0f);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var pwp = GetPlayerState();

        for (int i = 0; i < count; i++)
        {
            int slot = i;
            var tex = mod.Assets.Request<Texture2D>($"Assets_Build/Icons/Shapes/Stencil/StencilChoice{i + 1}", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            var btn = new UIIconButton(tex, $"Stencil {i + 1}")
            {
                IsAction = false,
                IsRadio = false,
            };
            btn.Width.Set(cellSize, 0f);
            btn.Height.Set(cellSize, 0f);
            btn.Left.Set(i * (cellSize + gap), 0f);
            btn.Top.Set(0f, 0f);
            btn.Toggled = pwp?.IsSlotVisible(slot) == true;
            btn.OnLeftClick += (_, _) =>
            {
                var state = GetPlayerState();
                if (state == null) return;
                state.ToggleSlotVisibility(slot);
                btn.Toggled = state.IsSlotVisible(slot);
                UpdateSlotButtons();
            };
            body.Append(btn);
        }

        return body;
    }

    private static PlanificationWandPlayer GetPlayerState()
        => Main.LocalPlayer?.GetModPlayer<PlanificationWandPlayer>();

    private static PlanificationWandSettings GetSettings()
        => GetPlayerState()?.Settings;

    private void SetMode(DelimitationWandMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Mode = mode;
        UpdateModeButtons();
    }

    private void SetOperation(SelectionOperation operation)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Operation = operation;
        UpdateOperationButtons();
    }

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;

        settings.Shape = new ShapeInfo(
            type,
            mode,
            settings.Shape.Thickness,
            settings.Shape.EqualDimensions,
            settings.Shape.Slice,
            settings.Shape.ConnectDiameter,
            settings.Shape.InvertSelection,
            settings.Shape.InvertHalfOrientation,
            settings.Shape.DrawFromCenter);

        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;

        var shape = settings.Shape;
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
        shape.Thickness = Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        _thicknessValue?.SetText(shape.Thickness.ToString());
    }

    private void OnSliceChanged(SliceMode slice)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.Slice = slice;
        settings.Shape = shape;
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

    private void CycleDrawFromCenter()
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.DrawFromCenter = sh.DrawFromCenter.Next();
        s.Shape = sh;
        UpdateDrawFromCenterButton();
    }

    private void UpdateDrawFromCenterButton()
    {
        var s = GetSettings();
        if (s == null || _drawFromCenterBtn == null) return;
        bool supported = s.Shape.SupportsDrawFromCenter;
        _drawFromCenterBtn.Disabled = !supported;
        switch (supported ? s.Shape.DrawFromCenter : DrawFromCenterMode.Off)
        {
            case DrawFromCenterMode.Odd:
                _drawFromCenterBtn.Toggled = true;
                _drawFromCenterBtn.ActiveColor = WandPanelTheme.Colors.ActiveBlue;
                _drawFromCenterBtn.SetTexture(_texDrawFromCenterOdd);
                _drawFromCenterBtn.HoverText = Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Common.DrawFromCenter.Odd");
                break;
            case DrawFromCenterMode.Even:
                _drawFromCenterBtn.Toggled = true;
                _drawFromCenterBtn.ActiveColor = WandPanelTheme.Colors.ActiveGreen;
                _drawFromCenterBtn.SetTexture(_texDrawFromCenterEven);
                _drawFromCenterBtn.HoverText = Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Common.DrawFromCenter.Even");
                break;
            default:
                _drawFromCenterBtn.Toggled = false;
                _drawFromCenterBtn.SetTexture(_texDrawFromCenterOff);
                _drawFromCenterBtn.HoverText = Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Common.DrawFromCenter.Off");
                break;
        }
    }

    private void ApplyRenderConfig()
    {
        var pwp = GetPlayerState();
        if (pwp == null) return;

        var cfg = new PlanificationRenderConfig
        {
            ShowOutline = _renderOutlineBtn.Toggled,
            ShowGrid = _renderGridBtn.Toggled,
            ShowFill = _renderFillBtn.Toggled,
        };

        pwp.SetRenderConfig(pwp.ActiveEditSlot, cfg);
    }

    private void TransformActiveSlot(Func<System.Collections.Generic.IEnumerable<Point>, System.Collections.Generic.HashSet<Point>> transform, string message)
    {
        var pwp = GetPlayerState();
        if (pwp == null) return;

        int activeSlot = pwp.ActiveEditSlot;
        var slot = BuildStencilSlotState(pwp, activeSlot);
        if (!slot.HasCanvas && !slot.HasSelection)
            return;

        if (slot.HasCanvas)
            slot.SetCanvas(transform(slot.CanvasTiles));

        if (slot.HasSelection)
            slot.SetSelection(transform(slot.SelectionTiles));

        ApplyStencilSlotState(pwp, activeSlot, slot);
        Main.NewText(message, WandColors.MsgInfo);
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
    }

    private static StencilSlotState BuildStencilSlotState(PlanificationWandPlayer pwp, int slot)
    {
        var state = new StencilSlotState();
        state.SetCanvas(pwp.GetSlotCanvasWorldTiles(slot));
        state.SetSelection(pwp.GetSlotSelectionWorldTiles(slot));
        return state;
    }

    private static void ApplyStencilSlotState(PlanificationWandPlayer pwp, int slot, StencilSlotState state)
    {
        pwp.ClearSlot(slot);

        if (state.CanvasCount > 0)
            pwp.SetSlotCanvasShape(slot, state.CloneCanvas());

        if (state.SelectionCount > 0)
            pwp.SetSlotSelectionShape(slot, state.CloneSelection());
    }

    private void OpenTransformModeSubUI(UIIconButton hostButton)
    {
        var settings = GetSettings();
        var sys = ModContent.GetInstance<WandUISystem>();
        if (sys?.WandSubPanelHost == null || hostButton == null || settings == null)
            return;

        if (settings.TransformModeEnabled)
        {
            if (ReferenceEquals(hostButton, _rotateCwBtn))
                TransformActiveSlot(TileCoordTransforms.Rotate90CW, "Rotated slot CW");
            else if (ReferenceEquals(hostButton, _rotateCcwBtn))
                TransformActiveSlot(TileCoordTransforms.Rotate90CCW, "Rotated slot CCW");
            return;
        }

        settings.TransformModeEnabled = true;

        foreach (var panel in sys.WandSubPanelHost.Panels)
        {
            if (panel.IdentityKey == TransformModeSubUIIdentityKey)
                return;
        }

        var panelShell = WandSubPanelFactories.CreateTransformModeShell(
            host: hostButton,
            titleKey: TransformModeSubUITitleKey,
            identityKey: TransformModeSubUIIdentityKey,
            ownerFamilies: WandFamilyMask.Planification,
            onSetPivotPersistent: OnTransformSubUISetPivotPersistent,
            onSetPivotTemporary: OnTransformSubUISetPivotTemporary,
            onMove: OnTransformSubUIMove,
            out _transformMoveBtn,
            out _transformPivotPersistentBtn,
            out _transformPivotTemporaryBtn);

        UpdateTransformButtons();

        sys.OpenWandSubPanel(panelShell);
        panelShell.AnchorToHost();
    }

    private static bool IsTransformModeSubUIOpen()
    {
        var sys = ModContent.GetInstance<WandUISystem>();
        if (sys?.WandSubPanelHost == null)
            return false;

        foreach (var panel in sys.WandSubPanelHost.Panels)
        {
            if (panel.IdentityKey == TransformModeSubUIIdentityKey && !panel.IsHidden)
                return true;
        }

        return false;
    }

    private static void UpdateTransformModeLifecycle()
    {
        var settings = GetSettings();
        if (settings == null)
            return;

        if (!IsTransformModeSubUIOpen())
        {
            settings.TransformModeEnabled = false;
            settings.ActiveTransformAction = TransformActionMode.None;
            settings.PendingTransformMoveStart = null;
            settings.TemporaryPivot = null;
        }
    }

    private static void OnTransformSubUISetPivotPersistent()
    {
        var settings = GetSettings();
        if (settings == null)
            return;

        settings.TransformModeEnabled = true;
        settings.ActiveTransformAction = TransformActionMode.SetPivotPersistent;
        settings.PendingTransformMoveStart = null;
        Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotPersistentHint"), WandColors.MsgInfo);
    }

    private static void OnTransformSubUISetPivotTemporary()
    {
        var settings = GetSettings();
        if (settings == null)
            return;

        settings.TransformModeEnabled = true;
        settings.ActiveTransformAction = TransformActionMode.SetPivotTemporary;
        settings.PendingTransformMoveStart = null;
        Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.SetPivotTemporaryHint"), WandColors.MsgInfo);
    }

    private static void OnTransformSubUIMove()
    {
        var pwp = GetPlayerState();
        var settings = GetSettings();
        if (pwp == null || settings == null)
            return;

        var slot = BuildStencilSlotState(pwp, pwp.ActiveEditSlot);
        if (!slot.HasCanvas && !slot.HasSelection)
        {
            Main.NewText("No canvas or selection active â€” nothing to move.", Color.OrangeRed);
            return;
        }

        settings.TransformModeEnabled = true;
        settings.ActiveTransformAction = TransformActionMode.Move;
        settings.PendingTransformMoveStart = null;
        Main.NewText(Language.GetTextValue("Mods.WorldShapingWandsMod.UI.TransformMode.MoveHint"), WandColors.MsgInfo);
    }

    private void TeleportActiveSlotToPlayer()
    {
        var pwp = GetPlayerState();
        if (pwp == null) return;

        var tiles = pwp.GetSlotSelectionWorldTiles(pwp.ActiveEditSlot);
        if (tiles.Count == 0)
            return;

        var bounds = TileCoordTransforms.ComputeBounds(tiles);
        if (bounds.IsEmpty)
            return;

        int centerX = bounds.Left + bounds.Width / 2;
        int centerY = bounds.Top + bounds.Height / 2;

        Point playerTile = Main.LocalPlayer.Center.ToTileCoordinates();
        int dx = playerTile.X - centerX;
        int dy = playerTile.Y - centerY;

        var moved = new System.Collections.Generic.HashSet<Point>(tiles.Count);
        foreach (var tile in tiles)
            moved.Add(new Point(tile.X + dx, tile.Y + dy));

        pwp.ReplaceSlotWorldTiles(pwp.ActiveEditSlot, moved);
        Main.NewText("Teleported active slot to player", WandColors.MsgInfo);
    }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _modeSelectionBtn.Toggled = settings.Mode == DelimitationWandMode.Selection;
        _modeCanvasEditBtn.Toggled = settings.Mode == DelimitationWandMode.CanvasEdit;
    }

    private void UpdateOperationButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _opAddBtn.Toggled = settings.Operation == SelectionOperation.Add;
        _opRemoveBtn.Toggled = settings.Operation == SelectionOperation.Remove;
        _opIntersectBtn.Toggled = settings.Operation == SelectionOperation.Intersect;
        _opXorBtn.Toggled = settings.Operation == SelectionOperation.XOR;
    }

    private void UpdateSlotButtons()
    {
        var pwp = GetPlayerState();
        if (pwp == null) return;

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            _slotButtons[i].Toggled = i == pwp.ActiveEditSlot;
            _slotButtons[i].ActiveColor = pwp.IsSlotVisible(i)
                ? new Color(255, 120, 180)
                : WandPanelTheme.Colors.Disabled;
        }
    }

    private void UpdateRenderConfigButtons()
    {
        var pwp = GetPlayerState();
        if (pwp == null) return;

        var cfg = pwp.GetRenderConfig(pwp.ActiveEditSlot);
        _renderOutlineBtn.Toggled = cfg.ShowOutline;
        _renderGridBtn.Toggled = cfg.ShowGrid;
        _renderFillBtn.Toggled = cfg.ShowFill;
    }

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        var shape = settings.Shape;
        _rectFilledBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _rectHollowBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Hollow;
        _ellipseFilledBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Filled;
        _ellipseHollowBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Hollow;
        _diamondFilledBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _diamondHollowBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Hollow;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
        _triangleHollowBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Hollow;
        _edgeBtn.Toggled = shape.Shape == ShapeType.Elbow;
        _cardinalBtn.Toggled = shape.Shape == ShapeType.CardinalLine;
        _straightLineBtn.Toggled = shape.Shape == ShapeType.StraightLine;
        _moldBtn.Toggled = shape.Shape == ShapeType.Mold;
        _magicWandReadBtn.Toggled = shape.Shape == ShapeType.MagicWandRead;

        _equalDimensionsBtn.Toggled = shape.EqualDimensions;
        _connectDiameterBtn.Toggled = shape.ConnectDiameter;
        _invertSelectionBtn.Toggled = shape.InvertSelection;
        _flipHalfOrientationBtn.Toggled = shape.InvertHalfOrientation;
        UpdateDrawFromCenterButton();
        _autoCreateCanvasBtn.Toggled = settings.AutoCreateCanvas;

        _sliceGrid.SetValue(shape.Slice);
        _thicknessValue.SetText(shape.Thickness.ToString());
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!IsVisible)
            return;

        UpdateTransformModeLifecycle();

        UpdateModeButtons();
        UpdateOperationButtons();
        UpdateSlotButtons();
        UpdateRenderConfigButtons();
        UpdateShapeButtons();
        UpdateTransformButtons();
    }

    private void UpdateTransformButtons()
    {
        var settings = GetSettings();
        var pwp = GetPlayerState();
        if (settings == null || pwp == null)
            return;

        var slot = BuildStencilSlotState(pwp, pwp.ActiveEditSlot);
        bool enabled = slot.HasCanvas || slot.HasSelection;

        if (_flipHorizontalBtn != null) _flipHorizontalBtn.Disabled = !enabled;
        if (_flipVerticalBtn != null) _flipVerticalBtn.Disabled = !enabled;
        if (_rotateCwBtn != null) _rotateCwBtn.Disabled = !enabled;
        if (_rotateCcwBtn != null) _rotateCcwBtn.Disabled = !enabled;

        if (_transformMoveBtn != null)
            _transformMoveBtn.Toggled = settings.ActiveTransformAction == TransformActionMode.Move;
        if (_transformPivotPersistentBtn != null)
            _transformPivotPersistentBtn.Toggled = settings.ActiveTransformAction == TransformActionMode.SetPivotPersistent;
        if (_transformPivotTemporaryBtn != null)
            _transformPivotTemporaryBtn.Toggled = settings.ActiveTransformAction == TransformActionMode.SetPivotTemporary;

        if (!(_transformMoveBtn?.Toggled == true || _transformPivotPersistentBtn?.Toggled == true || _transformPivotTemporaryBtn?.Toggled == true))
            settings.ActiveTransformAction = TransformActionMode.None;
    }
}


