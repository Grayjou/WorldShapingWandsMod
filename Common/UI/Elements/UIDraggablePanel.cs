using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// Reusable draggable panel primitive used by wand settings panels,
/// InventoryView, and popout hosts.
/// </summary>
/// <remarks>
/// S6 unified drag behavior around explicit policies:
/// <see cref="DragPolicy.Anywhere"/>, <see cref="DragPolicy.HandleOnly"/>,
/// and <see cref="DragPolicy.HandleOrAnywhere"/>.
/// </remarks>
public class UIDraggablePanel : UIPanel
{
    private bool _dragging = false;
    private Vector2 _dragOffset;
    private bool _hasBeenDragged = false;

    /// <summary>
    /// (S6 §1) Drag-arming policy. Default preserves legacy behavior.
    /// </summary>
    public DragPolicy DragPolicy { get; set; } = DragPolicy.Anywhere;

    /// <summary>
    /// (S6 §3) Opacity scalar applied during draw. Set by the owning
    /// host to implement fade transitions.
    /// </summary>
    public float DrawAlpha { get; set; } = 1f;

    /// <summary>
    /// Called by <see cref="UIDragHandle"/> when the user clicks the handle.
    /// Arms drag regardless of <see cref="DragPolicy"/>.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen coordinates.</param>
    internal void ArmDragFromHandle(Vector2 mousePos)
    {
        _dragging = true;
        CalculatedStyle dims = GetDimensions();
        _dragOffset = new Vector2(mousePos.X - dims.X, mousePos.Y - dims.Y);
    }

    /// <summary>
    /// Determines whether a mouse-down at the given target may arm drag.
    /// </summary>
    /// <param name="mousePos">Mouse position in screen coordinates.</param>
    /// <param name="evtTarget">UI element that received the mouse event.</param>
    /// <returns><c>true</c> if drag should arm; otherwise <c>false</c>.</returns>
    protected virtual bool CanDragAt(Vector2 mousePos, UIElement evtTarget)
    {
        // (S6 §1) HandleOnly panels can ONLY be dragged via UIDragHandle.
        if (DragPolicy == DragPolicy.HandleOnly)
            return false;

        // Anywhere and HandleOrAnywhere: legacy behavior.
        return evtTarget == this || evtTarget is UISectionTitle;
    }

    /// <summary>
    /// Arms drag when policy and event target allow it.
    /// </summary>
    /// <param name="evt">Mouse-down event payload.</param>
    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        if (!CanDragAt(evt.MousePosition, evt.Target))
            return;

        _dragging = true;
        CalculatedStyle dims = GetDimensions();
        _dragOffset = new Vector2(
            evt.MousePosition.X - dims.X,
            evt.MousePosition.Y - dims.Y);
    }

    // (S6 §2) LeftMouseUp removed. The polled clamp in Update is now the
    // SOLE mechanism that ends a drag, eliminating event/poll races.

    /// <summary>
    /// Updates drag state, applies panel clamping, and marks mouse-interface
    /// ownership while the cursor is inside panel bounds.
    /// </summary>
    /// <param name="gameTime">Frame timing info.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // (S6 §2 — Sticky drag) The polled clamp is the SOLE mechanism that
        // ends a drag. LeftMouseUp was removed to eliminate event/poll races.
        // Drag arms via LeftMouseDown or ArmDragFromHandle; drag persists
        // every frame Main.mouseLeft is held; ends the first frame it reads
        // false. Self-heals if OS swallows a button-release (e.g. Alt-Tab).
        if (_dragging && !Main.mouseLeft)
            _dragging = false;

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

            (newX, newY) = ClampPanelToScreen(newX, newY);

            Left.Set(newX, 0f);
            Top.Set(newY, 0f);
            Recalculate();
        }
        else if (_hasBeenDragged)
        {
            // (S1 2026-04-28 P 1*2) Re-clamp every frame after the panel has
            // ever been dragged. Fixes the "stuck offscreen" bug where a
            // resolution change (or a stale persisted position) could leave
            // the drag handle outside the screen with no way to reach it.
            CalculatedStyle dims = GetDimensions();
            float curX = dims.X;
            float curY = dims.Y;
            (float clX, float clY) = ClampPanelToScreen(curX, curY);
            if (clX != curX || clY != curY)
            {
                Left.Set(clX, 0f);
                Top.Set(clY, 0f);
                Recalculate();
            }
        }

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    /// <summary>
    /// (S1 2026-04-28 P 1*2) Clamp a candidate panel top-left position so the
    /// panel stays mostly on-screen, guaranteeing the drag handle remains
    /// reachable regardless of where it sits on the panel chrome.
    /// </summary>
    /// <remarks>
    /// Previous (pre-S1 2026-04-28) clamp only required a 40px sliver of the
    /// panel to remain visible from any single edge. For panels whose drag
    /// handle is anchored opposite that edge (e.g. InventoryView's top-right
    /// handle clipped off when only the left sliver was visible), the user
    /// could lose the handle entirely. The new clamp keeps the entire panel
    /// within screen bounds plus a small <see cref="EdgeSlack"/> overshoot,
    /// which both fixes the bug and matches typical desktop window behavior.
    /// </remarks>
    private (float x, float y) ClampPanelToScreen(float x, float y)
    {
        const float EdgeSlack = 40f;
        CalculatedStyle dims = GetDimensions();

        float minX = -EdgeSlack;
        float maxX = Main.screenWidth - dims.Width + EdgeSlack;
        float minY = 0f;
        float maxY = Main.screenHeight - dims.Height + EdgeSlack;

        // Guard against pathological cases (panel larger than screen).
        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;

        return (
            MathHelper.Clamp(x, minX, maxX),
            MathHelper.Clamp(y, minY, maxY)
        );
    }

    /// <summary>
    /// Draws panel chrome with optional alpha modulation for fade transitions.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    public override void Draw(SpriteBatch spriteBatch)
    {
        // (S6 §3) Fade support — skip draw entirely if fully transparent.
        if (DrawAlpha <= 0.001f) return;

        if (DrawAlpha >= 0.999f)
        {
            base.Draw(spriteBatch);
            return;
        }

        // Modulate panel chrome alpha for fade.
        Color savedBg = BackgroundColor;
        Color savedBorder = BorderColor;
        BackgroundColor *= DrawAlpha;
        BorderColor *= DrawAlpha;

        base.Draw(spriteBatch);

        BackgroundColor = savedBg;
        BorderColor = savedBorder;
    }

    /// <summary>
    /// Resets panel position to centered defaults and clears drag state.
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