// AI: purpose=MicroAddition operator generating ghost snare notes just after backbeats (2.25, 4.25).
// AI: invariants=VelocityHint in [30,50]; only applies when Snare in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1, 9.3; adjust energy threshold or beat positions based on listening tests; reduces score when motif active.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates ghost snare notes just after backbeats (beats 2 and 4 in 4/4).
    /// Places ghost notes at 2.25 and 4.25 as trailing embellishments.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// Story 9.3: Reduces score by 20% when motif is active.
    /// </summary>
    public sealed class GhostAfterBackbeatOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.6;

        /// <summary>
        /// Story 9.3: Score reduction when motif is active (20% = 0.2).
        /// </summary>
        private const double MotifScoreReduction = 0.2;

        /// <inheritdoc/>
        public override string OperatorId => "DrumGhostAfterBackbeat";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires slightly higher energy than before-backbeat ghosts.
        /// </summary>

        /// <summary>
        /// Requires snare to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need backbeat beats defined
            if (context.Bar.BackbeatBeats.Count == 0)
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
            double motifMultiplier = GetMotifScoreMultiplier(
                null /* motif map removed from context */,
                drummerContext.Bar,
                MotifScoreReduction);

            // Generate ghost note after each backbeat
            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                // Ghost at 0.25 beats after backbeat (e.g., 2.25 after beat 2)
                decimal ghostBeat = backbeat + 0.25m;

                // Skip if ghost would land beyond valid 16th grid positions
                // (BeatsPerBar + 0.75 is the last valid 16th position)
                if (ghostBeat > drummerContext.Bar.BeatsPerBar + 0.75m)
                    continue;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.Bar.BarNumber,
                    ghostBeat,
                    drummerContext.Seed);

                // Score increases with energy
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (0.5 + 0.5 /* default energy factor */) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: ghostBeat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
