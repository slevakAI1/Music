// AI: purpose=PhrasePunctuation operator generating setup hit on 4& leading into next section.
// AI: invariants=Only applies when IsAtSectionBoundary=true; generates kick/snare on last 16th of bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.3; adjust velocity and beat position based on listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PhrasePunctuation
{
    /// <summary>
    /// Generates a setup hit (kick and/or snare) on beat 4 "and" leading into the next section.
    /// Provides a subtle anticipation before section transitions without a full fill.
    /// Story 3.3: Phrase Punctuation Operators (Boundaries &amp; Fills).
    /// </summary>
    public sealed class SetupHitOperator : DrumOperatorBase
    {
        private const int VelocityMin = 70;
        private const int VelocityMax = 100;
        private const double BaseScore = 0.65;

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.SetupHit;

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PhrasePunctuation;

        /// <summary>
        /// Requires moderate energy for setup hits.
        /// </summary>
        protected override double MinEnergyThreshold => 0.3;

        /// <summary>
        /// Requires kick for setup hit (primary).
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Setup hits are at section boundaries (end of section leading to next)
            if (!context.IsFillWindow && !context.IsAtSectionBoundary)
                return false;

            // Only in last bar of section or fill window
            if (context.BarsUntilSectionEnd > 1 && !context.IsFillWindow)
                return false;

            // Need at least 4 beats for "4&" position
            if (context.BeatsPerBar < 4)
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

            // Setup hit on the "and" of the last beat (e.g., 4.5 in 4/4)
            decimal setupBeat = drummerContext.BeatsPerBar + 0.5m;

            // Generate kick on setup beat
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                drummerContext.BarNumber, setupBeat,
                drummerContext.Seed);

            double score = ComputeScore(drummerContext);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: drummerContext.BarNumber,
                beat: setupBeat,
                strength: OnsetStrength.Pickup,
                score: score,
                velocityHint: kickVelocity,
                fillRole: FillRole.Setup);

            // Optionally add snare if energy is high enough and snare is active
            if (drummerContext.EnergyLevel >= 0.6 && drummerContext.ActiveRoles.Contains(GrooveRoles.Snare))
            {
                int snareVelocity = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    drummerContext.BarNumber, setupBeat + 0.01m, // Slightly different seed
                    drummerContext.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.BarNumber,
                    beat: setupBeat,
                    strength: OnsetStrength.Pickup,
                    score: score * 0.9, // Slightly lower score than kick
                    velocityHint: snareVelocity,
                    fillRole: FillRole.Setup);
            }
        }

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Higher at actual section end
            if (context.BarsUntilSectionEnd <= 1)
                score *= 1.2;

            // Energy scaling
            score *= (0.6 + 0.4 * context.EnergyLevel);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
