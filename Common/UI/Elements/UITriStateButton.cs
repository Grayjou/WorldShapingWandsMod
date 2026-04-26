using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using WorldShapingWandsMod.Common.Enums;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// A self-contained tri-state toggle button that cycles through
/// Ignore → Apply → Remove states. Handles its own visual feedback
/// (background color, label text suffix) and fires <see cref="OnStateChanged"/>
/// when the user clicks to advance the cycle.
/// </summary>
public class UITriStateButton : UIElement
{
    private readonly UIText _text;

    /// <summary>Current tri-state value.</summary>
    public TriStateValue State { get; set; }

    /// <summary>Base display name (e.g. "Illuminant"). The state suffix is appended automatically.</summary>
    public string BaseName { get; set; }

    /// <summary>Background color when in Ignore state.</summary>
    public Color IgnoreColor { get; set; } = WandPanelTheme.Colors.TriStateIgnore;

    /// <summary>Background color when in Apply (On) state.</summary>
    public Color ApplyColor { get; set; } = WandPanelTheme.Colors.ActiveGreen;

    /// <summary>Background color when in Remove (Off) state.</summary>
    public Color RemoveColor { get; set; } = WandPanelTheme.Colors.TriStateRemove;

    /// <summary>Background color when not active (same as OffColor in UIToggleButton).</summary>
    public Color InactiveColor { get; set; } = WandPanelTheme.Colors.ElementInactive;

    /// <summary>
    /// When true, the button is visually dimmed and ignores clicks.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>Tooltip text shown on hover. Null or empty = no tooltip.</summary>
    public string HoverText { get; set; }

    /// <summary>Fired after the state changes. The new state is passed as the argument.</summary>
    public event Action<TriStateValue> OnStateChanged;

    /// <summary>Localization keys for state suffixes.</summary>
    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI";
    private static string L(string key) => Language.GetTextValue($"{UIPrefix}.{key}");

    public UITriStateButton(string baseName, TriStateValue initialState = TriStateValue.Ignore)
    {
        BaseName = baseName;
        State = initialState;
        _text = new UIText(FormatLabel(), 0.85f);
        _text.HAlign = 0.5f;
        _text.VAlign = 0.5f;
        Append(_text);
    }

    /// <summary>
    /// Formats the display label as "BaseName: Suffix".
    /// </summary>
    private string FormatLabel()
    {
        string suffix = State switch
        {
            TriStateValue.Ignore => L("Coating.Ignore"),
            TriStateValue.Apply => L("Coating.On"),
            TriStateValue.Remove => L("Coating.Off"),
            _ => L("Coating.Ignore")
        };
        return $"{BaseName}: {suffix}";
    }

    /// <summary>
    /// Returns the background color for the current state.
    /// </summary>
    private Color GetStateColor() => State switch
    {
        TriStateValue.Ignore => IgnoreColor,
        TriStateValue.Apply => ApplyColor,
        TriStateValue.Remove => RemoveColor,
        _ => InactiveColor
    };

    /// <summary>
    /// Updates the visual display to match the current state.
    /// Call after programmatically changing <see cref="State"/>.
    /// </summary>
    public void RefreshDisplay()
    {
        _text.SetText(FormatLabel());
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        Color bgColor = GetStateColor();

        if (Disabled)
            bgColor = bgColor * 0.35f;
        else if (IsMouseHovering)
            bgColor = Color.Lerp(bgColor, Color.White, 0.15f);

        // Background
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);

        // Border
        bool isActive = State.IsActive();
        Color borderColor = isActive ? Color.White * 0.5f : Color.Black * 0.5f;
        if (Disabled) borderColor *= 0.35f;
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
        if (Disabled)
            return;

        State = State.Next();
        RefreshDisplay();
        SoundEngine.PlaySound(SoundID.MenuTick);
        OnStateChanged?.Invoke(State);
        base.LeftClick(evt);
    }
}
