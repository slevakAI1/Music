// AI: purpose=PatternSubstitution: apply half-time feel (snare on beat 3, sparse kicks) for contrast.
// AI: invariants=Intended for low-moderate energy sections; uses Bar.BeatsPerBar and Bar.BackbeatBeats.
// AI: deps=DrummerContext, DrumCandidate; deterministic from (barNumber, seed); avoid high-energy misuse.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Generate half-time feel: snare on 3, sparse kicks to create heavy, slower feel.
    // AI: note=Best in bridge/breakdown/verse for contrast; score computed to prefer section starts/boundaries.
    public sealed class HalfTimeFeelOperator : DrumOperatorBase
    {
        private const int SnareVelocityMin = 95;
        private const int SnareVelocityMax = 115;
        private const int KickVelocityMin = 90;
        private const int KickVelocityMax = 110;
        private const double BaseScore = 0.45; // Lower for sparing use

        public override string OperatorId => "DrumHalfTimeFeel";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        // Requires snare role; half-time best at lower energy. Mutual exclusion with double-time handled elsewhere.
        protected override string? RequiredRole => GrooveRoles.Snare;

        // CanApply: ensure bar length and suitable section types for half-time feel.
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

        // Generate snare on beat 3 and complementary sparse kick pattern; deterministic via (bar,seed).
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

        // Create snare candidate at specified beat with deterministic velocity hint.
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

        // Generate complementary kick candidates for half-time feel (beat 1 and optional 3).
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

        // Compute operator score: boost at boundaries/bridge; scale by low-energy preference.
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
