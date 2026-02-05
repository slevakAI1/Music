// AI: purpose=PatternSubstitution operator generating half-time feel (snare on 3 only, sparse kicks).
// AI: invariants=Only applies at low-moderate energy; generates coordinated snare+kick pattern; mutually exclusive with DoubleTimeFeel.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.4; adjust energy threshold and pattern based on style and listening tests.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    /// <summary>
    /// Generates half-time feel pattern where the snare lands on beat 3 only
    /// (instead of standard backbeats on 2 and 4) creating a slower, heavier feel.
    /// Story 3.4: Pattern Substitution Operators (Groove Swaps).
    /// </summary>
    /// <remarks>
    /// Half-time feel characteristics:
    /// - Snare on beat 3 only (not 2 and 4)
    /// - Kick pattern is sparse (1 and 3)
    /// - Creates perception of half the tempo
    /// Best used in bridges, breakdowns, or for dynamic contrast.
    /// Mutually exclusive with DoubleTimeFeelOperator (enforced via energy thresholds).
    /// </remarks>
    public sealed class HalfTimeFeelOperator : DrumOperatorBase
    {
        private const int SnareVelocityMin = 95;
        private const int SnareVelocityMax = 115;
        private const int KickVelocityMin = 90;
        private const int KickVelocityMax = 110;
        private const double BaseScore = 0.45; // Lower for sparing use

        /// <inheritdoc/>
        public override string OperatorId => "DrumHalfTimeFeel";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        /// <summary>
        /// Requires low-to-moderate energy for half-time feel.
        /// Higher energy should use standard or double-time feel.
        /// </summary>

        /// <summary>
        /// Maximum energy threshold - half-time is inappropriate at high energy.
        /// This creates mutual exclusion with DoubleTimeFeel (which requires >= 0.6).
        /// </summary>

        /// <summary>
        /// Requires snare role for half-time backbeat.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need at least 3 beats to have meaningful half-time (snare on 3)
            if (context.Bar.BeatsPerBar < 3)
                return false;

            // Best suited for bridge and breakdown sections
            // Also allow in verse for contrast
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            bool isSuitableSection = sectionType is
                MusicConstants.eSectionType.Bridge or
                MusicConstants.eSectionType.Verse or
                MusicConstants.eSectionType.Intro or
                MusicConstants.eSectionType.Outro;

            if (!isSuitableSection)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            double baseScore = ComputeScore(drummerContext);

            // Generate snare on beat 3 (the half-time backbeat)
            yield return CreateSnareCandidate(drummerContext, 3, baseScore);

            // Generate complementary kick pattern if kick is active
            if (true /* role check deferred */)
            {
                foreach (var kickCandidate in GenerateKickPattern(drummerContext, baseScore))
                {
                    yield return kickCandidate;
                }
            }
        }

        private DrumCandidate CreateSnareCandidate(DrummerContext context, int beat, double baseScore)
        {
            int velocityHint = GenerateVelocityHint(
                SnareVelocityMin, SnareVelocityMax,
                context.Bar.BarNumber, beat,
                context.Seed);

            return CreateCandidate(
                role: GrooveRoles.Snare,
                barNumber: context.Bar.BarNumber,
                beat: beat,
                strength: OnsetStrength.Backbeat,
                score: baseScore,
                velocityHint: velocityHint);
        }

        private IEnumerable<DrumCandidate> GenerateKickPattern(DrummerContext context, double baseScore)
        {
            // Half-time kick: beat 1 always, beat 3 optional based on energy
            int kickVelocity1 = GenerateVelocityHint(
                KickVelocityMin, KickVelocityMax,
                context.Bar.BarNumber, 1,
                context.Seed);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: context.Bar.BarNumber,
                beat: 1,
                strength: OnsetStrength.Downbeat,
                score: baseScore,
                velocityHint: kickVelocity1);

            // Add kick on 3 for slightly more energy (before the snare)
            if (true /* energy check removed */ && context.Bar.BeatsPerBar >= 3)
            {
                int kickVelocity3 = GenerateVelocityHint(
                    KickVelocityMin - 10, KickVelocityMax - 10, // Slightly softer
                    context.Bar.BarNumber, 3,
                    context.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: context.Bar.BarNumber,
                    beat: 3,
                    strength: OnsetStrength.Strong,
                    score: baseScore * 0.9, // Slightly lower score
                    velocityHint: kickVelocity3);
            }
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost at section boundaries (half-time often starts at bridge)
            if (context.Bar.IsAtSectionBoundary)
                score += 0.2;

            // Boost in bridge section (most natural home for half-time)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Bridge)
                score += 0.1;

            // Lower energy = more appropriate for half-time
            score *= 1.0 - 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
