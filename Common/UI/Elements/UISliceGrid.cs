using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// A compact horizontal row of 5 small icon buttons for selecting a SliceMode:
///   [Full] [Half-H] [Half-V] [Q-H] [Q-V]
/// Each button is a radio toggle \u2014 exactly one is active at any time.
///
/// <para>(S14 2026-04-29; per GrayJou S14 verbatim: *"I want to turn the
/// shape slices buttons into icon buttons. Very small ones. Please make
/// sure the size of the two variants of button icons we have are
/// centralized."*) Cell size now reads from
/// <see cref="WandPanelBuilder.SmallIconBtnSize"/> +
/// <see cref="WandPanelBuilder.SmallIconGap"/> so the two centralised icon
/// button sizes (36 = <see cref="WandPanelBuilder.IconBtnSize"/>;
/// 22 = <see cref="WandPanelBuilder.SmallIconBtnSize"/>) remain the
/// single source of truth for every cell in the mod. Pre-S14 the cells
/// hard-coded a bespoke 44\u00d728 size that bypassed both constants.</para>
///
/// <para><b>Asset status</b>: Slice icon assets are still pending (see
/// <c>dev_notes/dev_tasks/pending_assets.md</c> \u00a72). Until they ship
/// each cell renders a small text label (<c>"Full"</c> / <c>"H"</c> /
/// <c>"V"</c> / <c>"qH"</c> / <c>"qV"</c>) inside the small cell, plus the
/// full descriptive tooltip on hover. Swapping in real icons is a one-line
/// change at the per-cell construction site (see TODO marker).</para>
/// </summary>
public class UISliceGrid : UIElement
{
    /// <summary>Currently selected slice mode.</summary>
    public SliceMode Value { get; private set; } = SliceMode.Full;

    /// <summary>Raised when the selection changes.</summary>
    public event Action<SliceMode> OnChanged;

    // (S12 2026-04-29; HalfShapeQuickSlice.md \u00a76) Grid grows from 3 cells
    // to 5 cells to host SliceMode.QuickHalfHorizontal + QuickHalfVertical.
    // (S14 2026-04-29) Cell size now sourced from the centralised
    // SmallIconBtnSize / SmallIconGap constants on WandPanelBuilder so
    // every "small icon button" in the mod tracks one source of truth.
    private const int CellCount = 5;
    private readonly UISliceCell[] _cells = new UISliceCell[CellCount];

    // Cell size and gap \u2014 centralised via WandPanelBuilder.
    private const float CellWidth  = WandPanelBuilder.SmallIconBtnSize; // 22f
    private const float CellHeight = WandPanelBuilder.SmallIconBtnSize; // 22f (square)
    private const float Gap        = WandPanelBuilder.SmallIconGap;     // 4f

    private static readonly SliceMode[] Modes =
    {
        SliceMode.Full,
        SliceMode.HalfHorizontal,
        SliceMode.HalfVertical,
        SliceMode.QuickHalfHorizontal,
        SliceMode.QuickHalfVertical,
    };

    // (S14 2026-04-29) Short labels rendered inside the small cells until
    // the dedicated 16\u00d716 slice icons ship. See pending_assets.md \u00a72.
    // TODO: pending Slices/* assets \u2014 swap _cellLabels rendering for
    // texture-asset rendering once the 5 PNGs land.
    private static readonly string[] CellLabels =
    {
        "Assets_Build/Icons/Slices/SliceFull",
        "Assets_Build/Icons/Slices/SliceHalfHorizontal",
        "Assets_Build/Icons/Slices/SliceHalfVertical",
        "Assets_Build/Icons/Slices/SliceQuickHalfHorizontal",
        "Assets_Build/Icons/Slices/SliceQuickHalfVertical",
    };

