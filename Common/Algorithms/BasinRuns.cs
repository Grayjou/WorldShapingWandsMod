// ============================================================================
// BasinRuns.cs — Run-length encoding for basin water fill (C# port)
//
// Direct port of basin_runs.py.
// Convention: 0 = empty (air/water), 1 = solid (ground)
//
// Ported from: C:\Users\RYZEN 9\Documents\Cloned\BasinFill\ports\csharp\
// Namespace changed from BasinFill → WorldShapingWandsMod.Common.Algorithms
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace WorldShapingWandsMod.Common.Algorithms;

// ============================================================================
// Enums
// ============================================================================

public enum RunType : byte
{
    Empty = 0,   // Can potentially hold water
    Solid = 1    // Boundary / support
}

// ============================================================================
// Run — A contiguous horizontal segment of cells of the same type
// ============================================================================

public sealed class Run : IEquatable<Run>
{
    public int Row      { get; }
    public int Start    { get; }
    public int Length   { get; }
    public RunType Type { get; }

    public int End => Start + Length - 1;
    public bool IsEmpty => Type == RunType.Empty;
    public bool IsSolid => Type == RunType.Solid;

    public Run(int row, int start, int length, RunType type)
    {
        Row    = row;
        Start  = start;
        Length = length;
        Type   = type;
    }

    public bool ContainsX(int x) => x >= Start && x <= End;

    public bool Overlaps(Run other) =>
        !(End < other.Start || other.End < Start);

    // Equality by value (row, start, length, type)
    public bool Equals(Run? other) =>
        other is not null &&
        Row == other.Row && Start == other.Start &&
        Length == other.Length && Type == other.Type;

    public override bool Equals(object? obj) => Equals(obj as Run);

    public override int GetHashCode() =>
        HashCode.Combine(Row, Start, Length, Type);

    public override string ToString()
    {
        var t = IsEmpty ? "E" : "S";
        return $"Run(y={Row}, x={Start}..{End}, {t})";
    }
}

// ============================================================================
// RunRow — A single row decomposed into runs
// ============================================================================

public sealed class RunRow
{
    public int RowIndex { get; }
    public int Width    { get; }
    public List<Run> Runs { get; }

    private Dictionary<int, int>? _xToRunIdx;

    public RunRow(int rowIndex, int width, List<Run> runs)
    {
        RowIndex = rowIndex;
        Width    = width;
        Runs     = runs;
    }

    public void BuildIndex()
    {
        _xToRunIdx = new Dictionary<int, int>();
        for (int i = 0; i < Runs.Count; i++)
        {
            var run = Runs[i];
            for (int x = run.Start; x <= run.End; x++)
                _xToRunIdx[x] = i;
        }
    }

    public Run? GetRunAt(int x)
    {
        if (_xToRunIdx == null) BuildIndex();
        return _xToRunIdx!.TryGetValue(x, out int idx) ? Runs[idx] : null;
    }

    public IEnumerable<Run> EmptyRuns => Runs.Where(r => r.IsEmpty);
    public IEnumerable<Run> SolidRuns => Runs.Where(r => r.IsSolid);
}

// ============================================================================
// RunMatrix — Entire grid as runs + adjacency graph
// ============================================================================

public sealed class RunMatrix
{
    public int Width  { get; private set; }
    public int Height { get; private set; }
    public List<RunRow> Rows { get; private set; } = new();

    // Adjacency caches
    private Dictionary<Run, List<Run>> _runsBelow = new();
    private Dictionary<Run, List<Run>> _runsAbove = new();
    private bool _adjacencyBuilt;

    // All runs by type
    private List<Run> _allEmpty = new();
    private List<Run> _allSolid = new();

    // ========================================================================
    // Factory
    // ========================================================================

    public static RunMatrix FromGrid(int[,] grid)
    {
        var m = new RunMatrix();
        int height = grid.GetLength(0);
        int width  = grid.GetLength(1);
        if (height == 0 || width == 0) return m;

        m.Height = height;
        m.Width  = width;

        for (int y = 0; y < height; y++)
        {
            var rowData = new int[width];
            for (int x = 0; x < width; x++)
                rowData[x] = grid[y, x];

            var runs = ExtractRuns(rowData, y);
            var rr = new RunRow(y, width, runs);
            rr.BuildIndex();
            m.Rows.Add(rr);

            foreach (var run in runs)
            {
                if (run.IsEmpty) m._allEmpty.Add(run);
                else             m._allSolid.Add(run);
            }
        }

        return m;
    }

