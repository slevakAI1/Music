// AI: purpose=MicroAddition operator adding extra kicks on offbeats (1.5, 3.5 or 16th variants).
// AI: invariants=VelocityHint in [60,80]; uses 8th positions (1.5, 3.5) or 16th (1.25/1.75) based on grid/energy.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1; adjust position selection logic based on listening tests.

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates additional kick hits on offbeats to add rhythmic drive.
    /// Uses 8th positions (1.5, 3.5) by default, or 16th positions (1.25/1.75, 3.25/3.75) 
    /// when 16th grid is allowed and energy is high.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// </summary>
    public sealed class KickDoubleOperator : DrumOperatorBase
    {
        private const int VelocityMin = 60;
        private const int VelocityMax = 80;
        private const double BaseScore = 0.55;
        private const double HighEnergyThreshold = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => "DrumKickDouble";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires moderate energy for kick doubles.
        /// </summary>
        protected override double MinEnergyThreshold => 0.35;

        /// <summary>
        /// Requires kick to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suppress during fill windows
            if (context.IsFillWindow)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(Common.AgentContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Determine whether to use 16th grid positions
            bool use16thGrid = drummerContext.HatSubdivision == HatSubdivision.Sixteenth &&
                              drummerContext.EnergyLevel >= HighEnergyThreshold;

            // Select positions based on grid
            var positions = use16thGrid
                ? Select16thPositions(drummerContext)
                : Select8thPositions(drummerContext);

            foreach (decimal beat in positions)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.BarNumber,
                    beat,
                    drummerContext.Seed);

                // Score based on energy and position naturalness
                double score = BaseScore * (0.6 + 0.4 * drummerContext.EnergyLevel);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: drummerContext.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Offbeat,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }

        /// <summary>
        /// Returns 8th note positions for kick doubles (1.5, 3.5).
        /// </summary>
        private static IEnumerable<decimal> Select8thPositions(DrummerContext context)
        {
            if (context.BeatsPerBar >= 2)
                yield return 1.5m;

            if (context.BeatsPerBar >= 4)
                yield return 3.5m;
        }

        /// <summary>
        /// Returns 16th note positions for kick doubles, selected deterministically.
        /// Uses RNG to pick either early (1.25, 3.25) or late (1.75, 3.75) variants.
        /// </summary>
        private static IEnumerable<decimal> Select16thPositions(DrummerContext context)
        {
            // Deterministic selection based on bar number and seed
            int hash = HashCode.Combine(context.BarNumber, context.Seed, "KickDouble16th");
            bool useEarlyVariant = (hash & 1) == 0;

            if (context.BeatsPerBar >= 2)
            {
                yield return useEarlyVariant ? 1.25m : 1.75m;
            }

            if (context.BeatsPerBar >= 4)
            {
                yield return useEarlyVariant ? 3.25m : 3.75m;
            }
        }
    }
}
