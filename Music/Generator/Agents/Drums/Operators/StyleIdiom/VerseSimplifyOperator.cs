// AI: purpose=StyleIdiom operator that thins out verse grooves for contrast with chorus sections.
// AI: invariants=Only applies when StyleId=="PopRock" and SectionType==Verse; reduces density via lower velocities and score.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.5; simplification intensity configurable in PopRockStyleConfiguration.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.StyleIdiom
{
    /// <summary>
    /// Thins out verse grooves for contrast with chorus sections.
    /// Achieves simplification through lower velocity hints and reduced scoring.
    /// Story 3.5: Style Idiom Operators (Pop Rock Specifics).
    /// </summary>
    /// <remarks>
    /// Simplification strategies:
    /// - Lower velocity hints by 10-20 across all roles
    /// - Generate "thin" candidates that compete with busier patterns
    /// - Prefer minimal kick patterns (1 and 3 only)
    /// - Reduce hat density by generating fewer hat candidates
    /// 
    /// This operator produces candidates that, when selected, create a sparser feel.
    /// The selection engine's density targets and weights determine final outcome.
    /// This operator is PopRock-specific and will not apply for other styles.
    /// </remarks>
    public sealed class VerseSimplifyOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int KickVelocityMin = 70;
        private const int KickVelocityMax = 90;
        private const int HatVelocityMin = 50;
        private const int HatVelocityMax = 70;
        private const double BaseScore = 0.65;

        /// <inheritdoc/>
        public override string OperatorId => "DrumVerseSimplify";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.StyleIdiom;

        /// <summary>
        /// Works at any energy level (verses can be low or moderate energy).
        /// </summary>
        protected override double MinEnergyThreshold => 0.0;

        /// <summary>
        /// Works best at lower energy; high energy verses should be busier.
        /// </summary>
        protected override double MaxEnergyThreshold => 0.7;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Section gating: only verse
            if (context.SectionType != MusicConstants.eSectionType.Verse)
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

            // Generate simplified kick pattern (1 and 3 only)
            if (drummerContext.ActiveRoles.Contains(GrooveRoles.Kick))
            {
                foreach (var candidate in GenerateSimplifiedKickPattern(drummerContext))
                {
                    yield return candidate;
                }
            }

            // Generate sparser hat pattern (eighth notes only, lower velocity)
            if (drummerContext.ActiveRoles.Contains(GrooveRoles.ClosedHat))
            {
                foreach (var candidate in GenerateSimplifiedHatPattern(drummerContext))
                {
                    yield return candidate;
                }
            }
        }

        private IEnumerable<DrumCandidate> GenerateSimplifiedKickPattern(DrummerContext context)
        {
            // Simple "1 and 3" kick pattern for verses
            decimal[] kickBeats = context.BeatsPerBar >= 4
                ? [1.0m, 3.0m]
                : [1.0m];

            foreach (decimal beat in kickBeats)
            {
                int velocityHint = GenerateVelocityHint(
                    KickVelocityMin, KickVelocityMax,
                    context.BarNumber, beat,
                    context.Seed);

                OnsetStrength strength = beat == 1.0m ? OnsetStrength.Downbeat : OnsetStrength.Strong;

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: ComputeScore(context),
                    velocityHint: velocityHint);
            }
        }

        private IEnumerable<DrumCandidate> GenerateSimplifiedHatPattern(DrummerContext context)
        {
            // Eighth note pattern only (simpler than 16ths)
            for (int beatInt = 1; beatInt <= context.BeatsPerBar; beatInt++)
            {
                decimal beat = beatInt;

                int velocityHint = GenerateVelocityHint(
                    HatVelocityMin, HatVelocityMax,
                    context.BarNumber, beat,
                    context.Seed);

                // Lower score for off-beats to prefer sparse selection
                double score = beatInt % 2 == 1
                    ? ComputeScore(context)
                    : ComputeScore(context) * 0.8;

                yield return CreateCandidate(
                    role: GrooveRoles.ClosedHat,
                    barNumber: context.BarNumber,
                    beat: beat,
                    strength: beatInt == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong,
                    score: score,
                    velocityHint: velocityHint);
            }
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Lower score at higher energy (let busier patterns win)
            score -= 0.2 * context.EnergyLevel;

            // Slight boost at section start for establishing the simple pattern
            if (context.IsAtSectionBoundary)
                score += 0.05;

            return Math.Clamp(score, 0.2, 0.8);
        }
    }
}
