using System;
using System.Collections.Generic;
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
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI;

/// <summary>
/// Fluent section-based builder for wand settings panels.
/// Eliminates magic numbers by tracking Y offset automatically and encapsulating
/// common section layouts (shape grid, slice, thickness, options row, etc.).
/// <para>
/// Usage:
/// <code>
/// var builder = new WandPanelBuilder(mainPanel, PanelWidth, Padding);
/// builder.AddTitle("Building.Title")
///        .AddSectionHeader("Building.ObjectType")
///        .AddIconGrid(objectIcons, 4)
///        .AddShapeGrid(shapeIcons, includeHollow: true)
///        .AddSliceSection(out _sliceGrid, onSliceChanged)
///        .AddThicknessSection(out _thicknessValue, onAdjust)
///        .AddShapeOptionsSection(optionIcons)
///        .AddCloseButton()
///        .FinalizeHeight();
/// </code>
/// </para>
/// </summary>
public class WandPanelBuilder
{
    private readonly UIDraggablePanel _panel;
    private readonly float _panelWidth;
    private readonly float _padding;
    private bool _panelChromeAdded;

    /// <summary>Current Y offset � automatically advanced by each Add method.</summary>
    public float CurrentY { get; private set; }

    /// <summary>Enable to draw horizontal debug lines at each section boundary.</summary>
    public bool DebugSectionLines { get; set; }

    private readonly List<float> _sectionBoundaries = new();

    private static float ComputeRowWidth(int elementCount, float elementWidth, float elementGap)
    {
        if (elementCount <= 0)
            return 0f;

        float rowWidth = 0f;
        for (int i = 0; i < elementCount; i++)
            rowWidth = LayoutSpacing.AddHorizontalSpace(rowWidth, elementWidth, i == 0 ? 0f : elementGap);
        return rowWidth;
    }

    private float ComputeCenteredRowStartX(float rowWidth)
        => (_panelWidth - rowWidth) * 0.5f - _padding;

    // -------------------------------------------------------------------
    // Layout constants � single source of truth for all panels
    // -------------------------------------------------------------------

    /// <summary>Icon button outer size (includes visual padding).</summary>
    public const float IconBtnSize = 36f;

    /// <summary>Gap between icon buttons in a grid.</summary>
    public const float IconGap = 6f;

    /// <summary>
    /// (S12 2026-04-29; per GrayJou S12 prompt: <i>"Make an Icon button
    /// style that is 16x16 to apply to the Stencil icons in the SubPanel"</i>)
    /// Compact 22x22 icon button used by the stencil-slot row so that the
    /// new 16x16 stencil icons fit snugly with a 3px frame on each side
    /// instead of swimming inside a 36x36 cell. Apply via
    /// <see cref="AddSmallIconGrid"/>.
    /// </summary>
    public const float SmallIconBtnSize = 22f;

    /// <summary>(S12) Tighter inter-icon gap that matches the smaller cell size.</summary>
    public const float SmallIconGap = 4f;

    /// <summary>Standard toggle button width (text-based toggles).</summary>
    public const float ButtonWidth = 140f;

    /// <summary>Standard toggle button height.</summary>
    public const float ButtonHeight = 28f;

    /// <summary>Title section height.</summary>
    public const float TitleHeight = 28f;

    /// <summary>Space after title (includes the title height and gap below).</summary>
    public const float TitleSpacing = 36f;

    /// <summary>Sub-section header height (UISectionTitle).</summary>
    public const float SectionHeaderHeight = 22f;

    /// <summary>Space after a section header.</summary>
    public const float SectionHeaderSpacing = 28f;

    /// <summary>Space after the last row of an icon grid before the next section.</summary>
    public const float AfterIconGridSpacing = 12f;

    /// <summary>Space after a thickness section.</summary>
    public const float AfterThicknessSpacing = 42f;

    /// <summary>Space after a toggle row (text-based 2-column toggles).</summary>
    public const float AfterToggleRowSpacing = 34f;

    /// <summary>Space after the last toggle row before the next section.</summary>
    public const float AfterToggleGroupSpacing = 42f;

    /// <summary>Space after the options icon row.</summary>
    public const float AfterOptionsSpacing = 12f;

    /// <summary>Initial Y offset at the top of the panel.</summary>
    public const float InitialY = 8f;   

    public const float AfterCloseButtonSpacing = 50f;
    public const float ChromeIconSize = 22f;
    public const float ChromeIconGap = 4f;
    public const float ChromeBottomMargin = 8f;

    // Helper for localization
    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    // -------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------

