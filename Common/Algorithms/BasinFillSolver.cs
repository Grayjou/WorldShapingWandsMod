// ============================================================================
// BasinFillSolver.cs — 6-phase physics-based water fill solver (C# port)
//
// Direct port of basin_fill_raindrop_mixed.py::WaterFillSolver.
//
// Ported from: C:\Users\RYZEN 9\Documents\Cloned\BasinFill\ports\csharp\
// Namespace changed from BasinFill → WorldShapingWandsMod.Common.Algorithms
// Console.WriteLine calls replaced with conditional debug logging.
// ============================================================================

using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace WorldShapingWandsMod.Common.Algorithms;

// ============================================================================
// Water State
// ============================================================================

public enum HoldingStatus : byte
{
    Unknown  = 0,
    CanHold  = 1,
    Spilled  = 2,
}

public sealed class RunWaterState
{
    public Run Run { get; }
    public HoldingStatus Status { get; private set; } = HoldingStatus.Unknown;
    public int EmptySupportsNeeded { get; set; }
    public int SupportsConfirmed   { get; set; }
    public bool HasSolidFloor      { get; set; }

    public RunWaterState(Run run) => Run = run;

    public bool IsBaseRun => HasSolidFloor && EmptySupportsNeeded == 0;

    public bool IsReadyToFill =>
        Status == HoldingStatus.Unknown &&
        SupportsConfirmed >= EmptySupportsNeeded;

    public void MarkCanHold() => Status = HoldingStatus.CanHold;
    public void MarkSpilled() => Status = HoldingStatus.Spilled;
    public void ReceiveSupportConfirmation() => SupportsConfirmed++;
}

// ============================================================================
// Solver
// ============================================================================

public sealed class WaterFillSolver
{
    public bool Debug { get; set; }

    /// <summary>
    /// When true, uses "Pocket Fill" mode: every run enclosed by solid blocks
    /// on both ends is treated as a base, not only top-reachable runs.
    /// This fills sealed cavities below overhangs.
    /// </summary>
    public bool FillAllPockets { get; set; }

    // Public result state
    public HashSet<Run> FilledRuns     { get; } = new();
    public HashSet<Run> SpilledRuns    { get; } = new();
    public HashSet<Run> BaseRuns       { get; private set; } = new();
    public HashSet<Run> ReachableBases { get; } = new();

    // Private state
    private readonly RunMatrix _matrix;
    private readonly int _x0, _y0, _x1, _y1;
    private readonly Dictionary<Run, RunWaterState> _states = new();
    private HashSet<(int x, int y)> _reachableCells = new();
    private readonly Queue<Run> _fillQueue  = new();
    private readonly Queue<Run> _spillQueue = new();

    public WaterFillSolver(RunMatrix matrix,
                           int x0 = -1, int y0 = -1,
                           int x1 = -1, int y1 = -1)
    {
        _matrix = matrix;
        _matrix.BuildAdjacency();

        _x0 = x0 >= 0 ? x0 : 0;
        _y0 = y0 >= 0 ? y0 : 0;
        _x1 = x1 >= 0 ? x1 : matrix.Width  - 1;
        _y1 = y1 >= 0 ? y1 : matrix.Height - 1;
    }

    // ========================================================================
    // Phase 1 — Gravity-aware reachability (raindrop seeding)
    // ========================================================================

