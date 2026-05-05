using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using WorldShapingWandsMod.Common.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// A small image-based toggle button that shows a hover label.
/// Designed for compact selectors like slope icons.
/// </summary>
public class UIIconButton : UIElement
{
    private Asset<Texture2D> _texture;
    private string _hoverText;

    /// <summary>
    /// Optional alternate texture displayed only while the cursor hovers the
    /// button. Used by hover-icon-swap patterns (e.g. Mold-cell stencil hint,
    /// Magic Wand Read/Apply hover variants, ColorReplace 2-half preview).
    /// When both <see cref="HoverTexture"/> and <see cref="HoverTextureProvider"/>
    /// are non-null, the provider wins (lets callers vary by selection state).
    /// </summary>
    public Asset<Texture2D> HoverTexture { get; set; }

    /// <summary>
    /// Optional lambda returning the hover-state texture (overrides
    /// <see cref="HoverTexture"/> when non-null). May return null to fall
    /// back to the base icon.
    /// </summary>
    public System.Func<Asset<Texture2D>> HoverTextureProvider { get; set; }

    public bool Toggled { get; set; }
    public bool IsRadio { get; set; } = true;

    /// <summary>
    /// When true, the button is visually dimmed and clicks are ignored.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the tooltip text shown on hover.</summary>
    public string HoverText { get => _hoverText; set => _hoverText = value; }

    /// <summary>
    /// (S12 2026-04-29) Optional dynamic tooltip provider, evaluated every
    /// frame the button is hovered. When set and returning a non-empty
    /// string, takes precedence over <see cref="HoverText"/>. Used by the
    /// Mold cell to render "Mold{N} (from Wand of Molding)" without panel
    /// rebuilds. Mirrors the <see cref="HoverTextureProvider"/> pattern.
    /// </summary>
    public System.Func<string> HoverTextProvider { get; set; }

    /// <summary>
    /// When true and <see cref="IsRadio"/> is also true, clicking an already-toggled
    /// radio button will deselect it (set <see cref="Toggled"/> to false) and fire
    /// <see cref="OnToggled"/>. Default is false (standard radio: cannot deselect).
    /// </summary>
    public bool AllowDeselect { get; set; }

    /// <summary>Background color when selected.</summary>
    public Color ActiveColor { get; set; } = WandPanelTheme.Colors.ActiveGreen;

    /// <summary>Background color when not selected.</summary>
    public Color InactiveColor { get; set; } = WandPanelTheme.Colors.ElementInactive;

    /// <summary>
    /// When true, the button is a fire-and-forget action button with no toggle state.
    /// Clicks play the tick sound and fire <see cref="UIElement.OnLeftClick"/>, but
    /// <see cref="Toggled"/> is never changed and <see cref="OnToggled"/> is never raised.
    /// </summary>
    public bool IsAction { get; set; }

    /// <summary>
    /// Draws the SubUI affordance badge on this icon when true.
    /// </summary>
    public bool HasSubUIBadge { get; set; }

    /// <summary>
    /// Optional dynamic provider for SubUI badge visibility.
    /// When non-null, overrides <see cref="HasSubUIBadge"/>.
    /// </summary>
    public System.Func<bool> HasSubUIBadgeProvider { get; set; }

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

    /// <summary>
    /// Swaps the displayed icon texture. Used by tri-state cyclers (e.g. PaintSprayer source toggle)
    /// that present different artwork per state. Pass a null asset to keep the current texture.
    /// </summary>
    public void SetTexture(Asset<Texture2D> texture)
    {
        if (texture != null) _texture = texture;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        // Background
        Color bgColor = Disabled ? WandPanelTheme.Colors.Disabled : (Toggled ? ActiveColor : InactiveColor);
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
        Asset<Texture2D> drawAsset = _texture;
        if (IsMouseHovering && !Disabled)
        {
            Asset<Texture2D> hover = HoverTextureProvider?.Invoke() ?? HoverTexture;
            if (hover?.Value != null) drawAsset = hover;
        }
        if (drawAsset?.Value != null)
        {
            Texture2D tex = drawAsset.Value;
            Vector2 iconPos = new Vector2(
                rect.X + (rect.Width - tex.Width) / 2f,
                rect.Y + (rect.Height - tex.Height) / 2f
            );
            spriteBatch.Draw(tex, iconPos, Disabled ? Color.White * 0.4f : Color.White);
        }

        bool showBadge = HasSubUIBadgeProvider?.Invoke() ?? HasSubUIBadge;
        if (showBadge)
            BadgeAssets.DrawSubUIBadge(spriteBatch, rect, dimmed: Disabled);

        // Hover tooltip
        if (IsMouseHovering)
        {
            string txt = HoverTextProvider?.Invoke();
            if (string.IsNullOrEmpty(txt)) txt = _hoverText;
            if (!string.IsNullOrEmpty(txt))
                Main.hoverItemName = txt;
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (Disabled) return;

        if (IsAction)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            base.LeftClick(evt);
            return;
        }

        if (IsRadio && Toggled && !AllowDeselect)
            return;

        Toggled = !Toggled;
        SoundEngine.PlaySound(SoundID.MenuTick);
        OnToggled?.Invoke(evt, this);
        base.LeftClick(evt);
    }
}
