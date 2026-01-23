// AI: purpose=Collects groove decision data during pipeline and builds GrooveBarDiagnostics (Story G1).
// AI: invariants=Mutable during collection, immutable output; zero-cost when disabled (collector is null).
// AI: deps=GrooveBarDiagnostics and sub-records; RandomPurpose for RNG stream names.
// AI: change=Story G1 provides opt-in diagnostics collection without changing generation behavior.

namespace Music.Generator.Groove;

/// <summary>
/// Collects groove decision data during pipeline execution.
/// Story G1: Opt-in diagnostics collection for decision tracing.
/// Usage: Create when diagnostics enabled; null when disabled for zero-cost.
/// </summary>
public sealed class GrooveDiagnosticsCollector
{
    private readonly int _barNumber;
    private readonly string _role;
    private readonly List<string> _enabledTags = [];
    private readonly List<FilterDecision> _filterDecisions = [];
    private readonly List<SelectionDecision> _selectionDecisions = [];
    private readonly List<PruneDecision> _pruneDecisions = [];

    private int _candidateGroupCount;
    private int _totalCandidateCount;
    private DensityTargetDiagnostics? _densityTarget;
    private int _baseCount;
    private int _variationCount;
    private int _finalCount;

    /// <summary>
    /// Creates a new diagnostics collector for a specific bar and role.
    /// </summary>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="role">Role name (e.g., "Kick", "Snare").</param>
    public GrooveDiagnosticsCollector(int barNumber, string role)
    {
        _barNumber = barNumber;
        _role = role;
    }

    /// <summary>
    /// Records the enabled tags after phrase/segment/policy resolution.
    /// </summary>
    public void RecordEnabledTags(IEnumerable<string> tags)
    {
        _enabledTags.Clear();
        _enabledTags.AddRange(tags);
    }

    /// <summary>
    /// Records candidate pool statistics after filtering.
    /// </summary>
    /// <param name="groupCount">Number of candidate groups.</param>
    /// <param name="candidateCount">Total number of individual candidates.</param>
    public void RecordCandidatePool(int groupCount, int candidateCount)
    {
        _candidateGroupCount = groupCount;
        _totalCandidateCount = candidateCount;
    }

    /// <summary>
    /// Records a filter decision for a candidate that was excluded.
    /// </summary>
    /// <param name="candidateId">Stable identifier for the candidate.</param>
    /// <param name="reason">Reason for filtering (e.g., "tag mismatch", "never-add", "grid invalid").</param>
    public void RecordFilter(string candidateId, string reason)
    {
        _filterDecisions.Add(new FilterDecision(candidateId, reason));
    }

    /// <summary>
    /// Records density target computation for this bar/role.
    /// </summary>
    public void RecordDensityTarget(
        double density01,
        int maxEventsPerBar,
        int targetCount,
        double multiplier = 1.0,
        bool policyOverrideApplied = false)
    {
        _densityTarget = new DensityTargetDiagnostics
        {
            Density01 = density01,
            MaxEventsPerBar = maxEventsPerBar,
            TargetCount = targetCount,
            Multiplier = multiplier,
            PolicyOverrideApplied = policyOverrideApplied
        };
    }

    /// <summary>
    /// Records a candidate selection with weight and RNG stream used.
    /// </summary>
    /// <param name="candidateId">Stable identifier for the candidate.</param>
    /// <param name="weight">Computed weight used for selection.</param>
    /// <param name="rngPurpose">The RandomPurpose enum value used for RNG.</param>
    public void RecordSelection(string candidateId, double weight, RandomPurpose rngPurpose)
    {
        _selectionDecisions.Add(new SelectionDecision(candidateId, weight, rngPurpose.ToString()));
    }

    /// <summary>
    /// Records a candidate selection with weight and RNG stream name.
    /// </summary>
    /// <param name="candidateId">Stable identifier for the candidate.</param>
    /// <param name="weight">Computed weight used for selection.</param>
    /// <param name="rngStreamName">Name of the RNG stream (e.g., from RandomPurpose.ToString()).</param>
    public void RecordSelection(string candidateId, double weight, string rngStreamName)
    {
        _selectionDecisions.Add(new SelectionDecision(candidateId, weight, rngStreamName));
    }

