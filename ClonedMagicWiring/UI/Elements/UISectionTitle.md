using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicWiring.UI.Elements;

public class UISectionTitle : UIElement
{
    private readonly UIText _text;

    public UISectionTitle(string text)
    {
        // Normal font, slightly larger scale — NOT large:true which uses the huge font
        _text = new UIText(text, 0.9f, false);
        _text.HAlign = 0.0f;
        _text.VAlign = 0.5f;

        // Make the UIText ignore mouse interactions so clicks
        // fall through to this UISectionTitle element.
        // This lets UIDraggablePanel detect that the click
        // target is a UISectionTitle (non-interactive) and
        // allows dragging from section headers.
        _text.IgnoresMouseInteraction = true;

        Append(_text);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        // Draw a subtle underline below the title
        CalculatedStyle dims = GetDimensions();
        Rectangle lineRect = new Rectangle(
            (int)dims.X,
            (int)(dims.Y + dims.Height - 2),
            (int)dims.Width,
            1);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineRect, Color.White * 0.3f);
    }
}