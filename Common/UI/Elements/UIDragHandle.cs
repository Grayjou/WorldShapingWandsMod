using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Small clickable widget that arms drag on its owning
/// <see cref="UIDraggablePanel"/>. The only element that initiates drag
/// when the panel's <see cref="UIDraggablePanel.DragPolicy"/> is
/// <see cref="DragPolicy.HandleOnly"/>.
/// (S6 §1 — Draggable Panel Unification.)
/// </summary>
public class UIDragHandle : UIElement
{
    private static readonly Color RestColor = new Color(80, 80, 80, 180);
    private static readonly Color HoverColor = new Color(140, 140, 140, 220);
    private static readonly Color DotColor = new Color(200, 200, 200, 255);

    /// <summary>
    /// Initializes a drag-handle widget.
    /// </summary>
    /// <param name="size">Square size in UI pixels.</param>
    public UIDragHandle(int size = 16)
    {
        Width.Set(size, 0f);
        Height.Set(size, 0f);
    }

    /// <summary>
    /// Arms drag on the nearest ancestor <see cref="UIDraggablePanel"/>.
    /// </summary>
    /// <param name="evt">Mouse-down event payload.</param>
    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        // Walk up the tree to find the owning UIDraggablePanel.
        for (UIElement n = Parent; n != null; n = n.Parent)
        {
            if (n is UIDraggablePanel panel)
            {
                panel.ArmDragFromHandle(evt.MousePosition);
                return;
            }
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        var rect = new Rectangle((int)dims.X, (int)dims.Y,
                                 (int)dims.Width, (int)dims.Height);

        // Background square.
        Color bg = IsMouseHovering ? HoverColor : RestColor;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

        // Simple 4-dot grid as placeholder move affordance.
        int dotSize = 2;
        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;
        int spread = 3;

        void Dot(int x, int y) =>
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x - dotSize / 2, y - dotSize / 2, dotSize, dotSize),
                DotColor);

        Dot(cx - spread, cy - spread);
        Dot(cx + spread, cy - spread);
        Dot(cx - spread, cy + spread);
        Dot(cx + spread, cy + spread);
    }
}