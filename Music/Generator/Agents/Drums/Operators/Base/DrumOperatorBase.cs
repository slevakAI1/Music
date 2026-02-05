// AI: purpose=Abstract base for all drum operators; provides common CanApply checks and helper methods.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily; base CanApply validates context type.
// AI: deps=IDrumOperator interface, DrummerContext (minimal: Bar + cross-bar state), DrumCandidate.
// AI: change=Epic DrummerContext-Dedup; removed energy thresholds; bar properties accessed via context.Bar.


using Music.Generator;
using Music.Generator.Core;
using Music.Generator.Groove;
using Music.Generator.Material;

namespace Music.Generator.Agents.Drums.Operators
{
    /// <summary>
    /// Abstract base class for drum operators providing common functionality.
    /// Implements IDrumOperator with default behaviors that subclasses can override.
    /// Story 3.1-3.5: Base for all drum operator implementations.
    /// </summary>
    public abstract class DrumOperatorBase : IDrumOperator
    {
        /// <inheritdoc/>
        public abstract string OperatorId { get; }

        /// <inheritdoc/>
        public abstract OperatorFamily OperatorFamily { get; }

        /// <summary>
        /// Required drum role for this operator. Null = no role requirement.
        /// Subclasses override to specify required ActiveRoles.
        /// </summary>
        protected virtual string? RequiredRole => null;

        /// <summary>
        /// Whether operator requires 16th grid subdivision to apply.
        /// </summary>
        protected virtual bool Requires16thGrid => false;

        /// <inheritdoc/>
        public virtual bool CanApply(Common.AgentContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                return false;

            return CanApply(drummerContext);
        }

        /// <inheritdoc/>
        public virtual bool CanApply(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Required role check (if role is required, check against groove preset active roles)
            if (RequiredRole is not null)
            {
                // TODO: Access active roles from groove preset via context.Bar
                // For now, assume role is available (operators will self-gate if needed)
            }

            return true;
        }

        /// <inheritdoc/>
        public abstract IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context);

        /// <inheritdoc/>
        public virtual IEnumerable<DrumCandidate> GenerateCandidates(DrummerContext context)
        {
            return GenerateCandidates((Common.AgentContext)context);
        }

        /// <inheritdoc/>
        public virtual double Score(DrumCandidate candidate, Common.AgentContext context)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Score;
        }

        /// <inheritdoc/>
        public virtual double Score(DrumCandidate candidate, DrummerContext context)
        {
            return Score(candidate, (Common.AgentContext)context);
        }

        /// <summary>
        /// Creates a DrumCandidate with common fields set from this operator.
        /// </summary>
        protected DrumCandidate CreateCandidate(
            string role,
            int barNumber,
            decimal beat,
            OnsetStrength strength,
            double score,
            int? velocityHint = null,
            int? timingHint = null,
            DrumArticulation? articulationHint = null,
            FillRole fillRole = FillRole.None)
        {
            return new DrumCandidate
            {
                CandidateId = DrumCandidate.GenerateCandidateId(OperatorId, role, barNumber, beat, articulationHint),
                OperatorId = OperatorId,
                Role = role,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                VelocityHint = velocityHint,
                TimingHint = timingHint,
                ArticulationHint = articulationHint,
                FillRole = fillRole,
                Score = score
            };
        }

        /// <summary>
        /// Generates a deterministic velocity hint within the specified range.
        /// Uses bar number and beat for deterministic jitter.
        /// </summary>
        protected static int GenerateVelocityHint(int min, int max, int barNumber, decimal beat, int seed)
        {
            // Simple deterministic pseudo-random within range
            int hash = HashCode.Combine(barNumber, beat, seed);
            int range = max - min + 1;
            return min + (Math.Abs(hash) % range);
        }

        /// <summary>
        /// Applies motif presence score reduction. Story 9.3.
        /// Returns 1.0 if no motif is active or MotifPresenceMap is null.
        /// </summary>
        /// <param name="motifPresenceMap">Optional motif presence map for the current song.</param>
        /// <param name="bar">Canonical bar context.</param>
        /// <param name="reductionFactor">Score reduction factor when motif active (e.g., 0.5 = 50% reduction).</param>
        /// <returns>Multiplier to apply to score (1.0 = no reduction, 0.5 = 50% of original score).</returns>
        protected static double GetMotifScoreMultiplier(MotifPresenceMap? motifPresenceMap, Bar bar, double reductionFactor)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (motifPresenceMap is null)
                return 1.0;

            if (!motifPresenceMap.IsMotifActive(bar.BarNumber))
                return 1.0;

            // Reduction factor is how much to reduce by (e.g., 0.5 = reduce by 50%)
            // Multiplier is what remains (e.g., 1.0 - 0.5 = 0.5)
            return 1.0 - reductionFactor;
        }
    }
}