    /// <summary>
    /// Finds all cells reachable from the seed positions by gravity-aware flow.
    /// Raindrops fall straight down; they spread horizontally only when blocked.
    /// </summary>
    /// <param name="seedPositions">
    /// Custom seed positions (local grid coordinates). If null, seeds from the
    /// entire top row — the classic "rain from above" behavior.
    /// When using shape-aware seeding (ellipse, diamond, etc.), pass the topmost
    /// tile per column that belongs to the shape.
    /// </param>
    private HashSet<(int x, int y)> FindReachableCells(HashSet<(int x, int y)>? seedPositions = null)
    {
        var reachable = new HashSet<(int, int)>();
        var active    = new HashSet<(int, int)>();

        if (seedPositions != null)
        {
            // Custom shape seeding: use provided positions
            foreach (var (sx, sy) in seedPositions)
            {
                var r = _matrix.GetRunAt(sx, sy);
                if (r != null && r.IsEmpty)
                    active.Add((sx, sy));
            }
        }
        else
        {
            // Default: seed from top row
            for (int x = _x0; x <= _x1; x++)
            {
                var r = _matrix.GetRunAt(x, _y0);
                if (r != null && r.IsEmpty)
                    active.Add((x, _y0));
            }
        }

        int maxIter = _matrix.Width * _matrix.Height * 2;
        int iter = 0;

        while (active.Count > 0 && iter < maxIter)
        {
            iter++;
            var nextActive = new HashSet<(int, int)>();

            foreach (var (x, y) in active)
            {
                if (reachable.Contains((x, y))) continue;
                var r = _matrix.GetRunAt(x, y);
                if (r == null || r.IsSolid) continue;

                reachable.Add((x, y));

                // Gravity: try to fall first
                int belowY = y + 1;
                bool fell = false;

                if (belowY <= _y1)
                {
                    var br = _matrix.GetRunAt(x, belowY);
                    if (br != null && br.IsEmpty)
                    {
                        nextActive.Add((x, belowY));
                        fell = true;
                    }
                }

                // Spread horizontally only if can't fall
                if (!fell)
                {
                    foreach (int dx in new[] { -1, 1 })
                    {
                        int nx = x + dx;
                        if (nx >= _x0 && nx <= _x1)
                        {
                            var nr = _matrix.GetRunAt(nx, y);
                            if (nr != null && nr.IsEmpty && !reachable.Contains((nx, y)))
                                nextActive.Add((nx, y));
                        }
                    }
                }
            }

            active = nextActive;
        }

        return reachable;
    }

    // ========================================================================
    // Phase 2 — Initialise states for ALL empty runs
    // ========================================================================

    private void InitStates(HashSet<(int x, int y)>? seedPositions = null)
    {
        Dbg("Finding cells reachable by rain...");
        _reachableCells = FindReachableCells(seedPositions);
        Dbg($"  Found {_reachableCells.Count} reachable cells");

        Dbg("Initializing states for ALL empty runs...");

        foreach (var er in _matrix.AllEmptyRuns)
        {
            var state = new RunWaterState(er);

            var below = _matrix.GetRunsBelow(er);

            var solidXs = new HashSet<int>();
            var emptySupports = new HashSet<Run>();

            foreach (var b in below)
            {
                if (b.IsSolid)
                {
                    for (int x = b.Start; x <= b.End; x++)
                        if (er.ContainsX(x)) solidXs.Add(x);
                }
                else
                {
                    if (b.Overlaps(er)) emptySupports.Add(b);
                }
            }

            bool allSolid = true;
            for (int x = er.Start; x <= er.End; x++)
            {
                if (!solidXs.Contains(x)) { allSolid = false; break; }
            }

            state.HasSolidFloor       = allSolid && emptySupports.Count == 0;
            state.EmptySupportsNeeded = emptySupports.Count;

            _states[er] = state;

            if (state.IsBaseRun)
            {
                BaseRuns.Add(er);

                bool reachable = false;
                for (int x = er.Start; x <= er.End; x++)
                {
                    if (_reachableCells.Contains((x, er.Row)))
                    {
                        reachable = true;
                        break;
                    }
                }

                if (reachable)
                {
                    ReachableBases.Add(er);
                    Dbg($"  Reachable base: {er}");
                }
            }
        }
    }

    // ========================================================================
    // Phase 3 — Mark border spills
    // ========================================================================

    private void MarkBorderSpills()
    {
        Dbg("Marking border spills...");
        foreach (var (run, state) in _states)
        {
            if (TouchesBorder(run))
            {
                state.MarkSpilled();
                SpilledRuns.Add(run);
                _spillQueue.Enqueue(run);
            }
        }
    }

    // ========================================================================
    // Phase 4 — Propagate spills upward
    // ========================================================================

    private bool CheckDependency(Run upper, Run lower)
    {
        var belowUpper = _matrix.GetRunsBelow(upper);

        for (int x = upper.Start; x <= upper.End; x++)
        {
            if (!lower.ContainsX(x)) continue;

            bool solidSupport = false;
            var emptySupports = new List<Run>();

            foreach (var b in belowUpper)
            {
                if (b.ContainsX(x))
                {
                    if (b.IsSolid)
                        solidSupport = true;
                    else if (_states.ContainsKey(b))
                        emptySupports.Add(b);
                }
            }

            if (!solidSupport && emptySupports.Count == 1 &&
                emptySupports[0] == lower)
                return true;
        }
        return false;
    }

