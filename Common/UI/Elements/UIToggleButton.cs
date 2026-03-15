using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

public class UIToggleButton : UIElement
{
    private readonly UIText _text;

    public bool Toggled { get; set; }
    public bool IsRadio { get; set; } = false;
    public Color? TintColor { get; set; } = null;
    public Color OffColor { get; set; } = new Color(60, 60, 60);

    /// <summary>Tooltip text shown on hover. Null or empty = no tooltip.</summary>
    public string HoverText { get; set; }

    public event MouseEvent OnToggled;

    public UIToggleButton(string text, bool initialState = false)
    {
        Toggled = initialState;
        _text = new UIText(text, 0.85f);
        _text.HAlign = 0.5f;
        _text.VAlign = 0.5f;
        Append(_text);
    }

    public void SetText(string text) => _text.SetText(text);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        Color bgColor = Toggled 
            ? (TintColor ?? new Color(80, 200, 80)) 
            : OffColor;

        if (IsMouseHovering)
            bgColor = Color.Lerp(bgColor, Color.White, 0.15f);

        // Background
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

        // Border
        Color borderColor = Toggled ? Color.White * 0.5f : Color.Black * 0.5f;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, rect.Width, 1), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, 1, rect.Height), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), borderColor);

        if (IsMouseHovering && !string.IsNullOrEmpty(HoverText))
        {
            Main.hoverItemName = HoverText;
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (IsRadio && Toggled)
            return;

        Toggled = !Toggled;
        SoundEngine.PlaySound(SoundID.MenuTick);
        OnToggled?.Invoke(evt, this);
        base.LeftClick(evt);
    }
}