// AI: purpose=Structured diagnostics for groove decision tracing from Story G1.
// AI: invariants=Opt-in only; when disabled, Diagnostics is null; when enabled, captures full trace.
// AI: deps=RandomPurpose for RNG stream names; GrooveOnset for onset references.
// AI: change=Story G1 defines structured diagnostics; future stories may add serialization.

namespace Music.Generator;

/// <summary>
/// Captures all decision-trace data for a single bar and role during groove generation.
/// Story G1: Add Groove Decision Trace (Opt-in, No Behavior Change).
/// When diagnostics disabled, GrooveBarPlan.Diagnostics remains null (zero allocations).
/// </summary>
public sealed record GrooveBarDiagnostics
{
    /// <summary>
    /// Bar number (1-based) this diagnostics applies to.
    /// </summary>
    public required int BarNumber { get; init; }

    /// <summary>
    /// Role name (e.g., "Kick", "Snare") this diagnostics applies to.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Enabled tags after phrase/segment/policy resolution.
    /// </summary>
    public required IReadOnlyList<string> EnabledTags { get; init; }

    /// <summary>
    /// Count of candidate groups considered for this bar/role.
    /// </summary>
    public required int CandidateGroupCount { get; init; }

    /// <summary>
    /// Total count of individual candidates across all groups.
    /// </summary>
    public required int TotalCandidateCount { get; init; }

    /// <summary>
    /// Filters applied to candidates with reasons for filtering.
    /// </summary>
    public required IReadOnlyList<FilterDecision> FiltersApplied { get; init; }

    /// <summary>
    /// Density target computation inputs and result.
    /// </summary>
    public required DensityTargetDiagnostics DensityTarget { get; init; }

    /// <summary>
    /// Candidates selected for variation with weights and RNG stream used.
    /// </summary>
    public required IReadOnlyList<SelectionDecision> SelectedCandidates { get; init; }

    /// <summary>
    /// Prune events with reasons and protection status.
    /// </summary>
    public required IReadOnlyList<PruneDecision> PruneEvents { get; init; }

    /// <summary>
    /// Final onset list summary counts.
    /// </summary>
    public required OnsetListSummary FinalOnsetSummary { get; init; }
}

/// <summary>
/// Records why a candidate was filtered out during groove generation.
/// </summary>
/// <param name="CandidateId">Stable identifier for the candidate (GroupId:Beat or GroupId:CandidateIndex).</param>
/// <param name="Reason">Human-readable reason for filtering (e.g., "tag mismatch", "never-add", "grid invalid").</param>
public sealed record FilterDecision(string CandidateId, string Reason);

/// <summary>
/// Records density target computation inputs and computed target count.
/// </summary>
public sealed record DensityTargetDiagnostics
{
    /// <summary>
    /// Base density value (0.0-1.0) from role density target.
    /// </summary>
    public required double Density01 { get; init; }

    /// <summary>
    /// Maximum events per bar from density target.
    /// </summary>
    public required int MaxEventsPerBar { get; init; }

    /// <summary>
    /// Computed target count after rounding and multipliers.
    /// </summary>
    public required int TargetCount { get; init; }

    /// <summary>
    /// Multiplier applied from section role presence defaults (default 1.0).
    /// </summary>
    public double Multiplier { get; init; } = 1.0;

    /// <summary>
    /// Whether policy override was applied for density.
    /// </summary>
    public bool PolicyOverrideApplied { get; init; }
}

/// <summary>
/// Records a candidate selection with weight and RNG stream used.
/// </summary>
/// <param name="CandidateId">Stable identifier for the candidate.</param>
/// <param name="Weight">Computed weight used for selection (ProbabilityBias * GroupBaseProbabilityBias).</param>
/// <param name="RngStreamUsed">Name of the RandomPurpose enum value used for RNG (e.g., "GrooveCandidatePick").</param>
public sealed record SelectionDecision(string CandidateId, double Weight, string RngStreamUsed);

/// <summary>
/// Records a prune event with reason and protection status.
/// </summary>
/// <param name="OnsetId">Stable identifier for the onset (bar:beat:role or similar).</param>
/// <param name="Reason">Reason for pruning (e.g., "cap violated", "tie-break", "lowest-scored").</param>
/// <param name="WasProtected">Whether the onset was protected (IsMustHit, IsNeverRemove, or IsProtected).</param>
public sealed record PruneDecision(string OnsetId, string Reason, bool WasProtected);

/// <summary>
/// Summary counts of onset lists at different pipeline stages.
/// </summary>
public sealed record OnsetListSummary
{
    /// <summary>
    /// Count of base onsets from anchor layer.
    /// </summary>
    public required int BaseCount { get; init; }

    /// <summary>
    /// Count of onsets added by variation selection.
    /// </summary>
    public required int VariationCount { get; init; }

    /// <summary>
    /// Count of final onsets after constraint enforcement.
    /// </summary>
    public required int FinalCount { get; init; }
}
