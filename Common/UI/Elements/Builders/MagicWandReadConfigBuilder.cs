using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Geometry;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.Settings;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI.Elements.Builders;

/// <summary>
/// (S4 2026-05-01 — <c>StencilMagicWandSelectionPlan.md</c> §0.2 + §4.1.)
/// Companion-builder for the Magic Wand (Read) configuration SubPanel
/// body. Two top-level sections per the S4 2026-04-28 GrayJou ratification:
///
/// <list type="number">
///   <item><b>SampleMode</b> — 12 cells across two balanced 6-cell rows.
///   Row 1: SameTile, SameWall, Solid, Wall, Rope, Platform.
///   Row 2: Rail, PlanterBox, Empty, Liquid, PaintTile, PaintWall.
///   Drives <see cref="MagicWandReadConfig.ObjectType"/>.</item>
///   <item><b>Contiguity</b> — 3-cell radio (4-/8-/Non-contiguous).
///   Drives <see cref="MagicWandReadConfig.Continuity"/>.</item>
/// </list>
///
/// <para><b>Visual style (S6 2026-05-01)</b>: icon-first compact radios
/// (22×22 cells with 16×16 icons), matching the worried-client request to
/// avoid reusing 32×32 shape-grid assets in this smaller SubUI. For cells
/// whose dedicated MagicWand icon isn't shipped yet, builder-level fallback
/// reuses existing 16×16 object icons (e.g. platform/rail/planter).</para>
///
/// <para><b>Persistence</b>: the SubUI writes <see cref="WandPlayer.MagicWandReadConfig"/>
/// directly. That struct round-trips through <see cref="WandPlayer.SaveData"/>
/// /<see cref="WandPlayer.LoadData"/> as a 2-byte tag pair, so picks
/// survive logout/login. The captured shape (<see cref="WandPlayer.LastMagicWandShape"/>)
/// is in-memory only per <c>MultipleStencilsPlan.md</c> §8.</para>
///
/// <para><b>Lifecycle metadata</b>: the matching factory entry
/// <see cref="WandSubPanelFactories.CreateMagicWandReadConfig"/> declares
/// <c>Type=Panel</c>, <c>OwnerFamilies=None</c> (every WSW wand can
/// summon this picker — the Read shape itself is stencil-only but the
/// CONFIG is a player-scoped preference, mirroring the ACT-ON Stencil
/// picker model from <see cref="StencilPickerBuilder"/>),
/// <c>LockBehaviourDecl=DefaultLocked</c> (≥2 sections per
/// <c>WSWSubUIPrimitivePlan.md</c> §0), <c>OnChoice=NeverCloses</c>
/// (player typically wants to set Sample Mode AND Contiguity in one
/// open), <c>OnParentClose=StaysUpIfLocked</c>.</para>
/// </summary>
internal static class MagicWandReadConfigBuilder
{
    public const string IdentityKey = "MagicWand.ReadConfig";
    public const string TitleKey    = "Mods.WorldShapingWandsMod.UI.MagicWandRead.SubPanelTitle";

    // ── Layout constants (mirror ColorReplaceConfigBuilder's spirit) ──

    private const float HorizontalPad      = 2f;
    private const float SectionGap         = 10f;
    private const float SectionHeaderHeight = 18f;
    private const float RowGap             = 4f;
    private const float BottomPad          = 12f;

    private const float TextFitPadding = 12f;

    private const float SampleCellW = WandPanelBuilder.SmallIconBtnSize;
    private const float SampleCellH = WandPanelBuilder.SmallIconBtnSize;
    private const float SampleCellGap = 4f;

    private const float ContigCellW = WandPanelBuilder.SmallIconBtnSize;
    private const float ContigCellH = WandPanelBuilder.SmallIconBtnSize;
    private const float ContigCellGap = 4f;
    private const float IconDrawSize = 16f;

    private static float ComputeRowWidth(int elementCount, float elementWidth, float elementGap)
    {
        // This conditional is not necessary cause then elementGap * (elementCount - 1) would be zero when elementCount is 1, but it makes the intention clearer and avoids unnecessary multiplication.
        float rowWidth = elementCount == 1 ? elementWidth : elementWidth * elementCount + elementGap * (elementCount - 1);
        return rowWidth;
    }