    private static readonly string[] CellTooltips =
    {
        "Full shape (no slicing)",
        "Half shape \u2014 horizontal split (drag bbox = full shape; drag direction picks top/bottom)",
        "Half shape \u2014 vertical split (drag bbox = full shape; drag direction picks left/right)",
        "Quick half \u2014 horizontal split (drag bbox = the half itself; saves you dragging twice as far)",
        "Quick half \u2014 vertical split (drag bbox = the half itself; saves you dragging twice as far)",
    };

    public UISliceGrid()
    {
        float totalWidth = CellWidth * CellCount + Gap * (CellCount - 1);
        Width.Set(totalWidth, 0f);
        Height.Set(CellHeight, 0f);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        for (int i = 0; i < CellCount; i++)
        {
            Asset<Texture2D> icon = mod.Assets.Request<Texture2D>(CellLabels[i], AssetRequestMode.ImmediateLoad);
            var cell = new UISliceCell(icon, CellTooltips[i]);
            cell.Width.Set(CellWidth, 0f);
            cell.Height.Set(CellHeight, 0f);
            cell.Left.Set(i * (CellWidth + Gap), 0f);
            cell.Top.Set(0f, 0f);
            cell.Toggled = (Modes[i] == SliceMode.Full);

            int capturedIndex = i;
            cell.OnLeftClick += (_, _) => SelectCell(capturedIndex);
            Append(cell);

            _cells[i] = cell;
        }
    }

    private void SelectCell(int index)
    {
        var newMode = Modes[index];
        if (newMode == Value) return;

        Value = newMode;
        SoundEngine.PlaySound(SoundID.MenuTick);

        // Update radio states
        for (int i = 0; i < CellCount; i++)
            _cells[i].Toggled = (i == index);

        OnChanged?.Invoke(Value);
    }

    /// <summary>
    /// Externally set the value (e.g. when syncing from settings).
    /// Does NOT fire OnChanged.
    /// </summary>
    public void SetValue(SliceMode mode)
    {
        Value = mode;
        for (int i = 0; i < CellCount; i++)
            _cells[i].Toggled = (Modes[i] == mode);
    }

    /// <summary>
    /// A single small cell in the slice picker. Renders identically to a
    /// <see cref="UIIconButton"/> in the SmallIconBtn-size family
    /// (background \u2192 border \u2192 centred content \u2192 hover lerp), but
    /// substitutes a short text label for the missing slice icon asset
    /// until the dedicated PNGs ship (see <c>pending_assets.md</c> \u00a72).
    /// </summary>
    private class UISliceCell : UIElement
    {
        public bool Toggled { get; set; }

        private readonly string _label;
        private readonly string _tooltip;

        private static readonly Color ActiveBg = WandPanelTheme.Colors.ActiveGreen;
        private static readonly Color InactiveBg = WandPanelTheme.Colors.SliceGridInactive;

        public UISliceCell(string label, string tooltip)
        {
            _label = label;
            _tooltip = tooltip;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            Rectangle rect = dims.ToRectangle();

            // Background
            Color bg = Toggled ? ActiveBg : InactiveBg;
            if (IsMouseHovering) bg = Color.Lerp(bg, Color.White, 0.2f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

            // Border
            Color border = Toggled ? Color.White * 0.5f : Color.Black * 0.4f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

            // Label text \u2014 small, centered. (Asset placeholder; see class
            // docblock + pending_assets.md \u00a72.) Scale tuned to fit the
            // SmallIconBtnSize cell (22\u00d722) without clipping the wider
            // 2-char labels ("qH", "qV").
            var font = Terraria.GameContent.FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(_label);
            float scale = 0.55f;
            Vector2 pos = new Vector2(
                rect.X + (rect.Width - textSize.X * scale) / 2f,
                rect.Y + (rect.Height - textSize.Y * scale) / 2f
            );
            Utils.DrawBorderString(spriteBatch, _label, pos, Color.White, scale);

            // Tooltip
            if (IsMouseHovering)
                Main.hoverItemName = _tooltip;
        }
    }
}
