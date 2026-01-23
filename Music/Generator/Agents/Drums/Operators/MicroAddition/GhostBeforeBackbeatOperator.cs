// AI: purpose=MicroAddition operator generating ghost snare notes just before backbeats (1.75→2, 3.75→4).
// AI: invariants=VelocityHint in [30,50]; only applies when Snare in ActiveRoles and energy >= 0.3.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1, 9.3; adjust energy threshold or beat positions based on listening tests; reduces score when motif active.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates ghost snare notes just before backbeats (beats 2 and 4 in 4/4).
    /// Places ghost notes at 1.75 and 3.75 to lead into the backbeat.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// Story 9.3: Reduces score by 20% when motif is active.
    /// </summary>
    public sealed class GhostBeforeBackbeatOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.7;

        /// <summary>
        /// Story 9.3: Score reduction when motif is active (20% = 0.2).
        /// </summary>
        private const double MotifScoreReduction = 0.2;

        /// <inheritdoc/>
        public override string OperatorId => "DrumGhostBeforeBackbeat";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires minimum energy of 0.3 for ghost notes to be musical.
        /// </summary>
        protected override double MinEnergyThreshold => 0.3;

        /// <summary>
        /// Requires snare to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suppress during fill windows - fills handle their own embellishments
            if (context.IsFillWindow)
                return false;

            // Need backbeat beats defined
            if (context.BackbeatBeats.Count == 0)
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

            // Story 9.3: Get motif score multiplier (20% reduction when motif active)
            double motifMultiplier = GetMotifScoreMultiplier(drummerContext, MotifScoreReduction);

            // Generate ghost note before each backbeat
            foreach (int backbeat in drummerContext.BackbeatBeats)
            {
                // Ghost at 0.25 beats before backbeat (e.g., 1.75 before beat 2)
                decimal ghostBeat = backbeat - 0.25m;

                // Skip if ghost would land before beat 1
                if (ghostBeat < 1.0m)
                    continue;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.BarNumber,
                    ghostBeat,
                    drummerContext.Seed);

                // Score increases with energy (more appropriate at higher energy)
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (0.5 + 0.5 * drummerContext.EnergyLevel) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.BarNumber,
                    beat: ghostBeat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