    /// <summary>
    /// Builds the body element (SampleMode 2-row grid + Contiguity row).
    /// The factory wraps this in a <see cref="WandSubPanel"/> with
    /// lifecycle metadata declared in
    /// <see cref="WandSubPanelFactories.CreateMagicWandReadConfig"/>.
    /// </summary>
    /// <param name="onChanged">Optional host callback fired after each
    /// pick. Receives the post-change config snapshot. Pass null when no
    /// host bookkeeping is needed (the SubUI writes
    /// <see cref="WandPlayer.MagicWandReadConfig"/> regardless).</param>
    public static UIElement BuildBody(Action<MagicWandReadConfig> onChanged = null)
    {
        var initial = ReadCurrentConfig();

        // Pre-allocate cell arrays so the click-handler closures can flip
        // sibling Toggled visuals in lock-step (UIIconButton's IsRadio
        // covers only the cell's own bit, not the row).
        var sampleCells = new IconRadioCell[12];
        var contigCells = new IconRadioCell[3];

        Action applySampleVisual = () =>
        {
            var cfg = ReadCurrentConfig();
            int sel = (int)cfg.ObjectType;
            for (int i = 0; i < sampleCells.Length; i++)
                sampleCells[i].IsSelected = (i == sel);
        };
        Action applyContigVisual = () =>
        {
            var cfg = ReadCurrentConfig();
            int sel = (int)cfg.Continuity;
            for (int i = 0; i < contigCells.Length; i++)
                contigCells[i].IsSelected = (i == sel);
        };

        // Section 1 row layouts (cell index → row span)
        // Row 1: indices 0..5   (SameTile, SameWall, Solid, Wall, Rope, Platform)
        // Row 2: indices 6..11  (Rail, PlanterBox, Empty, Liquid, PaintTile, PaintWall)
        var rowGroups = new[]
        {
            new RowGroup(0, 6),
            new RowGroup(6, 6),
        };

        float contentW = 0f;
        foreach (var grp in rowGroups)
        {
            float rowW = ComputeRowWidth(grp.Count, SampleCellW, SampleCellGap);
            contentW = LayoutSpacing.FitHorizontalSpace(contentW, rowW);
        }
        float contigRowWForFold = ComputeRowWidth(3, ContigCellW, ContigCellGap);
        contentW = LayoutSpacing.FitHorizontalSpace(contentW, contigRowWForFold);
        /*
            * The body is 176 pixels wide
            * 6*22 + 5*4 = 152
            * extra 12 pixels at the end
            * 2*2 pixels horizontal = 2 * 2 = 4
            * where are the extra 8 pixels comming from?
            * I hardcoded bodyW to 60f to see if it was the Halign, but even tho most of the buttons were out of the frame, the first button
            * was 12f from the left border, even though rowLeft was initialized as 0
            * This ensures the body is always just wide enough to fit its content, without hardcoding any particular cell count or gap size.
            */
        float bodyW = contentW + HorizontalPad * 2f + 12f + TextFitPadding * 2f;

        float sampleSectionH = SectionHeaderHeight
            + 2 * SampleCellH + RowGap;
        float contigSectionH = SectionHeaderHeight + ContigCellH;
        float bodyH = sampleSectionH + SectionGap + contigSectionH + BottomPad;

        var body = new UIElement();
        body.Width.Set(bodyW, 0f);
        body.Height.Set(bodyH, 0f);

        // ── Section 1: SampleMode ──
        float yCursor = 0f;
        var sampleHeader = new UIText(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.MagicWandRead.SampleMode.Header"),
            0.85f)
        {
            HAlign = 0.5f,
            IgnoresMouseInteraction = true,
        };
        sampleHeader.Top.Set(yCursor, 0f);
        body.Append(sampleHeader);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: SectionHeaderHeight,
            bottomPadding: 0f);

