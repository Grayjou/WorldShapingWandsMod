using System.Collections.Generic;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Helpers;

/// <summary>
/// Direction in which buttons are stacked by <see cref="StackButtons"/>.
/// </summary>
public enum StackDirection
{
    /// <summary>Buttons are placed left-to-right (increasing X).</summary>
    LeftToRight,
    /// <summary>Buttons are placed right-to-left (decreasing X from the anchor).</summary>
    RightToLeft,
}

/// <summary>
/// Utility for laying out a list of UI elements in a horizontal stack.
/// Each element is sized to <paramref name="iconSize"/> and spaced by <paramref name="gap"/>.
/// The anchor position (HAlign/VAlign + Left/Top offsets) is applied to the first button;
/// subsequent buttons are offset automatically.
/// </summary>
public static class StackButtons
{
    /// <summary>
    /// Positions and appends <paramref name="buttons"/> to <paramref name="parent"/> in a
    /// horizontal stack, skipping any <c>null</c> entries.
    /// </summary>
    /// <param name="parent">The container to append buttons into.</param>
    /// <param name="buttons">Ordered list of buttons (nulls are skipped/invisible).</param>
    /// <param name="direction">Stack direction (right-to-left anchors from the right edge).</param>
    /// <param name="iconSize">Width and height of each button in pixels.</param>
    /// <param name="gap">Horizontal gap between adjacent buttons in pixels.</param>
    /// <param name="hAlign">HAlign of the anchor button (0 = left, 1 = right).</param>
    /// <param name="vAlign">VAlign of the anchor button (0 = top, 1 = bottom).</param>
    /// <param name="anchorLeft">Left pixel offset from the HAlign position for the anchor.</param>
    /// <param name="anchorTop">Top pixel offset from the VAlign position for each button.</param>
    public static void Stack(
        UIElement parent,
        IReadOnlyList<UIElement> buttons,
        StackDirection direction,
        float iconSize,
        float gap,
        float hAlign,
        float vAlign,
        float anchorLeft,
        float anchorTop)
    {
        float step = iconSize + gap;
        int slotIndex = 0;

        foreach (var btn in buttons)
        {
            if (btn == null)
                continue;

            float leftOffset = direction == StackDirection.RightToLeft
                ? anchorLeft - slotIndex * step
                : anchorLeft + slotIndex * step;

            btn.Width.Set(iconSize, 0f);
            btn.Height.Set(iconSize, 0f);
            btn.HAlign = hAlign;
            btn.VAlign = vAlign;
            btn.Left.Set(leftOffset, 0f);
            btn.Top.Set(anchorTop, 0f);

            parent.Append(btn);
            slotIndex++;
        }
    }
}