    private static List<Run> ExtractRuns(int[] rowData, int rowIndex)
    {
        var runs = new List<Run>();
        if (rowData.Length == 0) return runs;

        int currentVal = rowData[0];
        int startX = 0;
        int len = 1;

        for (int x = 1; x < rowData.Length; x++)
        {
            if (rowData[x] == currentVal)
            {
                len++;
            }
            else
            {
                var rt = (currentVal == 0) ? RunType.Empty : RunType.Solid;
                runs.Add(new Run(rowIndex, startX, len, rt));
                currentVal = rowData[x];
                startX = x;
                len = 1;
            }
        }

        var finalRt = (currentVal == 0) ? RunType.Empty : RunType.Solid;
        runs.Add(new Run(rowIndex, startX, len, finalRt));
        return runs;
    }

    // ========================================================================
    // Adjacency
    // ========================================================================

    public void BuildAdjacency()
    {
        if (_adjacencyBuilt) return;
        _runsBelow.Clear();
        _runsAbove.Clear();

        for (int y = 0; y < Height; y++)
        {
            foreach (var run in Rows[y].Runs)
            {
                // Below
                if (y < Height - 1)
                {
                    var below = Rows[y + 1].Runs
                        .Where(o => run.Overlaps(o)).ToList();
                    _runsBelow[run] = below;
                }
                else
                {
                    _runsBelow[run] = new List<Run>();
                }

                // Above
                if (y > 0)
                {
                    var above = Rows[y - 1].Runs
                        .Where(o => run.Overlaps(o)).ToList();
                    _runsAbove[run] = above;
                }
                else
                {
                    _runsAbove[run] = new List<Run>();
                }
            }
        }

        _adjacencyBuilt = true;
    }

    public List<Run> GetRunsBelow(Run run) =>
        _runsBelow.TryGetValue(run, out var v) ? v : new List<Run>();

    public List<Run> GetRunsAbove(Run run) =>
        _runsAbove.TryGetValue(run, out var v) ? v : new List<Run>();

    public List<Run> GetEmptyRunsBelow(Run run) =>
        GetRunsBelow(run).Where(r => r.IsEmpty).ToList();

    public List<Run> GetEmptyRunsAbove(Run run) =>
        GetRunsAbove(run).Where(r => r.IsEmpty).ToList();

    // ========================================================================
    // Queries
    // ========================================================================

    public Run? GetRunAt(int x, int y) =>
        (y >= 0 && y < Height) ? Rows[y].GetRunAt(x) : null;

    public IReadOnlyList<Run> AllEmptyRuns => _allEmpty;
    public IReadOnlyList<Run> AllSolidRuns => _allSolid;

    // ========================================================================
    // Border checks
    // ========================================================================

    public bool TouchesLeftBorder(Run r, int x0 = 0) => r.Start <= x0;
    public bool TouchesRightBorder(Run r)  => r.End >= Width - 1;
    public bool TouchesBottomBorder(Run r) => r.Row >= Height - 1;

    public bool TouchesAnyBorder(Run r, int x0 = 0) =>
        TouchesLeftBorder(r, x0) || TouchesRightBorder(r) || TouchesBottomBorder(r);

    public bool IsSupportedBySolid(Run r)
    {
        var below = GetRunsBelow(r);
        if (below.Count == 0) return false;

        var covered = new HashSet<int>();
        foreach (var b in below)
            if (b.IsSolid)
                for (int x = b.Start; x <= b.End; x++)
                    if (r.ContainsX(x)) covered.Add(x);

        return covered.Count == r.Length;
    }

    // ========================================================================
    // Reconstruction
    // ========================================================================

    public int[,] ToGrid()
    {
        var grid = new int[Height, Width];
        foreach (var row in Rows)
            foreach (var run in row.Runs)
                if (run.IsSolid)
                    for (int x = run.Start; x <= run.End; x++)
                        grid[run.Row, x] = 1;
        return grid;
    }
}
