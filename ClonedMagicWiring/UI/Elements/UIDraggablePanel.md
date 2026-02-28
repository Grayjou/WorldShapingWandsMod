using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicWiring.UI.Elements;

/// <summary>
/// A UIPanel that can be dragged by clicking and holding on its background.
/// Clicking child elements (buttons, etc.) won't trigger a drag because
/// we check evt.Target to see if the panel background itself was clicked.
///
/// This follows the pattern used by popular tModLoader mods like
/// Magic Storage, Recipe Browser, and Boss Checklist.
/// </summary>
public class UIDraggablePanel : UIPanel
{
    private bool _dragging = false;
    private Vector2 _dragOffset;

    // Once the user drags, we switch from relative (HAlign/VAlign)
    // to absolute pixel positioning. This flag tracks that transition.
    private bool _hasBeenDragged = false;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        // Only start dragging if the click landed directly on the panel background,
        // NOT on a child element like a button or section title.
        // evt.Target is the deepest element that was actually hit.
        // If it's a UIToggleButton, UIText, etc., we don't want to drag.
        //
        // We allow dragging if the target is:
        // - This panel itself
        // - A UISectionTitle (non-interactive, just a label — feels natural to drag from)
        if (evt.Target != this && evt.Target is not UISectionTitle)
            return;

        _dragging = true;

        // Calculate offset from mouse to panel's top-left corner
        // so the panel doesn't snap its corner to the cursor
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
            // On first drag, clear HAlign/VAlign so pixel positioning works.
            // Without this, the panel fights between "center of screen" (HAlign=0.5)
            // and the pixel position we're setting, causing erratic behavior.
            if (!_hasBeenDragged)
            {
                HAlign = 0f;
                VAlign = 0f;
                _hasBeenDragged = true;
            }

            // Calculate new position
            float newX = Main.mouseX - _dragOffset.X;
            float newY = Main.mouseY - _dragOffset.Y;

            // Clamp to screen bounds so the panel can't be lost off-screen.
            // Leave a 40px handle always visible so the player can always grab it back.
            CalculatedStyle dims = GetDimensions();
            float minX = -(dims.Width - 40f);
            float maxX = Main.screenWidth - 40f;
            float minY = 0f; // Don't allow above top of screen
            float maxY = Main.screenHeight - 40f;

            newX = MathHelper.Clamp(newX, minX, maxX);
            newY = MathHelper.Clamp(newY, minY, maxY);

            Left.Set(newX, 0f);
            Top.Set(newY, 0f);

            Recalculate();
        }

        // Prevent game interactions (mining, placing, attacking)
        // when the mouse is over the panel
        if (ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
        }
    }

    /// <summary>
    /// Resets position to centered. Call this if you want to re-center
    /// the panel (e.g., when toggling the UI back open).
    /// </summary>
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