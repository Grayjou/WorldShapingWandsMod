using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// A small image-based toggle button that shows a hover label.
/// Designed for compact selectors like slope icons.
/// </summary>
public class UIIconButton : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private string _hoverText;

    public bool Toggled { get; set; }
    public bool IsRadio { get; set; } = true;

    /// <summary>
    /// When true, the button is visually dimmed and clicks are ignored.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the tooltip text shown on hover.</summary>
    public string HoverText { get => _hoverText; set => _hoverText = value; }

    /// <summary>
    /// When true and <see cref="IsRadio"/> is also true, clicking an already-toggled
    /// radio button will deselect it (set <see cref="Toggled"/> to false) and fire
    /// <see cref="OnToggled"/>. Default is false (standard radio: cannot deselect).
    /// </summary>
    public bool AllowDeselect { get; set; }

    /// <summary>Background color when selected.</summary>
    public Color ActiveColor { get; set; } = new Color(80, 200, 80);

    /// <summary>Background color when not selected.</summary>
    public Color InactiveColor { get; set; } = new Color(60, 60, 60);

    public event MouseEvent OnToggled;

    /// <param name="texture">The icon texture asset.</param>
    /// <param name="hoverText">Tooltip text shown on hover.</param>
    /// <param name="initialState">Initial toggle state.</param>
    public UIIconButton(Asset<Texture2D> texture, string hoverText, bool initialState = false)
    {
        _texture = texture;
        _hoverText = hoverText;
        Toggled = initialState;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        // Background
        Color bgColor = Disabled ? new Color(40, 40, 40) : (Toggled ? ActiveColor : InactiveColor);
        if (IsMouseHovering && !Disabled)
            bgColor = Color.Lerp(bgColor, Color.White, 0.2f);

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

        // Border
        Color borderColor = Toggled ? Color.White * 0.6f : Color.Black * 0.4f;
        int bw = 1;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, rect.Width, bw), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Bottom - bw, rect.Width, bw), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.X, rect.Y, bw, rect.Height), borderColor);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle(rect.Right - bw, rect.Y, bw, rect.Height), borderColor);

        // Icon — draw centered within the button
        if (_texture?.Value != null)
        {
            Texture2D tex = _texture.Value;
            Vector2 iconPos = new Vector2(
                rect.X + (rect.Width - tex.Width) / 2f,
                rect.Y + (rect.Height - tex.Height) / 2f
            );
            spriteBatch.Draw(tex, iconPos, Disabled ? Color.White * 0.4f : Color.White);
        }

        // Hover tooltip
        if (IsMouseHovering && !string.IsNullOrEmpty(_hoverText))
        {
            Main.hoverItemName = _hoverText;
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (Disabled) return;

        if (IsRadio && Toggled && !AllowDeselect)
            return;

        Toggled = !Toggled;
        SoundEngine.PlaySound(SoundID.MenuTick);
        OnToggled?.Invoke(evt, this);
        base.LeftClick(evt);
    }
}
