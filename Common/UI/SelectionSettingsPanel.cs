using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Settings panel for the Wand of Delimitation.
/// Lets the player toggle between Selection/Canvas Edit mode, choose a boolean operation
/// (Add, Remove, Intersect, XOR), select a shape, adjust thickness, and perform
/// action commands (Clear Selection, Invert Selection, Clear Canvas).
/// Displays live tile counts for both canvas and selection.
/// </summary>
public class SelectionSettingsPanel : UIState
{
    public bool IsVisible { get; set; }
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Mode toggle (Selection / Canvas Edit)
    private UIIconButton _modeSelectionBtn, _modeCanvasEditBtn;

    // Operation buttons (Add, Remove, Intersect, XOR)
    private UIIconButton _opAddBtn, _opRemoveBtn, _opIntersectBtn, _opXorBtn;

    // Shape buttons (standard 11-shape grid)
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;

    // Shape options
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn;

    // Thickness
    private UIText _thicknessValue;

    // Slice
    private UISliceGrid _sliceGrid;

    // Action buttons (icon-based)
    private UIIconButton _clearSelectionBtn, _invertBtn, _clearAllBtn, _teleportToPlayerBtn;

    // Status displays
    private UIText _canvasCountText, _selectionCountText, _customShapeText;

    // Auto-create canvas toggle
    private UIToggleButton _autoCreateCanvasBtn;

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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.SelectionBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.SelectionBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Selection.Title");

        // ═══════════════════════════════════════════════════════════════
        //  Mode Toggle (Selection / Canvas Edit)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Selection.Mode");

