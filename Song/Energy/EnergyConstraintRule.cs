// AI: purpose=Abstract base for energy constraint rules enforcing songwriting heuristics.
// AI: invariants=Evaluate must be deterministic (same context -> same result); must return non-null result.
// AI: deps=Subclassed by specific rules; consumed by EnergyConstraintPolicy; uses EnergyConstraintContext.

namespace Music.Generator
{
    /// <summary>
    /// Abstract base class for energy constraint rules.
    /// Each rule represents a single songwriting/arranging heuristic that can adjust section energy.
    /// Rules are deterministic: same input context always produces same output.
    /// </summary>
    public abstract class EnergyConstraintRule
    {
        /// <summary>
        /// Human-readable name of this rule.
        /// </summary>
        public abstract string RuleName { get; }

        /// <summary>
        /// Priority/strength of this rule for conflict resolution.
        /// Higher values take precedence when rules conflict.
        /// Default strength is 1.0. Range typically [0.0..2.0].
        /// </summary>
        public virtual double Strength => 1.0;

        /// <summary>
        /// Evaluates the rule against the given context and returns an adjusted energy value.
        /// </summary>
        /// <param name="context">Context containing section info and related energies.</param>
        /// <returns>
        /// Result containing adjusted energy (if rule applies) and optional diagnostic message.
        /// Returns NoOpinion if rule doesn't apply to this section.
        /// </returns>
        public abstract EnergyConstraintResult Evaluate(EnergyConstraintContext context);

        /// <summary>
        /// Helper to clamp energy to valid range [0..1].
        /// </summary>
        protected static double Clamp(double energy)
        {
            return Math.Clamp(energy, 0.0, 1.0);
        }

        /// <summary>
        /// Helper to blend two energy values with a strength factor.
        /// </summary>
        /// <param name="original">Original energy value.</param>
        /// <param name="suggested">Suggested energy value from rule.</param>
        /// <param name="strength">Blend strength [0..1]. 0 = keep original, 1 = use suggested.</param>
        protected static double Blend(double original, double suggested, double strength)
        {
            return Clamp(original + (suggested - original) * strength);
        }
    }
}
