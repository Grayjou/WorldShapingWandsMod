using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.Elements.Builders;

/// <summary>
/// (S5 2026-04-29 — SubUI Architecture Phase B) Companion-builder for the
/// Color Replace configuration SubPanel body. Extracted from the prior
/// <c>ColorReplaceConfigSubPanel</c> static factory per the C-2
/// ratification (Idiom B = factory-per-instance) + the architecture
/// doc's "Companion Builder Rule": the previous file ran ~358 LOC of
/// child-element wiring, well above the ~80-line threshold, so it
/// graduates from "inline in the factory" to a dedicated builder class.
///
/// <para><b>Identity + title constants</b> live here so any consumer
/// needing the stable string for the <c>OpenWandSubPanel</c> "already-open?"
/// check (currently <c>CoatingSettingsPanel</c>) reads them from the
/// builder.</para>
///
/// <para><b>Behaviour preserved verbatim</b> from the S8/S9/S10/S11/S13/S14
/// originals. All layout constants, click handlers, channel-row
/// behaviour, and initial-selection sync are byte-for-byte identical;
/// only the wiring lives in a different file. The <see cref="WandSubPanel"/>
/// instantiation + lifecycle metadata + the β-model
/// <see cref="WandSubPanel.OwnerVisibilityCheck"/> lambda are now declared
/// in <see cref="WandSubPanelFactories.CreateColorReplaceConfig"/>.</para>
/// </summary>
internal static class ColorReplaceConfigBuilder
{
    public const string IdentityKey = "WandOfCoating.ColorReplaceConfig";
    public const string TitleKey = "Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.SubUITitle";

    // S9 widening: bigger swatch + larger column count headroom. The picker
    // is the visual focus of this SubUI, so it earns the breath.
    // S10 padding fix: bumped horizontal pad 6 → 16 + explicit 20px bottom pad.
    // S14 padding revision: BottomPad 20 → 26 (+6 px breath per GrayJou).
    private const int   PickerColumns    = 8;
    private const float SwatchSize       = 26f;
    private const float SwatchGap        = 5f;
    private const float RowLabelHeight   = 18f;
    private const float SectionGap       = 8f;
    private const float HorizontalPad    = 16f;
    private const float BottomPad        = 26f;
    private const float ChannelRowHeight = 30f;
    private const float ChannelGap       = 8f;

    /// <summary>
    /// Builds the body element (channel row + source picker + target picker)
    /// and returns it. The factory is responsible for wrapping this in a
    /// <see cref="WandSubPanel"/> with the appropriate lifecycle metadata.
    /// </summary>
    /// <param name="onChanged">Host callback fired after each swatch click
    /// or channel change. Receives the (sourcePaint, targetPaint) tuple
    /// after the change so the host can refresh its baked icon + tooltip.
    /// Never null — caller must subscribe.</param>
    public static UIElement BuildBody(Action<byte, byte> onChanged)
    {
        if (onChanged is null) throw new ArgumentNullException(nameof(onChanged));

        var initial = ReadCurrentSettings();
        byte initialSource = initial.source;
        byte initialTarget = initial.target;
        ColorReplaceChannel initialChannel = initial.channel;

        // S10: lambdas need to re-apply IsSelected on the swatch arrays after
        // each click, but the arrays are out parameters of Build() — not yet
        // available when the lambda is constructed. Declare the holding refs
        // up front so the closures can write into them once Build() returns.
        UIPaintColorButton[] _lateSourceButtons = null;
        UIPaintColorButton[] _lateTargetButtons = null;

        var sourceGrid = CoatingPaintColorGrid.Build(
            columns: PickerColumns,
            containerInnerWidth: 0f, // ignored since v1.1
            onSelect: paint =>
            {
                var s = LocateSettings();
                if (s == null) return;
                s.ColorReplaceSource = paint;
                ApplyInitialSelected(_lateSourceButtons, paint);
                onChanged(s.ColorReplaceSource, s.ColorReplaceTarget);
            },
            out var sourceButtons,
            swatchSize: SwatchSize,
            swatchGap: SwatchGap);

        var targetGrid = CoatingPaintColorGrid.Build(
            columns: PickerColumns,
            containerInnerWidth: 0f,
            onSelect: paint =>
            {
                var s = LocateSettings();
                if (s == null) return;
                s.ColorReplaceTarget = paint;
                ApplyInitialSelected(_lateTargetButtons, paint);
                onChanged(s.ColorReplaceSource, s.ColorReplaceTarget);
            },
            out var targetButtons,
            swatchSize: SwatchSize,
            swatchGap: SwatchGap);

        _lateSourceButtons = sourceButtons;
        _lateTargetButtons = targetButtons;

        ApplyInitialSelected(sourceButtons, initialSource);
        ApplyInitialSelected(targetButtons, initialTarget);

        // ── Compose body: channel row + two labelled picker sections ──────
        float gridIntrinsicW = sourceGrid.Width.Pixels;
        float gridIntrinsicH = sourceGrid.Height.Pixels;
        float bodyW = gridIntrinsicW + HorizontalPad * 2f;
        float bodyH = ChannelRowHeight + ChannelGap
                    + (RowLabelHeight + gridIntrinsicH) * 2 + SectionGap
                    + BottomPad;

        var body = new UIElement();
        body.Width.Set(bodyW, 0f);
        body.Height.Set(bodyH, 0f);

        // -- Channel row (Tile / Wall radio) --
        var channelRow = BuildChannelRow(initialChannel, out var tileBtn, out var wallBtn);
        channelRow.Top.Set(0f, 0f);
        channelRow.HAlign = 0.5f;
        body.Append(channelRow);

        tileBtn.OnLeftClick += (_, _) =>
        {
            var s = LocateSettings();
            if (s == null) return;
            s.ColorReplaceChannel = ColorReplaceChannel.Tile;
            tileBtn.IsSelected = true;
            wallBtn.IsSelected = false;
            onChanged(s.ColorReplaceSource, s.ColorReplaceTarget);
        };
        wallBtn.OnLeftClick += (_, _) =>
        {
            var s = LocateSettings();
            if (s == null) return;
            s.ColorReplaceChannel = ColorReplaceChannel.Wall;
            tileBtn.IsSelected = false;
            wallBtn.IsSelected = true;
            onChanged(s.ColorReplaceSource, s.ColorReplaceTarget);
        };

        float yCursor = ChannelRowHeight + ChannelGap;

        var srcLabel = new UIText(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.SourceLabel"),
            0.85f);
        srcLabel.Top.Set(yCursor, 0f);
        srcLabel.HAlign = 0.5f;
        body.Append(srcLabel);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: RowLabelHeight,
            bottomPadding: 0f);

