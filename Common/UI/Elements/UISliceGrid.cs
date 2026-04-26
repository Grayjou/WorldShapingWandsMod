using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// A compact horizontal row of 3 buttons for selecting a SliceMode:
///   [Full] [Half-H] [Half-V]
/// Each button is a radio toggle — exactly one is active at any time.
/// </summary>
public class UISliceGrid : UIElement
{
    /// <summary>Currently selected slice mode.</summary>
    public SliceMode Value { get; private set; } = SliceMode.Full;

    /// <summary>Raised when the selection changes.</summary>
    public event Action<SliceMode> OnChanged;

    private readonly UISliceCell[] _cells = new UISliceCell[3];

    // Cell size and gap
    private const float CellWidth = 54f;
    private const float CellHeight = 28f;
    private const float Gap = 4f;

    private static readonly SliceMode[] Modes =
    {
        SliceMode.Full,
        SliceMode.HalfHorizontal,
        SliceMode.HalfVertical,
    };

    private static readonly string[] CellLabels =
    {
        "Full",
        "Half-H",
        "Half-V",
    };

    private static readonly string[] CellTooltips =
    {
        "Full shape (no slicing)",
        "Half shape — horizontal split (drag direction picks top/bottom)",
        "Half shape — vertical split (drag direction picks left/right)",
    };

    public UISliceGrid()
    {
        float totalWidth = CellWidth * 3 + Gap * 2;
        Width.Set(totalWidth, 0f);
        Height.Set(CellHeight, 0f);

        for (int i = 0; i < 3; i++)
        {
            var mode = Modes[i];
            var cell = new UISliceCell(CellLabels[i], CellTooltips[i]);
            cell.Width.Set(CellWidth, 0f);
            cell.Height.Set(CellHeight, 0f);
            cell.Left.Set(i * (CellWidth + Gap), 0f);
            cell.Top.Set(0f, 0f);
            cell.Toggled = (mode == SliceMode.Full); // default: Full selected

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
        for (int i = 0; i < 3; i++)
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
        for (int i = 0; i < 3; i++)
            _cells[i].Toggled = (Modes[i] == mode);
    }

    /// <summary>A single cell in the slice picker.</summary>
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

            // Label text — centered
            var font = Terraria.GameContent.FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(_label);
            float scale = 0.65f;
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
