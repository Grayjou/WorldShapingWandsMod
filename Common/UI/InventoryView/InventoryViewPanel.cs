using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI.Elements;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// InventoryView companion panel that shows per-source candidate slots and
/// lets the user choose/clear a specific item choice per source.
/// </summary>
public sealed class InventoryViewPanel : UIState
{
    /// <summary>Whether the panel should currently be rendered.</summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Sticky user intent toggle. Visibility still depends on provider
    /// availability and active wand participation.
    /// </summary>
    public bool IsUserOpenIntent { get; set; }

    /// <summary>Main draggable panel element (for hit-testing by UI system).</summary>
    public UIElement PanelElement => _mainPanel;

    private const float PanelWidth = 380f;
    private const float Padding = 6f;
    private const float SlotGap = 4f;
    private const int MaxSlotsPerSource = 9;
    private const string UIPrefix = "Mods.WorldShapingWandsMod.UI.InventoryView";

    private UIDraggablePanel _mainPanel;
    private UIDragHandle _dragHandle;
    // _titleText removed v1.3.x per GrayJou S6 prompt — subtitles per source
    // (tiles/walls for WoB, Replace…/With… for WoR) are sufficient.
    private readonly List<SourceSection> _sections = new();

    public override void OnInitialize()
    {
        _mainPanel = new UIDraggablePanel();
        _mainPanel.Width.Set(PanelWidth, 0f);
        _mainPanel.HAlign = 0.5f;
        _mainPanel.VAlign = 0.5f;
        _mainPanel.BackgroundColor = WandPanelTheme.PanelChrome.InventoryViewBg;
        _mainPanel.BorderColor = WandPanelTheme.PanelChrome.InventoryViewBorder;
        // (S6 §1) HandleOnly drag — only the handle initiates drag.
        _mainPanel.DragPolicy = DragPolicy.HandleOnly;
        Append(_mainPanel);

        // (S6 §1) Handle at top-right, mirrored from WandPanel's top-right
        // for visual consistency across all WSW panels.
        _dragHandle = new UIDragHandle(16);
        _dragHandle.HAlign = 1f;
        //_dragHandle.Top.Set(Padding + 4f, 0f);
        _dragHandle.Top.Set(Padding, 0f);
        _dragHandle.Left.Set(-Padding, 0f);
        _mainPanel.Append(_dragHandle);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!IsVisible) return;

        Player player = Main.LocalPlayer;
        if (player == null || !player.active) return;

        IInventoryViewProvider provider = InventoryViewRegistry.GetProvider(player);
        if (provider == null)
            return;

        Rebuild(player, provider);