        for (int g = 0; g < rowGroups.Length; g++)
        {
            var grp = rowGroups[g];
            float rowW = ComputeRowWidth(grp.Count, SampleCellW, SampleCellGap);
            float rowLeft = TextFitPadding;
            for (int k = 0; k < grp.Count; k++)
            {
                int idx = grp.StartIndex + k;
                var type = (MagicWandObjectType)idx;
                string label = ResolveSampleLabel(type);
                string tooltip = ResolveSampleTooltip(type);
                var cell = new IconRadioCell(
                    icon: ResolveSampleIcon(type),
                    hoverText: tooltip,
                    fallbackLabel: label,
                    initial: (int)initial.ObjectType == idx);
                cell.Width.Set(SampleCellW, 0f);
                cell.Height.Set(SampleCellH, 0f);
                cell.Left.Set(rowLeft + k * (SampleCellW + SampleCellGap), 0f);
                cell.Top.Set(yCursor, 0f);

                int captured = idx;
                cell.OnLeftClick += (_, _) =>
                {
                    var p = Main.LocalPlayer?.GetModPlayer<WandPlayer>();
                    if (p == null) return;
                    var cfg = p.MagicWandReadConfig;
                    cfg.ObjectType = (MagicWandObjectType)captured;
                    p.MagicWandReadConfig = cfg;
                    applySampleVisual();
                    onChanged?.Invoke(cfg);
                };
                sampleCells[idx] = cell;
                body.Append(cell);
            }
            yCursor = LayoutSpacing.AddVerticalSpace(
                currentSize: yCursor,
                elementSize: SampleCellH,
                bottomPadding: RowGap);
        }
        // Strip the trailing RowGap added after the last row.
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: 0f,
            bottomPadding: SectionGap - RowGap);

        // ── Section 2: Contiguity ──
        var contigHeader = new UIText(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.MagicWandRead.Contiguity.Header"),
            0.85f)
        {
            HAlign = 0f, // gets 12f just as the row buttons, idk why still
            IgnoresMouseInteraction = true,
        };
        contigHeader.Top.Set(yCursor, 0f);
        body.Append(contigHeader);
        var actHeader = new UIText(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.MagicWandRead.ActuationFilter.Header"),
            0.85f)
        {
            HAlign = 1.0f,
            // Actually 120 pixels from the left, well I don't know anything anymore    
            IgnoresMouseInteraction = true,
        };
        actHeader.Top.Set(yCursor, 0f);
        body.Append(actHeader);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: SectionHeaderHeight,
            bottomPadding: 0f);

        float contigRowW = ComputeRowWidth(3, ContigCellW, ContigCellGap);
        float contigLeft = 0f;//(bodyW - contigRowW) * 0.25f;
        for (int i = 0; i < 3; i++)
        {
            var cont = (MagicWandContinuity)i;
            string label = ResolveContigLabel(cont);
            string tooltip = ResolveContigTooltip(cont);
            var cell = new IconRadioCell(
                icon: ResolveContigIcon(cont),
                hoverText: tooltip,
                fallbackLabel: label,
                initial: (int)initial.Continuity == i);
            cell.Width.Set(ContigCellW, 0f);
            cell.Height.Set(ContigCellH, 0f);
            cell.Left.Set(contigLeft + i * (ContigCellW + ContigCellGap), 0f);
            cell.Top.Set(yCursor, 0f);

            int captured = i;
            cell.OnLeftClick += (_, _) =>
            {
                var p = Main.LocalPlayer?.GetModPlayer<WandPlayer>();
                if (p == null) return;
                var cfg = p.MagicWandReadConfig;
                cfg.Continuity = (MagicWandContinuity)captured;
                p.MagicWandReadConfig = cfg;
                applyContigVisual();
                onChanged?.Invoke(cfg);
            };
            contigCells[i] = cell;
            body.Append(cell);
        }


        // ── Section 3: Actuation Filter (C-S3 2026-05-03) ──



        // Single cycling button — clicking cycles Both → NonActuated → Actuated → Both.
        var actuationBtn = new TriStateIconButton<ActuationFilter>(
            getValue:       () => (Main.LocalPlayer?.GetModPlayer<WandPlayer>()?.MagicWandReadConfig ?? MagicWandReadConfig.Default).ActuationFilter,
            setValue:       v =>
            {
                var wp = Main.LocalPlayer?.GetModPlayer<WandPlayer>();
                if (wp == null) return;
                var cfg = wp.MagicWandReadConfig;
                cfg.ActuationFilter = v;
                wp.MagicWandReadConfig = cfg;
                onChanged?.Invoke(cfg);
            },
            next:           f => f.Next(),
            iconForValue:   f => ResolveActuationFilterIcon(f),
            tooltipForValue: f => ResolveActuationFilterTooltip(f),
            stateColorForValue: f => ResolveActuationFilterColor(f));
        actuationBtn.Width.Set(ContigCellW, 0f);
        actuationBtn.Height.Set(ContigCellH, 0f);
        actuationBtn.Left.Set(bodyW-64f, 0f); //((bodyW - ContigCellW) * 0.5f, 0f); Until I determine where the 12f extra pixels are coming from
        actuationBtn.Top.Set(yCursor, 0f);
        body.Append(actuationBtn);

        // Grow body to fit the new section.
        float newBodyH = yCursor + ContigCellH + BottomPad + 10f; // Need to inspect the height to see why the extra 10f is needed to make the section not hideous
        // To have 14f pixels at the bottom just like there are 14f at the top before the chromes
        body.Height.Set(newBodyH, 0f);

        // Initial visual sync (cells were constructed with their own
        // initial bit; this guarantees the row-wide invariant if the
        // config flips between Build() and the first Draw).
        applySampleVisual();
        applyContigVisual();

        return body;
    }

    private static Asset<Texture2D> ResolveActuationFilterIcon(ActuationFilter filter)
    {
        // All three states reuse the same ToggleActuation icon as a placeholder.
        // Future: create dedicated 16×16 icons per state and update these paths.
        return RequestFirstExisting("Assets_Build/Icons/Toggles/ToggleActuation");
    }

    private static string ResolveActuationFilterTooltip(ActuationFilter filter)
        => Language.GetTextValue(
            $"Mods.WorldShapingWandsMod.UI.MagicWandRead.ActuationFilter.{filter}.Tooltip");

    private static Color ResolveActuationFilterColor(ActuationFilter filter)
    {
        return filter switch
        {
            // Both = neutral / do-nothing state (matches tri-state "ignore" feel).
            ActuationFilter.Both => WandPanelTheme.Colors.ButtonInactive,
            // Non-actuated-only = exclusion filter (convention: red).
            ActuationFilter.NonActuatedOnly => WandPanelTheme.Colors.ActiveRed,
            // Actuated-only = focused include filter (convention: green).
            ActuationFilter.ActuatedOnly => WandPanelTheme.Colors.ActiveGreen,
            _ => WandPanelTheme.Colors.ButtonInactive,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private readonly struct RowGroup
    {
        public RowGroup(int startIndex, int count) { StartIndex = startIndex; Count = count; }
        public int StartIndex { get; }
        public int Count { get; }
    }

    private static MagicWandReadConfig ReadCurrentConfig()
    {
        var p = Main.LocalPlayer?.GetModPlayer<WandPlayer>();
        return p?.MagicWandReadConfig ?? MagicWandReadConfig.Default;
    }

    private static string ResolveSampleLabel(MagicWandObjectType type)
        => Language.GetTextValue($"Mods.WorldShapingWandsMod.UI.MagicWandRead.SampleMode.{type}.Label");

    private static string ResolveSampleTooltip(MagicWandObjectType type)
        => Language.GetTextValue($"Mods.WorldShapingWandsMod.UI.MagicWandRead.SampleMode.{type}.Tooltip");

    private static string ResolveContigLabel(MagicWandContinuity cont)
        => Language.GetTextValue($"Mods.WorldShapingWandsMod.UI.MagicWandRead.Contiguity.{cont}.Label");

    private static string ResolveContigTooltip(MagicWandContinuity cont)
        => Language.GetTextValue($"Mods.WorldShapingWandsMod.UI.MagicWandRead.Contiguity.{cont}.Tooltip");

    private static Asset<Texture2D> ResolveSampleIcon(MagicWandObjectType type)
    {
        return type switch
        {
            MagicWandObjectType.SameTile   => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleSameSolid", "Assets_Build/Icons/Objects/ObjSameType", "Assets_Build/Icons/Objects/ObjSameType"),
            MagicWandObjectType.SameWall   => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleSameWall", "Assets_Build/Icons/Objects/ObjWall", "Assets_Build/Icons/Objects/ObjWall"),
            MagicWandObjectType.Solid      => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleAllSolids", "Assets_Build/Icons/Objects/ObjSolid", "Assets_Build/Icons/Objects/ObjSolid"),
            MagicWandObjectType.Wall       => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleAllWalls", "Assets_Build/Icons/Objects/ObjWall", "Assets_Build/Icons/Objects/ObjWall"),
            MagicWandObjectType.Rope       => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleSameRope", "Assets_Build/Icons/Objects/ObjRope", "Assets_Build/Icons/Objects/ObjRope"),
            MagicWandObjectType.Platform   => RequestFirstExisting("Assets_Build/Icons/Objects/ObjPlatform", "Assets_Build/Icons/Objects/ObjPlatform"),
            MagicWandObjectType.Rail       => RequestFirstExisting("Assets_Build/Icons/Objects/ObjRail", "Assets_Build/Icons/Objects/ObjRail"),
            MagicWandObjectType.PlanterBox => RequestFirstExisting("Assets_Build/Icons/Objects/ObjPlanter", "Assets_Build/Icons/Objects/ObjPlanter"),
            MagicWandObjectType.Empty      => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleEmpty", "Assets_Build/Icons/Objects/ObjAir"),
            MagicWandObjectType.Liquid     => RequestFirstExisting("Assets_Build/Icons/MagicWand/SampleLiquid", "Assets_Build/Icons/MagicWand/SampleLiquid"),
            MagicWandObjectType.PaintTile  => RequestFirstExisting("Assets_Build/Icons/MagicWand/SamePaintTile", "Assets_Build/Icons/Objects/ObjTile", "Assets_Build/Icons/Objects/ObjTile"),
            MagicWandObjectType.PaintWall  => RequestFirstExisting("Assets_Build/Icons/MagicWand/SamePaintWall", "Assets_Build/Icons/Objects/ObjWall", "Assets_Build/Icons/Objects/ObjWall"),
            _ => null,
        };
    }

    private static Asset<Texture2D> ResolveContigIcon(MagicWandContinuity continuity)
    {
        return continuity switch
        {
            MagicWandContinuity.FourNeighbour => RequestFirstExisting("Assets_Build/Icons/MagicWand/Contiguity4N"),
            MagicWandContinuity.EightNeighbour => RequestFirstExisting("Assets_Build/Icons/MagicWand/Contiguity8N"),
            MagicWandContinuity.NonContiguous => RequestFirstExisting("Assets_Build/Icons/MagicWand/NonContiguous"),
            _ => null,
        };
    }

    private static Asset<Texture2D> RequestFirstExisting(params string[] candidatePaths)
    {
        var mod = ModContent.GetInstance<WorldShapingWandsMod>();
        for (int i = 0; i < candidatePaths.Length; i++)
        {
            var p = candidatePaths[i];
            if (string.IsNullOrWhiteSpace(p)) continue;
            try
            {
                return mod.Assets.Request<Texture2D>(p, AssetRequestMode.ImmediateLoad);
            }
            catch
            {
            }
        }

        return null;
    }

    /// <summary>
    /// Lightweight icon-first radio cell reused across both sections.
    /// Draws a 16×16 icon centered in a 22×22 toggle cell; falls back to
    /// compact text when an icon asset is absent.
    /// </summary>
    private class IconRadioCell : UIElement
    {
        public bool IsSelected { get; set; }
        public string HoverText { get; set; }
        private readonly string _fallbackLabel;
        private readonly Asset<Texture2D> _icon;

        public IconRadioCell(Asset<Texture2D> icon, string hoverText, string fallbackLabel, bool initial)
        {
            _icon = icon;
            HoverText = hoverText;
            _fallbackLabel = fallbackLabel;
            IsSelected = initial;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var dims = GetDimensions();
            var rect = dims.ToRectangle();
            Color bg = IsSelected
                ? WandPanelTheme.Colors.ActiveGreen
                : (IsMouseHovering ? WandPanelTheme.Colors.NeutralHover : WandPanelTheme.Colors.ElementInactive);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

            if (_icon?.Value != null)
            {
                float ix = rect.X + (rect.Width - IconDrawSize) * 0.5f;
                float iy = rect.Y + (rect.Height - IconDrawSize) * 0.5f;
                spriteBatch.Draw(_icon.Value, new Rectangle((int)ix, (int)iy, (int)IconDrawSize, (int)IconDrawSize), Color.White);
            }
            else
            {
                var font = FontAssets.MouseText.Value;
                float scale = 0.7f;
                var size = font.MeasureString(_fallbackLabel) * scale;
                float maxW = rect.Width - 4f;
                if (size.X > maxW)
                {
                    scale *= maxW / size.X;
                    size = font.MeasureString(_fallbackLabel) * scale;
                }

                float tx = rect.X + (rect.Width  - size.X) * 0.5f;
                float ty = rect.Y + (rect.Height - size.Y) * 0.5f;
                Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch, font, _fallbackLabel,
                    new Vector2(tx, ty), Color.White, 0f, Vector2.Zero,
                    new Vector2(scale, scale));
            }

            if (IsMouseHovering && !string.IsNullOrEmpty(HoverText))
                Main.instance.MouseText(HoverText);
        }
    }
}
