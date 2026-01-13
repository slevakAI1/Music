// AI: purpose=Public stable API for querying section variation plans without requiring planner internals.
// AI: invariants=All queries deterministic for same sectionIndex; thread-safe immutable reads; plans precomputed and cached.
// AI: deps=Implemented by DeterministicVariationQuery (Story 7.6.4); consumed by role generators; produces SectionVariationPlan from SectionVariationPlanner (Story 7.6.3).
// AI: change=Story 7.6.4 query surface; Story 7.6.5 will add parameter application adapters consuming this query.

namespace Music.Generator;

/// <summary>
/// Public API for querying section variation plans.
/// Provides stable contract for role generators to access variation plans without
/// needing knowledge of internal variation planning/computation.
/// All queries are deterministic and thread-safe.
/// </summary>
/// <remarks>
/// Story 7.6.4 acceptance criteria:
/// - Stable query surface for generators: GetVariationPlan(sectionIndex)
/// - Plans are precomputed and cached (same pattern as EnergyArc and ITensionQuery)
/// - Immutable and thread-safe
/// - Optional: if not provided, generation remains unchanged
/// </remarks>
public interface IVariationQuery
{
    /// <summary>
    /// Gets the variation plan for a specific section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>Immutable variation plan for the section.</returns>
    SectionVariationPlan GetVariationPlan(int absoluteSectionIndex);

    /// <summary>
    /// Checks if variation data is available for a section.
    /// </summary>
    /// <param name="absoluteSectionIndex">Absolute 0-based section index in the song.</param>
    /// <returns>True if variation plan exists for this section.</returns>
    bool HasVariationData(int absoluteSectionIndex);

    /// <summary>
    /// Gets the total number of sections with variation plans.
    /// </summary>
    int SectionCount { get; }
}