    public WandPanelBuilder(UIDraggablePanel panel, float panelWidth, float padding)
    {
        _panel = panel;
        _panelWidth = panelWidth;
        _padding = padding;
        CurrentY = InitialY;

        // (S6 §1) All wand panels get HandleOrAnywhere policy.
        // Handle itself is appended in FinalizeHeight so it draws
        // on top of all content and receives mouse events.
        _panel.DragPolicy = DragPolicy.HandleOrAnywhere;
    }

    // -------------------------------------------------------------------
    // Section: Title
    // -------------------------------------------------------------------

    /// <summary>Adds the panel title (larger UISectionTitle).</summary>
    public WandPanelBuilder AddTitle(string locKey)
    {
        MarkSection();
        var title = new UISectionTitle(L(locKey));
        title.Width.Set(0f, 1f);
        title.Height.Set(TitleHeight, 0f);
        title.Top.Set(CurrentY, 0f);
        _panel.Append(title);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: TitleSpacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Sub-header
    // -------------------------------------------------------------------

    /// <summary>Adds a section header (smaller UISectionTitle with underline).</summary>
    public WandPanelBuilder AddSectionHeader(string locKey)
    {
        MarkSection();
        var header = new UISectionTitle(L(locKey));
        header.Width.Set(0f, 1f);
        header.Height.Set(SectionHeaderHeight, 0f);
        header.Top.Set(CurrentY, 0f);
        _panel.Append(header);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: SectionHeaderSpacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Icon Grid (radio buttons � for shapes, object types, etc.)
    // -------------------------------------------------------------------

    /// <summary>
    /// Descriptor for an icon button to be placed in a grid.
    /// </summary>
    public struct IconDef
    {
        public Asset<Texture2D> Texture;
        public string HoverText;
        /// <summary>When true, creates a non-radio (independent toggle) button.</summary>
        public bool IsToggle;
        /// <summary>Initial state for toggle buttons.</summary>
        public bool InitialState;

        public IconDef(Asset<Texture2D> texture, string locKey, bool isToggle = false, bool initialState = false)
        {
            Texture = texture;
            HoverText = L(locKey);
            IsToggle = isToggle;
            InitialState = initialState;
        }

        /// <summary>Creates an IconDef with a pre-resolved hover text (not a loc key).</summary>
        public static IconDef WithText(Asset<Texture2D> texture, string hoverText, bool isToggle = false, bool initialState = false)
            => new() { Texture = texture, HoverText = hoverText, IsToggle = isToggle, InitialState = initialState };
    }

    /// <summary>
    /// Adds a grid of icon buttons arranged in rows of <paramref name="iconsPerRow"/>.
    /// Returns the created buttons in order for event wiring.
    /// </summary>
    public WandPanelBuilder AddIconGrid(IconDef[] icons, int iconsPerRow, out UIIconButton[] buttons)
    {
        MarkSection();
        buttons = new UIIconButton[icons.Length];
        int totalIcons = icons.Length;
        int rowCount = (totalIcons + iconsPerRow - 1) / iconsPerRow;

        for (int row = 0; row < rowCount; row++)
        {
            int startIdx = row * iconsPerRow;
            int iconsInThisRow = Math.Min(iconsPerRow, totalIcons - startIdx);

            float totalWidth = ComputeRowWidth(iconsInThisRow, IconBtnSize, IconGap);
            float startX = ComputeCenteredRowStartX(totalWidth);

            for (int col = 0; col < iconsInThisRow; col++)
            {
                int idx = startIdx + col;
                var def = icons[idx];
                UIIconButton btn;

                if (def.IsToggle)
                    btn = MakeToggleIconBtn(def.Texture, def.HoverText, startX + (IconBtnSize + IconGap) * col, CurrentY, def.InitialState);
                else
                    btn = MakeIconBtn(def.Texture, def.HoverText, startX + (IconBtnSize + IconGap) * col, CurrentY);

                _panel.Append(btn);
                buttons[idx] = btn;
            }

            // Row spacing: icon gap between rows, extra spacing after last row
            bool isLastRow = (row == rowCount - 1);
            float rowBottomPadding = isLastRow ? AfterIconGridSpacing : IconGap;
            CurrentY = LayoutSpacing.AddVerticalSpace(
                currentSize: CurrentY,
                elementSize: IconBtnSize,
                bottomPadding: rowBottomPadding);
        }

        return this;
    }

    /// <summary>
    /// (S12 2026-04-29; "16x16 icon button style" per GrayJou S12 prompt)
    /// Compact variant of <see cref="AddIconGrid"/> using
    /// <see cref="SmallIconBtnSize"/> + <see cref="SmallIconGap"/>. Built
    /// for the stencil-slot row whose new 16x16 art clipped out of the
    /// SubPanel inside the standard 36x36 cells. Same row-centering and
    /// section-tracking semantics as <see cref="AddIconGrid"/>.
    /// </summary>
    public WandPanelBuilder AddSmallIconGrid(IconDef[] icons, int iconsPerRow, out UIIconButton[] buttons)
    {
        MarkSection();
        buttons = new UIIconButton[icons.Length];
        int totalIcons = icons.Length;
        int rowCount = (totalIcons + iconsPerRow - 1) / iconsPerRow;

        for (int row = 0; row < rowCount; row++)
        {
            int startIdx = row * iconsPerRow;
            int iconsInThisRow = Math.Min(iconsPerRow, totalIcons - startIdx);

            float totalWidth = ComputeRowWidth(iconsInThisRow, SmallIconBtnSize, SmallIconGap);
            float startX = ComputeCenteredRowStartX(totalWidth);

            for (int col = 0; col < iconsInThisRow; col++)
            {
                int idx = startIdx + col;
                var def = icons[idx];
                var btn = new UIIconButton(def.Texture, def.HoverText, def.InitialState)
                {
                    IsRadio = !def.IsToggle,
                };
                btn.Width.Set(SmallIconBtnSize, 0f);
                btn.Height.Set(SmallIconBtnSize, 0f);
                btn.Left.Set(startX + (SmallIconBtnSize + SmallIconGap) * col, 0f);
                btn.Top.Set(CurrentY, 0f);
                _panel.Append(btn);
                buttons[idx] = btn;
            }

            bool isLastRow = (row == rowCount - 1);
            float rowBottomPadding = isLastRow ? AfterIconGridSpacing : SmallIconGap;
            CurrentY = LayoutSpacing.AddVerticalSpace(
                currentSize: CurrentY,
                elementSize: SmallIconBtnSize,
                bottomPadding: rowBottomPadding);
        }

        return this;
    }

    // -------------------------------------------------------------------
    // Section: Full Shape Grid (the 11-shape standard grid)
    // -------------------------------------------------------------------

    /// <summary>
    /// Shape definition for the standard 11-shape grid.
    /// </summary>
    public struct ShapeGridResult
    {
        public UIIconButton RectFilled, RectHollow;
        public UIIconButton EllipseFilled, EllipseHollow;
        public UIIconButton DiamondFilled, DiamondHollow;
        public UIIconButton TriangleFilled, TriangleHollow;
        public UIIconButton Elbow, Cardinal, StraightLine;
        public UIIconButton Mold;
        public UIIconButton MagicWandRead;
    }

    /// <summary>
    /// Adds the standard 11-shape icon grid (5+5+1 layout) with section header.
    /// Used by Building, Replacement, Coating, Safekeeping panels.
    /// </summary>
    public WandPanelBuilder AddFullShapeSection(out ShapeGridResult shapes)
    {
        AddSectionHeader("Common.Shape");

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var mwReadHover = Language.GetTextValue("Mods.WorldShapingWandsMod.Shape.MagicWandRead.Label");
        var icons = new IconDef[]
        {
            // Row 1
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeRectFilled",     AssetRequestMode.ImmediateLoad), "Common.ShapeRectFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeRectHollow",     AssetRequestMode.ImmediateLoad), "Common.ShapeRectHollow"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeEllipseFilled",  AssetRequestMode.ImmediateLoad), "Common.ShapeEllipseFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeEllipseHollow",  AssetRequestMode.ImmediateLoad), "Common.ShapeEllipseHollow"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeElbow",          AssetRequestMode.ImmediateLoad), "Common.ShapeElbow"),
            // Row 2
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeDiamondFilled",  AssetRequestMode.ImmediateLoad), "Common.ShapeDiamondFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeDiamondHollow",  AssetRequestMode.ImmediateLoad), "Common.ShapeDiamondHollow"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad), "Common.ShapeTriangleFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeTriangleHollow", AssetRequestMode.ImmediateLoad), "Common.ShapeTriangleHollow"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeCardinal",       AssetRequestMode.ImmediateLoad), "Common.ShapeCardinal"),
            // Row 3
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeStraightLine",   AssetRequestMode.ImmediateLoad), "Common.ShapeStraightLine"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeMold",           AssetRequestMode.ImmediateLoad), "Common.ShapeMold"),
            IconDef.WithText(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/MagicWandRead", AssetRequestMode.ImmediateLoad), mwReadHover),
        };

        AddIconGrid(icons, 5, out var btns);

        shapes = new ShapeGridResult
        {
            RectFilled    = btns[0],  RectHollow    = btns[1],
            EllipseFilled = btns[2],  EllipseHollow = btns[3],
            Elbow         = btns[4],
            DiamondFilled = btns[5],  DiamondHollow = btns[6],
            TriangleFilled = btns[7], TriangleHollow = btns[8],
            Cardinal      = btns[9],
            StraightLine  = btns[10],
            Mold          = btns[11],
            MagicWandRead = btns[12],
        };

        return this;
    }

    // -------------------------------------------------------------------
    // Section: Stencil-augmented Shape Grid (stencil wands only)
    // -------------------------------------------------------------------

    /// <summary>
    /// Result of <see cref="AddStencilShapeSection"/> — the standard 12-cell
    /// shape grid plus a 5-cell stencil-slot row appended below.
    /// </summary>
    public struct StencilShapeGridResult
    {
        public ShapeGridResult Shapes;
        /// <summary>The 5 stencil-slot buttons (index 0..4 = slots 1..5).</summary>
        public UIIconButton[] StencilSlots;
    }

    /// <summary>
    /// Adds the standard shape grid AND a 5-cell stencil-slot row underneath,
    /// per <c>MultipleStencilsPlan.md</c> §0.1. Used by stencil wands (Molding,
    /// Delimitation when wired). The stencil row uses
    /// <c>Assets_Build/Icons/Shapes/Stencil/StencilChoice{1..5}.png</c>.
    /// </summary>
    public WandPanelBuilder AddStencilShapeSection(out StencilShapeGridResult result)
    {
        // 1. Standard shape grid first (re-uses all existing layout logic).
        AddFullShapeSection(out var shapes);

        // 2. Stencil-slot row — (S12 2026-04-29) uses AddSmallIconGrid so
        // the new 16x16 StencilChoice art (resized via
        // Scripts/AssetGen/scale_down_stencil_icons.py) sits in compact
        // 22x22 cells instead of clipping out of a 36x36 chrome.
        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var stencilIcons = new IconDef[]
        {
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/Stencil/StencilChoice1", AssetRequestMode.ImmediateLoad), "Stencil.Slot1"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/Stencil/StencilChoice2", AssetRequestMode.ImmediateLoad), "Stencil.Slot2"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/Stencil/StencilChoice3", AssetRequestMode.ImmediateLoad), "Stencil.Slot3"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/Stencil/StencilChoice4", AssetRequestMode.ImmediateLoad), "Stencil.Slot4"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/Stencil/StencilChoice5", AssetRequestMode.ImmediateLoad), "Stencil.Slot5"),
        };
        AddSmallIconGrid(stencilIcons, 5, out var stencilBtns);

        result = new StencilShapeGridResult
        {
            Shapes = shapes,
            StencilSlots = stencilBtns,
        };
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Wiring Shape Grid (reduced)
    // -------------------------------------------------------------------

    /// <summary>
    /// Shape definition for the reduced Wiring shape grid (no hollow variants, no ellipse).
    /// </summary>
    public struct WiringShapeGridResult
    {
        public UIIconButton RectFilled;
        public UIIconButton DiamondFilled, TriangleFilled;
        public UIIconButton Elbow, Cardinal, StraightLine;
        public UIIconButton MagicWandRead;
    }

    /// <summary>
    /// Adds the reduced 6-shape icon grid for Wiring (5+1 layout) with section header.
    /// Wiring only supports filled shapes � no hollow, no ellipse.
    /// </summary>
    public WandPanelBuilder AddWiringShapeSection(out WiringShapeGridResult shapes)
    {
        AddSectionHeader("Common.Shape");

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var mwReadHover = Language.GetTextValue("Mods.WorldShapingWandsMod.Shape.MagicWandRead.Label");
        var icons = new IconDef[]
        {
            // Row 1: 5 icons
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeRectFilled",     AssetRequestMode.ImmediateLoad), "Common.ShapeRectFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeElbow",          AssetRequestMode.ImmediateLoad), "Common.ShapeElbow"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeCardinal",       AssetRequestMode.ImmediateLoad), "Common.ShapeCardinal"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeDiamondFilled",  AssetRequestMode.ImmediateLoad), "Common.ShapeDiamondFilled"),
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeTriangleFilled", AssetRequestMode.ImmediateLoad), "Common.ShapeTriangleFilled"),
            // Row 2: 1 icon
            new(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/ShapeStraightLine",   AssetRequestMode.ImmediateLoad), "Common.ShapeStraightLine"),
            IconDef.WithText(mod.Assets.Request<Texture2D>("Assets_Build/Icons/Shapes/MagicWandRead", AssetRequestMode.ImmediateLoad), mwReadHover),
        };

        AddIconGrid(icons, 5, out var btns);

        shapes = new WiringShapeGridResult
        {
            RectFilled    = btns[0],
            Elbow         = btns[1],
            Cardinal      = btns[2],
            DiamondFilled = btns[3],
            TriangleFilled = btns[4],
            StraightLine  = btns[5],
            MagicWandRead = btns[6],
        };

        return this;
    }

    // -------------------------------------------------------------------
    // Section: Slice Grid
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds a Slice section with header and UISliceGrid.
    /// </summary>
    public WandPanelBuilder AddSliceSection(out UISliceGrid sliceGrid, Action<SliceMode> onChanged)
    {
        AddSectionHeader("Common.Slice");

        sliceGrid = new UISliceGrid();
        sliceGrid.HAlign = 0.5f;
        sliceGrid.Top.Set(CurrentY, 0f);
        sliceGrid.OnChanged += onChanged;
        _panel.Append(sliceGrid);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: sliceGrid.Height.Pixels,
            bottomPadding: AfterIconGridSpacing);

        return this;
    }

    // -------------------------------------------------------------------
    // Section: Thickness (+/- stepper)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds an Outline Thickness section with label, minus button, value display, and plus button.
    /// </summary>
    /// <remarks>
    /// (S2 2026-04-30 — Notes_WandPanelScrollNotConsumedBug.md #ROI; Fix B)
    /// Each <c>OnScrollWheel</c> handler now actively CONSUMES the scroll delta by zeroing
    /// <c>Terraria.GameInput.PlayerInput.ScrollWheelDelta</c> after applying the thickness
    /// adjustment. Without this, the vanilla hotbar selection logic in <c>Player.cs</c>
    /// (decompiled @ line 31225: <c>num += PlayerInput.ScrollWheelDelta / -120;</c>) reads
    /// the same delta later in the same frame and leaks scroll input into the held-item
    /// selector. Setting <c>Main.LocalPlayer.mouseInterface = true</c> in each panel's
    /// <c>Update()</c> ("Fix A", already in tree across all 10 settings panels) does NOT
    /// gate this code path — the hotbar reads <c>ScrollWheelDelta</c> directly, not via
    /// <c>mouseInterface</c>. Direct consumption is the only reliable fix.
    /// </remarks>
    public WandPanelBuilder AddThicknessSection(out UIText thicknessValue, Action<int> onAdjust)
    {
        MarkSection();

        float col1 = _padding;

        // Local helper: apply adjustment, then consume the vanilla scroll delta so the
        // hotbar selector doesn't also act on this scroll event.
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

        var label = new UIText(L("Common.OutlineThickness"), 0.85f);
        label.Left.Set(col1, 0f);
        label.Top.Set(CurrentY, 0f);
        _panel.Append(label);

        var minusBtn = new UITextPanel<string>("-", 0.8f, false);
        minusBtn.Width.Set(30f, 0f);
        minusBtn.Height.Set(26f, 0f);
        minusBtn.Left.Set(col1 + 130f, 0f);
        minusBtn.Top.Set(CurrentY - 2f, 0f);
        minusBtn.OnLeftClick += (_, _) => onAdjust(-1);
        minusBtn.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _panel.Append(minusBtn);

        thicknessValue = new UIText("1", 0.9f);
        thicknessValue.Left.Set(col1 + 170f, 0f);
        thicknessValue.Top.Set(CurrentY, 0f);
        thicknessValue.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _panel.Append(thicknessValue);

        var plusBtn = new UITextPanel<string>("+", 0.8f, false);
        plusBtn.Width.Set(30f, 0f);
        plusBtn.Height.Set(26f, 0f);
        plusBtn.Left.Set(col1 + 200f, 0f);
        plusBtn.Top.Set(CurrentY - 2f, 0f);
        plusBtn.OnLeftClick += (_, _) => onAdjust(1);
        plusBtn.OnScrollWheel += (evt, _) => ScrollAdjust(evt);
        _panel.Append(plusBtn);

        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: AfterThicknessSpacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Options Icon Row (toggle icon buttons)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds an Options section with "Options" header and a centered row of toggle icon buttons.
    /// Convenience wrapper around <see cref="AddIconToggleRow"/>.
    /// </summary>
    public WandPanelBuilder AddShapeOptionsSection(IconDef[] optionIcons, out UIIconButton[] buttons)
    {
        return AddIconToggleRow("Common.ShapeOptions", optionIcons, out buttons);
    }

    /// <summary>
    /// Adds a section with a custom header and a centered row of toggle icon buttons.
    /// Generic version of <see cref="AddShapeOptionsSection"/> for any section.
    /// </summary>
    public WandPanelBuilder AddIconToggleRow(string headerLocKey, IconDef[] icons, out UIIconButton[] buttons)
    {
        AddSectionHeader(headerLocKey);

        int count = icons.Length;
        buttons = new UIIconButton[count];

        float totalWidth = ComputeRowWidth(count, IconBtnSize, IconGap);
        float startX = ComputeCenteredRowStartX(totalWidth);

        for (int i = 0; i < count; i++)
        {
            var def = icons[i];
            var btn = MakeToggleIconBtn(def.Texture, def.HoverText, startX + (IconBtnSize + IconGap) * i, CurrentY, def.InitialState);
            _panel.Append(btn);
            buttons[i] = btn;
        }

        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: IconBtnSize,
            bottomPadding: AfterOptionsSpacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Tri-State Row (UITriStateButton pair)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds a row of two UITriStateButton controls side by side.
    /// </summary>
    public WandPanelBuilder AddTriStateRow(
        string leftBaseName, out UITriStateButton leftBtn,
        string rightBaseName, out UITriStateButton rightBtn,
        float spacing = -1f)
    {
        MarkSection();
        float col1 = _padding;
        float col2 = _panelWidth - _padding - ButtonWidth - 12f;

        leftBtn = MakeTriState(leftBaseName, col1, CurrentY);
        rightBtn = MakeTriState(rightBaseName, col2, CurrentY);
        _panel.Append(leftBtn);
        _panel.Append(rightBtn);

        float bottomPadding = spacing > 0 ? spacing : AfterToggleGroupSpacing;
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: bottomPadding);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Toggle Row (text-based 2-column toggles)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds a row of two text-based toggle buttons side by side.
    /// </summary>
    public WandPanelBuilder AddToggleRow(
        string leftLocKey, out UIToggleButton leftBtn, Color? leftTint,
        string rightLocKey, out UIToggleButton rightBtn, Color? rightTint,
        float spacing = -1f)
    {
        MarkSection();
        float col1 = _padding;
        float col2 = _panelWidth - _padding - ButtonWidth - 12f;

        leftBtn = MakeToggle(L(leftLocKey), col1, CurrentY, leftTint);
        rightBtn = MakeToggle(L(rightLocKey), col2, CurrentY, rightTint);
        _panel.Append(leftBtn);
        _panel.Append(rightBtn);

        float bottomPadding = spacing > 0 ? spacing : AfterToggleRowSpacing;
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: bottomPadding);
        return this;
    }

    /// <summary>
    /// Adds a single text-based toggle button (left-aligned).
    /// </summary>
    public WandPanelBuilder AddToggleSingle(string locKey, out UIToggleButton btn, Color? tint = null, float spacing = -1f)
    {
        MarkSection();
        float col1 = _padding;
        btn = MakeToggle(L(locKey), col1, CurrentY, tint);
        _panel.Append(btn);
        float bottomPadding = spacing > 0 ? spacing : AfterToggleGroupSpacing;
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: bottomPadding);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Wide Toggle (centered, like OverwriteSlope)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds a centered UIToggleButton (used for OverwriteSlope, etc.).
    /// </summary>
    public WandPanelBuilder AddCenteredToggle(string locKey, bool initialState, out UIToggleButton btn, float spacing = 38f)
    {
        MarkSection();
        btn = new UIToggleButton(L(locKey), initialState);
        btn.Width.Set(200f, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.HAlign = 0.5f;
        btn.Top.Set(CurrentY, 0f);
        _panel.Append(btn);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: spacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Close Button
    // -------------------------------------------------------------------

    /// <summary>Adds a centered Close button.</summary>
    public WandPanelBuilder AddCloseButton()
    {
        EnsurePanelChromeButtons();

        MarkSection();
        var closeBtn = new UITextPanel<string>(L("Common.Close"), 0.9f, false);
        closeBtn.Width.Set(80f, 0f);
        closeBtn.Height.Set(30f, 0f);
        closeBtn.HAlign = 0.5f;
        closeBtn.Top.Set(CurrentY, 0f);
        // (S5 2026-04-25 — GrayJou Letter #4 §2: IV survives X-button.) Switched
        // from CloseAllUI() to CloseAllPanels() so the X-button matches right-click
        // toggle semantics: settings panel closes, InventoryView intent is
        // preserved. The IV is an orthogonal companion — only its own toggle
        // (button on Building/Replacement panel, or the keybind) flips its intent.
        closeBtn.OnLeftClick += (_, _) => ModContent.GetInstance<WandUISystem>().CloseAllPanels();
        _panel.Append(closeBtn);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: AfterCloseButtonSpacing);
        return this;
    }

    /// <summary>
    /// Adds bottom-right WandPanel chrome action buttons: (?) Help and (i) Info.
    /// Help button dispatches verbosity-aware tooltip text to chat.
    /// Info button is intentionally inert placeholder (pending G-43/G-44 payload design).
    /// </summary>
    private void EnsurePanelChromeButtons()
    {
        if (_panelChromeAdded)
            return;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        var texHelp = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Chromes/QuestionMark", AssetRequestMode.ImmediateLoad);
        var texInfo = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Chromes/Info", AssetRequestMode.ImmediateLoad);

        var helpBtn = new UIIconButton(texHelp, L("Chrome.Help"))
        {
            IsRadio = false,
            IsAction = true,
        };
        helpBtn.Width.Set(ChromeIconSize, 0f);
        helpBtn.Height.Set(ChromeIconSize, 0f);
        helpBtn.HAlign = 1f;
        helpBtn.VAlign = 1f;
        helpBtn.Left.Set(-_padding, 0f);
        helpBtn.Top.Set(-ChromeBottomMargin, 0f);
        
        // (G-42 Session 4 2026-05-02) Wire Help button to dispatch tooltip to chat
        // using per-player verbosity preference. Shows both variants as examples.
        helpBtn.OnLeftClick += (evt, elem) =>
        {
            var player = Main.LocalPlayer;
            if (player != null)
            {
                var wandPlayer = player.GetModPlayer<Common.Players.WandPlayer>();
                if (wandPlayer != null)
                {
                    var longText = Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Chrome.Help");
                    var shortText = Common.UI.Resolvers.TooltipVerbosityResolver.ResolveTooltip(
                        "Mods.WorldShapingWandsMod.UI.Chrome.Help", player);
                    
                    bool isVerbose = wandPlayer.TooltipVerbosityEnabled;
                    var verbosityStatus = isVerbose ? "Verbose (Long)" : "Concise (Short)";
                    
                    Main.NewText("[Wand Help - Current Mode: " + verbosityStatus + "]", Color.Cyan);
                    Main.NewText(shortText, Color.White);
                    
                    // Show both variants if on long form to demonstrate options
                    if (isVerbose)
                    {
                        Main.NewText("(Long form above; click again or use verbosity toggle to see short form)", Color.Gray);
                    }
                }
            }
        };

        var infoBtn = new UIIconButton(texInfo, L("Chrome.Info"))
        {
            IsRadio = false,
            IsAction = true,
        };
        infoBtn.Width.Set(ChromeIconSize, 0f);
        infoBtn.Height.Set(ChromeIconSize, 0f);
        infoBtn.HAlign = 1f;
        infoBtn.VAlign = 1f;
        infoBtn.Left.Set(-_padding - ChromeIconSize - ChromeIconGap, 0f);
        infoBtn.Top.Set(-ChromeBottomMargin, 0f);

        _panel.Append(helpBtn);
        _panel.Append(infoBtn);
        _panelChromeAdded = true;
    }

    // -------------------------------------------------------------------
    // Section: Action Icon Row (non-toggle clickable icon buttons)
    // -------------------------------------------------------------------

    /// <summary>
    /// Adds a section with a header and a centered row of clickable (non-toggle, non-radio)
    /// icon buttons for action commands like Clear Selection, Invert, Clear Canvas, etc.
    /// Unlike <see cref="AddIconToggleRow"/>, these buttons have no toggled state �
    /// they fire a one-shot action on click.
    /// </summary>
    public WandPanelBuilder AddActionIconRow(string headerLocKey, IconDef[] icons, out UIIconButton[] buttons)
    {
        AddSectionHeader(headerLocKey);

        int count = icons.Length;
        buttons = new UIIconButton[count];

        float totalWidth = ComputeRowWidth(count, IconBtnSize, IconGap);
        float startX = ComputeCenteredRowStartX(totalWidth);

        for (int i = 0; i < count; i++)
        {
            var def = icons[i];
            var btn = MakeActionIconBtn(def.Texture, def.HoverText, startX + (IconBtnSize + IconGap) * i, CurrentY);
            _panel.Append(btn);
            buttons[i] = btn;
        }

        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: IconBtnSize,
            bottomPadding: AfterOptionsSpacing);
        return this;
    }

    /// <summary>Adds a centered row of action icon buttons (no header).</summary>
    public WandPanelBuilder AddActionIconRowNoHeader(IconDef[] icons, out UIIconButton[] buttons)
    {
        MarkSection();
        int count = icons.Length;
        buttons = new UIIconButton[count];

        float totalWidth = ComputeRowWidth(count, IconBtnSize, IconGap);
        float startX = ComputeCenteredRowStartX(totalWidth);

        for (int i = 0; i < count; i++)
        {
            var def = icons[i];
            var btn = MakeActionIconBtn(def.Texture, def.HoverText, startX + (IconBtnSize + IconGap) * i, CurrentY);
            _panel.Append(btn);
            buttons[i] = btn;
        }

        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: IconBtnSize,
            bottomPadding: AfterOptionsSpacing);
        return this;
    }

    /// <summary>Adds a custom button (centered, like Clear All).</summary>
    public WandPanelBuilder AddCenteredButton(string locKey, float width, float height, out UITextPanel<string> button, float spacing = 38f)
    {
        MarkSection();
        button = new UITextPanel<string>(L(locKey), 0.85f, false);
        button.Width.Set(width, 0f);
        button.Height.Set(height, 0f);
        button.HAlign = 0.5f;
        button.Top.Set(CurrentY, 0f);
        _panel.Append(button);
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: spacing);
        return this;
    }

    // -------------------------------------------------------------------
    // Section: Raw Y advance (escape hatch for custom layouts)
    // -------------------------------------------------------------------

    /// <summary>Advances the Y cursor by a custom amount.</summary>
    public WandPanelBuilder AdvanceY(float amount)
    {
        CurrentY = LayoutSpacing.AddVerticalSpace(
            currentSize: CurrentY,
            elementSize: 0f,
            bottomPadding: amount);
        return this;
    }

    /// <summary>Appends a UIElement at the current Y offset (escape hatch).</summary>
    public WandPanelBuilder AppendAt(UIElement element)
    {
        element.Top.Set(CurrentY, 0f);
        _panel.Append(element);
        return this;
    }

    // -------------------------------------------------------------------
    // Finalize: set panel height to fit contents
    // -------------------------------------------------------------------

    /// <summary>Sets the panel height to fit all added sections (CurrentY + bottom padding).</summary>
    public WandPanelBuilder FinalizeHeight(float bottomPadding = 8f)
    {
        _panel.Height.Set(CurrentY + bottomPadding, 0f);

        // (S6 §1) Append handle LAST so it draws on top of all content
        // (title, sections, buttons) and isn't occluded for mouse interaction.
        // Top-right corner, mirrored from IV's top-left per spec §1.5.
        var handle = new UIDragHandle(16);
        handle.HAlign = 1f;
        handle.Top.Set(_padding, 0f);
        handle.Left.Set(-_padding, 0f);
        _panel.Append(handle);

        return this;
    }
    // -------------------------------------------------------------------
    // Debug drawing
    // -------------------------------------------------------------------

    /// <summary>
    /// Call from the panel's Draw override to render debug section boundary lines.
    /// Only draws if <see cref="DebugSectionLines"/> is true.
    /// </summary>
    public void DrawDebugLines(SpriteBatch spriteBatch, CalculatedStyle panelDims)
    {
        if (!DebugSectionLines) return;

        foreach (var y in _sectionBoundaries)
        {
            var lineRect = new Rectangle(
                (int)panelDims.X,
                (int)(panelDims.Y + y),
                (int)panelDims.Width,
                1);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineRect, Color.Magenta * 0.7f);
        }
    }

    // -------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------

    private void MarkSection()
    {
        _sectionBoundaries.Add(CurrentY);
    }

    private static UIIconButton MakeIconBtn(Asset<Texture2D> texture, string hoverText, float left, float top)
    {
        var btn = new UIIconButton(texture, hoverText);
        btn.Width.Set(IconBtnSize, 0f);
        btn.Height.Set(IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = true;
        return btn;
    }

    private static UIIconButton MakeToggleIconBtn(Asset<Texture2D> texture, string hoverText, float left, float top, bool initialState = false)
    {
        var btn = new UIIconButton(texture, hoverText, initialState);
        btn.Width.Set(IconBtnSize, 0f);
        btn.Height.Set(IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = false;
        return btn;
    }

    /// <summary>
    /// Creates a non-toggle, non-radio icon button for one-shot actions.
    /// Visually identical to a radio button but with no persistent state.
    /// </summary>
    private static UIIconButton MakeActionIconBtn(Asset<Texture2D> texture, string hoverText, float left, float top)
    {
        var btn = new UIIconButton(texture, hoverText);
        btn.Width.Set(IconBtnSize, 0f);
        btn.Height.Set(IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        btn.IsRadio = false;
        btn.IsAction = true;
        return btn;
    }

    private static UIToggleButton MakeToggle(string text, float left, float top, Color? tint = null)
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

    private static UITriStateButton MakeTriState(string baseName, float left, float top)
    {
        var btn = new UITriStateButton(baseName);
        btn.Width.Set(ButtonWidth, 0f);
        btn.Height.Set(ButtonHeight, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        return btn;
    }
}