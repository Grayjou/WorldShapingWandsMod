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
/// [Full] [Half-H] [Half-V] [Q-H] [Q-V].
///
/// <para>Cell size comes from <see cref="WandPanelBuilder.SmallIconBtnSize"/>
/// and <see cref="WandPanelBuilder.SmallIconGap"/> so compact icon sizing
/// stays centralized across the UI. Icons load from
/// <c>Assets_Build/Icons/Slices/*</c>.</para>
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

    private static readonly string[] IconPaths =
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
        float totalWidth = 0f;
        for (int i = 0; i < CellCount; i++)
            totalWidth = LayoutSpacing.AddHorizontalSpace(totalWidth, CellWidth, i == 0 ? 0f : Gap);
        Width.Set(totalWidth, 0f);
        Height.Set(CellHeight, 0f);

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        for (int i = 0; i < CellCount; i++)
        {
            Asset<Texture2D> icon = mod.Assets.Request<Texture2D>(IconPaths[i], AssetRequestMode.ImmediateLoad);
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
    /// A single small icon cell in the slice picker.
    /// </summary>
    private class UISliceCell : UIElement
    {
        public bool Toggled { get; set; }

        private readonly Asset<Texture2D> _icon;
        private readonly string _tooltip;

        private static readonly Color ActiveBg = WandPanelTheme.Colors.ActiveGreen;
        private static readonly Color InactiveBg = WandPanelTheme.Colors.SliceGridInactive;

        public UISliceCell(Asset<Texture2D> icon, string tooltip)
        {
            _icon = icon;
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

            if (_icon?.Value != null)
            {
                Texture2D tex = _icon.Value;
                Vector2 pos = new(
                    rect.X + (rect.Width - tex.Width) / 2f,
                    rect.Y + (rect.Height - tex.Height) / 2f);
                spriteBatch.Draw(tex, pos, Color.White);
            }

            // Tooltip
            if (IsMouseHovering)
                Main.hoverItemName = _tooltip;
        }
    }
}
