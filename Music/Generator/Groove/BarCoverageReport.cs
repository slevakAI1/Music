// AI: purpose=Immutable snapshot of per-bar coverage analysis with optional diagnostic metrics.
// AI: invariants=Coverage map is dense (all bars present); diagnostic counts are non-negative.
// AI: deps=Produced by PartTrackBarCoverageAnalyzer.AnalyzeWithDiagnostics; consumers use for UI and validation.

namespace Music.Generator.Groove
{
    // AI: Record is immutable; coverage map and per-bar dictionaries are readonly snapshots.
    // AI: Diagnostic counts help identify data quality issues without throwing exceptions.
    public sealed record BarCoverageReport(
        IReadOnlyDictionary<int, BarFillState> Coverage,
        IReadOnlyDictionary<int, int> EventCountPerBar,
        IReadOnlyDictionary<int, long> OccupiedTicksPerBar,
        int OutOfRangeEventCount,
        int DegenerateEventCount,
        int NegativeTickEventCount);
}
