using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.UI;

internal static class BadgeAssets
{
    public const int BadgeInset = 3;

    private static Asset<Texture2D> _subUiBadge6;
    private static Asset<Texture2D> _subUiBadge5;

    private static Asset<Texture2D> RequestFirstExisting(Mod mod, params string[] paths)
    {
        foreach (var path in paths)
        {
            string fullPath = mod.Name + "/" + path;
            if (ModContent.HasAsset(fullPath))
                return ModContent.Request<Texture2D>(fullPath, AssetRequestMode.ImmediateLoad);
        }

        return null;
    }

    private static Asset<Texture2D> SubUIBadge6
    {
        get
        {
            if (_subUiBadge6 == null)
            {
                var mod = ModContent.GetInstance<WorldShapingWandsMod>();
                _subUiBadge6 = RequestFirstExisting(mod,
                    "Assets_Build/Icons/Misc/HasSubUI",
                    "Assets/Icons/Misc/HasSubUI");
            }
            return _subUiBadge6;
        }
    }

    private static Asset<Texture2D> SubUIBadge5
    {
        get
        {
            if (_subUiBadge5 == null)
            {
                var mod = ModContent.GetInstance<WorldShapingWandsMod>();
                _subUiBadge5 = RequestFirstExisting(mod,
                    "Assets_Build/Icons/Misc/HasSubUI5x5",
                    "Assets/Icons/Misc/HasSubUI5x5");
            }
            return _subUiBadge5;
        }
    }

    public static Asset<Texture2D> GetSubUIBadge(int iconSize)
    {
        if (iconSize >= 28)
            return SubUIBadge6;

        return SubUIBadge5 ?? SubUIBadge6;
    }

    public enum BadgeCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    /// <summary>
    /// Returns the top-left draw position for a badge of <paramref name="badgeSize"/>
    /// placed at <paramref name="corner"/> inside <paramref name="buttonRect"/>,
    /// inset by <see cref="BadgeInset"/> pixels from the corner.
    /// </summary>
    public static Vector2 BadgeCornerOffset(Rectangle buttonRect, int badgeSize, BadgeCorner corner)
    {
        int inset = BadgeInset;
        return corner switch
        {
            BadgeCorner.TopLeft     => new(buttonRect.X + inset,                         buttonRect.Y + inset),
            BadgeCorner.TopRight    => new(buttonRect.Right - badgeSize - inset,          buttonRect.Y + inset),
            BadgeCorner.BottomLeft  => new(buttonRect.X + inset,                         buttonRect.Bottom - badgeSize - inset),
            BadgeCorner.BottomRight => new(buttonRect.Right - badgeSize - inset,          buttonRect.Bottom - badgeSize - inset),
            _                      => new(buttonRect.Right - badgeSize - inset,          buttonRect.Bottom - badgeSize - inset),
        };
    }

    /// <summary>
    /// Draws <paramref name="tex"/> at <paramref name="corner"/> of
    /// <paramref name="buttonRect"/> with a small dark backing rectangle, inset
    /// by <see cref="BadgeInset"/> from the corner.
    /// </summary>
    public static void DrawWithInset(SpriteBatch spriteBatch, Texture2D tex, Rectangle buttonRect,
        BadgeCorner corner, Color tint)
    {
        var pos = BadgeCornerOffset(buttonRect, tex.Width, corner);
        var backingRect = new Rectangle((int)pos.X - 1, (int)pos.Y - 1, tex.Width + 2, tex.Height + 2);
        float backingAlpha = tint.A / 255f * 0.35f;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, backingRect, Color.Black * backingAlpha);
        spriteBatch.Draw(tex, pos, tint);
    }

    private static void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public static void DrawSubUIBadge(SpriteBatch spriteBatch, Rectangle buttonRect, bool dimmed = false)
    {
        int iconSize = buttonRect.Width < buttonRect.Height ? buttonRect.Width : buttonRect.Height;
        var badgeAsset = GetSubUIBadge(iconSize);
        var tex = badgeAsset?.Value;
        if (tex == null)
            return;

        Color tint = dimmed ? Color.White * 0.4f : Color.White;
        DrawWithInset(spriteBatch, tex, buttonRect, BadgeCorner.BottomRight, tint);

#if DEBUG
        bool debugRects = Main.keyState.IsKeyDown(Keys.F8);
        if (debugRects)
        {
            var pos = BadgeCornerOffset(buttonRect, tex.Width, BadgeCorner.BottomRight);
            var badgeRect = new Rectangle((int)pos.X, (int)pos.Y, tex.Width, tex.Height);
            var backingRect = new Rectangle((int)pos.X - 1, (int)pos.Y - 1, tex.Width + 2, tex.Height + 2);
            DrawRectOutline(spriteBatch, buttonRect, Color.Cyan * 0.95f, 2);
            DrawRectOutline(spriteBatch, backingRect, Color.Orange * 0.95f, 1);
            DrawRectOutline(spriteBatch, badgeRect, Color.Magenta * 0.95f, 1);
            var center = new Rectangle((int)pos.X + tex.Width / 2 - 1, (int)pos.Y + tex.Height / 2 - 1, 3, 3);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, center, Color.Lime * 0.95f);
        }
#endif
    }
}
