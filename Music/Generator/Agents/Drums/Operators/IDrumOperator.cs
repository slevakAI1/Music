// AI: purpose=Drum-specific operator interface extending IMusicalOperator<DrumCandidate>; specialized for drummer agent.
// AI: invariants=Same determinism rules as base interface; OperatorId must be unique within DrumOperatorRegistry.
// AI: deps=IMusicalOperator<TCandidate>, DrumCandidate, DrummerContext; consumed by DrummerCandidateSource.
// AI: change=Story 2.4; base interface for all drum operators in Stories 3.1-3.5.

using Music.Generator.Agents.Common;
using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums
{
    /// <summary>
    /// Drum-specific operator interface extending IMusicalOperator with DrumCandidate as output.
    /// All drum operators (Stories 3.1-3.5) implement this interface for use with DrummerCandidateSource.
    /// Story 2.4: Implement Drummer Candidate Source.
    /// </summary>
    /// <remarks>
    /// Drum operators generate DrumCandidate instances that are:
    /// - Mapped to GrooveOnsetCandidate by DrumCandidateMapper
    /// - Grouped by OperatorFamily for structured selection
    /// - Filtered by PhysicalityFilter for playability
    /// 
    /// Operators must be deterministic: same DrummerContext → same candidates.
    /// </remarks>
    public interface IDrumOperator : IMusicalOperator<DrumCandidate>
    {
        /// <summary>
        /// Drum-specific pre-filter check using DrummerContext.
        /// Default implementation delegates to base CanApply with AgentContext.
        /// Override for drum-specific context checks (hat mode, fill window, etc.).
        /// </summary>
        /// <param name="context">Drummer-specific context with active roles, hat mode, etc.</param>
        /// <returns>True if operator should generate candidates for this context.</returns>
        bool CanApply(DrummerContext context)
        {
            // Default: delegate to base CanApply
            return CanApply((AgentContext)context);
        }

        /// <summary>
        /// Generates drum candidates using drummer-specific context.
        /// Default implementation delegates to base GenerateCandidates with AgentContext.
        /// Override for drum-specific generation logic.
        /// </summary>
        /// <param name="context">Drummer-specific context.</param>
        /// <returns>Enumerable of DrumCandidate instances.</returns>
        IEnumerable<DrumCandidate> GenerateCandidates(DrummerContext context)
        {
            // Default: delegate to base GenerateCandidates
            return GenerateCandidates((AgentContext)context);
        }

        /// <summary>
        /// Scores a drum candidate using drummer-specific context.
        /// Default implementation delegates to base Score with AgentContext.
        /// Override for drum-specific scoring logic.
        /// </summary>
        /// <param name="candidate">Candidate to score.</param>
        /// <param name="context">Drummer-specific context.</param>
        /// <returns>Score in [0.0, 1.0].</returns>
        double Score(DrumCandidate candidate, DrummerContext context)
        {
            // Default: delegate to base Score
            return Score(candidate, (AgentContext)context);
        }
    }
}