    private void PropagateSpills()
    {
        Dbg("Propagating spills upward...");

        while (_spillQueue.Count > 0)
        {
            var spilled = _spillQueue.Dequeue();

            foreach (var above in _matrix.GetEmptyRunsAbove(spilled))
            {
                if (!_states.TryGetValue(above, out var aboveState)) continue;
                if (aboveState.Status != HoldingStatus.Unknown) continue;

                if (CheckDependency(above, spilled))
                {
                    aboveState.MarkSpilled();
                    SpilledRuns.Add(above);
                    _spillQueue.Enqueue(above);
                }
            }
        }
    }

    // ========================================================================
    // Phase 5 — Queue valid bases
    // ========================================================================

    private void FindAndQueueValidBases()
    {
        Dbg("Queueing valid bases...");

        var valid = new HashSet<Run>();

        if (FillAllPockets)
        {
            // Pocket Fill mode: ALL base runs with non-spilled status are valid
            foreach (var baseRun in BaseRuns)
            {
                var st = _states[baseRun];
                if (st.Status == HoldingStatus.Unknown)
                {
                    valid.Add(baseRun);
                    _fillQueue.Enqueue(baseRun);
                }
            }
        }
        else
        {
            // Rain Fill mode: only reachable bases are valid
            foreach (var baseRun in ReachableBases)
            {
                var st = _states[baseRun];
                if (st.Status == HoldingStatus.Unknown)
                {
                    valid.Add(baseRun);
                    _fillQueue.Enqueue(baseRun);
                }
            }
        }

        BaseRuns = valid;
    }

    // ========================================================================
    // Phase 6 — Propagate fills upward
    // ========================================================================

    private void PropagateFills()
    {
        Dbg("Propagating fills upward...");

        while (_fillQueue.Count > 0)
        {
            var run = _fillQueue.Dequeue();
            var state = _states[run];

            if (state.Status != HoldingStatus.Unknown) continue;
            if (!state.IsReadyToFill) continue;

            state.MarkCanHold();
            FilledRuns.Add(run);

            foreach (var above in _matrix.GetEmptyRunsAbove(run))
            {
                if (!_states.TryGetValue(above, out var aboveState)) continue;
                if (aboveState.Status != HoldingStatus.Unknown) continue;

                aboveState.ReceiveSupportConfirmation();
                if (aboveState.IsReadyToFill)
                    _fillQueue.Enqueue(above);
            }
        }
    }

    // ========================================================================
    // Main solve
    // ========================================================================

    /// <summary>
    /// Runs the 6-phase solve algorithm.
    /// </summary>
    /// <param name="seedPositions">
    /// Custom seed positions for Phase 1 raindrop reachability.
    /// If null, seeds from the entire top row (default rain behavior).
    /// For shape-aware fills, pass the topmost empty tile per column
    /// that belongs to the shape (e.g. ellipse, diamond).
    /// </param>
    public HashSet<Run> Solve(HashSet<(int x, int y)>? seedPositions = null)
    {
        Dbg(new string('=', 50));
        Dbg("WATER FILL SOLVER");
        Dbg(new string('=', 50));
        Dbg($"Bounds: x=[{_x0},{_x1}], y=[{_y0},{_y1}]");

        // Phase 1-2
        InitStates(seedPositions);
        Dbg($"Initialized {_states.Count} empty run states");
        Dbg($"Reachable bases: {ReachableBases.Count}");

        // Phase 3
        MarkBorderSpills();
        Dbg($"Border spills: {SpilledRuns.Count}");

        // Phase 4
        PropagateSpills();
        Dbg($"Total spills after propagation: {SpilledRuns.Count}");

        // Phase 5
        FindAndQueueValidBases();
        Dbg($"Valid bases: {BaseRuns.Count}");

        // Phase 6
        PropagateFills();
        Dbg($"Filled runs: {FilledRuns.Count}");

        Dbg(new string('=', 50));
        Dbg($"RESULT: {FilledRuns.Count} runs can hold water");
        Dbg(new string('=', 50));

        return FilledRuns;
    }

    // ========================================================================
    // Post-solve queries
    // ========================================================================

    public HashSet<(int x, int y)> GetWaterCoordinates()
    {
        var coords = new HashSet<(int, int)>();
        foreach (var run in FilledRuns)
            for (int x = run.Start; x <= run.End; x++)
                coords.Add((x, run.Row));
        return coords;
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private bool TouchesBorder(Run r) =>
        r.Start <= _x0 || r.End >= _x1 || r.Row >= _y1;

    private void Dbg(string msg)
    {
        if (Debug) System.Diagnostics.Debug.WriteLine($"[BasinFill] {msg}");
    }
}
