// AI: purpose=Define candidate source interface for drum generator (Story 4.2).
// AI: invariants=Implementations must be deterministic; same inputs => same output.
// AI: deps=BarContext for bar context; DrumCandidateGroup for return type.
// AI: change=Story 4.2: moved from Groove namespace; Drum generator owns candidate contracts.


using Music.Generator.Agents.Common;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Interface for providing candidate groups to the drum generator.
    /// Story 4.2: Moved from Groove namespace - Drum generator owns candidate contracts.
    /// Enables operator-based drummer logic to supply candidates.
    /// </summary>
    public interface IDrumCandidateSource
    {
        /// <summary>
        /// Gets candidate groups for a specific bar and role.
        /// </summary>
        /// <param name="barContext">Bar context with section, segment profile, and phrase position.</param>
        /// <param name="role">Role name (e.g., "Kick", "Snare", "ClosedHat"). See GrooveRoles constants.</param>
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
        /// The generator will consume these groups for weighted selection.
        /// </remarks>
        IReadOnlyList<DrumCandidateGroup> GetCandidateGroups(
            BarContext barContext,
            string role);
    }
}