        if (_mainPanel.ContainsPoint(Main.MouseScreen))
            player.mouseInterface = true;
    }

    private void Rebuild(Player player, IInventoryViewProvider provider)
    {
        /*
        * InventoryViewPanel vertical layout trace
        * =========================================
        * Constants: Padding=6, SlotGap=4, SlotSize=36, UIPanel inset=12 (all sides)
        *
        * outer y=0    ┌─ Panel Start ─────────────────────────────────────┐
        *              │  12px UIPanel inset + 6px Padding = 18px          │
        * outer y=18   │  ┌─ Handle Start (16×16, HAlign=1 right)  ───────┐│
        *              │  │         16px                                  ││
        * outer y=34   │  └─ Handle End ──────────────────────────────────┘│
        *              │  4px (slots at +20 inside section, section at +6  │
        *              │       inner, so 12+6+20=38, handle ends 34→38=4)  │
        * outer y=38   │  ┌──────┐ ┌──────┐ ┌──────┐                       │
        *              │  │36×36 │ │36×36 │ │36×36 │  Slot Start           │
        *              │  │      │ │      │ │      │  36px                 │
        * outer y=74   │  └──────┘ └──────┘ └──────┘  Slot End             │
        *              │  20px (section pad 4 + SlotGap×2 8 + Padding×3 18 │
        *              │         − UIPanel bottom inset 12 + 2 = 20)       │
        * outer y=94   └─ Panel End ───────────────────────────────────────┘
        *
        * Height.Set(cursorY) where cursorY = 6+62+8+18 = 94
        * Section height = 22 (header) + 36 (slot) + 4 (pad) = 62
        *
        * Two-source adds: +62 (section) +8 (SlotGap×2) = +70 → 164px total
        */
        WandPlayer wp = player.GetModPlayer<WandPlayer>();

        foreach (var section in _sections)
            _mainPanel.RemoveChild(section.Container);
        _sections.Clear();

        float cursorY = Padding;   // At drag handle level
        for (int i = 0; i < provider.Sources.Count; i++)
        {
            IInventoryViewSource source = provider.Sources[i];
            var sec = BuildSection(source, wp, player);
            sec.Container.Top.Set(cursorY, 0f);
            sec.Container.Left.Set(Padding, 0f);
            _mainPanel.Append(sec.Container);
            _sections.Add(sec);
            cursorY += sec.Height + SlotGap * 2;
        }

        cursorY += Padding *3f;
        _mainPanel.Height.Set(cursorY, 0f);
        _mainPanel.RemoveChild(_dragHandle);
        _mainPanel.Append(_dragHandle);
        _mainPanel.Recalculate();
    }

    private SourceSection BuildSection(IInventoryViewSource source, WandPlayer wp, Player player)
    {
        var container = new UIElement();
        container.Width.Set(-Padding * 2f, 1f);

        string titleStr = Language.GetTextValue($"Mods.WorldShapingWandsMod.{source.TitleKey}");
        var header = new UIText(titleStr, 0.85f);
        header.Top.Set(0f, 0f);
        container.Append(header);

        int? chosenType = source.GetSelectedItemType(wp);
        var inventoryCandidates = source.GetCandidateItemTypes(player).Distinct().ToList();

        // (S16 2026-04-28; GrayJou Pin/Carefree clarification) Pins are a
        // Carefree-Mode-only construct. Per S16 verbatim:
        //   Regular Mode → "No pins visible, no pinning allowed"
        //   Carefree Mode → "Pins are visible, and can be added/removed"
        // Storage on WandPlayer is preserved across mode toggles — leaving
        // Carefree only HIDES the pins; re-entering restores them. Whence
        // we gate the pinnedSet at read time here rather than wiping data.
        bool carefreeEnabled = WandConfigs.Carefree?.EnableCarefreeMode == true;
        var pinnedSet = carefreeEnabled
            ? new HashSet<int>(source.GetPinnedItemTypes(wp))
            : new HashSet<int>();

        // (S15 PersistentPin) Merge pinned items into the candidate list so that
        // pinned items the player no longer carries still render as ghost-pinned
        // slots (one-click handle to unpin). Order: chosen > pinned > rest.
        // (S16 Carefree gate) `pinnedSet` is empty in Regular mode, so this
        // merge collapses to a no-op and ghost-pinned slots are not drawn.
        var candidates = new List<int>(inventoryCandidates);
        var inventorySet = new HashSet<int>(inventoryCandidates);
        foreach (int pin in pinnedSet)
            if (!inventorySet.Contains(pin)) candidates.Add(pin);

        bool chosenIsGhost = chosenType.HasValue && !inventorySet.Contains(chosenType.Value);
        if (chosenIsGhost && !candidates.Contains(chosenType.Value))
            candidates.Insert(0, chosenType.Value);

        candidates.Sort((a, b) =>
        {
            bool ac = chosenType.HasValue && a == chosenType.Value;
            bool bc = chosenType.HasValue && b == chosenType.Value;
            if (ac != bc) return ac ? -1 : 1;
            bool ap = pinnedSet.Contains(a);
            bool bp = pinnedSet.Contains(b);
            if (ap != bp) return ap ? -1 : 1;
            return a.CompareTo(b);
        });
        int shown = System.Math.Min(candidates.Count, MaxSlotsPerSource);

        for (int s = 0; s < shown; s++)
        {
            int itemType = candidates[s];
            int stack = CountStack(player, itemType);
            string hoverName = ResolveItemDisplayName(itemType);
            bool isChosen = chosenType.HasValue && itemType == chosenType.Value;
            bool isPinned = pinnedSet.Contains(itemType);
            bool isGhost = !inventorySet.Contains(itemType); // ghost = stale-reference (chosen OR pinned)

            var slot = new UIInventoryViewSlot();
            slot.Configure(itemType, stack, isChosen, hoverName, isGhost, isPinned);
            slot.Left.Set(s * (UIInventoryViewSlot.SlotSize + SlotGap), 0f);
            slot.Top.Set(20f, 0f);
            slot.OnChoiceClick = (clicked, wasChosen) =>
            {
                if (wasChosen)
                    source.SetSelectedItemType(wp, null);
                else
                    source.SetSelectedItemType(wp, clicked.ItemType);
            };
            // (S15 PersistentPin) Right-click toggles pin state for this item.
            // (S16 Carefree gate) Only wire the pin handler in Carefree mode —
            // in Regular mode right-click is a no-op (no pinning allowed).
            // The slot's RightClick override safely no-ops when OnPinClick is
            // null, so simply skipping the assignment is sufficient.
            if (carefreeEnabled)
                slot.OnPinClick = (clicked, _) => source.TogglePin(wp, clicked.ItemType);
            container.Append(slot);
        }

        if (candidates.Count == 0)
        {
            var empty = new UIText(
                Language.GetTextValue($"{UIPrefix}.NoCandidates"), 0.75f)
            {
                TextColor = WandPanelTheme.Colors.SubduedText,
            };
            empty.Top.Set(22f, 0f);
            container.Append(empty);
        }

        float height = 22f + UIInventoryViewSlot.SlotSize + 4f;
        container.Height.Set(height, 0f);
        return new SourceSection(container, height);
    }

    private static int CountStack(Player player, int itemType)
    {
        int total = 0;
        for (int i = 0; i < 58; i++)
        {
            Item it = player.inventory[i];
            if (it != null && !it.IsAir && it.type == itemType)
                total += it.stack;
        }
        return total;
    }

    private static string ResolveItemDisplayName(int itemType)
    {
        var probe = new Item();
        probe.SetDefaults(itemType);
        return probe.Name;
    }

    private readonly struct SourceSection
    {
        public UIElement Container { get; }
        public float Height { get; }
        public SourceSection(UIElement container, float height)
        {
            Container = container;
            Height = height;
        }
    }
}