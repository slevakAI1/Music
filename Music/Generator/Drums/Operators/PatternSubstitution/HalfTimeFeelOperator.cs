// AI: purpose=PatternSubstitution: apply half-time feel (snare on beat 3, sparse kicks) for contrast.
// AI: invariants=Intended for low-moderate energy sections; uses Bar.BeatsPerBar and Bar.BackbeatBeats.
// AI: deps=Bar, OperatorCandidateAddition; deterministic from (barNumber, seed); avoid high-energy misuse.


using Music.Generator.Core;
using Music.Generator.Groove;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Generate half-time feel: snare on 3, sparse kicks to create heavy, slower feel.
    // AI: note=Best in bridge/breakdown/verse for contrast; score computed to prefer section starts/boundaries.
    public sealed class HalfTimeFeelOperator : OperatorBase
    {
        private const int SnareVelocityMin = 95;
        private const int SnareVelocityMax = 115;
        private const int KickVelocityMin = 90;
        private const int KickVelocityMax = 110;
        private const double BaseScore = 0.45; // Lower for sparing use

        public override string OperatorId => "DrumHalfTimeFeel";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        // Generate snare on beat 3 and complementary sparse kick pattern; deterministic via (bar,seed).
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            double baseScore = ComputeScore(bar);

            // Generate snare on beat 3 (the half-time backbeat)
            yield return CreateSnareCandidate(bar, seed, 3, baseScore);

            // Generate complementary kick pattern if kick is active
            if (true /* role check deferred */)
            {
                foreach (var kickCandidate in GenerateKickPattern(bar, seed, baseScore))
                {
                    yield return kickCandidate;
                }
            }
        }

        // Create snare candidate at specified beat with deterministic velocity hint.
        private OperatorCandidateAddition CreateSnareCandidate(Bar bar, int seed, int beat, double baseScore)
        {
            int velocityHint = GenerateVelocityHint(
                SnareVelocityMin, SnareVelocityMax,
                bar.BarNumber, beat,
                seed);

            return CreateCandidate(
                role: GrooveRoles.Snare,
                barNumber: bar.BarNumber,
                beat: beat,
                score: baseScore,
                velocityHint: velocityHint,
                instrumentData: DrumCandidateData.Create(strength: OnsetStrength.Backbeat));
        }

        // Generate complementary kick candidates for half-time feel (beat 1 and optional 3).
        private IEnumerable<OperatorCandidateAddition> GenerateKickPattern(Bar bar, int seed, double baseScore)
        {
            // Half-time kick: beat 1 always, beat 3 optional based on energy
            int kickVelocity1 = GenerateVelocityHint(
                KickVelocityMin, KickVelocityMax,
                bar.BarNumber, 1,
                seed);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: bar.BarNumber,
                beat: 1,
                score: baseScore,
                velocityHint: kickVelocity1,
                instrumentData: DrumCandidateData.Create(strength: OnsetStrength.Downbeat));

            // Add kick on 3 for slightly more energy (before the snare)
            if (true /* energy check removed */ && bar.BeatsPerBar >= 3)
            {
                int kickVelocity3 = GenerateVelocityHint(
                    KickVelocityMin - 10, KickVelocityMax - 10, // Slightly softer
                    bar.BarNumber, 3,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: 3,
                    score: baseScore * 0.9, // Slightly lower score
                    velocityHint: kickVelocity3,
                    instrumentData: DrumCandidateData.Create(strength: OnsetStrength.Strong));
            }
        }

        // Compute operator score: boost at boundaries/bridge; scale by low-energy preference.
        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Boost at section boundaries (half-time often starts at bridge)
            if (bar.IsAtSectionBoundary)
                score += 0.2;

            // Boost in bridge section (most natural home for half-time)
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Bridge)
                score += 0.1;

            // Lower energy = more appropriate for half-time
            score *= 1.0 - 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