        sourceGrid.Top.Set(yCursor, 0f);
        sourceGrid.HAlign = 0.5f;
        body.Append(sourceGrid);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: gridIntrinsicH,
            bottomPadding: SectionGap);

        var tgtLabel = new UIText(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.TargetLabel"),
            0.85f);
        tgtLabel.Top.Set(yCursor, 0f);
        tgtLabel.HAlign = 0.5f;
        body.Append(tgtLabel);
        yCursor = LayoutSpacing.AddVerticalSpace(
            currentSize: yCursor,
            elementSize: RowLabelHeight,
            bottomPadding: 0f);

        targetGrid.Top.Set(yCursor, 0f);
        targetGrid.HAlign = 0.5f;
        body.Append(targetGrid);

        return body;
    }

    // ── Helpers (verbatim from the original ColorReplaceConfigSubPanel) ────

    private static UIElement BuildChannelRow(
        ColorReplaceChannel initial,
        out ChannelRadioButton tileBtn,
        out ChannelRadioButton wallBtn)
    {
        const float CellW = 90f;
        const float CellGap = 10f;

        var row = new UIElement();
        row.Width.Set(CellW * 2 + CellGap, 0f);
        row.Height.Set(ChannelRowHeight, 0f);

        tileBtn = new ChannelRadioButton(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.ChannelTile"),
            initial == ColorReplaceChannel.Tile);
        tileBtn.Width.Set(CellW, 0f);
        tileBtn.Height.Set(ChannelRowHeight, 0f);
        tileBtn.Left.Set(0f, 0f);
        tileBtn.Top.Set(0f, 0f);
        row.Append(tileBtn);

        wallBtn = new ChannelRadioButton(
            Language.GetTextValue("Mods.WorldShapingWandsMod.UI.Coating.ColorReplace.ChannelWall"),
            initial == ColorReplaceChannel.Wall);
        wallBtn.Width.Set(CellW, 0f);
        wallBtn.Height.Set(ChannelRowHeight, 0f);
        wallBtn.Left.Set(CellW + CellGap, 0f);
        wallBtn.Top.Set(0f, 0f);
        row.Append(wallBtn);

        return row;
    }

    private static (byte source, byte target, ColorReplaceChannel channel) ReadCurrentSettings()
    {
        var s = LocateSettings();
        if (s == null) return (255, 255, ColorReplaceChannel.Tile);
        return (s.ColorReplaceSource, s.ColorReplaceTarget, s.ColorReplaceChannel);
    }

    private static Settings.WandOfCoatingSettings LocateSettings()
    {
        var p = Main.LocalPlayer?.GetModPlayer<WandPlayer>();
        return p?.CoatingSettings;
    }

    private static void ApplyInitialSelected(UIPaintColorButton[] buttons, byte current)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            byte cellByte = (byte)(i < 31 ? i : 255 /* Ignore */);
            buttons[i].IsSelected = cellByte == current;
        }
    }

    /// <summary>
    /// Lightweight text-cell radio button used for the Tile/Wall channel row.
    /// </summary>
    private class ChannelRadioButton : UIElement
    {
        public bool IsSelected { get; set; }
        private readonly string _label;

        public ChannelRadioButton(string label, bool initial)
        {
            _label = label;
            IsSelected = initial;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var dims = GetDimensions();
            var rect = dims.ToRectangle();
            Color bg = IsSelected
                ? WandPanelTheme.Colors.ActiveGreen
                : (IsMouseHovering ? WandPanelTheme.Colors.NeutralHover : WandPanelTheme.Colors.ElementInactive);
            spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, rect, bg);

            // (S11/S13 nudge: MouseText vertical-centre lands slightly above
            // optical centre on a tall row; +2f restores the visual centre.)
            var font = Terraria.GameContent.FontAssets.MouseText.Value;
            var size = font.MeasureString(_label);
            float tx = rect.X + (rect.Width  - size.X) * 0.5f;
            float ty = rect.Y + (rect.Height - size.Y) * 0.5f + 2f;
            Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, font, _label,
                new Vector2(tx, ty), Color.White, 0f, Vector2.Zero, Vector2.One);
        }
    }
}
