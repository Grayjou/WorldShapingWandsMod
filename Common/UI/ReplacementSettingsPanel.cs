using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

public class ReplacementSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    private UIDraggablePanel _mainPanel;

    // Shape buttons (icon-based)
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _halfEllipseHFilledBtn, _halfEllipseHHollowBtn;
    private UIIconButton _halfEllipseVFilledBtn, _halfEllipseVHollowBtn;
    private UIIconButton _edgeBtn;
    private UIIconButton _straightBtn;

    private UIText _thicknessValue;

    // Source (OldObject) type buttons (icon-based)
    private UIIconButton _srcTileBtn, _srcPlatformBtn, _srcRopeBtn;
    private UIIconButton _srcPlanterBtn, _srcRailBtn, _srcSeedsBtn;

    // Target (NewObject) type buttons (icon-based)
    private UIIconButton _tgtTileBtn, _tgtPlatformBtn, _tgtRopeBtn;
    private UIIconButton _tgtPlanterBtn, _tgtRailBtn, _tgtSeedsBtn, _tgtAirBtn;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float PanelHeight = 510f;
    private const float Padding = 10f;
    private const float IconBtnSize = 36f;
    private const float IconGap = 6f;

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.Height.Set(PanelHeight, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(44, 79, 44, 220);  // Greenish tint
        _mainPanel.BorderColor = new Color(20, 60, 20);
        Append(_mainPanel);

        float y = 8f;
        float col1 = Padding;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        // Load object type icon textures
        var texTile      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjTile", AssetRequestMode.ImmediateLoad);
        var texPlatform  = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlatform", AssetRequestMode.ImmediateLoad);
        var texRope      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjRope", AssetRequestMode.ImmediateLoad);
        var texPlanter   = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlanter", AssetRequestMode.ImmediateLoad);
        var texRail      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjRail", AssetRequestMode.ImmediateLoad);
        var texSeeds     = mod.Assets.Request<Texture2D>("Assets/Icons/ObjSeeds", AssetRequestMode.ImmediateLoad);
        var texAir       = mod.Assets.Request<Texture2D>("Assets/Icons/ObjAir", AssetRequestMode.ImmediateLoad);

        // Title
        var title = new UISectionTitle(L("Replacement.Title"));
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // === SOURCE TYPE SECTION (OldObject — what to find) ===
        var srcSection = new UISectionTitle(L("Replacement.SourceType"));
        srcSection.Width.Set(0f, 1f);
        srcSection.Height.Set(22f, 0f);
        srcSection.Top.Set(y, 0f);
        _mainPanel.Append(srcSection);
        y += 28f;

        // Row of 6 source type icons, centered
        float totalSrcWidth = IconBtnSize * 6 + IconGap * 5;
        float srcStartX = (PanelWidth - totalSrcWidth) / 2f - Padding;

        _srcTileBtn     = MakeIconBtn(texTile,     L("Replacement.Tile"),     srcStartX + (IconBtnSize + IconGap) * 0, y);
        _srcPlatformBtn = MakeIconBtn(texPlatform, L("Replacement.Platform"), srcStartX + (IconBtnSize + IconGap) * 1, y);
        _srcRopeBtn     = MakeIconBtn(texRope,     L("Replacement.Rope"),     srcStartX + (IconBtnSize + IconGap) * 2, y);
        _srcPlanterBtn  = MakeIconBtn(texPlanter,  L("Replacement.Planter"),  srcStartX + (IconBtnSize + IconGap) * 3, y);
        _srcRailBtn     = MakeIconBtn(texRail,     L("Replacement.Rail"),     srcStartX + (IconBtnSize + IconGap) * 4, y);
        _srcSeedsBtn    = MakeIconBtn(texSeeds,    L("Replacement.Seeds"),    srcStartX + (IconBtnSize + IconGap) * 5, y);
        _mainPanel.Append(_srcTileBtn);
        _mainPanel.Append(_srcPlatformBtn);
        _mainPanel.Append(_srcRopeBtn);
        _mainPanel.Append(_srcPlanterBtn);
        _mainPanel.Append(_srcRailBtn);
        _mainPanel.Append(_srcSeedsBtn);
        y += IconBtnSize + 12f;

        // === TARGET TYPE SECTION (NewObject — what to replace with) ===
        var tgtSection = new UISectionTitle(L("Replacement.TargetType"));
        tgtSection.Width.Set(0f, 1f);
        tgtSection.Height.Set(22f, 0f);
        tgtSection.Top.Set(y, 0f);
        _mainPanel.Append(tgtSection);
        y += 28f;

        // Row of 7 target type icons, centered
        float totalTgtWidth = IconBtnSize * 7 + IconGap * 6;
        float tgtStartX = (PanelWidth - totalTgtWidth) / 2f - Padding;

        _tgtTileBtn     = MakeIconBtn(texTile,     L("Replacement.Tile"),     tgtStartX + (IconBtnSize + IconGap) * 0, y);
        _tgtPlatformBtn = MakeIconBtn(texPlatform, L("Replacement.Platform"), tgtStartX + (IconBtnSize + IconGap) * 1, y);
        _tgtRopeBtn     = MakeIconBtn(texRope,     L("Replacement.Rope"),     tgtStartX + (IconBtnSize + IconGap) * 2, y);
        _tgtPlanterBtn  = MakeIconBtn(texPlanter,  L("Replacement.Planter"),  tgtStartX + (IconBtnSize + IconGap) * 3, y);
        _tgtRailBtn     = MakeIconBtn(texRail,     L("Replacement.Rail"),     tgtStartX + (IconBtnSize + IconGap) * 4, y);
        _tgtSeedsBtn    = MakeIconBtn(texSeeds,    L("Replacement.Seeds"),    tgtStartX + (IconBtnSize + IconGap) * 5, y);
        _tgtAirBtn      = MakeIconBtn(texAir,      L("Replacement.Air"),      tgtStartX + (IconBtnSize + IconGap) * 6, y);
        _mainPanel.Append(_tgtTileBtn);
        _mainPanel.Append(_tgtPlatformBtn);
        _mainPanel.Append(_tgtRopeBtn);
        _mainPanel.Append(_tgtPlanterBtn);
        _mainPanel.Append(_tgtRailBtn);
        _mainPanel.Append(_tgtSeedsBtn);
        _mainPanel.Append(_tgtAirBtn);
        y += IconBtnSize + 12f;

        // === SHAPE SECTION ===
        var shapeSection = new UISectionTitle(L("Common.Shape"));
        shapeSection.Width.Set(0f, 1f);
        shapeSection.Height.Set(22f, 0f);
        shapeSection.Top.Set(y, 0f);
        _mainPanel.Append(shapeSection);
        y += 28f;

        // Load shape icon textures
        var texRectFilled     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectFilled", AssetRequestMode.ImmediateLoad);
        var texRectHollow     = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeRectHollow", AssetRequestMode.ImmediateLoad);
        var texEllipseFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseFilled", AssetRequestMode.ImmediateLoad);
        var texEllipseHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEllipseHollow", AssetRequestMode.ImmediateLoad);
        var texDiamondFilled  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondFilled", AssetRequestMode.ImmediateLoad);
        var texDiamondHollow  = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeDiamondHollow", AssetRequestMode.ImmediateLoad);
        var texTriangleFilled = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad);
        var texTriangleHollow = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeTriangleHollow", AssetRequestMode.ImmediateLoad);
        var texHalfEHFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseHFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEHHollow   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseHHollow", AssetRequestMode.ImmediateLoad);
        var texHalfEVFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseVFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEVHollow   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseVHollow", AssetRequestMode.ImmediateLoad);
        var texEdge           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEdge", AssetRequestMode.ImmediateLoad);
        var texStraight       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraight", AssetRequestMode.ImmediateLoad);

        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

        // Shape row 1: Rect, Ellipse, Edge
        _rectFilledBtn    = MakeIconBtn(texRectFilled,     L("Common.ShapeRectFilled"),    shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _rectHollowBtn    = MakeIconBtn(texRectHollow,     L("Common.ShapeRectHollow"),    shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _ellipseFilledBtn = MakeIconBtn(texEllipseFilled,  L("Common.ShapeEllipseFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _ellipseHollowBtn = MakeIconBtn(texEllipseHollow,  L("Common.ShapeEllipseHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _edgeBtn          = MakeIconBtn(texEdge,           L("Common.ShapeEdge"),          shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_rectFilledBtn);
        _mainPanel.Append(_rectHollowBtn);
        _mainPanel.Append(_ellipseFilledBtn);
        _mainPanel.Append(_ellipseHollowBtn);
        _mainPanel.Append(_edgeBtn);
        y += IconBtnSize + IconGap;

        // Shape row 2: Diamond, Triangle, Straight
        _diamondFilledBtn  = MakeIconBtn(texDiamondFilled,  L("Common.ShapeDiamondFilled"),  shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _diamondHollowBtn  = MakeIconBtn(texDiamondHollow,  L("Common.ShapeDiamondHollow"),  shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _triangleFilledBtn = MakeIconBtn(texTriangleFilled, L("Common.ShapeTriangleFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _triangleHollowBtn = MakeIconBtn(texTriangleHollow, L("Common.ShapeTriangleHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _straightBtn       = MakeIconBtn(texStraight,       L("Common.ShapeStraight"),       shapeStartX + (IconBtnSize + IconGap) * 4, y);
        _mainPanel.Append(_diamondFilledBtn);
        _mainPanel.Append(_diamondHollowBtn);
        _mainPanel.Append(_triangleFilledBtn);
        _mainPanel.Append(_triangleHollowBtn);
        _mainPanel.Append(_straightBtn);
        y += IconBtnSize + IconGap;

        // Shape row 3: Half-Ellipses
        float totalRow3Width = IconBtnSize * 4 + IconGap * 3;
        float row3StartX = (PanelWidth - totalRow3Width) / 2f - Padding;

        _halfEllipseHFilledBtn = MakeIconBtn(texHalfEHFilled, L("Common.ShapeHalfEllipseHFilled"), row3StartX + (IconBtnSize + IconGap) * 0, y);
        _halfEllipseHHollowBtn = MakeIconBtn(texHalfEHHollow, L("Common.ShapeHalfEllipseHHollow"), row3StartX + (IconBtnSize + IconGap) * 1, y);
        _halfEllipseVFilledBtn = MakeIconBtn(texHalfEVFilled, L("Common.ShapeHalfEllipseVFilled"), row3StartX + (IconBtnSize + IconGap) * 2, y);
        _halfEllipseVHollowBtn = MakeIconBtn(texHalfEVHollow, L("Common.ShapeHalfEllipseVHollow"), row3StartX + (IconBtnSize + IconGap) * 3, y);
        _mainPanel.Append(_halfEllipseHFilledBtn);
        _mainPanel.Append(_halfEllipseHHollowBtn);
        _mainPanel.Append(_halfEllipseVFilledBtn);
        _mainPanel.Append(_halfEllipseVHollowBtn);
        y += IconBtnSize + 12f;

        // Thickness
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
        _mainPanel.Append(minusBtn);

        _thicknessValue = new UIText("1", 0.9f);
        _thicknessValue.Left.Set(col1 + 170f, 0f);
        _thicknessValue.Top.Set(y, 0f);
        _mainPanel.Append(_thicknessValue);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(y - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => AdjustThickness(1);
        _mainPanel.Append(plusBtn);
        y += 42f;

        // Close button
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => IsVisible = false;
        _mainPanel.Append(closeBtn);

        // Wire up events
        _rectFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Filled);
        _rectHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Rectangle, ShapeMode.Hollow);
        _edgeBtn.OnToggled += (_, _) => SetShape(ShapeType.Edge, ShapeMode.Filled);
        _straightBtn.OnToggled += (_, _) => SetShape(ShapeType.StraightLine, ShapeMode.Filled);
        _ellipseFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Filled);
        _ellipseHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Ellipse, ShapeMode.Hollow);
        _diamondFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Filled);
        _diamondHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Diamond, ShapeMode.Hollow);
        _triangleFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Filled);
        _triangleHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.Triangle, ShapeMode.Hollow);
        _halfEllipseHFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseH, ShapeMode.Filled);
        _halfEllipseHHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseH, ShapeMode.Hollow);
        _halfEllipseVFilledBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseV, ShapeMode.Filled);
        _halfEllipseVHollowBtn.OnToggled += (_, _) => SetShape(ShapeType.HalfEllipseV, ShapeMode.Hollow);

        // Source type events
        _srcTileBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Tile);
        _srcPlatformBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Platform);
        _srcRopeBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Rope);
        _srcPlanterBtn.OnToggled += (_, _) => SetSourceType(ObjectType.PlanterBox);
        _srcRailBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Rail);
        _srcSeedsBtn.OnToggled += (_, _) => SetSourceType(ObjectType.Seeds);

        // Target type events
        _tgtTileBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Tile);
        _tgtPlatformBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Platform);
        _tgtRopeBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Rope);
        _tgtPlanterBtn.OnToggled += (_, _) => SetTargetType(ObjectType.PlanterBox);
        _tgtRailBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Rail);
        _tgtSeedsBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Seeds);
        _tgtAirBtn.OnToggled += (_, _) => SetTargetType(ObjectType.Air);
    }

    private WandOfReplacementSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.ReplacementSettings;

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness);
        UpdateShapeButtons();
    }

    private void SetSourceType(ObjectType type)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.OldObject = type;
        UpdateSourceButtons();
    }

    private void SetTargetType(ObjectType type)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.NewObject = type;
        UpdateTargetButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, 50);
        settings.Shape = shape;
        UpdateThicknessDisplay();
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

    private void UpdateShapeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;

        _rectFilledBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Filled;
        _rectHollowBtn.Toggled = shape.Shape == ShapeType.Rectangle && shape.FillMode == ShapeMode.Hollow;
        _edgeBtn.Toggled = shape.Shape == ShapeType.Edge;
        _straightBtn.Toggled = shape.Shape == ShapeType.StraightLine;
        _ellipseFilledBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Filled;
        _ellipseHollowBtn.Toggled = shape.Shape == ShapeType.Ellipse && shape.FillMode == ShapeMode.Hollow;
        _diamondFilledBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Filled;
        _diamondHollowBtn.Toggled = shape.Shape == ShapeType.Diamond && shape.FillMode == ShapeMode.Hollow;
        _triangleFilledBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Filled;
        _triangleHollowBtn.Toggled = shape.Shape == ShapeType.Triangle && shape.FillMode == ShapeMode.Hollow;
        _halfEllipseHFilledBtn.Toggled = shape.Shape == ShapeType.HalfEllipseH && shape.FillMode == ShapeMode.Filled;
        _halfEllipseHHollowBtn.Toggled = shape.Shape == ShapeType.HalfEllipseH && shape.FillMode == ShapeMode.Hollow;
        _halfEllipseVFilledBtn.Toggled = shape.Shape == ShapeType.HalfEllipseV && shape.FillMode == ShapeMode.Filled;
        _halfEllipseVHollowBtn.Toggled = shape.Shape == ShapeType.HalfEllipseV && shape.FillMode == ShapeMode.Hollow;
    }

    private void UpdateThicknessDisplay()
    {
        var settings = GetSettings();
        _thicknessValue?.SetText(settings?.Shape.Thickness.ToString() ?? "1");
    }

    private void UpdateSourceButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var src = settings.OldObject;

        _srcTileBtn.Toggled = src == ObjectType.Tile;
        _srcPlatformBtn.Toggled = src == ObjectType.Platform;
        _srcRopeBtn.Toggled = src == ObjectType.Rope;
        _srcPlanterBtn.Toggled = src == ObjectType.PlanterBox;
        _srcRailBtn.Toggled = src == ObjectType.Rail;
        _srcSeedsBtn.Toggled = src == ObjectType.Seeds;
    }

    private void UpdateTargetButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        var tgt = settings.NewObject;

        _tgtTileBtn.Toggled = tgt == ObjectType.Tile;
        _tgtPlatformBtn.Toggled = tgt == ObjectType.Platform;
        _tgtRopeBtn.Toggled = tgt == ObjectType.Rope;
        _tgtPlanterBtn.Toggled = tgt == ObjectType.PlanterBox;
        _tgtRailBtn.Toggled = tgt == ObjectType.Rail;
        _tgtSeedsBtn.Toggled = tgt == ObjectType.Seeds;
        _tgtAirBtn.Toggled = tgt == ObjectType.Air;
    }

    private void SyncFromSettings()
    {
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateSourceButtons();
        UpdateTargetButtons();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SyncFromSettings();

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            IsVisible = false;
    }

    public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        base.Draw(spriteBatch);
    }
}