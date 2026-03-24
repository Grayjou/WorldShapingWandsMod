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

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;

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

    // Vanilla paint RGB values indexed by PaintID (0-30).
    private static readonly Color[] PaintColors = new Color[]
    {
        Color.Transparent,           // 0 = none
        new Color(195, 39, 39),      // 1  Red
        new Color(219, 118, 33),     // 2  Orange
        new Color(228, 210, 21),     // 3  Yellow
        new Color(100, 196, 72),     // 4  Lime
        new Color(53, 165, 38),      // 5  Green
        new Color(0, 142, 124),      // 6  Teal
        new Color(0, 165, 209),      // 7  Cyan
        new Color(0, 118, 209),      // 8  Sky Blue
        new Color(0, 56, 221),       // 9  Blue
        new Color(147, 0, 209),      // 10 Purple
        new Color(102, 0, 209),      // 11 Violet
        new Color(209, 0, 142),      // 12 Pink
        new Color(124, 0, 0),        // 13 Deep Red
        new Color(140, 60, 0),       // 14 Deep Orange
        new Color(140, 128, 0),      // 15 Deep Yellow
        new Color(34, 100, 0),       // 16 Deep Lime
        new Color(0, 80, 0),         // 17 Deep Green
        new Color(0, 68, 60),        // 18 Deep Teal
        new Color(0, 68, 110),       // 19 Deep Cyan
        new Color(0, 40, 124),       // 20 Deep Sky Blue
        new Color(0, 0, 110),        // 21 Deep Blue
        new Color(70, 0, 124),       // 22 Deep Purple
        new Color(56, 0, 110),       // 23 Deep Violet
        new Color(110, 0, 68),       // 24 Deep Pink
        new Color(20, 20, 20),       // 25 Black
        new Color(252, 252, 252),    // 26 White
        new Color(127, 127, 127),    // 27 Gray
        new Color(151, 107, 75),     // 28 Brown
        new Color(0, 0, 0),          // 29 Shadow
        new Color(230, 0, 255),      // 30 Negative
    };

    private static readonly string[] PaintColorNames = new string[]
    {
        "None",
        "Red", "Orange", "Yellow", "Lime", "Green",
        "Teal", "Cyan", "Sky Blue", "Blue", "Purple", "Violet", "Pink",
        "Deep Red", "Deep Orange", "Deep Yellow", "Deep Lime", "Deep Green",
        "Deep Teal", "Deep Cyan", "Deep Sky Blue", "Deep Blue",
        "Deep Purple", "Deep Violet", "Deep Pink",
        "Black", "White", "Gray", "Brown", "Shadow", "Negative"
    };

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(30, 50, 55, 220);
        _mainPanel.BorderColor = new Color(0, 100, 90);
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Coating.Title");

        // === MODE (non-radio icon buttons) ===
        var texPaintTile   = mod.Assets.Request<Texture2D>("Assets/Icons/ModePaintTile",   AssetRequestMode.ImmediateLoad);
        var texPaintWall   = mod.Assets.Request<Texture2D>("Assets/Icons/ModePaintWall",   AssetRequestMode.ImmediateLoad);
        var texScrapeMoss  = mod.Assets.Request<Texture2D>("Assets/Icons/ModeScrapeMoss",  AssetRequestMode.ImmediateLoad);
        var texHarvestMoss = mod.Assets.Request<Texture2D>("Assets/Icons/ModeHarvestMoss", AssetRequestMode.ImmediateLoad);

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

        // === PAINT COLOR PICKER (manual section using builder's Y tracker) ===
        _builder.AddSectionHeader("Coating.PaintColor");

        float y = _builder.CurrentY;
        _colorPickerContainer = new UIElement();
        _colorPickerContainer.Width.Set(0f, 1f);
        _colorPickerContainer.Top.Set(y, 0f);

        _colorButtons = new UIPaintColorButton[SwatchCount];
        float totalWidth = SwatchCols * SwatchSize + (SwatchCols - 1) * SwatchGap;
        float startX = (PanelWidth - 2 * Padding - totalWidth) / 2f;

        for (int i = 0; i < SwatchCount; i++)
        {
            int arrayIndex = i;
            byte paintByte = (byte)(i < 31 ? i : WandOfCoatingBase.IgnorePaintColor);
            Color swatchColor = i < 31 ? PaintColors[i] : Color.Transparent;
            string swatchName = i < 31 ? PaintColorNames[i] : L("Coating.Ignore");

            int col = i % SwatchCols;
            int row = i / SwatchCols;
            float sx = startX + col * (SwatchSize + SwatchGap);
            float sy = row * (SwatchSize + SwatchGap);

            var btn = new UIPaintColorButton(paintByte, swatchColor, swatchName);
            btn.Width.Set(SwatchSize, 0f);
            btn.Height.Set(SwatchSize, 0f);
            btn.Left.Set(sx, 0f);
            btn.Top.Set(sy, 0f);
            btn.OnLeftClick += (_, _) => SetPaintColor(paintByte);
            _colorButtons[arrayIndex] = btn;
            _colorPickerContainer.Append(btn);
        }

        float colorPickerHeight = SwatchRows * (SwatchSize + SwatchGap);
        _colorPickerContainer.Height.Set(colorPickerHeight, 0f);
        _mainPanel.Append(_colorPickerContainer);
        _builder.AdvanceY(colorPickerHeight + WandPanelBuilder.AfterIconGridSpacing);

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
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        var texRepaint     = mod.Assets.Request<Texture2D>("Assets/Icons/ToggleRepaint", AssetRequestMode.ImmediateLoad);

        _builder.AddOptionsSection(new WandPanelBuilder.IconDef[]
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
        int max = ModContent.GetInstance<WandServerConfig>()?.MaxOutlineThickness ?? 10;
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
            ModContent.GetInstance<WandUISystem>().CloseAllUI();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
        _builder?.DrawDebugLines(spriteBatch, _mainPanel.GetDimensions());
    }

    // ── Inner class: paint color swatch button ────────────────────────
    private class UIPaintColorButton : UIElement
    {
        private readonly byte _colorIndex;
        private readonly Color _color;
        private readonly string _name;
        public bool IsSelected { get; set; }

        public UIPaintColorButton(byte colorIndex, Color color, string name)
        {
            _colorIndex = colorIndex;
            _color = color;
            _name = name;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var dims = GetDimensions();
            var rect = dims.ToRectangle();

            Color bg = _colorIndex == 0 || _colorIndex == WandOfCoatingBase.IgnorePaintColor
                ? new Color(40, 40, 40) : _color;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

            if (_colorIndex == 0)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, 2),
                    Color.Red * 0.85f);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 3, rect.Bottom - 5, rect.Width - 6, 2),
                    Color.Red * 0.85f);
            }

            if (_colorIndex == WandOfCoatingBase.IgnorePaintColor)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 4, rect.Y + rect.Height / 2 - 1, rect.Width - 8, 2),
                    Color.LightGray * 0.9f);
            }

            if (IsSelected)
            {
                int t = 2;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, t), Color.White);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), Color.White);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, t, rect.Height), Color.White);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), Color.White);
            }
            else if (IsMouseHovering)
            {
                int t = 1;
                Color hover = Color.LightGray * 0.7f;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, t), hover);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), hover);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, t, rect.Height), hover);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), hover);
            }

            if (IsMouseHovering && Main.playerInventory == false)
            {
                Main.hoverItemName = _name;
            }
        }
    }
}