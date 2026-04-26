using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using WorldShapingWandsMod.Common.Drawing;
using WorldShapingWandsMod.Content.Items;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Single paint-color swatch button for the Wand of Coating's PaintColor grid.
/// Renders a flat color rect with selection / hover ring, plus per-state
/// glyphs for the two sentinel slots (slot 0 = "Erase paint" red bars; slot 31
/// = <see cref="WandOfCoatingBase.IgnorePaintColor"/> grey dash).
/// </summary>
/// <remarks>
/// Promoted from a private nested class inside <c>CoatingSettingsPanel</c> to
/// public top-level in S6 2026-04-24 (W-S6-3) so that
/// <see cref="CoatingPaintColorGrid.Build"/> can construct it from outside the
/// panel. Behaviour is byte-identical to the pre-promotion form; only the
/// access modifier (private→public) and the file location changed.
///
/// <para>The S+2 popout migration (next session) will re-parent the
/// <see cref="CoatingPaintColorGrid"/>'s container into a
/// <c>CollapsedPopoutHost</c>; this swatch type travels with the container
/// without further modification.</para>
/// </remarks>
public class UIPaintColorButton : UIElement
{
    private readonly byte _colorIndex;
    private readonly Color _color;
    private readonly string _name;

    /// <summary>True when this swatch represents the player's currently chosen paint.</summary>
    public bool IsSelected { get; set; }

    /// <summary>The vanilla paint byte this swatch commits when clicked.</summary>
    public byte ColorIndex => _colorIndex;

    public UIPaintColorButton(byte colorIndex, Color color, string name)
    {
        _colorIndex = colorIndex;
        _color = color;
        _name = name;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dims = GetDimensions();
        var rect = dims.ToRectangle();

        Color bg = _colorIndex == 0 || _colorIndex == WandOfCoatingBase.IgnorePaintColor
            ? WandPanelTheme.Colors.Disabled : _color;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

        if (_colorIndex == 0)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, 2),
                Color.Red * 0.85f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X + 3, rect.Bottom - 5, rect.Width - 6, 2),
                Color.Red * 0.85f);
        }

        if (_colorIndex == WandOfCoatingBase.IgnorePaintColor)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X + 4, rect.Y + rect.Height / 2 - 1, rect.Width - 8, 2),
                Color.LightGray * 0.9f);
        }

        if (IsSelected)
        {
            int t = 2;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, t), Color.White);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), Color.White);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, t, rect.Height), Color.White);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), Color.White);
        }
        else if (IsMouseHovering)
        {
            int t = 1;
            Color hover = Color.LightGray * 0.7f;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, rect.Width, t), hover);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), hover);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.X, rect.Y, t, rect.Height), hover);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), hover);
        }

        if (IsMouseHovering && Main.playerInventory == false)
        {
            Main.hoverItemName = _name;
        }
    }
}
