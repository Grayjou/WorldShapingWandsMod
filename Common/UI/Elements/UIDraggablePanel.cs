using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

public class UIDraggablePanel : UIPanel
{
    private bool _dragging = false;
    private Vector2 _dragOffset;
    private bool _hasBeenDragged = false;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        if (evt.Target != this && evt.Target is not UISectionTitle)
            return;

        _dragging = true;
        CalculatedStyle dims = GetDimensions();
        _dragOffset = new Vector2(
            evt.MousePosition.X - dims.X,
            evt.MousePosition.Y - dims.Y);
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        _dragging = false;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_dragging)
        {
            if (!_hasBeenDragged)
            {
                HAlign = 0f;
                VAlign = 0f;
                _hasBeenDragged = true;
            }

            float newX = Main.mouseX - _dragOffset.X;
            float newY = Main.mouseY - _dragOffset.Y;

            CalculatedStyle dims = GetDimensions();
            float minX = -(dims.Width - 40f);
            float maxX = Main.screenWidth - 40f;
            float minY = 0f;
            float maxY = Main.screenHeight - 40f;

            newX = MathHelper.Clamp(newX, minX, maxX);
            newY = MathHelper.Clamp(newY, minY, maxY);

            Left.Set(newX, 0f);
            Top.Set(newY, 0f);
            Recalculate();
        }

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    public void ResetPosition()
    {
        _hasBeenDragged = false;
        _dragging = false;
        HAlign = 0.5f;
        VAlign = 0.5f;
        Left.Set(0f, 0f);
        Top.Set(0f, 0f);
        Recalculate();
    }
}