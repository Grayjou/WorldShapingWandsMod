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
using static WorldShapingWandsMod.Common.Utilities.Msg;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Settings panel for the Wand of Coating.
/// Lets the player choose paint mode (PaintTile/PaintWall/ScrapeMoss/HarvestMoss),
/// pick a paint color from the 30 vanilla colors, select a shape, and adjust thickness.
/// </summary>
public class CoatingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    /// <summary>Exposes the inner draggable panel for accurate ContainsPoint checks in WandUISystem.</summary>
    public UIElement PanelElement => _mainPanel;

    private UIDraggablePanel _mainPanel;

    // Mode toggles
    private UIToggleButton _paintTileBtn, _paintWallBtn, _scrapeMossBtn, _harvestMossBtn;

    // Coating toggles (Illuminant / Echo) — applied alongside paint
    private UIToggleButton _illuminantBtn, _echoBtn;

    // Paint color picker — 30 colored buttons
    private UIPaintColorButton[] _colorButtons;
    private UIElement _colorPickerContainer;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn;
    private UIIconButton _cardinalBtn;
    private UIIconButton _straightLineBtn;

    private UIText _thicknessValue;
    private UIToggleButton _equalDimensionsBtn;

    // Slice grid
    private UISliceGrid _sliceGrid;

    // Connect diameter toggle
    private UIToggleButton _connectDiameterBtn;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth  = 320f;
    private const float Padding     = 10f;
    private const float ButtonWidth = 140f;
    private const float ButtonHeight = 28f;
    private const float IconBtnSize = 36f;
    private const float IconGap     = 6f;

    // Paint color swatch size and layout
    private const float SwatchSize = 22f;
    private const float SwatchGap  = 4f;
    private const int   SwatchCols = 8;
    // 30 colors + 1 "no paint" + 1 "Ignore" = 32 swatches → ceil(32/8) = 4 rows
    private const int   SwatchRows = 4;
    /// <summary>Total number of paint swatches including None (0) and Ignore (255).</summary>
    private const int   SwatchCount = 32;

    // Vanilla paint RGB values indexed by PaintID (0–30).
    // Index 0 = transparent/none (shown as an eraser swatch).
    // Indices match Terraria.ID.PaintID exactly:
    //   1–12 = basic colors, 13–24 = deep colors, 25 = Black,
    //   26 = White, 27 = Gray, 28 = Brown, 29 = Shadow, 30 = Negative.
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
        _mainPanel.BackgroundColor = new Color(30, 50, 55, 220); // Dark teal tint
        _mainPanel.BorderColor = new Color(0, 100, 90);
        Append(_mainPanel);

        float y = 8f;
        float col1 = Padding;
        float col2 = PanelWidth - Padding - ButtonWidth - 12f;

        // ── Title ─────────────────────────────────────────────────────
        var title = new UISectionTitle(L("Coating.Title"));
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // ── Mode Section ──────────────────────────────────────────────
        var modeSection = new UISectionTitle(L("Coating.Mode"));
        modeSection.Width.Set(0f, 1f);
        modeSection.Height.Set(22f, 0f);
        modeSection.Top.Set(y, 0f);
        _mainPanel.Append(modeSection);
        y += 28f;

        // Row 1: Paint Tile | Paint Wall
        _paintTileBtn  = MakeToggle(L("Coating.PaintTile"),   col1, y, new Color(70, 180, 170));
        _paintWallBtn  = MakeToggle(L("Coating.PaintWall"),   col2, y, new Color(50, 140, 200));
        _mainPanel.Append(_paintTileBtn);
        _mainPanel.Append(_paintWallBtn);
        y += 34f;

        // Row 2: Scrape Moss | Harvest Moss
        _scrapeMossBtn   = MakeToggle(L("Coating.ScrapeMoss"),   col1, y, new Color(100, 170, 80));
        _harvestMossBtn  = MakeToggle(L("Coating.HarvestMoss"),  col2, y, new Color(160, 200, 60));
        _mainPanel.Append(_scrapeMossBtn);
        _mainPanel.Append(_harvestMossBtn);
        y += 42f;

        // ── Coating Section (Illuminant / Echo) ───────────────────────
        var coatingSection = new UISectionTitle(L("Coating.CoatingType"));
        coatingSection.Width.Set(0f, 1f);
        coatingSection.Height.Set(22f, 0f);
        coatingSection.Top.Set(y, 0f);
        _mainPanel.Append(coatingSection);
        y += 28f;

        _illuminantBtn = MakeToggle(L("Coating.Illuminant"), col1, y, new Color(255, 255, 180));
        _echoBtn       = MakeToggle(L("Coating.Echo"),       col2, y, new Color(180, 180, 255));
        _mainPanel.Append(_illuminantBtn);
        _mainPanel.Append(_echoBtn);
        y += 42f;

        // ── Paint Color Picker ────────────────────────────────────────
        var colorSection = new UISectionTitle(L("Coating.PaintColor"));
        colorSection.Width.Set(0f, 1f);
        colorSection.Height.Set(22f, 0f);
        colorSection.Top.Set(y, 0f);
        _mainPanel.Append(colorSection);
        y += 28f;

        _colorPickerContainer = new UIElement();
        _colorPickerContainer.Width.Set(0f, 1f);
        _colorPickerContainer.Top.Set(y, 0f);

        // Build the 8×4 grid of paint swatches (colors 0–30 + Ignore = 32 entries)
        // Color 0 (index 0) = "No Paint" (eraser X symbol)
        // Last entry (array index 31) = "Ignore" (paint byte 255)
        _colorButtons = new UIPaintColorButton[SwatchCount];
        float totalWidth = SwatchCols * SwatchSize + (SwatchCols - 1) * SwatchGap;
        float startX = (PanelWidth - 2 * Padding - totalWidth) / 2f;

        for (int i = 0; i < SwatchCount; i++)
        {
            int arrayIndex = i;
            // Array index 0–30 → paint color 0–30; index 31 → paint color 255 (Ignore)
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
        y += colorPickerHeight + 12f;

        // ── Shape Section ─────────────────────────────────────────────
        var shapeSection = new UISectionTitle(L("Common.Shape"));
        shapeSection.Width.Set(0f, 1f);
        shapeSection.Height.Set(22f, 0f);
        shapeSection.Top.Set(y, 0f);
        _mainPanel.Append(shapeSection);
        y += 28f;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var texRectFilled     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectFilled",       AssetRequestMode.ImmediateLoad);
        var texRectHollow     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectHollow",       AssetRequestMode.ImmediateLoad);
        var texEllipseFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseFilled",    AssetRequestMode.ImmediateLoad);
        var texEllipseHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseHollow",    AssetRequestMode.ImmediateLoad);
        var texDiamondFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondFilled",    AssetRequestMode.ImmediateLoad);
        var texDiamondHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondHollow",    AssetRequestMode.ImmediateLoad);
        var texTriangleFilled = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleFilled",   AssetRequestMode.ImmediateLoad);
        var texTriangleHollow = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleHollow",   AssetRequestMode.ImmediateLoad);
        var texElbow           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeElbow",            AssetRequestMode.ImmediateLoad);
        var texCardinal       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeCardinal",         AssetRequestMode.ImmediateLoad);
        var texStraightLine   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraightLine",     AssetRequestMode.ImmediateLoad);

        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

        // Row 1
        _rectFilledBtn    = MakeIconBtn(texRectFilled,     L("Common.ShapeRectFilled"),    shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _rectHollowBtn    = MakeIconBtn(texRectHollow,     L("Common.ShapeRectHollow"),    shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _ellipseFilledBtn = MakeIconBtn(texEllipseFilled,  L("Common.ShapeEllipseFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _ellipseHollowBtn = MakeIconBtn(texEllipseHollow,  L("Common.ShapeEllipseHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _edgeBtn          = MakeIconBtn(texElbow,           L("Common.ShapeElbow"),          shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_rectHollowBtn);
        _mainPanel.Append(_ellipseFilledBtn);
        _mainPanel.Append(_ellipseHollowBtn);
        _mainPanel.Append(_edgeBtn);
        y += IconBtnSize + IconGap;

        // Row 2
        _diamondFilledBtn  = MakeIconBtn(texDiamondFilled,  L("Common.ShapeDiamondFilled"),  shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _diamondHollowBtn  = MakeIconBtn(texDiamondHollow,  L("Common.ShapeDiamondHollow"),  shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _triangleFilledBtn = MakeIconBtn(texTriangleFilled, L("Common.ShapeTriangleFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _triangleHollowBtn = MakeIconBtn(texTriangleHollow, L("Common.ShapeTriangleHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _cardinalBtn       = MakeIconBtn(texCardinal,       L("Common.ShapeCardinal"),       shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_diamondHollowBtn);
        _mainPanel.Append(_triangleFilledBtn);
        _mainPanel.Append(_triangleHollowBtn);
        _mainPanel.Append(_cardinalBtn);
        y += IconBtnSize + IconGap;

        // Row 3: additional line shapes
        _straightLineBtn   = MakeIconBtn(texStraightLine,   L("Common.ShapeStraightLine"),   shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _mainPanel.Append(_straightLineBtn);
        y += IconBtnSize + 12f;

        // === SLICE SECTION ===
        var sliceSection = new UISectionTitle(L("Common.Slice"));
        sliceSection.Width.Set(0f, 1f);
        sliceSection.Height.Set(22f, 0f);
        sliceSection.Top.Set(y, 0f);
        _mainPanel.Append(sliceSection);
        y += 28f;

        _sliceGrid = new UISliceGrid();
        _sliceGrid.HAlign = 0.5f;
        _sliceGrid.Top.Set(y, 0f);
        _sliceGrid.OnChanged += OnSliceChanged;
        _mainPanel.Append(_sliceGrid);
        y += _sliceGrid.Height.Pixels + 12f;

        // ── Outline Thickness ─────────────────────────────────────────
        var thicknessLabel = new UIText(L("Common.OutlineThickness"), 0.85f);
        thicknessLabel.Left.Set(col1, 0f);
        thicknessLabel.Top.Set(y, 0f);
        _mainPanel.Append(thicknessLabel);

        var minusBtn = new UITextPanel<string>("-", 0.8f, false);
        minusBtn.Width.Set(30f, 0f);
        minusBtn.Height.Set(26f, 0f);
        minusBtn.Left.Set(col1 + 130f, 0f);
        minusBtn.Top.Set(y - 2f, 0f);
        minusBtn.OnLeftClick += (_, _) => AdjustThickness(-1);
        minusBtn.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
        _mainPanel.Append(minusBtn);

        _thicknessValue = new UIText("1", 0.9f);
        _thicknessValue.Left.Set(col1 + 170f, 0f);
        _thicknessValue.Top.Set(y, 0f);
        _thicknessValue.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
        _mainPanel.Append(_thicknessValue);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(y - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => AdjustThickness(1);
        plusBtn.OnScrollWheel += (evt, _) => AdjustThickness(evt.ScrollWheelValue > 0 ? 1 : -1);
        _mainPanel.Append(plusBtn);
        y += 42f;

        // ── Equal Dimensions ─────────────────────────────────────────
        _equalDimensionsBtn = new UIToggleButton(L("Common.EqualDimensions"), false);
        _equalDimensionsBtn.Width.Set(200f, 0f);
        _equalDimensionsBtn.Height.Set(28f, 0f);
        _equalDimensionsBtn.HAlign = 0.5f;
        _equalDimensionsBtn.Top.Set(y, 0f);
        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _mainPanel.Append(_equalDimensionsBtn);
        y += 38f;

        // ── Connect Diameter Toggle ──────────────────────────────────
        _connectDiameterBtn = new UIToggleButton(L("Common.ConnectDiameter"), true);
        _connectDiameterBtn.Width.Set(200f, 0f);
        _connectDiameterBtn.Height.Set(28f, 0f);
        _connectDiameterBtn.HAlign = 0.5f;
        _connectDiameterBtn.Top.Set(y, 0f);
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _mainPanel.Append(_connectDiameterBtn);
        y += 38f;

        // ── Close Button ──────────────────────────────────────────────
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => ModContent.GetInstance<WandUISystem>().CloseAllUI();
        _mainPanel.Append(closeBtn);
        y += 40f;

        // Finalize panel height
        _mainPanel.Height.Set(y + 8f, 0f);

        // ── Wire up mode events ───────────────────────────────────────
        _paintTileBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.PaintTile;   UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _paintWallBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.PaintWall;   UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _scrapeMossBtn.OnToggled  += (_, _) => { GetSettings().Mode = CoatingMode.ScrapeMoss;  UpdateModeButtons(); UpdateColorPickerVisibility(); };
        _harvestMossBtn.OnToggled += (_, _) => { GetSettings().Mode = CoatingMode.HarvestMoss; UpdateModeButtons(); UpdateColorPickerVisibility(); };

        // ── Wire up coating toggle events ─────────────────────────────
        _illuminantBtn.OnToggled += (_, _) => { ToggleIlluminant(); };
        _echoBtn.OnToggled       += (_, _) => { ToggleEcho(); };

        // ── Wire up shape events ──────────────────────────────────────
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
    }

    // ── Settings helpers ──────────────────────────────────────────────
    private WandOfCoatingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.CoatingSettings;

    private void SetPaintColor(byte colorIndex)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.PaintColor = colorIndex;
        UpdateColorButtons();
    }

    /// <summary>
    /// Cycles the Illuminant coating through three states:
    ///   Apply (on) → Remove (off) → Ignore → Apply …
    /// </summary>
    private void ToggleIlluminant()
    {
        var settings = GetSettings();
        if (settings == null) return;

        if (settings.IgnoreIlluminant)
        {
            // Ignore → Apply
            settings.ApplyIlluminant = true;
            settings.IgnoreIlluminant = false;
        }
        else if (settings.ApplyIlluminant)
        {
            // Apply → Remove
            settings.ApplyIlluminant = false;
        }
        else
        {
            // Remove → Ignore
            settings.IgnoreIlluminant = true;
        }

        UpdateCoatingButtons();
    }

    /// <summary>
    /// Cycles the Echo coating through three states:
    ///   Apply (on) → Remove (off) → Ignore → Apply …
    /// </summary>
    private void ToggleEcho()
    {
        var settings = GetSettings();
        if (settings == null) return;

        if (settings.IgnoreEcho)
        {
            // Ignore → Apply
            settings.ApplyEcho = true;
            settings.IgnoreEcho = false;
        }
        else if (settings.ApplyEcho)
        {
            // Apply → Remove
            settings.ApplyEcho = false;
        }
        else
        {
            // Remove → Ignore
            settings.IgnoreEcho = true;
        }

        UpdateCoatingButtons();
    }

    /// <summary>
    /// Updates the visual state of the Illuminant/Echo toggle buttons to reflect
    /// the current tri-state: Apply (green), Remove (red-ish), Ignore (gray-blue).
    /// </summary>
    private void UpdateCoatingButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        // Illuminant tri-state visual
        if (settings.IgnoreIlluminant)
        {
            _illuminantBtn.Toggled = false;
            _illuminantBtn.TintColor = new Color(120, 120, 140); // gray-blue = Ignore
            _illuminantBtn.SetText(L("Coating.Illuminant") + ": " + L("Coating.Ignore"));
        }
        else if (settings.ApplyIlluminant)
        {
            _illuminantBtn.Toggled = true;
            _illuminantBtn.TintColor = new Color(255, 255, 180); // warm yellow = Apply
            _illuminantBtn.SetText(L("Coating.Illuminant") + ": " + L("Coating.On"));
        }
        else
        {
            _illuminantBtn.Toggled = true;
            _illuminantBtn.TintColor = new Color(180, 70, 70); // red-ish = Remove
            _illuminantBtn.SetText(L("Coating.Illuminant") + ": " + L("Coating.Off"));
        }

        // Echo tri-state visual
        if (settings.IgnoreEcho)
        {
            _echoBtn.Toggled = false;
            _echoBtn.TintColor = new Color(120, 120, 140);
            _echoBtn.SetText(L("Coating.Echo") + ": " + L("Coating.Ignore"));
        }
        else if (settings.ApplyEcho)
        {
            _echoBtn.Toggled = true;
            _echoBtn.TintColor = new Color(180, 180, 255);
            _echoBtn.SetText(L("Coating.Echo") + ": " + L("Coating.On"));
        }
        else
        {
            _echoBtn.Toggled = true;
            _echoBtn.TintColor = new Color(180, 70, 70);
            _echoBtn.SetText(L("Coating.Echo") + ": " + L("Coating.Off"));
        }
    }

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        int max = ModContent.GetInstance<Configs.WandConfig>()?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
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

    private void OnSliceChanged(SliceMode slice)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.Slice = slice;
        settings.Shape = shape;
    }

    // ── Button constructors ───────────────────────────────────────────
    private UIToggleButton MakeToggle(string text, float left, float top, Color? tint = null)
    {
        var btn = new UIToggleButton(text, false);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = false;
        if (tint.HasValue) btn.TintColor = tint.Value;
        return btn;
    }

    private UIIconButton MakeIconBtn(Asset<Texture2D> texture, string hoverText, float left, float top)
    {
        var btn = new UIIconButton(texture, hoverText);
        btn.Width.Set(IconBtnSize, 0f);
        btn.Height.Set(IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = true;
        return btn;
    }

    // ── Sync methods ──────────────────────────────────────────────────
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

        // Hide the color picker when scraping (no color needed)
        bool showPicker = settings.Mode == CoatingMode.PaintTile || settings.Mode == CoatingMode.PaintWall;
        _colorPickerContainer.MarginBottom = showPicker ? 0f : 0f; // Always shown; grayed-out when not applicable
        // We just visually show it at reduced opacity via UpdateColorButtons
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

    private void UpdateThicknessDisplay()
    {
        var settings = GetSettings();
        _thicknessValue?.SetText(settings?.Shape.Thickness.ToString() ?? "1");
    }

    private void UpdateEqualDimensionsButton()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _equalDimensionsBtn.Toggled = settings.Shape.EqualDimensions;
    }

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
    }

    private void UpdateConnectDiameterButton()
    {
        var settings = GetSettings();
        if (settings == null || _connectDiameterBtn == null) return;
        _connectDiameterBtn.Toggled = settings.Shape.ConnectDiameter;
    }

    private void UpdateSliceGrid()
    {
        var settings = GetSettings();
        if (settings == null || _sliceGrid == null) return;
        _sliceGrid.SetValue(settings.Shape.Slice);
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
    }

    // ─────────────────────────────────────────────────────────────────
    // Inner class: paint color swatch button
    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// A small square button displaying a paint color. Shows a selection outline when active.
    /// For color index 0 (no paint), draws a diagonal cross instead.
    /// </summary>
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

            // Background: the paint color (or dark gray for "no paint" / "ignore")
            Color bg = _colorIndex == 0 || _colorIndex == WandOfCoatingBase.IgnorePaintColor
                ? new Color(40, 40, 40) : _color;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

            // For "no paint" (index 0): draw a simple X
            if (_colorIndex == 0)
            {
                // Top-left to bottom-right diagonal
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, 2),
                    Color.Red * 0.85f);
                // Top-right to bottom-left diagonal (approximate via second rect)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 3, rect.Bottom - 5, rect.Width - 6, 2),
                    Color.Red * 0.85f);
            }

            // For "Ignore" (255): draw a horizontal dash "—"
            if (_colorIndex == WandOfCoatingBase.IgnorePaintColor)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rect.X + 4, rect.Y + rect.Height / 2 - 1, rect.Width - 8, 2),
                    Color.LightGray * 0.9f);
            }

            // Selection outline: bright white border when selected
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

            // Tooltip
            if (IsMouseHovering && Main.playerInventory == false)
            {
                Main.hoverItemName = _name;
            }
        }
    }
}
