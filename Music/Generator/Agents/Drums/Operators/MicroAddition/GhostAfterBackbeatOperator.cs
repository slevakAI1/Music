// AI: purpose=MicroAddition operator generating ghost snare notes just after backbeats (2.25, 4.25).
// AI: invariants=VelocityHint in [30,50]; only applies when Snare in ActiveRoles and energy >= 0.4.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1; adjust energy threshold or beat positions based on listening tests.

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates ghost snare notes just after backbeats (beats 2 and 4 in 4/4).
    /// Places ghost notes at 2.25 and 4.25 as trailing embellishments.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// </summary>
    public sealed class GhostAfterBackbeatOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => "DrumGhostAfterBackbeat";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires slightly higher energy than before-backbeat ghosts.
        /// </summary>
        protected override double MinEnergyThreshold => 0.4;

        /// <summary>
        /// Requires snare to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suppress during fill windows
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

            // Generate ghost note after each backbeat
            foreach (int backbeat in drummerContext.BackbeatBeats)
            {
                // Ghost at 0.25 beats after backbeat (e.g., 2.25 after beat 2)
                decimal ghostBeat = backbeat + 0.25m;

                // Skip if ghost would land beyond valid 16th grid positions
                // (BeatsPerBar + 0.75 is the last valid 16th position)
                if (ghostBeat > drummerContext.BeatsPerBar + 0.75m)
                    continue;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.BarNumber,
                    ghostBeat,
                    drummerContext.Seed);

                // Score increases with energy
                double score = BaseScore * (0.5 + 0.5 * drummerContext.EnergyLevel);

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
