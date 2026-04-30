using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace WorldShapingWandsMod.Common.UI.Elements;

/// <summary>
/// (S4 2026-04-28; renamed S6 2026-04-29 Phase D from <c>UISubPanelHost</c>)
/// Hosting <see cref="UIState"/> for the SubPanel stack. Owns rendering for
/// every currently-open <see cref="WandSubPanel"/> (top-level + nested
/// children). Lives behind the popout layer in <see cref="WandUISystem"/>.
///
/// <para>
/// Stack discipline: top-level panels push at index 0; their children push
/// AFTER them. Closing a panel removes it AND every descendant child
/// (transitive cleanup, per <c>WSWSubUIPrimitivePlan.md</c> §9 nested-SubUI
/// invariant). Direct iteration order is insertion order so children draw
/// on top of parents naturally.
/// </para>
/// </summary>
public sealed class WandSubPanelHost : UIState
{
    private readonly List<WandSubPanel> _panels = new();

    public IReadOnlyList<WandSubPanel> Panels => _panels;
    public int Count => _panels.Count;
    public WandSubPanel Topmost => _panels.Count > 0 ? _panels[_panels.Count - 1] : null;

    /// <summary>Push a new panel onto the host. If <paramref name="parent"/> is
    /// non-null, it becomes the parent's <see cref="WandSubPanel.Child"/> (auto-
    /// closing any previous child of the same parent).</summary>
    internal void Push(WandSubPanel panel, WandSubPanel parent = null)
    {
        if (panel == null || _panels.Contains(panel)) return;

        if (parent != null)
        {
            // Close any previously open child of this parent (one child at a time).
            if (parent.Child != null && parent.Child != panel)
                Pop(parent.Child);
            parent.Child = panel;
            panel.Parent = parent;
        }

        _panels.Add(panel);
        Append(panel);
        panel.Activate();
        panel.AnchorToHost();
    }

    /// <summary>Remove a panel and all its descendants. Returns the list of removed panels
    /// so the caller can fire close events in deterministic (deepest-first) order.</summary>
    internal List<WandSubPanel> Pop(WandSubPanel panel)
    {
        var removed = new List<WandSubPanel>();
        if (panel == null) return removed;

        // Walk the child chain and collect deepest-first.
        var chain = new List<WandSubPanel>();
        for (var cur = panel; cur != null; cur = cur.Child)
            chain.Add(cur);

        for (int i = chain.Count - 1; i >= 0; i--)
        {
            var p = chain[i];
            if (_panels.Remove(p))
            {
                if (p.Parent != null && p.Parent.Child == p)
                    p.Parent.Child = null;
                p.Parent = null;
                p.Child = null;
                RemoveChild(p);
                removed.Add(p);
            }
        }
        return removed;
    }

    /// <summary>Remove every panel. Returns deepest-first removal order.</summary>
    internal List<WandSubPanel> Clear()
    {
        var removed = new List<WandSubPanel>();
        // Iterate copy because Pop mutates _panels.
        var roots = new List<WandSubPanel>();
        foreach (var p in _panels)
            if (p.Parent == null) roots.Add(p);
        foreach (var r in roots)
            removed.AddRange(Pop(r));
        return removed;
    }

    /// <summary>True iff <paramref name="screen"/> lies inside any open panel.</summary>
    public bool ContainsScreenPoint(Vector2 screen)
    {
        foreach (var p in _panels)
            if (p.ContainsScreenPoint(screen)) return true;
        return false;
    }
}
