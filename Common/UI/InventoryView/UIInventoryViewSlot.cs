using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// One inventory slot in the InventoryView panel. Renders a 36×36 cell with
/// the item icon centered, stack-count in the bottom-right, and a coloured
/// 2-px frame when chosen. Left-click fires <see cref="OnLeftClick"/>;
/// hovering shows the vanilla item name tooltip.
///
/// <para>This is a <b>stateless render-and-dispatch widget</b>: it does NOT
/// own the underlying choice state. The panel passes <see cref="ItemType"/> and
/// <see cref="IsChosen"/> into <see cref="Configure"/> every frame; the slot
/// just paints.</para>
///
/// <para>Why not <c>Terraria.UI.ItemSlot.Draw</c>? <c>ItemSlot</c> requires a
/// mutable <c>Item[]</c> backing array and routes through the player's
/// inventory event system (mouse pickup, accessory checks, etc.). For a
/// pure read-only candidate display, that's the wrong contract — we want
/// a click to <i>choose</i>, not to <i>swap into MouseItem</i>. Custom draw
/// keeps the choose-click semantics clean.</para>
/// </summary>
public sealed class UIInventoryViewSlot : UIElement
{
    public const float SlotSize = 36f;

    private static readonly Color FillNormal = new(35, 50, 75, 220);
    private static readonly Color FillChosen = new(80, 65, 30, 240);
    private static readonly Color FillGhost = new(40, 40, 50, 200);
    private static readonly Color BorderNormal = new(70, 90, 130, 255);
    private static readonly Color BorderChosenHi = new(255, 210, 80, 255);
    private static readonly Color BorderGhost = new(150, 130, 60, 200);
    private static readonly Color BorderHover = new(180, 200, 255, 255);
    private static readonly Color GhostIconTint = new(255, 255, 255, 110); // ~43% opacity

    public int ItemType { get; private set; }
    public int StackCount { get; private set; }
    public bool IsChosen { get; private set; }
    /// <summary>
    /// True when the slot represents a choice whose item is no longer in the
    /// player's inventory. The slot still renders so the user knows the choice
    /// is "stale-but-remembered"; render is dimmed and the icon is desaturated
    /// so it reads as inactive at a glance. Click still clears the choice.
    /// (S10 — Letter #10 polish: ghost-chosen slot state, top of the
    /// re-ranked Phase 2 polish list.)
    /// </summary>
    public bool IsGhost { get; private set; }
    public Action<UIInventoryViewSlot, bool /* wasChosenAtClick */> OnChoiceClick;

    private string _hoverName;

    public UIInventoryViewSlot()
    {
        Width.Set(SlotSize, 0f);
        Height.Set(SlotSize, 0f);
    }

    /// <summary>
    /// Called by the panel each frame to push the latest state in. Cheap;
    /// no allocations on the steady path.
    /// </summary>
    public void Configure(int itemType, int stackCount, bool chosen, string hoverName, bool isGhost = false)
    {
        ItemType = itemType;
        StackCount = stackCount;
        IsChosen = chosen;
        IsGhost = isGhost;
        _hoverName = hoverName;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        OnChoiceClick?.Invoke(this, IsChosen);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();
        var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
        bool hovered = ContainsPoint(Main.MouseScreen);

        // Background fill.
        Color fill = IsGhost ? FillGhost : (IsChosen ? FillChosen : FillNormal);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, fill);

        // Border (thicker when chosen, accent on hover, muted when ghost).
        Color border;
        int borderThickness;
        if (IsGhost)
        {
            border = hovered ? BorderHover : BorderGhost;
            borderThickness = 2;
        }
        else
        {
            border = IsChosen ? BorderChosenHi : (hovered ? BorderHover : BorderNormal);
            borderThickness = IsChosen ? 2 : 1;
        }
        DrawBorder(spriteBatch, rect, border, borderThickness);

        // Item icon (centered, scaled to fit ~28×28). Desaturated/dimmed when ghost.
        Color iconTint = IsGhost ? GhostIconTint : Color.White;
        if (ItemType > 0 && ItemType < TextureAssets.Item.Length)
        {
            Main.instance.LoadItem(ItemType);
            Asset<Texture2D> tex = TextureAssets.Item[ItemType];
            if (tex?.Value != null)
            {
                Rectangle src = Main.itemAnimations[ItemType] != null
                    ? Main.itemAnimations[ItemType].GetFrame(tex.Value)
                    : tex.Value.Frame(1, 1, 0, 0);

                const float maxIconExtent = 28f;
                float iconScale = MathHelper.Min(
                    maxIconExtent / Math.Max(1, src.Width),
                    maxIconExtent / Math.Max(1, src.Height));
                if (iconScale > 1f) iconScale = 1f;

                Vector2 origin = new(src.Width * 0.5f, src.Height * 0.5f);
                Vector2 center = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

                spriteBatch.Draw(tex.Value, center, src, iconTint,
                    0f, origin, iconScale, SpriteEffects.None, 0f);
            }
        }

        // Stack count (bottom-right). Only show when > 1. Suppressed in ghost
        // state because StackCount is 0 by definition (item not in inventory);
        // checking > 1 already short-circuits, this is just defensive.
        if (!IsGhost && StackCount > 1)
        {
            string text = StackCount > 999 ? "999+" : StackCount.ToString();
            Vector2 size = FontAssets.ItemStack.Value.MeasureString(text) * 0.7f;
            Vector2 pos = new(rect.X + rect.Width - size.X - 3f, rect.Y + rect.Height - size.Y - 2f);
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch, FontAssets.ItemStack.Value, text,
                pos, Color.White, 0f, Vector2.Zero, new Vector2(0.7f));
        }

        // Hover tooltip. When chosen, append "chosen — click to free" so the
        // gesture is discoverable without players having to guess that the
        // chosen-fill colour is also the unchoice target. Ghost slots use a
        // dedicated suffix making the stale-but-remembered state explicit.
        // (S10 — Letter #10 polish pass.)
        if (hovered && !string.IsNullOrEmpty(_hoverName))
        {
            Main.LocalPlayer.mouseInterface = true;
            string text = _hoverName;
            if (IsGhost)
            {
                string suffix = Terraria.Localization.Language.GetTextValue(
                    "Mods.WorldShapingWandsMod.UI.Common.ChosenNotInInventory");
                text = _hoverName + "\n" + suffix;
            }
            else if (IsChosen)
            {
                string suffix = Terraria.Localization.Language.GetTextValue(
                    "Mods.WorldShapingWandsMod.UI.Common.ChosenClickToFree");
                text = _hoverName + "\n" + suffix;
            }
            Main.instance.MouseText(text);
        }
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle r, Color c, int thickness)
    {
        var px = TextureAssets.MagicPixel.Value;
        // Top, bottom, left, right.
        sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(px, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c);
        sb.Draw(px, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(px, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c);
    }
}
