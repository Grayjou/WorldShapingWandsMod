using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using WorldShapingWandsMod.Common.Players;

namespace WorldShapingWandsMod.Common.UI.Elements.Builders;

/// <summary>
/// (S5 2026-04-29 — SubUI Architecture Phase B) Companion-builder for the
/// Stencil Slot picker SubPanel body. Extracted from the prior
/// <c>StencilPickerSubPanel</c> static factory per the C-2 ratification
/// (Idiom B = factory-per-instance) + the architecture doc's
/// "Companion Builder Rule" (any SubUI whose wiring exceeds ~80 lines is
/// extracted to a builder; the <see cref="WandSubPanelFactories"/> entry then
/// collapses to a thin lifecycle-metadata wrapper).
///
/// <para><b>Identity + title constants</b> live here so any consumer needing
/// a stable string (e.g. the <c>OpenWandSubPanel</c> "already-open?" check in
/// <c>MoldingSettingsPanel</c>) reads them from the builder rather than
/// from a defunct factory façade.</para>
///
/// <para><b>Behaviour preserved verbatim</b> from the S7 2026-04-28 original
/// (default-locked = false; first-pick auto-close when unlocked; reopens
/// re-sync the active slot from <see cref="MoldingWandPlayer.ActiveStencilSlot"/>).
/// No semantic change — only the wiring lives in a different file.</para>
/// </summary>
internal static class StencilPickerBuilder
{
    /// <summary>(S11 2026-04-29) Distinguishes the two stencil-slot picker
    /// flavours per <c>StencilEditVsActOn.md</c> \u00a71: EDIT writes to
    /// <see cref="MoldingWandPlayer.ActiveStencilSlot"/> (Wand-of-Molding
    /// editing cursor); ACT-ON writes to
    /// <see cref="MoldingWandPlayer.ActOnStencilSlot"/> (every wand's Mold
    /// Shape source). Same body wiring; the only difference is which byte
    /// the click handler writes to and which byte the toggled-visual
    /// re-syncs from.</summary>
    public enum PickerKind
    {
        /// <summary>Writes <see cref="MoldingWandPlayer.ActiveStencilSlot"/>.</summary>
        Edit,
        /// <summary>Writes <see cref="MoldingWandPlayer.ActOnStencilSlot"/>.</summary>
        ActOn,
    }

    public const string IdentityKey      = "WandOfMolding.StencilSlotPicker";       // legacy = EDIT picker
    public const string ActOnIdentityKey = "WSWWand.StencilActOnPicker";             // S11 ACT-ON picker
    public const string TitleKey         = "Mods.WorldShapingWandsMod.UI.Stencil.PickerTitle";
    public const string ActOnTitleKey    = "Mods.WorldShapingWandsMod.UI.Stencil.ActOnPickerTitle";

    // (S13 2026-04-29; per GrayJou S13: *"This panel actually still has the
    // big icons, it should have the small icons"*.) Picker body now uses
    // the same compact cell metrics as the in-panel stencil row introduced
    // S12 (`AddSmallIconGrid`). The small icon assets (16×16) sit inside
    // 22×22 cells with a 4-px frame on each side instead of swimming in a
    // 36×36 chrome.
    private const float CellSize = WandPanelBuilder.SmallIconBtnSize; // 22f
    private const float CellGap  = WandPanelBuilder.SmallIconGap;     // 4f

