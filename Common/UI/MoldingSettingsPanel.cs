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
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;
using WorldShapingWandsMod.Common.UI.Elements.Builders;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Settings panel for the Wand of Molding.
/// Structurally mirrors <see cref="SelectionSettingsPanel"/> but reads from / writes to
/// <see cref="MoldingWandPlayer"/> and <see cref="MoldingWandSettings"/> instead of the
/// Delimitation counterparts, ensuring the two systems remain fully independent.
/// </summary>
public class MoldingSettingsPanel : UIState
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

    // Stencil-slot row (per MultipleStencilsPlan.md §0.1, S6 2026-04-28)
    private UIIconButton[] _stencilSlotBtns;
    private ReLogic.Content.Asset<Texture2D>[] _stencilSlotIconAssets;

    // Shape options
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _flipHalfOrientationBtn;

    // Thickness
    private UIText _thicknessValue;

    // Slice
    private UISliceGrid _sliceGrid;

    // Action buttons (icon-based)
    private UIIconButton _clearSelectionBtn, _invertBtn, _clearAllBtn, _teleportToPlayerBtn;

    // Status displays
    private UIText _canvasCountText, _selectionCountText, _moldedShapeText;

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
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.MoldingBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.MoldingBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Molding.Title");

        // ═══════════════════════════════════════════════════════════════
        //  Mode Toggle (Selection / Canvas Edit)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Molding.Mode");

        var texModeSelection = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/ModeSelection", AssetRequestMode.ImmediateLoad);
        var texModeCanvasEdit = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/ModeCanvasEdit", AssetRequestMode.ImmediateLoad);

        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texModeSelection, "Molding.ModeSelection"),
            new(texModeCanvasEdit, "Molding.ModeCanvasEdit"),
        }, iconsPerRow: 5, out var modeBtns);
        _modeSelectionBtn = modeBtns[0];
        _modeCanvasEditBtn = modeBtns[1];

        // ═══════════════════════════════════════════════════════════════
        //  Operation Selector (Add, Remove, Intersect, XOR)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Molding.Operation");

        var texOpAdd = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/OpAdd", AssetRequestMode.ImmediateLoad);
        var texOpRemove = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/OpRemove", AssetRequestMode.ImmediateLoad);
        var texOpIntersect = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/OpIntersect", AssetRequestMode.ImmediateLoad);
        var texOpXor = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Stencils/OpXOR", AssetRequestMode.ImmediateLoad);

        _builder.AddIconGrid(new WandPanelBuilder.IconDef[]
        {
            new(texOpAdd, "Molding.OpAdd"),
            new(texOpRemove, "Molding.OpRemove"),
            new(texOpIntersect, "Molding.OpIntersect"),
            new(texOpXor, "Molding.OpXOR"),
        }, iconsPerRow: 5, out var opBtns);
        _opAddBtn = opBtns[0];
        _opRemoveBtn = opBtns[1];
        _opIntersectBtn = opBtns[2];
        _opXorBtn = opBtns[3];

        // ═══════════════════════════════════════════════════════════════
        //  Shape Grid (standard 11-shape layout)
        // ═══════════════════════════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════
        //  Shape Grid (12-cell standard + 5-cell stencil-slot row)
        //  Per MultipleStencilsPlan.md §0.1 — stencil wands gain a +1 row of
        //  StencilChoice{1..5} cells under the standard grid for direct slot
        //  switching. Mold cell hover-icon swap wired below per §0.2.
        // ════════════════════════════════════════════════════════════════

        _builder.AddStencilShapeSection(out var stencilGrid);
        var shapes = stencilGrid.Shapes;
        _rectFilledBtn = shapes.RectFilled; _rectHollowBtn = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled; _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled; _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;
        _stencilSlotBtns = stencilGrid.StencilSlots;

        // Cache stencil icon assets so the Mold-cell HoverTextureProvider lambda
        // can resolve them without repeated Asset<>.Request<> calls per frame.
        _stencilSlotIconAssets = new ReLogic.Content.Asset<Texture2D>[MoldingWandPlayer.StencilSlotCount];
        for (int i = 0; i < MoldingWandPlayer.StencilSlotCount; i++)
        {
            _stencilSlotIconAssets[i] = mod.Assets.Request<Texture2D>(
                $"Assets_Build/Icons/Shapes/Stencil/StencilChoice{i + 1}",
                AssetRequestMode.ImmediateLoad);
        }

        // (S11 2026-04-29 \u2014 Bug 3 fix; StencilEditVsActOn.md \u00a73)
        // Mold cell wiring is shared across every WSW wand panel via
        // MoldCellWiring.WireActOnPicker: hover-icon swap shows the
        // ACT-ON slot's StencilChoice; right-click opens the ACT-ON
        // picker (writes ActOnStencilSlot, NOT ActiveStencilSlot \u2014 the
        // EDIT slot belongs to the 5-button stencil row underneath the
        // shape grid). The Wand of Molding uses the same helper as every
        // other wand because per the user's S11 example, WoM also stamps
        // Mold Shape from the ACT-ON slot.
        Common.UI.Elements.MoldCellWiring.WireActOnPicker(_moldBtn);

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
        // (S2 2026-04-30 — InvertHalfOrientation #IOP) placeholder reuses ToggleInvertSel.
        var texFlipHalf = mod.Assets.Request<Texture2D>(
            // TODO: pending ToggleFlipHalfOrientation dedicated asset (placeholder = ToggleInvertSel byte-copy; tracked in dev_notes/dev_tasks/pending_assets.md §3b)
            "Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim, "Common.EqualDimensions", isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel, "Common.InvertSelection", isToggle: true),
            new(texFlipHalf, "Common.FlipHalfOrientation", isToggle: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];

        // ═══════════════════════════════════════════════════════════════
        //  Auto-Create Canvas Toggle
        // ═══════════════════════════════════════════════════════════════

        _builder.AddCenteredToggle("Molding.AutoCreateCanvas", true, out _autoCreateCanvasBtn, spacing: 38f);

        // ═══════════════════════════════════════════════════════════════
        //  Action Buttons (icon-based: Clear Selection, Invert, Clear Canvas, Clear All, Teleport)
        // ═══════════════════════════════════════════════════════════════

        var texActionClearSel = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionClearSelection", AssetRequestMode.ImmediateLoad);
        var texActionInvert = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionInvert", AssetRequestMode.ImmediateLoad);
        var texActionClearAll = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionClearAll", AssetRequestMode.ImmediateLoad);
        var texActionTeleport = mod.Assets.Request<Texture2D>(
            "Assets_Build/Icons/Actions/ActionTeleportToPlayer", AssetRequestMode.ImmediateLoad);

        _builder.AddActionIconRow("Molding.Actions", new WandPanelBuilder.IconDef[]
        {
            WandPanelBuilder.IconDef.WithText(texActionClearSel, L("Molding.ClearSelection")),
            WandPanelBuilder.IconDef.WithText(texActionInvert, L("Molding.InvertSelection")),
            WandPanelBuilder.IconDef.WithText(texActionClearAll, L("Molding.ClearAll")),
            WandPanelBuilder.IconDef.WithText(texActionTeleport, L("Molding.TeleportToPlayer")),
        }, out var actionBtns);
        _clearSelectionBtn = actionBtns[0];
        _invertBtn = actionBtns[1];
        _clearAllBtn = actionBtns[2];
        _teleportToPlayerBtn = actionBtns[3];

        // ═══════════════════════════════════════════════════════════════
        //  Status Display (tile counts)
        // ═══════════════════════════════════════════════════════════════

        _builder.AddSectionHeader("Molding.Status");

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

        _moldedShapeText = new UIText("Molded Shape: none", 0.8f);
        _moldedShapeText.Left.Set(Padding, 0f);
        _moldedShapeText.Top.Set(_builder.CurrentY, 0f);
        _mainPanel.Append(_moldedShapeText);
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
        _modeSelectionBtn.OnToggled += (_, _) => SetMode(MoldingWandMode.Selection);
        _modeCanvasEditBtn.OnToggled += (_, _) => SetMode(MoldingWandMode.CanvasEdit);

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

        // Stencil-slot row — left-click sets ActiveStencilSlot to N (0-indexed).
        // Per plan §0.1: "Each cell is a direct-select for that slot." Toggled
        // state synced in UpdateStencilSlotButtons() below.
        for (int i = 0; i < _stencilSlotBtns.Length; i++)
        {
            int slotIdx = i; // capture for closure
            _stencilSlotBtns[i].OnToggled += (_, _) =>
            {
                var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
                if (mwp != null) mwp.ActiveStencilSlot = (byte)slotIdx;
            };
        }

        // Shape options
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();

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
    //  Settings access — reads from MoldingWandPlayer (NOT Delimitation)
    // ═══════════════════════════════════════════════════════════════════

    private static MoldingWandSettings GetSettings()
    {
        return Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>()?.Settings;
    }

    private static MoldingWandPlayer GetMoldingWandPlayer()
    {
        return Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Setters — write to MoldingWandSettings
    // ═══════════════════════════════════════════════════════════════════

    private void SetMode(MoldingWandMode mode)
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
            s.Shape.Slice, s.Shape.ConnectDiameter, s.Shape.InvertSelection, s.Shape.InvertHalfOrientation);
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

    private void ToggleFlipHalfOrientation()
    {
        var s = GetSettings();
        if (s == null) return;
        var sh = s.Shape;
        sh.InvertHalfOrientation = _flipHalfOrientationBtn.Toggled;
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
    //  Action handlers — operate on MoldingWandPlayer (NOT Delimitation)
    // ═══════════════════════════════════════════════════════════════════

    private static void OnClearSelection()
    {
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;
        int count = mwp.Selection.Count;
        mwp.Selection.Clear();
        mwp.ClearMoldedShape(); // Selection cleared → mold shape is invalid
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Mold selection cleared ({count} tiles removed)", WandColors.MsgMolding);
    }

    private static void OnInvertSelection()
    {
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;

        if (!mwp.Canvas.IsActive)
        {
            Main.NewText("No canvas active — cannot invert mold selection.", Color.OrangeRed);
            return;
        }

        mwp.Selection.ApplyOperation(
            System.Array.Empty<Point>(),
            SelectionOperation.Invert,
            mwp.Canvas);

        // Auto-promote so other wands see the inverted mold immediately
        if (mwp.AutoPromote && mwp.Selection.IsActive)
            mwp.PromoteMoldToCustomShape();
        else if (!mwp.Selection.IsActive)
            mwp.ClearMoldedShape();

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Mold selection inverted ({mwp.Selection.Count} tiles)", WandColors.MsgMolding);
    }

    private static void OnClearAll()
    {
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;
        mwp.ClearAll();
        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText("All molding state cleared.", WandColors.MsgMolding);
    }

    private static void OnTeleportToPlayer()
    {
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;

        if (!mwp.Canvas.IsActive)
        {
            Main.NewText("No canvas active — nothing to teleport.", Color.OrangeRed);
            return;
        }

        var player = Main.LocalPlayer;
        var playerTile = player.Center.ToTileCoordinates();
        var com = mwp.Canvas.CenterOfMass;

        int dx = playerTile.X - (int)System.Math.Round(com.X);
        int dy = playerTile.Y - (int)System.Math.Round(com.Y);

        if (dx == 0 && dy == 0)
        {
            Main.NewText("Canvas is already centered on the player.", WandColors.MsgMolding);
            return;
        }

        mwp.Canvas.Translate(dx, dy);
        mwp.Selection.Translate(dx, dy);

        // Re-promote if there's an active mold
        if (mwp.AutoPromote && mwp.Selection.IsActive)
            mwp.PromoteMoldToCustomShape();

        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f });
        Main.NewText($"Canvas teleported to player (Δ{dx},{dy})", WandColors.MsgMolding);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UI sync from settings (called every frame)
    // ═══════════════════════════════════════════════════════════════════

    private void UpdateModeButtons()
    {
        var s = GetSettings();
        if (s == null) return;
        _modeSelectionBtn.Toggled = s.Mode == MoldingWandMode.Selection;
        _modeCanvasEditBtn.Toggled = s.Mode == MoldingWandMode.CanvasEdit;
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
        // (S2 2026-04-30 — InvertHalfOrientation #IOP)
        if (_flipHalfOrientationBtn != null)
        {
            _flipHalfOrientationBtn.Toggled = s.Shape.InvertHalfOrientation;
            _flipHalfOrientationBtn.Disabled = s.Shape.Slice == SliceMode.Full;
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
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;

        int canvasCount = mwp.Canvas.Count;
        int selCount = mwp.Selection.Count;
        bool hasMolded = mwp.MoldedShape != null;

        _canvasCountText?.SetText($"Canvas: {canvasCount:N0} tiles");
        _selectionCountText?.SetText($"Selection: {selCount:N0} tiles");
        _moldedShapeText?.SetText(hasMolded
            ? $"Molded Shape: {mwp.MoldedShape.Count:N0} tiles"
            : "Molded Shape: none");
    }

    private void SyncFromSettings()
    {
        UpdateModeButtons();
        UpdateOperationButtons();
        UpdateShapeButtons();
        UpdateStencilSlotButtons();
        UpdateThicknessDisplay();
        UpdateShapeOptions();
        UpdateAutoCreateCanvas();
        UpdateSliceGrid();
        UpdateStatusDisplay();
    }

    private void UpdateStencilSlotButtons()
    {
        if (_stencilSlotBtns == null) return;
        var mwp = GetMoldingWandPlayer();
        if (mwp == null) return;
        int active = mwp.ActiveStencilSlot;
        for (int i = 0; i < _stencilSlotBtns.Length; i++)
            _stencilSlotBtns[i].Toggled = (i == active);
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
