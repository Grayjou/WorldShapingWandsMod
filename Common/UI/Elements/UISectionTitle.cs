using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

public class UISectionTitle : UIElement
{
    private readonly UIText _text;

    public UISectionTitle(string text)
    {
        _text = new UIText(text, 0.9f, false);
        _text.HAlign = 0.0f;
        _text.VAlign = 0.5f;
        _text.IgnoresMouseInteraction = true;
        Append(_text);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle lineRect = new Rectangle(
            (int)dims.X,
            (int)(dims.Y + dims.Height - 2),
            (int)dims.Width,
            1);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineRect, Color.White * 0.3f);
    }
}