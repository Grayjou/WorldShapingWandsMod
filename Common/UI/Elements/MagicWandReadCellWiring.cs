using Terraria.ModLoader;
using WorldShapingWandsMod.Common.UI.Elements.Builders;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S4 2026-05-01 — <c>StencilMagicWandSelectionPlan.md</c> §0.2 + §4.1.)
/// Shared wiring for the Magic Wand (Read) shape cell that lives on
/// EVERY WSW wand's settings panel (the cell is part of the standard
/// shape grid built by <see cref="WandPanelBuilder"/>'s shape-section
/// helpers). One behaviour:
///
/// <list type="number">
/// <item><b>Right-click → Magic Wand Read configuration SubUI</b> —
/// opens the picker that writes
/// <see cref="Players.WandPlayer.MagicWandReadConfig"/>. Toggle-close
/// on a second right-click against the same host (matches the EDIT-
/// picker's UX in <see cref="MoldingSettingsPanel"/>).</item>
/// </list>
///
/// <para><b>Singleton invariant</b> — mirrors <see cref="MoldCellWiring"/>:
/// the SubUI's underlying state (<see cref="Settings.MagicWandReadConfig"/>)
/// is a player-scoped preference shared across every wand the player
/// owns, so there must never be more than one Read-config SubUI open at
/// a time. Right-clicking the Read cell on a different wand panel while
/// the SubUI is already open closes the stale instance and re-anchors a
/// fresh one to the new host. The underlying config bytes are
/// authoritative, so close/reopen is a pure visual operation — picks
/// survive verbatim.</para>
///
/// <para><b>Why every wand, not just stencil wands</b>: the Read SHAPE
/// is stencil-wand-only (gated by
/// <c>MagicWandReadShape.IsAvailable</c>), but the Read CONFIG is a
/// player-scoped preference per the plan's
/// <c>"lives on `WandPlayer` — one config per player"</c> contract. A
/// player exploring on Wand of Building (no stencil canvas) can still
/// pre-configure their preferred SampleMode + Contiguity; the picks
/// take effect the moment they swap to a stencil wand and click. Mirrors
/// the cross-wand persistence model already in place for the ACT-ON
/// Stencil picker (<see cref="StencilPickerBuilder.PickerKind.ActOn"/>).</para>
/// </summary>
internal static class MagicWandReadCellWiring
{
    /// <summary>
    /// Wires the right-click → SubUI handler on the supplied Magic Wand
    /// Read shape cell button. Idempotent per host — the SubUI uses a
    /// stable IdentityKey so re-right-clicking the same host toggles
    /// closed instead of stacking. Safe to call multiple times across
    /// panel rebuilds; each panel rebuild produces a fresh button so
    /// duplicate handler registration is impossible.
    /// </summary>
    /// <param name="readBtn">The Magic Wand Read shape cell from the
    /// panel's <c>shapes.MagicWandRead</c> extraction.</param>
    public static void WireConfigSubUI(UIIconButton readBtn)
    {
        if (readBtn == null) return;

        readBtn.OnRightClick += (_, _) =>
        {
            var sys = ModContent.GetInstance<WandUISystem>();
            if (sys?.WandSubPanelHost == null) return;

            // Scan once for any existing Read-config SubUI (singleton invariant).
            WandSubPanel existing = null;
            foreach (var p in sys.WandSubPanelHost.Panels)
            {
                if (p.IdentityKey == MagicWandReadConfigBuilder.IdentityKey)
                {
                    existing = p;
                    break;
                }
            }

            if (existing != null)
            {
                bool wasOurHost = ReferenceEquals(existing.Host, readBtn);
                sys.CloseWandSubPanel(existing);
                if (wasOurHost)
                    return; // plain toggle-close
                // fall through: re-anchor a fresh one to the current host.
            }

            var picker = WandSubPanelFactories.CreateMagicWandReadConfig(readBtn);
            sys.OpenWandSubPanel(picker);
            picker.AnchorToHost();
        };
    }
}
