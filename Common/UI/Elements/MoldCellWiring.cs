using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using WorldShapingWandsMod.Common.Players;
using WorldShapingWandsMod.Common.UI;
using WorldShapingWandsMod.Common.UI.Elements.Builders;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S11 2026-04-29 — Bug 3 fix; <c>StencilEditVsActOn.md</c> §3) Shared
/// wiring for the Mold cell that lives on EVERY WSW wand's settings panel
/// (the Mold cell is part of the standard 12-cell shape grid built by
/// <see cref="WandPanelBuilder.AddFullShapeSection"/>). Two behaviours:
///
/// <list type="number">
/// <item><b>Hover-icon swap</b> — the Mold cell's displayed icon morphs
/// from <c>ShapeMold.png</c> to the active ACT-ON slot's
/// <c>StencilChoice{N}.png</c> while hovered, so the player can see at a
/// glance which slot will stamp without opening the picker.</item>
/// <item><b>Right-click → ACT-ON Stencil Picker SubUI</b> — opens the
/// picker that writes <see cref="MoldingWandPlayer.ActOnStencilSlot"/>.
/// Toggle-close on a second right-click against the same host (matches
/// the EDIT-picker's UX in <c>MoldingSettingsPanel</c>).</item>
/// </list>
///
/// <para>Pre-S11 only the Wand of Molding's settings panel had any wiring
/// on its Mold cell. Every other wand panel extracted <c>_moldBtn</c> and
/// left it bare \u2014 meaning Building, Dismantling, Coating, etc. had no
/// way to switch the ACT-ON slot at all. Calling
/// <see cref="WireActOnPicker"/> from each panel after
/// <c>_moldBtn = shapes.Mold;</c> fixes that with a single line per panel.</para>
///
/// <para><b>Wand of Molding note</b>: WoM's Mold cell calls this helper
/// the same as every other panel \u2014 it gets the ACT-ON picker via
/// right-click, exactly like the user's S11 example specified ("Wand of
/// Molding can use Mold shape from the stencils into the stencils edit
/// mode as well"). The 5-button stencil row underneath the shape grid
/// (built by <see cref="WandPanelBuilder.AddStencilShapeSection"/>) is
/// the EDIT entry point and stays WoM-only.</para>
/// </summary>
internal static class MoldCellWiring
{
    /// <summary>(S11) Wires hover-icon swap + ACT-ON picker right-click on
    /// the supplied Mold cell button. Idempotent per host \u2014 the picker
    /// uses a stable IdentityKey so re-right-clicking on the same host
    /// toggles closed instead of stacking. Safe to call multiple times
    /// across panel rebuilds; each panel rebuild produces a fresh
    /// <c>_moldBtn</c> so duplicate handler registration is impossible.</summary>
    /// <param name="moldBtn">The Mold cell from
    /// <c>WandPanelBuilder.AddFullShapeSection(out var shapes).Mold</c>.</param>
    public static void WireActOnPicker(UIIconButton moldBtn)
    {
        if (moldBtn == null) return;

        moldBtn.HasSubUIBadge = true;

        var mod = ModContent.GetInstance<WorldShapingWandsMod>();

        // Cache the 5 StencilChoice icons once \u2014 the HoverTextureProvider
        // lambda below must be cheap (called every frame while hovered).
        var stencilIconAssets = new Asset<Texture2D>[MoldingWandPlayer.StencilSlotCount];
        for (int i = 0; i < MoldingWandPlayer.StencilSlotCount; i++)
        {
            stencilIconAssets[i] = mod.Assets.Request<Texture2D>(
                $"Assets_Build/Icons/Shapes/Stencil/StencilChoice{i + 1}",
                AssetRequestMode.ImmediateLoad);
        }

        // Hover: swap icon to ACT-ON slot's StencilChoice (NOT EDIT slot —
        // the cell must reflect what THIS wand will stamp, which is ACT-ON).
        moldBtn.HoverTextureProvider = () =>
        {
            var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
            if (mwp == null) return null;
            int idx = mwp.ActOnStencilSlot;
            if (idx < 0 || idx >= stencilIconAssets.Length) return null;
            return stencilIconAssets[idx];
        };

        // (S12 2026-04-29; per GrayJou S12 prompt: *"the Mold Shape Label is
        // Mold (From Wand of Molding), and I had already stated more than
        // once that it should be Mold{SelectedStencilNum} (From Wand of
        // Molding) to be more clear and encourage exploration"*) Hover text
        // becomes "Mold{N} (from Wand of Molding)" where N is the 1-indexed
        // ACT-ON stencil slot. Kept dynamic via HoverTextProvider so the
        // label tracks slot changes without a panel rebuild.
        moldBtn.HoverTextProvider = () =>
        {
            var mwp = Main.LocalPlayer?.GetModPlayer<MoldingWandPlayer>();
            int slotNum = (mwp?.ActOnStencilSlot ?? 0) + 1;
            return Language.GetTextValue(
                "Mods.WorldShapingWandsMod.UI.Common.ShapeMoldFmt", slotNum);
        };

        // Right-click: open the ACT-ON picker (Locked by default for
        // cross-wand persistence).
        //
        // (S14 2026-04-29; per GrayJou S14 verbatim: *"I right clicked
        // mold shape on every wand panel and it opened Stencil Selector
        // Every time, like, it opened 10 of them, there should be only
        // one instance of that one. Every wand follows the centralized
        // Which Mold Stencil To Place, and it's only one. The Stencil
        // Selector Persists across wands precisely because this is the
        // intended behavior."*) The ACT-ON picker is conceptually a
        // SINGLETON across the whole mod \u2014 every wand's Mold cell drives
        // the same `MoldingWandPlayer.ActOnStencilSlot` byte, so there
        // must never be more than one ACT-ON picker open at a time.
        //
        // Pre-S14 the toggle-close check was gated on
        // `ReferenceEquals(existing.Host, moldBtn)` \u2014 fine when the player
        // right-clicks the SAME wand panel twice, but BROKEN when they
        // open the picker on Wand A, swap to Wand B, then right-click
        // Wand B's Mold cell: the host check fails, a SECOND picker is
        // opened, and after cycling 10 wands the host stack carries 10
        // identical pickers. The fix is to scan by IdentityKey only, and
        // \u2014 critically \u2014 always close the existing one regardless of
        // whether its host matches. Two cases:
        //
        //   (a) existing.Host == moldBtn    \u2192 toggle-close, exit. Same UX
        //       as before (player right-clicks twice = open / close).
        //   (b) existing.Host != moldBtn    \u2192 close the stale instance
        //       AND fall through to open a fresh one anchored to the
        //       current host. The picker visually "moves" to the wand
        //       panel the player just clicked; state is preserved
        //       because the underlying byte
        //       (`MoldingWandPlayer.ActOnStencilSlot`) is the
        //       authoritative store and is unchanged by close/reopen.
        moldBtn.OnRightClick += (_, _) =>
        {
            var sys = ModContent.GetInstance<WandUISystem>();
            if (sys?.WandSubPanelHost == null) return;

            // Scan once for any existing ACT-ON picker (singleton invariant).
            WandSubPanel existing = null;
            foreach (var p in sys.WandSubPanelHost.Panels)
            {
                if (p.IdentityKey == StencilPickerBuilder.ActOnIdentityKey)
                {
                    existing = p;
                    break;
                }
            }

            if (existing != null)
            {
                bool wasOurHost = ReferenceEquals(existing.Host, moldBtn);
                sys.CloseWandSubPanel(existing);
                if (wasOurHost)
                    return;        // case (a) \u2014 plain toggle-close.
                // case (b) \u2014 fall through to anchor a fresh one here.
            }

            var picker = WandSubPanelFactories.CreateStencilActOnPicker(moldBtn);
            sys.OpenWandSubPanel(picker);
            picker.AnchorToHost();
        };
    }
}
