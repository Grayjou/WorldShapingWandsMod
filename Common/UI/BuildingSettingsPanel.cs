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

public class BuildingSettingsPanel : UIState
{
    public bool IsVisible { get; set; }

    private UIDraggablePanel _mainPanel;

    // Object type buttons (icon-based)
    private UIIconButton _solidBtn, _platformBtn, _ropeBtn, _railBtn, _grassSeedBtn, _planterBtn;

    // Shape buttons (icon-based)
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _halfEllipseHFilledBtn, _halfEllipseHHollowBtn;
    private UIIconButton _halfEllipseVFilledBtn, _halfEllipseVHollowBtn;
    private UIIconButton _edgeBtn;
    private UIIconButton _straightBtn;

    // Thickness
    private UIText _thicknessValue;

    // Slope buttons
    private UIIconButton _slopeDefaultBtn, _slopeHalfBtn;
    private UIIconButton _slopeBottomRightBtn, _slopeBottomLeftBtn;
    private UIIconButton _slopeTopRightBtn, _slopeTopLeftBtn;

    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    private const float PanelWidth = 320f;
    private const float PanelHeight = 460f;
    private const float Padding = 10f;
    private const float IconBtnSize = 36f;  // icon button outer size (includes visual padding)
    private const float IconGap = 6f;       // gap between icon buttons

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.Height.Set(PanelHeight, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = new Color(44, 57, 105, 220);
        _mainPanel.BorderColor = new Color(20, 20, 60);
        Append(_mainPanel);

        float y = 8f;
        float col1 = Padding;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        // Title
        var title = new UISectionTitle(L("Building.Title"));
        title.Width.Set(0f, 1f);
        title.Height.Set(28f, 0f);
        title.Top.Set(y, 0f);
        _mainPanel.Append(title);
        y += 36f;

        // === OBJECT TYPE SECTION ===
        var objectSection = new UISectionTitle(L("Building.ObjectType"));
        objectSection.Width.Set(0f, 1f);
        objectSection.Height.Set(22f, 0f);
        objectSection.Top.Set(y, 0f);
        _mainPanel.Append(objectSection);
        y += 28f;

        // Load object type icon textures
        var texSolid     = mod.Assets.Request<Texture2D>("Assets/Icons/ObjSolid", AssetRequestMode.ImmediateLoad);
        var texPlatform  = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlatform", AssetRequestMode.ImmediateLoad);
        var texRope      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjRope", AssetRequestMode.ImmediateLoad);
        var texRail      = mod.Assets.Request<Texture2D>("Assets/Icons/ObjRail", AssetRequestMode.ImmediateLoad);
        var texGrassSeed = mod.Assets.Request<Texture2D>("Assets/Icons/ObjGrassSeed", AssetRequestMode.ImmediateLoad);
        var texPlanter   = mod.Assets.Request<Texture2D>("Assets/Icons/ObjPlanter", AssetRequestMode.ImmediateLoad);

        // Row of 6 object type icons, centered
        float totalObjWidth = IconBtnSize * 6 + IconGap * 5;
        float objStartX = (PanelWidth - totalObjWidth) / 2f - Padding;

        _solidBtn     = MakeIconBtn(texSolid,     L("Building.SolidBlock"),  objStartX + (IconBtnSize + IconGap) * 0, y);
        _platformBtn  = MakeIconBtn(texPlatform,   L("Building.Platform"),   objStartX + (IconBtnSize + IconGap) * 1, y);
        _ropeBtn      = MakeIconBtn(texRope,       L("Building.Rope"),       objStartX + (IconBtnSize + IconGap) * 2, y);
        _railBtn      = MakeIconBtn(texRail,       L("Building.Rail"),       objStartX + (IconBtnSize + IconGap) * 3, y);
        _grassSeedBtn = MakeIconBtn(texGrassSeed,  L("Building.GrassSeed"),  objStartX + (IconBtnSize + IconGap) * 4, y);
        _planterBtn   = MakeIconBtn(texPlanter,    L("Building.PlanterBox"), objStartX + (IconBtnSize + IconGap) * 5, y);
        _mainPanel.Append(_solidBtn);
        _mainPanel.Append(_platformBtn);
        _mainPanel.Append(_ropeBtn);
        _mainPanel.Append(_railBtn);
        _mainPanel.Append(_grassSeedBtn);
        _mainPanel.Append(_planterBtn);
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
        var texEdge           = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeEdge", AssetRequestMode.ImmediateLoad);
        var texStraight       = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeStraight", AssetRequestMode.ImmediateLoad);
        var texHalfEHFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseHFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEHHollow   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseHHollow", AssetRequestMode.ImmediateLoad);
        var texHalfEVFilled   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseVFilled", AssetRequestMode.ImmediateLoad);
        var texHalfEVHollow   = mod.Assets.Request<Texture2D>("Assets/Icons/ShapeHalfEllipseVHollow", AssetRequestMode.ImmediateLoad);

        // Row 1: 5 shape icons
        float totalShapeWidth = IconBtnSize * 5 + IconGap * 4;
        float shapeStartX = (PanelWidth - totalShapeWidth) / 2f - Padding;

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

        // Row 2: 5 shape icons
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

        // Row 3: 4 half-ellipse shape icons
        _halfEllipseHFilledBtn = MakeIconBtn(texHalfEHFilled, L("Common.ShapeHalfEllipseHFilled"), shapeStartX + (IconBtnSize + IconGap) * 0, y);
        _halfEllipseHHollowBtn = MakeIconBtn(texHalfEHHollow, L("Common.ShapeHalfEllipseHHollow"), shapeStartX + (IconBtnSize + IconGap) * 1, y);
        _halfEllipseVFilledBtn = MakeIconBtn(texHalfEVFilled, L("Common.ShapeHalfEllipseVFilled"), shapeStartX + (IconBtnSize + IconGap) * 2, y);
        _halfEllipseVHollowBtn = MakeIconBtn(texHalfEVHollow, L("Common.ShapeHalfEllipseVHollow"), shapeStartX + (IconBtnSize + IconGap) * 3, y);
        _mainPanel.Append(_halfEllipseHFilledBtn);
        _mainPanel.Append(_halfEllipseHHollowBtn);
        _mainPanel.Append(_halfEllipseVFilledBtn);
        _mainPanel.Append(_halfEllipseVHollowBtn);
        y += IconBtnSize + 12f;

        // === THICKNESS SECTION ===
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

        // === SLOPE SECTION ===
        var slopeSection = new UISectionTitle(L("Building.Slope"));
        slopeSection.Width.Set(0f, 1f);
        slopeSection.Height.Set(22f, 0f);
        slopeSection.Top.Set(y, 0f);
        _mainPanel.Append(slopeSection);
        y += 28f;

        // Load slope icon textures
        var texDefault     = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeDefault", AssetRequestMode.ImmediateLoad);
        var texHalf        = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeHalf", AssetRequestMode.ImmediateLoad);
        var texBottomRight = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeBottomRight", AssetRequestMode.ImmediateLoad);
        var texBottomLeft  = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeBottomLeft", AssetRequestMode.ImmediateLoad);
        var texTopRight    = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeTopRight", AssetRequestMode.ImmediateLoad);
        var texTopLeft     = mod.Assets.Request<Texture2D>("Assets/Icons/SlopeTopLeft", AssetRequestMode.ImmediateLoad);

        // Row of 6 icon buttons, centered
        float totalSlopeWidth = IconBtnSize * 6 + IconGap * 5;
        float slopeStartX = (PanelWidth - totalSlopeWidth) / 2f - Padding; // account for panel inner padding

        _slopeDefaultBtn     = MakeIconBtn(texDefault,     L("Building.SlopeDefault"),     slopeStartX + (IconBtnSize + IconGap) * 0, y);
        _slopeHalfBtn        = MakeIconBtn(texHalf,        L("Building.SlopeHalf"),        slopeStartX + (IconBtnSize + IconGap) * 1, y);
        _slopeBottomRightBtn = MakeIconBtn(texBottomRight, L("Building.SlopeBottomRight"), slopeStartX + (IconBtnSize + IconGap) * 2, y);
        _slopeBottomLeftBtn  = MakeIconBtn(texBottomLeft,  L("Building.SlopeBottomLeft"),  slopeStartX + (IconBtnSize + IconGap) * 3, y);
        _slopeTopRightBtn    = MakeIconBtn(texTopRight,    L("Building.SlopeTopRight"),    slopeStartX + (IconBtnSize + IconGap) * 4, y);
        _slopeTopLeftBtn     = MakeIconBtn(texTopLeft,     L("Building.SlopeTopLeft"),     slopeStartX + (IconBtnSize + IconGap) * 5, y);

        _mainPanel.Append(_slopeDefaultBtn);
        _mainPanel.Append(_slopeHalfBtn);
        _mainPanel.Append(_slopeBottomRightBtn);
        _mainPanel.Append(_slopeBottomLeftBtn);
        _mainPanel.Append(_slopeTopRightBtn);
        _mainPanel.Append(_slopeTopLeftBtn);
        y += IconBtnSize + 12f;

        // Close button
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(y, 0f);
        closeBtn.OnLeftClick += (_, _) => IsVisible = false;
        _mainPanel.Append(closeBtn);

        // Wire up events
        _solidBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Solid);
        _platformBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Platform);
        _ropeBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Rope);
        _railBtn.OnToggled += (_, _) => SetObjectType(PlaceType.Rail);
        _grassSeedBtn.OnToggled += (_, _) => SetObjectType(PlaceType.GrassSeed);
        _planterBtn.OnToggled += (_, _) => SetObjectType(PlaceType.PlantPot);

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

        _slopeDefaultBtn.OnToggled += (_, _) => SetSlope(SlopeType.Default);
        _slopeHalfBtn.OnToggled += (_, _) => SetSlope(SlopeType.VerticalHalf);
        _slopeBottomRightBtn.OnToggled += (_, _) => SetSlope(SlopeType.BottomRight);
        _slopeBottomLeftBtn.OnToggled += (_, _) => SetSlope(SlopeType.BottomLeft);
        _slopeTopRightBtn.OnToggled += (_, _) => SetSlope(SlopeType.TopRight);
        _slopeTopLeftBtn.OnToggled += (_, _) => SetSlope(SlopeType.TopLeft);
    }

    private WandOfBuildingSettings GetSettings() =>
        Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.BuildingSettings;

    private void SetObjectType(PlaceType type)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Object = type;
        UpdateObjectButtons();
    }

    private void SetShape(ShapeType type, ShapeMode mode)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness);
        UpdateShapeButtons();
    }

    private void SetSlope(SlopeType slope)
    {
        var settings = GetSettings();
        if (settings == null) return;
        settings.Slope = slope;
        UpdateSlopeButtons();
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

    private void UpdateObjectButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _solidBtn.Toggled = settings.Object == PlaceType.Solid;
        _platformBtn.Toggled = settings.Object == PlaceType.Platform;
        _ropeBtn.Toggled = settings.Object == PlaceType.Rope;
        _railBtn.Toggled = settings.Object == PlaceType.Rail;
        _grassSeedBtn.Toggled = settings.Object == PlaceType.GrassSeed;
        _planterBtn.Toggled = settings.Object == PlaceType.PlantPot;
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

    private void UpdateSlopeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;

        _slopeDefaultBtn.Toggled     = settings.Slope == SlopeType.Default;
        _slopeHalfBtn.Toggled        = settings.Slope == SlopeType.VerticalHalf;
        _slopeBottomRightBtn.Toggled = settings.Slope == SlopeType.BottomRight;
        _slopeBottomLeftBtn.Toggled  = settings.Slope == SlopeType.BottomLeft;
        _slopeTopRightBtn.Toggled    = settings.Slope == SlopeType.TopRight;
        _slopeTopLeftBtn.Toggled     = settings.Slope == SlopeType.TopLeft;
    }

    private void SyncFromSettings()
    {
        UpdateObjectButtons();
        UpdateShapeButtons();
        UpdateThicknessDisplay();
        UpdateSlopeButtons();
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