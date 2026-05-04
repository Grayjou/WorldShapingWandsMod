using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using WorldShapingWandsMod.Common.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (C-S3 2026-05-03 — <c>ImplementationTicket_ActuationFilter_MagicRead.md</c> §3.4)
/// Generic icon button that cycles through the values of an enum on left-click and
/// displays the icon registered for the current value. Designed for N-state toggles
/// such as ActuationFilter (Both / NonActuatedOnly / ActuatedOnly).
/// </summary>
public class TriStateIconButton<TEnum> : UIElement
    where TEnum : struct, Enum
{
    private readonly Func<TEnum> _getValue;
    private readonly Action<TEnum> _setValue;
    private readonly Func<TEnum, TEnum> _next;
    private readonly Func<TEnum, Asset<Texture2D>> _iconForValue;
    private readonly Func<TEnum, string> _tooltipForValue;
    private readonly Func<TEnum, Color> _stateColorForValue;

    private const float IconDrawSize = 16f;

    public TriStateIconButton(
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        Func<TEnum, TEnum> next,
        Func<TEnum, Asset<Texture2D>> iconForValue,
        Func<TEnum, string> tooltipForValue,
        Func<TEnum, Color> stateColorForValue = null)
    {
        _getValue = getValue;
        _setValue = setValue;
        _next = next;
        _iconForValue = iconForValue;
        _tooltipForValue = tooltipForValue;
        _stateColorForValue = stateColorForValue;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        var advanced = _next(_getValue());
        _setValue(advanced);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dims = GetDimensions();
        var rect = dims.ToRectangle();
        var current = _getValue();

        bool isHovering = IsMouseHovering;
        Color baseBg = _stateColorForValue?.Invoke(current) ?? WandPanelTheme.Colors.ElementInactive;
        Color bg = isHovering
            ? Color.Lerp(baseBg, WandPanelTheme.Colors.NeutralHover, 0.35f)
            : baseBg;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

        var icon = _iconForValue(current)?.Value;
        if (icon != null)
        {
            float ix = rect.X + (rect.Width  - IconDrawSize) * 0.5f;
            float iy = rect.Y + (rect.Height - IconDrawSize) * 0.5f;
            spriteBatch.Draw(icon, new Rectangle((int)ix, (int)iy, (int)IconDrawSize, (int)IconDrawSize), Color.White);
        }

        if (isHovering)
        {
            string tooltip = _tooltipForValue(current);
            if (!string.IsNullOrEmpty(tooltip))
                Main.instance.MouseText(tooltip);
        }
    }
}