    /// <summary>
    /// Records a prune event for an onset that was removed or considered for removal.
    /// </summary>
    /// <param name="onsetId">Stable identifier for the onset.</param>
    /// <param name="reason">Reason for pruning (e.g., "cap violated", "tie-break", "lowest-scored").</param>
    /// <param name="wasProtected">Whether the onset was protected (IsMustHit, IsNeverRemove, or IsProtected).</param>
    public void RecordPrune(string onsetId, string reason, bool wasProtected)
    {
        _pruneDecisions.Add(new PruneDecision(onsetId, reason, wasProtected));
    }

    /// <summary>
    /// Records onset list counts at different pipeline stages.
    /// </summary>
    /// <param name="baseCount">Count of base onsets from anchor layer.</param>
    /// <param name="variationCount">Count of onsets added by variation selection.</param>
    /// <param name="finalCount">Count of final onsets after constraint enforcement.</param>
    public void RecordOnsetCounts(int baseCount, int variationCount, int finalCount)
    {
        _baseCount = baseCount;
        _variationCount = variationCount;
        _finalCount = finalCount;
    }

    /// <summary>
    /// Builds the immutable GrooveBarDiagnostics record from collected data.
    /// Call after all pipeline stages have recorded their data.
    /// </summary>
    /// <returns>Immutable diagnostics record.</returns>
    public GrooveBarDiagnostics Build()
    {
        return new GrooveBarDiagnostics
        {
            BarNumber = _barNumber,
            Role = _role,
            EnabledTags = _enabledTags.ToList(),
            CandidateGroupCount = _candidateGroupCount,
            TotalCandidateCount = _totalCandidateCount,
            FiltersApplied = _filterDecisions.ToList(),
            DensityTarget = _densityTarget ?? new DensityTargetDiagnostics
            {
                Density01 = 0.0,
                MaxEventsPerBar = 0,
                TargetCount = 0
            },
            SelectedCandidates = _selectionDecisions.ToList(),
            PruneEvents = _pruneDecisions.ToList(),
            FinalOnsetSummary = new OnsetListSummary
            {
                BaseCount = _baseCount,
                VariationCount = _variationCount,
                FinalCount = _finalCount
            }
        };
    }

    /// <summary>
    /// Creates a stable candidate ID from group and candidate information.
    /// Uses GroupId:Beat format for uniqueness within a bar.
    /// </summary>
    /// <param name="groupId">Group identifier.</param>
    /// <param name="beat">Beat position of the candidate.</param>
    /// <returns>Stable candidate identifier string.</returns>
    public static string MakeCandidateId(string groupId, decimal beat)
    {
        return $"{groupId}:{beat:F2}";
    }

    /// <summary>
    /// Creates a stable candidate ID using group and candidate index as fallback.
    /// </summary>
    /// <param name="groupIndex">Index of the group.</param>
    /// <param name="candidateIndex">Index of the candidate within the group.</param>
    /// <returns>Stable candidate identifier string.</returns>
    public static string MakeCandidateId(int groupIndex, int candidateIndex)
    {
        return $"Group_{groupIndex}:Candidate_{candidateIndex}";
    }

    /// <summary>
    /// Creates a stable onset ID from bar, role, and beat.
    /// </summary>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="role">Role name.</param>
    /// <param name="beat">Beat position.</param>
    /// <returns>Stable onset identifier string.</returns>
    public static string MakeOnsetId(int barNumber, string role, decimal beat)
    {
        return $"{barNumber}:{role}:{beat:F2}";
    }

    /// <summary>
    /// Creates a stable onset ID from a GrooveOnset.
    /// </summary>
    /// <param name="onset">The onset to create an ID for.</param>
    /// <returns>Stable onset identifier string.</returns>
    public static string MakeOnsetId(GrooveOnset onset)
    {
        ArgumentNullException.ThrowIfNull(onset);
        return MakeOnsetId(onset.BarNumber, onset.Role, onset.Beat);
    }
}
