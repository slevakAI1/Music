// AI: purpose=Define candidate source interface for groove system (Story B4).
// AI: invariants=Implementations must be deterministic; same inputs => same output.
// AI: deps=GrooveBarContext for bar context; GrooveCandidateGroup for return type.
// AI: change=Story B4 acceptance criteria: abstraction layer for future operator-based drummer logic.

namespace Music.Generator
{
    /// <summary>
    /// Interface for providing candidate groups to the groove engine.
    /// Story B4: Operator Candidate Source Hook - allows future operator-based drummer logic
    /// to supply candidates without changing the engine.
    /// </summary>
    public interface IGrooveCandidateSource
    {
        /// <summary>
        /// Gets candidate groups for a specific bar and role.
        /// </summary>
        /// <param name="barContext">Bar context with section, segment profile, and phrase position.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare", "Bass"). See GrooveRoles constants.</param>
        /// <returns>
        /// List of candidate groups available for this bar and role.
        /// Groups should already be merged from hierarchical layers and filtered by enabled tags.
        /// Empty list if no candidates available.
        /// </returns>
        /// <remarks>
        /// Implementations should:
        /// - Be deterministic: same inputs => same output
        /// - Apply layer merging if using hierarchical catalogs
        /// - Apply tag-based filtering if applicable
        /// - Return groups in deterministic order
        /// 
        /// The engine will consume these groups for weighted selection.
        /// </remarks>
        IReadOnlyList<GrooveCandidateGroup> GetCandidateGroups(
            GrooveBarContext barContext,
            string role);
    }
}
