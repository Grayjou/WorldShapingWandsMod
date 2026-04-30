using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
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
    /// <summary>
    /// Vanilla PaintID for the Negative paint, which inverts the rendered
    /// tile colour. Calling out the literal here so the swatch-draw branch
    /// below does not have to reference <see cref="PaintPalette"/> by index.
    /// </summary>
    private const byte NegativePaintIndex = 30;

    // Lazy-loaded baked icons for the Negative paint (S13 2026-04-28). The
    // flat magenta swatch read as "just another pink chip"; the baked glyph
    // (cyan + magenta quadrants + central crosshair) communicates the
    // colour-inversion concept far better. Two sizes ship so we can pick
    // the closer-fit at draw time and avoid linear-filter blur on the
    // hand-tuned 23×23 design. See generate_negative_paint_icon.py.
    private static Asset<Texture2D> _negativeIcon1x;
    private static Asset<Texture2D> _negativeIcon2x;

    private static Asset<Texture2D> NegativeIcon1x
        => _negativeIcon1x ??= ModContent.Request<Texture2D>(
            "WorldShapingWandsMod/Assets_Build/Icons/Misc/PaintNegative",
            AssetRequestMode.ImmediateLoad);

    private static Asset<Texture2D> NegativeIcon2x
        => _negativeIcon2x ??= ModContent.Request<Texture2D>(
            "WorldShapingWandsMod/Assets_Build/Icons/Misc/PaintNegative_2x",
            AssetRequestMode.ImmediateLoad);

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

        if (_colorIndex == NegativePaintIndex)
        {
            // Replace the flat magenta fill with the baked Negative glyph.
            // Pick the 2× source whenever the swatch is at least 30 px so
            // the larger pickers (e.g. Color Replace 26 px swatches @ 1.x
            // UI scale, ≥30 px effective) sample from the higher-fidelity
            // copy. Below 30 px we read from the native 23×23 source.
            int targetEdge = System.Math.Min(rect.Width, rect.Height);
            var icon = (targetEdge >= 30 ? NegativeIcon2x : NegativeIcon1x).Value;
            spriteBatch.Draw(icon, rect, Color.White);
        }

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