        // Delimitation shares the same Mode icons as Molding (Selection / Canvas Edit)
        var texModeSelection = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/MoldingModeSelection", AssetRequestMode.ImmediateLoad);
        var texModeCanvasEdit = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/MoldingModeCanvasEdit", AssetRequestMode.ImmediateLoad);

        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texModeSelection, "Selection.ModeSelection"),
            new(texModeCanvasEdit, "Selection.ModeCanvasEdit"),
        }, iconsPerRow: 5, out var modeBtns);
        _modeSelectionBtn = modeBtns[0];
        _modeCanvasEditBtn = modeBtns[1];

        // ═══════════════════════════════════════════════════════════════
        //  Operation Selector (Add, Remove, Intersect, XOR)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Selection.Operation");

        var texOpAdd = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/OpAdd", AssetRequestMode.ImmediateLoad);
        var texOpRemove = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/OpRemove", AssetRequestMode.ImmediateLoad);
        var texOpIntersect = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/OpIntersect", AssetRequestMode.ImmediateLoad);
        var texOpXor = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/OpXOR", AssetRequestMode.ImmediateLoad);

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

        // ═══════════════════════════════════════════════════════════════
        //  Shape Grid (standard 11-shape layout)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn = shapes.RectFilled; _rectHollowBtn = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled; _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled; _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;

        // ═══════════════════════════════════════════════════════════════
        //  Slice
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSliceSection(out _sliceGrid, OnSliceChanged);

        // ═══════════════════════════════════════════════════════════════
        //  Thickness
        // ═══════════════════════════════════════════════════════════════

        _builder.AddThicknessSection(out _thicknessValue, AdjustThickness);

        // ═══════════════════════════════════════════════════════════════
        //  Shape Options (Equal Dimensions, Connect Diameter, Invert)
        // ═══════════════════════════════════════════════════════════════

        var texEqualDim = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim, "Common.EqualDimensions", isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel, "Common.InvertSelection", isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];

        // ═══════════════════════════════════════════════════════════════
        //  Auto-Create Canvas Toggle
        // ═══════════════════════════════════════════════════════════════

        _builder.AddCenteredToggle("Selection.AutoCreateCanvas", true, out _autoCreateCanvasBtn, spacing: 38f);

        // ═══════════════════════════════════════════════════════════════
        //  Action Buttons (icon-based: Clear Selection, Invert, Clear All, Teleport)
        // ═══════════════════════════════════════════════════════════════

        var texActionClearSel = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionClearSelection", AssetRequestMode.ImmediateLoad);
        var texActionInvert = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionInvert", AssetRequestMode.ImmediateLoad);
        var texActionClearAll = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionClearAll", AssetRequestMode.ImmediateLoad);
        var texActionTeleport = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionTeleportToPlayer", AssetRequestMode.ImmediateLoad);

        _builder.AddActionIconRow("Selection.Actions", new WandPanelBuilder.IconDef[]
        {
            WandPanelBuilder.IconDef.WithText(texActionClearSel, L("Selection.ClearSelection")),
            WandPanelBuilder.IconDef.WithText(texActionInvert, L("Selection.InvertSelection")),
            WandPanelBuilder.IconDef.WithText(texActionClearAll, L("Selection.ClearAll")),
            WandPanelBuilder.IconDef.WithText(texActionTeleport, L("Selection.TeleportToPlayer")),
        }, out var actionBtns);
        _clearSelectionBtn = actionBtns[0];
        _invertBtn = actionBtns[1];
        _clearAllBtn = actionBtns[2];
        _teleportToPlayerBtn = actionBtns[3];

        // ═══════════════════════════════════════════════════════════════
        //  Status Display (tile counts)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Selection.Status");

        _canvasCountText = new UIText("Canvas: 0 tiles", 0.8f);
        _canvasCountText.Left.Set(Padding, 0f);
        _canvasCountText.Top.Set(_builder.CurrentY, 0f);
        _mainPanel.Append(_canvasCountText);
        _builder.AdvanceY(20f);

        _selectionCountText = new UIText("Selection: 0 tiles", 0.8f);
        _selectionCountText.Left.Set(Padding, 0f);
        _selectionCountText.Top.Set(_builder.CurrentY, 0f);
        _mainPanel.Append(_selectionCountText);
        _builder.AdvanceY(20f);

        _customShapeText = new UIText("Custom Shape: none", 0.8f);
        _customShapeText.Left.Set(Padding, 0f);
        _customShapeText.Top.Set(_builder.CurrentY, 0f);
        _mainPanel.Append(_customShapeText);
        _builder.AdvanceY(24f);

        // ═══════════════════════════════════════════════════════════════
        //  Close Button
        // ═══════════════════════════════════════════════════════════════

        _builder.AddCloseButton();
        _builder.FinalizeHeight();

        // ═══════════════════════════════════════════════════════════════
        //  Wire up events
        // ═══════════════════════════════════════════════════════════════

        // Mode toggle (radio behavior)
        _modeSelectionBtn.OnToggled += (_, _) => SetMode(DelimitationWandMode.Selection);
        _modeCanvasEditBtn.OnToggled += (_, _) => SetMode(DelimitationWandMode.CanvasEdit);

        // Operation selector (radio behavior)
        _opAddBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Add);
        _opRemoveBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Remove);
        _opIntersectBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.Intersect);
        _opXorBtn.OnToggled += (_, _) => SetOperation(SelectionOperation.XOR);

        // Shape selector
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

        // Shape options
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();

        // Auto-create canvas
        _autoCreateCanvasBtn.OnToggled += (_, _) =>
        {
            var s = GetSettings();
            if (s != null) s.AutoCreateCanvas = _autoCreateCanvasBtn.Toggled;
        };

        // Action buttons
        _clearSelectionBtn.OnLeftClick += (_, _) => OnClearSelection();
        _invertBtn.OnLeftClick += (_, _) => OnInvertSelection();
        _clearAllBtn.OnLeftClick += (_, _) => OnClearAll();
        _teleportToPlayerBtn.OnLeftClick += (_, _) => OnTeleportToPlayer();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Settings access
    // ═══════════════════════════════════════════════════════════════════

    private static DelimitationWandSettings GetSettings()
    {
        return Main.LocalPlayer?.GetModPlayer<DelimitationWandPlayer>()?.Settings;
    }

    private static DelimitationWandPlayer GetDelimitationWandPlayer()
    {
        return Main.LocalPlayer?.GetModPlayer<DelimitationWandPlayer>();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Setters
    // ═══════════════════════════════════════════════════════════════════

    private void SetMode(DelimitationWandMode mode)
    {
        var s = GetSettings();
        if (s == null) return;
        s.Mode = mode;
        UpdateModeButtons();
    }

    private void SetOperation(SelectionOperation op)
    {
        var s = GetSettings();
        if (s == null) return;
        s.Operation = op;
        UpdateOperationButtons();
    }

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var s = GetSettings();
        if (s == null) return;
        s.Shape = new ShapeInfo(type, mode, s.Shape.Thickness, s.Shape.EqualDimensions,
            s.Shape.Slice, s.Shape.ConnectDiameter, s.Shape.InvertSelection);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var s = GetSettings();
        if (s == null) return;
        var shape = s.Shape;
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        s.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions()
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.EqualDimensions = _equalDimensionsBtn.Toggled;
        s.Shape = sh;
    }

    private void ToggleConnectDiameter()
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.ConnectDiameter = _connectDiameterBtn.Toggled;
        s.Shape = sh;
    }

    private void ToggleInvertSelection()
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.InvertSelection = _invertSelectionBtn.Toggled;
        s.Shape = sh;
    }

    private void OnSliceChanged(SliceMode slice)
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.Slice = slice;
        s.Shape = sh;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Action handlers
    // ═══════════════════════════════════════════════════════════════════

    private static void OnClearSelection()
    {
        var swp = GetDelimitationWandPlayer();
        if (swp == null) return;
        int count = swp.Selection.Count;
        swp.Selection.Clear();
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Selection cleared ({count} tiles removed)", Color.Cyan);
    }

    private static void OnInvertSelection()
    {
        var swp = GetDelimitationWandPlayer();
        if (swp == null) return;

        if (!swp.Canvas.IsActive)
        {
            Main.NewText("No canvas active — cannot invert selection.", Color.OrangeRed);
            return;
        }

        // Use the Invert operation via TileSelection.ApplyOperation
        swp.Selection.ApplyOperation(
            System.Array.Empty<Microsoft.Xna.Framework.Point>(),
            SelectionOperation.Invert,
            swp.Canvas);

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Selection inverted ({swp.Selection.Count} tiles)", Color.Cyan);
    }

    private static void OnClearAll()
    {
        var swp = GetDelimitationWandPlayer();
        if (swp == null) return;
        swp.ClearAll();
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText("All selection state cleared.", Color.Gold);
    }

    private static void OnTeleportToPlayer()
    {
        var swp = GetDelimitationWandPlayer();
        if (swp == null) return;

        if (!swp.Canvas.IsActive)
        {
            Main.NewText("No canvas active — nothing to teleport.", Color.OrangeRed);
            return;
        }

        var player = Main.LocalPlayer;
        var playerTile = player.Center.ToTileCoordinates();
        var com = swp.Canvas.CenterOfMass;

        int dx = playerTile.X - (int)System.Math.Round(com.X);
        int dy = playerTile.Y - (int)System.Math.Round(com.Y);

        if (dx == 0 && dy == 0)
        {
            Main.NewText("Canvas is already centered on the player.", Color.Cyan);
            return;
        }

        swp.Canvas.Translate(dx, dy);
        swp.Selection.Translate(dx, dy);

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Canvas teleported to player (Δ{dx},{dy})", Color.Cyan);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UI sync from settings (called every frame)
    // ═══════════════════════════════════════════════════════════════════

    private void UpdateModeButtons()
    {
        var s = GetSettings();
        if (s == null) return;
        _modeSelectionBtn.Toggled = s.Mode == DelimitationWandMode.Selection;
        _modeCanvasEditBtn.Toggled = s.Mode == DelimitationWandMode.CanvasEdit;
    }

    private void UpdateOperationButtons()
    {
        var s = GetSettings();
        if (s == null) return;
        _opAddBtn.Toggled = s.Operation == SelectionOperation.Add;
        _opRemoveBtn.Toggled = s.Operation == SelectionOperation.Remove;
        _opIntersectBtn.Toggled = s.Operation == SelectionOperation.Intersect;
        _opXorBtn.Toggled = s.Operation == SelectionOperation.XOR;
    }

    private void UpdateShapeButtons()
    {
        var s = GetSettings();
        if (s == null) return;
        var shape = s.Shape;
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
        _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1");
    }

    private void UpdateShapeOptions()
    {
        var s = GetSettings();
        if (s == null) return;
        _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions;
        if (_connectDiameterBtn != null) _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter;
        if (_invertSelectionBtn != null)
        {
            _invertSelectionBtn.Toggled = s.Shape.InvertSelection;
            _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion;
        }
    }

    private void UpdateAutoCreateCanvas()
    {
        var s = GetSettings();
        if (s == null) return;
        if (_autoCreateCanvasBtn != null && _autoCreateCanvasBtn.Toggled != s.AutoCreateCanvas)
            _autoCreateCanvasBtn.Toggled = s.AutoCreateCanvas;
    }

    private void UpdateSliceGrid()
    {
        var s = GetSettings();
        if (s == null || _sliceGrid == null) return;
        _sliceGrid.SetValue(s.Shape.Slice);
    }

    private void UpdateStatusDisplay()
    {
        var swp = GetDelimitationWandPlayer();
        if (swp == null) return;

        int canvasCount = swp.Canvas.Count;
        int selCount = swp.Selection.Count;
        bool hasCustom = swp.ActiveCustomShape != null;

        _canvasCountText?.SetText($"Canvas: {canvasCount:N0} tiles");
        _selectionCountText?.SetText($"Selection: {selCount:N0} tiles");
        _customShapeText?.SetText(hasCustom
            ? $"Custom Shape: {swp.ActiveCustomShape.Count:N0} tiles"
            : "Custom Shape: none");
    }

    private void SyncFromSettings()
    {
        UpdateModeButtons();
        UpdateOperationButtons();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateShapeOptions();
        UpdateAutoCreateCanvas();
        UpdateSliceGrid();
        UpdateStatusDisplay();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Frame update
    // ═══════════════════════════════════════════════════════════════════

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
