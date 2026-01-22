// AI: purpose=Abstract base for all drum operators; provides common CanApply checks and helper methods.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily; base CanApply checks null context and ActiveRoles.
// AI: deps=IDrumOperator interface, DrummerContext, DrumCandidate; consumed by all MicroAddition/SubdivisionTransform operators.
// AI: change=Story 3.1; extend with additional helper methods as operator patterns emerge.

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
        public abstract Common.OperatorFamily OperatorFamily { get; }

        /// <summary>
        /// Minimum energy level required for this operator to apply.
        /// Subclasses can override to set operator-specific thresholds.
        /// </summary>
        protected virtual double MinEnergyThreshold => 0.0;

        /// <summary>
        /// Maximum energy level allowed for this operator to apply.
        /// Subclasses can override to set operator-specific thresholds.
        /// </summary>
        protected virtual double MaxEnergyThreshold => 1.0;

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

            if (context is DrummerContext drummerContext)
            {
                return CanApply(drummerContext);
            }

            // Non-drummer context: check energy only
            return context.EnergyLevel >= MinEnergyThreshold &&
                   context.EnergyLevel <= MaxEnergyThreshold;
        }

        /// <inheritdoc/>
        public virtual bool CanApply(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Energy check
            if (context.EnergyLevel < MinEnergyThreshold || context.EnergyLevel > MaxEnergyThreshold)
                return false;

            // Required role check
            if (RequiredRole is not null && !context.ActiveRoles.Contains(RequiredRole))
                return false;

            // 16th grid check
            if (Requires16thGrid && context.HatSubdivision != HatSubdivision.Sixteenth)
                return false;

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
    }
}
