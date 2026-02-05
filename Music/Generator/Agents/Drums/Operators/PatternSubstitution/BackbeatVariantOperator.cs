// AI: purpose=PatternSubstitution operator generating backbeat articulation variants (flam, rimshot, sidestick).
// AI: invariants=Only applies when Snare in ActiveRoles; generates backbeat candidates with articulation hints.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate, DrumArticulation; registered in DrumOperatorRegistry.
// AI: change=Story 3.4; adjust articulation selection and energy thresholds based on style and listening tests.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.PatternSubstitution
{
    /// <summary>
    /// Generates backbeat articulation variants (flam, rimshot, sidestick) to provide
    /// section character through snare sound changes rather than pattern changes.
    /// Story 3.4: Pattern Substitution Operators (Groove Swaps).
    /// </summary>
    /// <remarks>
    /// Articulation selection is section-aware:
    /// - Verse: SideStick for lighter feel
    /// - Chorus: Rimshot for power and cut
    /// - Bridge: Normal or Flam for texture variation
    /// Pattern substitution operators have lower base scores to encourage sparing use.
    /// </remarks>
    public sealed class BackbeatVariantOperator : DrumOperatorBase
    {
        private const int SideStickVelocityMin = 60;
        private const int SideStickVelocityMax = 80;
        private const int RimshotVelocityMin = 95;
        private const int RimshotVelocityMax = 115;
        private const int FlamVelocityMin = 85;
        private const int FlamVelocityMax = 105;
        private const double BaseScore = 0.5; // Lower than MicroAddition for sparing use

        /// <inheritdoc/>
        public override string OperatorId => "DrumBackbeatVariant";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.PatternSubstitution;

        /// <summary>
        /// Requires moderate energy for articulation changes to be noticeable.
        /// </summary>

        /// <summary>
        /// Requires snare role for backbeat hits.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need backbeat positions defined
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

            // Select articulation based on section type and energy
            DrumArticulation articulation = SelectArticulation(drummerContext);

            // Skip if no articulation change (use standard backbeat)
            if (articulation == DrumArticulation.None)
                yield break;

            // Generate candidates for each backbeat position
            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                // Skip if backbeat beyond bar
                if (backbeat > drummerContext.Bar.BeatsPerBar)
                    continue;

                decimal beat = backbeat;

                // Get velocity range for articulation
                (int velMin, int velMax) = GetVelocityRange(articulation);

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                // Optional timing offset for flam (grace note effect simulated via timing)
                int? timingHint = articulation == DrumArticulation.Flam ? -10 : null;

                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Backbeat,
                    score: score,
                    velocityHint: velocityHint,
                    timingHint: timingHint,
                    articulationHint: articulation);
            }
        }

        private static DrumArticulation SelectArticulation(DrummerContext context)
        {
            // Deterministic selection based on section type and bar number
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            int hash = HashCode.Combine(context.Bar.BarNumber, sectionType, context.Seed, "BackbeatVariant");

            return sectionType switch
            {
                // Verse: prefer sidestick for lighter feel
                MusicConstants.eSectionType.Verse => true /* energy check removed */
                    ? DrumArticulation.SideStick
                    : DrumArticulation.None,

                // Chorus: prefer rimshot for power
                MusicConstants.eSectionType.Chorus => true /* energy check removed */
                    ? DrumArticulation.Rimshot
                    : DrumArticulation.None,

                // Bridge: prefer flam for texture or sidestick
                MusicConstants.eSectionType.Bridge => true /* energy check removed */
                    ? DrumArticulation.Flam
                    : DrumArticulation.SideStick,

                // Solo: rimshot for cutting through
                MusicConstants.eSectionType.Solo => DrumArticulation.Rimshot,

                // Intro/Outro: sidestick for subtlety
                MusicConstants.eSectionType.Intro or MusicConstants.eSectionType.Outro =>
                    DrumArticulation.SideStick,

                // Default: use deterministic hash to vary
                _ => (Math.Abs(hash) % 3) switch
                {
                    0 => DrumArticulation.Rimshot,
                    1 => DrumArticulation.SideStick,
                    _ => DrumArticulation.None
                }
            };
        }

        private static (int min, int max) GetVelocityRange(DrumArticulation articulation)
        {
            return articulation switch
            {
                DrumArticulation.SideStick => (SideStickVelocityMin, SideStickVelocityMax),
                DrumArticulation.Rimshot => (RimshotVelocityMin, RimshotVelocityMax),
                DrumArticulation.Flam => (FlamVelocityMin, FlamVelocityMax),
                _ => (80, 100)
            };
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundaries (articulation change marks new section)
            if (context.Bar.IsAtSectionBoundary)
                score += 0.15;

            // Slight boost at section start (first few bars)
            if (context.Bar.BarsUntilSectionEnd >= 6)
                score += 0.05;

            // Energy scaling
            score *= 0.7 + 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
