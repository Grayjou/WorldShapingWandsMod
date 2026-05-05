using System.Linq;
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
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Networking.Handlers;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI.Elements;
using WorldShapingWandsMod.Common.UI.Elements.Builders;
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
    private PaintColorSection _paintColorSection;

    // Shape buttons
    private UIIconButton _rectFilledBtn, _rectHollowBtn;
    private UIIconButton _ellipseFilledBtn, _ellipseHollowBtn;
    private UIIconButton _diamondFilledBtn, _diamondHollowBtn;
    private UIIconButton _triangleFilledBtn, _triangleHollowBtn;
    private UIIconButton _edgeBtn, _cardinalBtn, _straightLineBtn;
    private UIIconButton _moldBtn;
    private UIIconButton _magicWandReadBtn;

    private UIText _thicknessValue;
    private UIIconButton _equalDimensionsBtn, _connectDiameterBtn, _invertSelectionBtn, _flipHalfOrientationBtn, _repaintBtn;
    private UISliceGrid _sliceGrid;

    // S8 2026-04-28 — ColorReplacePlan.md §3.1: single action button with
    // palette-swap rendering + right-click → ColorReplaceConfigSubPanel.
    private UIColorReplaceButton _colorReplaceBtn;

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

    // S6 2026-04-24 (W-S6-3): the local PaintColors/PaintColorNames aliases
    // were inlined into Common/UI/Elements/CoatingPaintColorGrid.Build(...).
    // Both arrays are still sourced from PaintPalette, just one indirection
    // shorter at the consumption site.

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.CoatingBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.CoatingBorder;
        Append(_mainPanel);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        _builder = new WandPanelBuilder(_mainPanel, PanelWidth, Padding);
        _builder.AddTitle("Coating.Title");

        // === MODE (non-radio icon buttons) ===
        var texPaintTile   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintTile",   AssetRequestMode.ImmediateLoad);
        var texPaintWall   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintWall",   AssetRequestMode.ImmediateLoad);
        var texScrapeMoss  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModeScrapeMoss",  AssetRequestMode.ImmediateLoad);
        var texHarvestMoss = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModeHarvestMoss", AssetRequestMode.ImmediateLoad);

        // (S9 2026-04-28; ColorReplacePlan.md §3.4 + GrayJou worried-client review)
        // The Mode section is now a 5-cell row: 4 mode toggles + 1 Color
        // Replace action button. The standalone "Color Replace" section that
        // S8 placed below the Options row is gone — it lived in its own
        // section header but the action is conceptually a Mode-adjacent
        // operation, so it sits beside the Mode toggles. Layout math mirrors
        // WandPanelBuilder.AddIconGrid so spacing is identical.
        var texReplaceBase   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintReplace",              AssetRequestMode.ImmediateLoad);
        var texIgnoreSource  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintReplaceIgnoreSource", AssetRequestMode.ImmediateLoad);
        var texIgnoreTarget  = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintReplaceIgnoreTarget", AssetRequestMode.ImmediateLoad);
        // S10 (GrayJou worried-client review): the both-Ignore composed view
        // of the two halves was opaque-checkerboard same-tone as the chrome
        // bg; resolve to the dedicated bake GrayJou shipped in S6 instead.
        var texIgnoreBoth    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/CoatingModes/ModePaintReplaceIgnoreBoth",   AssetRequestMode.ImmediateLoad);

        _builder.AddSectionHeader("Coating.Mode");
        const int modeCellCount = 5;
        float modeRowTotalWidth = 0f;
        for (int i = 0; i < modeCellCount; i++)
        {
            modeRowTotalWidth = LayoutSpacing.AddHorizontalSpace(
                modeRowTotalWidth,
                WandPanelBuilder.IconBtnSize,
                i == 0 ? 0f : WandPanelBuilder.IconGap);
        }
        float modeRowStartX = (PanelWidth - modeRowTotalWidth) / 2f - Padding;
        float modeRowY = _builder.CurrentY;
        float ModeCellLeft(int i) => modeRowStartX
                                   + (WandPanelBuilder.IconBtnSize + WandPanelBuilder.IconGap) * i;

        _paintTileBtn   = MakeModeToggle(texPaintTile,   "Coating.PaintTile",   ModeCellLeft(0), modeRowY);
        _paintWallBtn   = MakeModeToggle(texPaintWall,   "Coating.PaintWall",   ModeCellLeft(1), modeRowY);
        _scrapeMossBtn  = MakeModeToggle(texScrapeMoss,  "Coating.ScrapeMoss",  ModeCellLeft(2), modeRowY);
        _harvestMossBtn = MakeModeToggle(texHarvestMoss, "Coating.HarvestMoss", ModeCellLeft(3), modeRowY);
        _mainPanel.Append(_paintTileBtn);
        _mainPanel.Append(_paintWallBtn);
        _mainPanel.Append(_scrapeMossBtn);
        _mainPanel.Append(_harvestMossBtn);

        _colorReplaceBtn = new UIColorReplaceButton(texReplaceBase, texIgnoreSource, texIgnoreTarget, texIgnoreBoth);
        _colorReplaceBtn.HasSubUIBadge = true;
        _colorReplaceBtn.Width.Set(WandPanelBuilder.IconBtnSize, 0f);
        _colorReplaceBtn.Height.Set(WandPanelBuilder.IconBtnSize, 0f);
        _colorReplaceBtn.Left.Set(ModeCellLeft(4), 0f);
        _colorReplaceBtn.Top.Set(modeRowY, 0f);
        _mainPanel.Append(_colorReplaceBtn);

        _builder.AdvanceY(WandPanelBuilder.IconBtnSize + WandPanelBuilder.AfterIconGridSpacing);

        // === COATING TYPE (Illuminant / Echo tri-state) ===
        _builder.AddSectionHeader("Coating.CoatingType");
        _builder.AddTriStateRow(
            L("Coating.Illuminant"), out _illuminantBtn,
            L("Coating.Echo"), out _echoBtn,
            spacing: WandPanelBuilder.AfterToggleGroupSpacing);

        // === PAINT COLOR PICKER (PaintColorSection — popout-flavour UISection) ===
        // (Phase C 2026-04-29 — SubUI Migration Plan §Phase C) Migrated from
        // legacy `CollapsibleSection { Style = Popout }` + `CollapsedPopoutHost`
        // to the unified `UISection`/`WandSubPanel` substrate. Behaviour is
        // byte-identical for the player: ⧧ pops out, ✕ collapses back, position
        // persists across world restarts via `WandPlayer.PopoutPositions["Coating.PaintColor"]`
        // (legacy key preserved verbatim — zero save-data migration needed).
        //
        // Layout note (intentional, deferred for v1.2.0): when the user pops out
        // this section, the in-panel block shrinks from
        //   (HeaderHeight 22f + gridHeight 104f) = 126f
        // down to just HeaderHeight 22f, leaving a ~104f visual gap above the
        // Shape section below. This is the *honest* UX (the body really is
        // floating elsewhere now), and matches every prior session's contract.
        // A re-flow pass that shifts the below-sections up on collapse is a
        // proper UIList migration and is queued in DeferredForNextSession.md
        // as the v1.2.0 polish item — out of scope for Phase C.

        float paintSectionTop = _builder.CurrentY;

        _colorPickerContainer = CoatingPaintColorGrid.Build(
            columns: SwatchCols,
            containerInnerWidth: PanelWidth - 2 * Padding,
            onSelect: SetPaintColor,
            out _colorButtons,
            swatchSize: SwatchSize,
            swatchGap: SwatchGap);

        // (v1.1, 2026-04-25 S2 — DesignDoc_PopoutFrameworkV1_1 §3.5 / Invariant I-5;
        //  semantics revised v1.3 §B, 2026-04-25 S4 — DesignDoc_PopoutFrameworkV1_3 §B
        //  / Invariant I-11.)
        // Multi-family ownership: PaintColor is consumed by the Coating wand
        // family AND by Wand of Building / Wand of Replacement (both expose a
        // PaintSprayer toggle that applies the selected paint while building /
        // replacing). The popout therefore stays VISIBLE across the natural
        // build-with-paint → swap-to-replacement → swap-back flow.
        //
        // v1.3 §B reframed the predicate from "auto-close gate" to "visibility
        // gate" — when this returns false the popout PARKS (body + position
        // preserved), and resurfaces on the next tick the predicate returns
        // true. Only ✕ closes for real, so configured state survives any number
        // of brief hotbar excursions.
        //
        // To make this fully modeless (popout always shown until explicit ✕),
        // flip the predicate to `() => true` — single-line change, reversible
        // at any time, no schema impact.
        var paintColorSection = new PaintColorSection(
            swatchGrid: _colorPickerContainer,
            ownerVisibility: () =>
            {
                var heldMod = Main.LocalPlayer?.HeldItem?.ModItem;
                return heldMod is WandOfCoatingBase
                    || heldMod is WandOfBuildingBase
                    || heldMod is WandOfReplacementBase;
            });
        paintColorSection.Top.Set(paintSectionTop, 0f);
        _mainPanel.Append(paintColorSection);
        paintColorSection.Build();
        _paintColorSection = paintColorSection;

        // The section auto-derives its own Height from header + body (or just
        // header when popped-out). Read it back so the next section sits flush
        // below.
        float colorPickerHeight = SwatchRows * (SwatchSize + SwatchGap);
        _builder.AdvanceY(WandPanelBuilder.SectionHeaderSpacing - 22f /* UISection.HeaderHeightConst */
                          + colorPickerHeight + WandPanelBuilder.AfterIconGridSpacing);

        // === SHAPE ===
        _builder.AddFullShapeSection(out var shapes);
        _rectFilledBtn    = shapes.RectFilled;     _rectHollowBtn    = shapes.RectHollow;
        _ellipseFilledBtn = shapes.EllipseFilled;  _ellipseHollowBtn = shapes.EllipseHollow;
        _diamondFilledBtn = shapes.DiamondFilled;  _diamondHollowBtn = shapes.DiamondHollow;
        _triangleFilledBtn = shapes.TriangleFilled; _triangleHollowBtn = shapes.TriangleHollow;
        _edgeBtn = shapes.Elbow; _cardinalBtn = shapes.Cardinal; _straightLineBtn = shapes.StraightLine;
        _moldBtn = shapes.Mold;
        _magicWandReadBtn = shapes.MagicWandRead;

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
        var texEqualDim    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleEqualDim", AssetRequestMode.ImmediateLoad);
        var texConnectDiam = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleConnectDiam", AssetRequestMode.ImmediateLoad);
        var texInvertSel   = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleInvertSel", AssetRequestMode.ImmediateLoad);
        // (S2 2026-04-30 — InvertHalfOrientation #IOP) placeholder reuses ToggleInvertSel.
        // TODO: pending ToggleFlipHalfOrientation dedicated asset (placeholder = ToggleInvertSel byte-copy; tracked in dev_notes/dev_tasks/pending_assets.md §3b)
        var texFlipHalf    = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleFlipHalfOrientation", AssetRequestMode.ImmediateLoad);
        var texRepaint     = mod.Assets.Request<Texture2D>("Assets_Build/Icons/Toggles/ToggleRepaint", AssetRequestMode.ImmediateLoad);

        _builder.AddShapeOptionsSection(new WandPanelBuilder.IconDef[]
        {
            new(texEqualDim,    "Common.EqualDimensions",      isToggle: true),
            new(texConnectDiam, "Common.ConnectDiameterTooltip", isToggle: true, initialState: true),
            new(texInvertSel,   "Common.InvertSelection",      isToggle: true),
            new(texFlipHalf,    "Common.FlipHalfOrientation",  isToggle: true),
            new(texRepaint,     "Coating.Repaint",              isToggle: true, initialState: true),
        }, out var optBtns);
        _equalDimensionsBtn = optBtns[0];
        _connectDiameterBtn = optBtns[1];
        _invertSelectionBtn = optBtns[2];
        _flipHalfOrientationBtn = optBtns[3];
        _repaintBtn         = optBtns[4];

        // === COLOR REPLACE ACTION (S9 2026-04-28; moved from its own section
        //  into the Mode row above per ColorReplacePlan.md §3.4 / GrayJou
        //  worried-client review). The asset loads + button construction now
        //  live next to the Mode header. The standalone "Coating.ColorReplace.
        //  Section" header that S8 placed below the Options block is gone —
        //  it added an extra section gap for a single button that conceptually
        //  belongs alongside the Mode toggles. Tooltip + click wiring stay
        //  identical (left-click fires, right-click opens the picker SubUI).

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
        _moldBtn.OnToggled += (_, _) => SetShape(ShapeType.Mold, ShapeMode.Filled);
        _magicWandReadBtn.OnToggled += (_, _) => SetShape(ShapeType.MagicWandRead, ShapeMode.Filled);

        _equalDimensionsBtn.OnToggled += (_, _) => ToggleEqualDimensions();
        _connectDiameterBtn.OnToggled += (_, _) => ToggleConnectDiameter();
        _invertSelectionBtn.OnToggled += (_, _) => ToggleInvertSelection();
        _flipHalfOrientationBtn.OnToggled += (_, _) => ToggleFlipHalfOrientation();
        _repaintBtn.OnToggled += (_, _) => ToggleRepaint();

        // === COLOR REPLACE WIRING (S8 2026-04-28; revised S11) ===
        // S11 (GrayJou worried-client review): Color Replace is now a real
        // member of the Mode radio row (CoatingMode.ColorReplace). Left-click
        // SELECTS the mode (mutually exclusive with PaintTile/PaintWall/
        // ScrapeMoss/HarvestMoss); the actual operation fires when the player
        // CASTS the wand at the world, same as every other Mode. Right-click
        // still toggles the config SubUI for source/target/channel.
        _colorReplaceBtn.OnLeftClick += (_, _) =>
        {
            var s = GetSettings();
            if (s == null) return;
            s.Mode = CoatingMode.ColorReplace;
            UpdateModeButtons();
            UpdateColorReplaceButton();
            UpdateColorPickerVisibility();
        };
        _colorReplaceBtn.OnRightClick += (_, _) => ToggleColorReplaceSubUI();
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
        settings.Shape = new ShapeInfo(type, mode, settings.Shape.Thickness, settings.Shape.EqualDimensions, settings.Shape.Slice, settings.Shape.ConnectDiameter, settings.Shape.InvertSelection, settings.Shape.InvertHalfOrientation);
        UpdateShapeButtons();
    }

    private void AdjustThickness(int delta)
    {
        var settings = GetSettings();
        if (settings == null) return;
        var shape = settings.Shape;
        int max = WandConfigs.Limits?.MaxOutlineThickness ?? 10;
        shape.Thickness = System.Math.Clamp(shape.Thickness + delta, 0, max);
        settings.Shape = shape;
        UpdateThicknessDisplay();
    }

    private void ToggleEqualDimensions() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.EqualDimensions = _equalDimensionsBtn.Toggled; s.Shape = sh; }
    private void ToggleConnectDiameter() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.ConnectDiameter = _connectDiameterBtn.Toggled; s.Shape = sh; }
    private void ToggleInvertSelection() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertSelection = _invertSelectionBtn.Toggled; s.Shape = sh; }
    private void ToggleFlipHalfOrientation() { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.InvertHalfOrientation = _flipHalfOrientationBtn.Toggled; s.Shape = sh; }
    private void ToggleRepaint() { var s = GetSettings(); if (s == null) return; s.Repaint = _repaintBtn.Toggled; }
    private void OnSliceChanged(SliceMode slice) { var s = GetSettings(); if (s == null) return; var sh = s.Shape; sh.Slice = slice; s.Shape = sh; }

    // ============================================================================
    //  Color Replace SubUI wiring (S8 2026-04-28; ColorReplacePlan.md §3)
    //
    //  S11 NOTE: the standalone `FireColorReplace` method that lived here in
    //  S10 was promoted to `WandOfCoatingBase.ExecuteColorReplace` (called
    //  from `ExecuteCoating` when `Mode == ColorReplace`). Color Replace is
    //  now a real CoatingMode radio member; the panel button just selects
    //  the mode (left-click) or opens this SubUI (right-click).
    // ============================================================================

    /// <summary>
    /// Right-click handler on the Color Replace button. Toggle-close UX: if the
    /// picker SubUI is already open and anchored to this button, close it;
    /// otherwise open a fresh one. Mirrors the StencilPickerSubPanel pattern
    /// landed in S7 — same SubUI primitive, same toggle-close detection idiom.
    /// </summary>
    private void ToggleColorReplaceSubUI()
    {
        var sys = ModContent.GetInstance<WandUISystem>();
        if (sys?.WandSubPanelHost == null) return;

        foreach (var existing in sys.WandSubPanelHost.Panels)
        {
            if (existing.IdentityKey == ColorReplaceConfigBuilder.IdentityKey
                && ReferenceEquals(existing.Host, _colorReplaceBtn))
            {
                sys.CloseWandSubPanel(existing);
                if (_colorReplaceBtn != null) _colorReplaceBtn.IsWandSubPanelOpen = false;
                return;
            }
        }

        // S10 (GrayJou worried-client review): "the other panel doesn't get
        // deselected" — when opening the Color Replace picker, retract the
        // PaintColor popout so the player has a single visible config surface
        // at a time (matches every other modal-ish UI in the panel set).
        if (_paintColorSection != null && _paintColorSection.IsPoppedOut
            && _paintColorSection.ActivePopout != null)
        {
            sys.CloseWandSubPanel(_paintColorSection.ActivePopout);
        }

        var picker = WandSubPanelFactories.CreateColorReplaceConfig(
            host: _colorReplaceBtn,
            onChanged: (src, tgt) =>
            {
                if (_colorReplaceBtn == null) return;
                _colorReplaceBtn.SourcePaint = src;
                _colorReplaceBtn.TargetPaint = tgt;
                _colorReplaceBtn.HoverText = BuildColorReplaceTooltip(src, tgt);
            });
        // S10: keep the host icon's "I'm in active config mode" green visible
        // for as long as the picker lives. Cleared by the OnClose subscription
        // below so manual dismiss / Esc / click-outside also reset the visual.
        picker.OnClose += () =>
        {
            if (_colorReplaceBtn != null) _colorReplaceBtn.IsWandSubPanelOpen = false;
        };
        if (_colorReplaceBtn != null) _colorReplaceBtn.IsWandSubPanelOpen = true;
        sys.OpenWandSubPanel(picker);
        picker.AnchorToHost();
    }

    private string BuildColorReplaceTooltip(byte src, byte tgt)
    {
        string srcName = ResolvePaintName(src);
        string tgtName = ResolvePaintName(tgt);
        // S9: channel is now the explicit ColorReplaceChannel setting,
        // independent of the wand's CoatingMode toggle row.
        var ch = GetSettings()?.ColorReplaceChannel ?? ColorReplaceChannel.Tile;
        string channel = ch == ColorReplaceChannel.Wall
            ? L("Coating.ColorReplace.ChannelWall")
            : L("Coating.ColorReplace.ChannelTile");
        return Language.GetTextValue(
            "Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.ButtonTooltipFmt",
            srcName, tgtName, channel);
    }

    private static string ResolvePaintName(byte paint)
    {
        if (paint == 255)
            return Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.Ignore");
        if (paint < PaintPalette.Count)
            return PaintPalette.Names[paint];
        return paint.ToString();
    }

    private void UpdateColorReplaceButton()
    {
        var s = GetSettings();
        if (s == null || _colorReplaceBtn == null) return;
        _colorReplaceBtn.SourcePaint = s.ColorReplaceSource;
        _colorReplaceBtn.TargetPaint = s.ColorReplaceTarget;
        // S11 (GrayJou worried-client review of S10): no greying, no alpha
        // reduction. Per verbatim: *“we don't have to grey or reduce the
        // alpha of the button because the green icon background already
        // works. No greying always full alpha.”* Toggled (set inside
        // UpdateModeButtons) carries the active-mode signal; the radio
        // family makes the empty-selection greying redundant anyway since
        // selecting the mode without a selection just means “next cast
        // does Color Replace” — a perfectly normal pre-armed state.
        _colorReplaceBtn.IsActive = true;
        _colorReplaceBtn.Toggled = s.Mode == CoatingMode.ColorReplace;
        _colorReplaceBtn.HoverText = BuildColorReplaceTooltip(s.ColorReplaceSource, s.ColorReplaceTarget);
    }

    private void UpdateModeButtons()
    {
        var settings = GetSettings();
        if (settings == null) return;
        _paintTileBtn.Toggled  = settings.Mode == CoatingMode.PaintTile;
        _paintWallBtn.Toggled  = settings.Mode == CoatingMode.PaintWall;
        _scrapeMossBtn.Toggled = settings.Mode == CoatingMode.ScrapeMoss;
        _harvestMossBtn.Toggled = settings.Mode == CoatingMode.HarvestMoss;
        // S11: ColorReplace is the 5th radio member; Toggled drives the
        // ActiveGreen background on the custom button.
        if (_colorReplaceBtn != null)
            _colorReplaceBtn.Toggled = settings.Mode == CoatingMode.ColorReplace;
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
        _moldBtn.Toggled = shape.Shape == ShapeType.Mold;
        _magicWandReadBtn.Toggled = shape.Shape == ShapeType.MagicWandRead;
    }

    private void UpdateThicknessDisplay() { _thicknessValue?.SetText(GetSettings()?.Shape.Thickness.ToString() ?? "1"); }
    private void UpdateEqualDimensionsButton() { var s = GetSettings(); if (s == null) return; _equalDimensionsBtn.Toggled = s.Shape.EqualDimensions; }
    private void UpdateConnectDiameterButton() { var s = GetSettings(); if (s == null || _connectDiameterBtn == null) return; _connectDiameterBtn.Toggled = s.Shape.ConnectDiameter; }
    private void UpdateInvertSelectionButton() { var s = GetSettings(); if (s == null || _invertSelectionBtn == null) return; _invertSelectionBtn.Toggled = s.Shape.InvertSelection; _invertSelectionBtn.Disabled = !s.Shape.SupportsInversion; }
    private void UpdateFlipHalfOrientationButton() { var s = GetSettings(); if (s == null || _flipHalfOrientationBtn == null) return; _flipHalfOrientationBtn.Toggled = s.Shape.InvertHalfOrientation; _flipHalfOrientationBtn.Disabled = s.Shape.Slice == SliceMode.Full; }
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
        UpdateFlipHalfOrientationButton();
        UpdateRepaintButton();
        UpdateColorReplaceButton();
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

    // -- UIPaintColorButton was promoted to Common/UI/Elements/UIPaintColorButton.cs
    //    in S6 2026-04-24 (W-S6-3, S+2 prep) so CoatingPaintColorGrid.Build can
    //    construct it from outside this class. Behaviour is byte-identical.

    /// <summary>
    /// Builds a non-radio toggle icon button at the given (left, top) inside the
    /// Mode row. Mirrors <c>WandPanelBuilder.MakeToggleIconBtn</c> verbatim;
    /// duplicated here because that helper is private and the Mode row was
    /// hand-rolled in S9 to interleave a non-toggle <see cref="UIColorReplaceButton"/>
    /// as the 5th cell.
    /// </summary>
    private static UIIconButton MakeModeToggle(Asset<Texture2D> texture, string locKey, float left, float top)
    {
        var btn = new UIIconButton(texture, L(locKey), initialState: false)
        {
            IsRadio = false,
        };
        btn.Width.Set(WandPanelBuilder.IconBtnSize, 0f);
        btn.Height.Set(WandPanelBuilder.IconBtnSize, 0f);
        btn.Left.Set(left, 0f);
        btn.Top.Set(top, 0f);
        return btn;
    }
}