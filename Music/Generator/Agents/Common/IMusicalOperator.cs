// AI: purpose=Generic operator interface; all instrument agents implement specialized versions.
// AI: invariants=OperatorId stable across runs; Score returns [0.0..1.0]; CanApply is fast pre-filter.
// AI: deps=Generic over TCandidate; agent-specific contexts extend AgentContext.
// AI: change=Extend with additional methods if cross-cutting operator behaviors emerge.

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Generic interface for musical operators that generate and score candidates.
    /// 
    /// Operators are the decision-makers in the agent architecture:
    /// - They generate candidates procedurally (not from a pattern library)
    /// - They score candidates based on context
    /// - Selection engine picks from valid, scored options
    /// 
    /// Each instrument agent (Drums, Guitar, Keys, Bass, Vocals) implements
    /// specialized operators with instrument-specific TCandidate types.
    /// </summary>
    /// <typeparam name="TCandidate">The candidate type produced by this operator.
    /// Examples: DrumCandidate, GuitarCandidate, KeysCandidate.</typeparam>
    public interface IMusicalOperator<TCandidate>
    {
        /// <summary>
        /// Stable string identifier for this operator.
        /// Must be unique within an agent and consistent across runs for determinism.
        /// Format recommendation: "{InstrumentPrefix}{OperatorName}" (e.g., "DrumGhostBeforeBackbeat").
        /// </summary>
        string OperatorId { get; }

        /// <summary>
        /// Classification of this operator's functional category.
        /// Used by selection engine for family-level weighting and filtering.
        /// </summary>
        OperatorFamily OperatorFamily { get; }

        /// <summary>
        /// Fast pre-filter check to determine if this operator can apply in the given context.
        /// Should be cheap to evaluate (no RNG, no complex computation).
        /// Returns false to skip candidate generation entirely.
        /// </summary>
        /// <param name="context">Current agent context with bar/section/energy info.</param>
        /// <returns>True if operator should generate candidates; false to skip.</returns>
        bool CanApply(AgentContext context);

        /// <summary>
        /// Generates candidate additions/modifications for the current context.
        /// May return zero candidates if context doesn't warrant any.
        /// Generation should be deterministic given the same context and RNG state.
        /// </summary>
        /// <param name="context">Current agent context.</param>
        /// <returns>Enumerable of candidates (lazy evaluation encouraged).</returns>
        IEnumerable<TCandidate> GenerateCandidates(AgentContext context);

        /// <summary>
        /// Scores a candidate for selection (0.0 = worst, 1.0 = best).
        /// 
        /// Final selection uses: finalScore = Score * styleWeight * (1.0 - memoryPenalty)
        /// 
        /// Scoring should consider:
        /// - Musical appropriateness for the context
        /// - Density contribution vs. target
        /// - Stylistic fit
        /// </summary>
        /// <param name="candidate">The candidate to score.</param>
        /// <param name="context">Current agent context for contextual scoring.</param>
        /// <returns>Score in range [0.0, 1.0].</returns>
        double Score(TCandidate candidate, AgentContext context);
    }
}
