// AI: purpose=Deterministic variation query implementation precomputing and caching all section variation plans.
// AI: invariants=Plans precomputed at construction; deterministic for same inputs; thread-safe immutable reads; plans cached in dictionary.
// AI: deps=Consumes SectionTrack, ITensionQuery; uses SectionVariationPlanner.ComputePlans; implements IVariationQuery.
// AI: change=Energy removed - variation based on section type and tension only.

namespace Music.Generator;

/// <summary>
/// Deterministic variation query implementation that precomputes and caches
/// SectionVariationPlan for all sections using SectionVariationPlanner.
/// Provides efficient O(1) lookup for generators.
/// </summary>
/// <remarks>
/// Story 7.6.4 acceptance criteria:
/// - Precomputes and caches plans for whole SectionTrack
/// - Determinism: same inputs yield same plans
/// - Mirrors architecture of EnergyArc caching and DeterministicTensionQuery
/// - Thread-safe immutable reads
/// </remarks>
public sealed class DeterministicVariationQuery : IVariationQuery
{
    private readonly Dictionary<int, SectionVariationPlan> _plans;
    private readonly int _sectionCount;

    /// <summary>
    /// Creates a DeterministicVariationQuery by precomputing all variation plans.
    /// </summary>
    /// <param name="sectionTrack">The song's section track.</param>
    /// <param name="tensionQuery">Tension query (for transition hints driving variation decisions).</param>
    /// <param name="grooveName">Groove/style name for deterministic decisions.</param>
    /// <param name="seed">Seed for deterministic tie-breaking.</param>
    public DeterministicVariationQuery(
        SectionTrack sectionTrack,
        ITensionQuery tensionQuery,
        string grooveName,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(sectionTrack);
        ArgumentNullException.ThrowIfNull(tensionQuery);
        ArgumentNullException.ThrowIfNull(grooveName);

        _sectionCount = sectionTrack.Sections.Count;

        // Precompute all plans using SectionVariationPlanner
        var planList = SectionVariationPlanner.ComputePlans(
            sectionTrack,
            tensionQuery,
            grooveName,
            seed);

        // Cache in dictionary for O(1) lookup
        _plans = new Dictionary<int, SectionVariationPlan>(_sectionCount);
        for (int i = 0; i < planList.Count; i++)
        {
            _plans[i] = planList[i];
        }
    }

    /// <inheritdoc/>
    public SectionVariationPlan GetVariationPlan(int absoluteSectionIndex)
    {
        ValidateSectionIndex(absoluteSectionIndex);
        return _plans[absoluteSectionIndex];
    }

    /// <inheritdoc/>
    public bool HasVariationData(int absoluteSectionIndex)
    {
        return absoluteSectionIndex >= 0 && absoluteSectionIndex < _sectionCount;
    }

    /// <inheritdoc/>
    public int SectionCount => _sectionCount;

    private void ValidateSectionIndex(int absoluteSectionIndex)
    {
        if (absoluteSectionIndex < 0 || absoluteSectionIndex >= _sectionCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteSectionIndex),
                $"Section index {absoluteSectionIndex} is out of range [0..{_sectionCount - 1}]");
        }
    }
}
