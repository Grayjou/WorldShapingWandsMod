using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicWiring.UI.Elements;

public class UIToggleButton : UIElement
{
    private readonly UIText _text;
    public bool Toggled { get; set; }

    /// <summary>
    /// When true, clicking an already-active button does nothing.
    /// Use this for Mode and Shape groups where exactly one must be selected.
    /// </summary>
    public bool IsRadio { get; set; } = false;

    /// <summary>
    /// Optional tint color applied when the button is active.
    /// For wire buttons, set this to the wire's color.
    /// If null, uses default green/gray.
    /// </summary>
    public Color? TintColor { get; set; } = null;

    public event MouseEvent OnToggled;

    public UIToggleButton(string text, bool initialState)
    {
        Toggled = initialState;
        _text = new UIText(text);
        _text.HAlign = 0.5f;
        _text.VAlign = 0.5f;
        Append(_text);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        // Determine background color
        Color bgColor;
        if (Toggled)
            bgColor = TintColor ?? new Color(80, 200, 80); // Active: tint or green
        else
            bgColor = new Color(60, 60, 60); // Inactive: dark gray

        // Hover highlight
        if (IsMouseHovering)
            bgColor = Color.Lerp(bgColor, Color.White, 0.15f);

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

        // Draw a 1px border
        Color borderColor = Toggled ? Color.White * 0.5f : Color.Black * 0.5f;
        // Top
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, rect.Width, 1), borderColor);
        // Bottom
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), borderColor);
        // Left
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, 1, rect.Height), borderColor);
        // Right
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), borderColor);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        // Radio mode: can't deselect the active option
        if (IsRadio && Toggled)
            return;

        Toggled = !Toggled;
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
        OnToggled?.Invoke(evt, this);
        base.LeftClick(evt);
    }
}