    /// <summary>
    /// Builds the 5-cell button body and an action that re-syncs the
    /// <c>Toggled</c> visual to the player's active slot. The factory wires
    /// the <see cref="WandSubPanel.NotifySelection"/> callback into each
    /// button's click handler via <paramref name="onSlotPicked"/> so the
    /// builder remains panel-agnostic (it cannot reference a panel that
    /// hasn't been constructed yet).
    /// </summary>
    /// <param name="onSlotPicked">Invoked after the slot has been written to
    /// <see cref="MoldingWandPlayer.ActiveStencilSlot"/>. The factory wires
    /// this to <see cref="WandSubPanel.NotifySelection"/> so the unlock-aware
    /// auto-close path runs.</param>
    /// <param name="resyncToggled">Out-param action that re-applies the
    /// <c>Toggled</c> visual to whichever button matches the current
    /// <see cref="MoldingWandPlayer.ActiveStencilSlot"/>. Called once during
    /// the factory's post-construction step (so the panel's initial render
    /// shows the correct selected state).</param>
    public static UIElement BuildBody(System.Action onSlotPicked, out System.Action resyncToggled)
        => BuildBody(PickerKind.Edit, onSlotPicked, out resyncToggled);

    /// <summary>(S11 2026-04-29) Variant that picks which byte on
    /// <see cref="MoldingWandPlayer"/> the click handler writes to. See
    /// <see cref="PickerKind"/>.</summary>
    public static UIElement BuildBody(PickerKind kind, System.Action onSlotPicked, out System.Action resyncToggled)
    {
        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        const int slotCount = MoldingWandPlayer.StencilSlotCount;
        float bodyW = CellSize * slotCount + CellGap * (slotCount - 1);
        float bodyH = CellSize;

        var body = new UIElement();
        body.Width.Set(bodyW, 0f);
        body.Height.Set(bodyH, 0f);

        var buttons = new UIIconButton[slotCount];

        // (S13 2026-04-29) Per GrayJou S13: *"the mold source stencil isn't
        // a selector but a row of toggles, so you can select several
        // stencils at once which leads to unexpected behaviour… change it to
        // a selector so that only one stencil can be selected at once"*.
        // Pre-S13 each cell carried `IsRadio = true` but UIIconButton's
        // base radio logic only manages the cell's *own* Toggled bit — it
        // does not iterate siblings. The previous implementation only re-
        // synced sibling visuals once at construction (`resyncToggled`), so
        // subsequent clicks on different cells left the previously-selected
        // cell visually-on as well. Fix: factor the sibling-resync into a
        // local `applyToggled` closure and invoke it both from `OnLeftClick`
        // (every click) and from the initial `resyncToggled` out-param
        // (once during factory post-construction).
        System.Action applyToggled = () =>
        {
            var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
            if (mwp == null) return;
            int active = kind == PickerKind.Edit ? mwp.ActiveStencilSlot : mwp.ActOnStencilSlot;
            for (int i = 0; i < slotCount; i++)
                buttons[i].Toggled = (i == active);
        };

        for (int i = 0; i < slotCount; i++)
        {
            int slotIdx = i; // capture
            Asset<Texture2D> tex = mod.Assets.Request<Texture2D>(
                $"Assets_Build/Icons/Shapes/Stencil/StencilChoice{i + 1}",
                AssetRequestMode.ImmediateLoad);
            string hoverKey = $"Mods.WorldShapingWandsMod.UI.Stencil.Slot{i + 1}";

            var btn = new UIIconButton(tex, Language.GetTextValue(hoverKey))
            {
                IsRadio = true,
                IsAction = false,
            };
            btn.Width.Set(CellSize, 0f);
            btn.Height.Set(CellSize, 0f);
            btn.Left.Set(i * (CellSize + CellGap), 0f);
            btn.Top.Set(0f, 0f);

            btn.OnLeftClick += (_, _) =>
            {
                var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
                if (mwp == null) return;
                if (kind == PickerKind.Edit)
                    mwp.ActiveStencilSlot = (byte)slotIdx;
                else
                    mwp.ActOnStencilSlot = (byte)slotIdx;
                applyToggled();           // <- S13: keep radio visuals in lock-step
                onSlotPicked?.Invoke();
            };

            buttons[i] = btn;
            body.Append(btn);
        }

        // Re-sync action: applied once by the factory after construction so the
        // initial render shows the correct selected slot. Re-applied implicitly
        // on each fresh open because the factory always builds a new body.
        resyncToggled = applyToggled;

        return body;
    }
}
