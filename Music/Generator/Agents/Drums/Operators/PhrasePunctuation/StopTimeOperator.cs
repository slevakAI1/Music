// AI: purpose=PhrasePunctuation operator creating brief dropout (hats off for 2 beats) for emphasis.
// AI: invariants=Generates NO candidates during dropout beats; signals thinning through absence.
// AI: deps=DrumOperatorBase, DrummerContext; downstream interprets fewer candidates as natural thinning.
// AI: change=Story 3.3; this is a "negative space" operator that suppresses rather than generates.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PhrasePunctuation
{
    /// <summary>
    /// Creates a brief stop-time effect by NOT generating candidates during fill window.
    /// This operator generates a minimal accent pattern, leaving space for musical breath.
    /// Story 3.3: Phrase Punctuation Operators (Boundaries &amp; Fills).
    /// </summary>
    /// <remarks>
    /// Stop-time works by providing sparse accents only (kick on 1, snare on 3 if 4/4),
    /// allowing the natural thinning of the groove during fill windows.
    /// Downstream systems interpret fewer total candidates as the "dropout" effect.
    /// </remarks>
    public sealed class StopTimeOperator : DrumOperatorBase
    {
        private const int VelocityMin = 90;
        private const int VelocityMax = 115;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.StopTime;

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PhrasePunctuation;

        /// <summary>
        /// Stop-time works best at moderate energy (too sparse at low, overwhelmed at high).
        /// </summary>
        protected override double MinEnergyThreshold => 0.4;

        /// <summary>
        /// Stop-time works best at moderate energy (too sparse at low, overwhelmed at high).
        /// </summary>
        protected override double MaxEnergyThreshold => 0.7;

        /// <summary>
        /// Requires kick for accent hits.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Kick;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Stop-time is a fill window technique
            if (!context.IsFillWindow)
                return false;

            // Need at least 3 beats for stop-time to make sense
            if (context.BeatsPerBar < 3)
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

            // Stop-time: generate only sparse accents (kick on 1, snare on 3)
            // The absence of dense hat/fill candidates creates the "dropout" effect

            // Kick accent on beat 1
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                drummerContext.Bar.BarNumber, 1.0m,
                drummerContext.Seed);

            double score = ComputeScore(drummerContext);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: drummerContext.Bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: score,
                velocityHint: kickVelocity,
                fillRole: FillRole.None); // Stop-time isn't really a "fill"

            // Snare accent on beat 3 (if 4/4 or longer)
            if (drummerContext.BeatsPerBar >= 4 && drummerContext.ActiveRoles.Contains(GrooveRoles.Snare))
            {
                int snareVelocity = GenerateVelocityHint(
                    VelocityMin - 5, VelocityMax - 5,
                    drummerContext.Bar.BarNumber, 3.0m,
                    drummerContext.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: 3.0m,
                    strength: OnsetStrength.Backbeat,
                    score: score * 0.95,
                    velocityHint: snareVelocity,
                    fillRole: FillRole.None);
            }

            // NO hat candidates generated = the "dropout" effect
            // Other operators generating dense hats will NOT fire during this bar
            // because IsFillWindow is true and they suppress during fills
        }

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Stop-time is more effective near section boundaries
            if (context.IsAtSectionBoundary)
                score *= 1.1;

            // Moderate energy sweet spot
            double energyFromCenter = Math.Abs(context.EnergyLevel - 0.55);
            score *= (1.0 - energyFromCenter * 0.3);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
