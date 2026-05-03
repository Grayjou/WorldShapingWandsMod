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
///   <item><b>SampleMode</b> — 12 cells across three sub-rows (A: 2
///   <c>Same as origin</c>; B-1: 6 object types; B-2: 4 domain extras +
///   paint channels). Drives <see cref="MagicWandReadConfig.ObjectType"/>.</item>
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

    private const float HorizontalPad      = 10f;
    private const float SectionGap         = 10f;
    private const float SectionHeaderHeight = 18f;
    private const float RowGap             = 4f;
    private const float BottomPad          = 12f;

    private const float SampleCellW = WandPanelBuilder.SmallIconBtnSize;
    private const float SampleCellH = WandPanelBuilder.SmallIconBtnSize;
    private const float SampleCellGap = 4f;

    private const float ContigCellW = WandPanelBuilder.SmallIconBtnSize;
    private const float ContigCellH = WandPanelBuilder.SmallIconBtnSize;
    private const float ContigCellGap = 4f;
    private const float IconDrawSize = 16f;

    /// <summary>
    /// Builds the body element (SampleMode 3-row grid + Contiguity row).
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

        // Section 1 sub-row layouts (cell index → (row, col, count-in-row))
        // Row A:   indices 0..1   (SameTile, SameWall)            → 2 cells
        // Row B-1: indices 2..7   (Solid..PlanterBox)             → 6 cells
        // Row B-2: indices 8..11  (Empty, Liquid, PaintTile, PaintWall) → 4 cells
        var rowGroups = new[]
        {
            new RowGroup(0, 2),
            new RowGroup(2, 6),
            new RowGroup(8, 4),
        };

        float bodyW = 6 * SampleCellW + 5 * SampleCellGap + HorizontalPad * 2f;
        float sampleSectionH = SectionHeaderHeight
            + 3 * SampleCellH + 2 * RowGap;
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
            float rowW = grp.Count * SampleCellW + (grp.Count - 1) * SampleCellGap;
            float rowLeft = (bodyW - rowW) * 0.5f;
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
            HAlign = 0.5f,
            IgnoresMouseInteraction = true,
        };
        contigHeader.Top.Set(yCursor, 0f);
        body.Append(contigHeader);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: SectionHeaderHeight,
            bottomPadding: 0f);

        float contigRowW = 3 * ContigCellW + 2 * ContigCellGap;
        float contigLeft = (bodyW - contigRowW) * 0.5f;
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

        // Initial visual sync (cells were constructed with their own
        // initial bit; this guarantees the row-wide invariant if the
        // config flips between Build() and the first Draw).
        applySampleVisual();
        applyContigVisual();

        return body;
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
