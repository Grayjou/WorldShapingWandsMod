using System.Collections.Generic;

namespace WorldShapingWandsMod.Common.UI.InventoryView;

/// <summary>
/// Wraps one or more <see cref="IInventoryViewSource"/> instances into the
/// data model for a single InventoryView panel instance. Most wand families
/// expose a single-source provider (1 grid). The Wand of Replacement is the
/// notable exception: it exposes a 2-source provider (Source grid above
/// Target grid in the same panel) per the Letter #3 design.
///
/// <para>Wand families that do not participate in InventoryView (Wiring,
/// Dismantling, Safekeeping, Delimitation, Molding) return no provider from
/// <see cref="InventoryViewRegistry.GetProvider"/> — the panel toggle in
/// their wand UI will appear disabled with an explanatory tooltip.</para>
/// </summary>
public interface IInventoryViewProvider
{
    /// <summary>
    /// Localization key for the panel title (e.g.
    /// <c>UI.InventoryView.Building.PanelTitle</c>).
    /// </summary>
    string PanelTitleKey { get; }

    /// <summary>
    /// 1 or 2 source(s). Order matters: index 0 renders on top, index 1 below.
    /// </summary>
    IReadOnlyList<IInventoryViewSource> Sources { get; }
}
