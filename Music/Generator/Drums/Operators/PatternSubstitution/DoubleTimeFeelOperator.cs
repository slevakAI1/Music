// AI: purpose=PatternSubstitution: produce double-time feel (denser kicks, driving energy) without tempo change.
// AI: invariants=Apply in high-energy suitable sections; uses Bar.BackbeatBeats and BeatsPerBar; deterministic from seed.
// AI: deps=OperatorBase, Bar, OperatorCandidateAddition; integrates with section type for suitability decisions.


using Music.Generator.Core;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Apply double-time feel: 8th-note dense kick pattern + strong backbeats.
    // AI: note=Intended for choruses/solos; lower base score to keep sparing; mutually exclusive with half-time.
    public sealed class DoubleTimeFeelOperator : OperatorBase
    {
        private const int KickDownbeatVelocityMin = 95;
        private const int KickDownbeatVelocityMax = 115;
        private const int KickOffbeatVelocityMin = 75;
        private const int KickOffbeatVelocityMax = 95;
        private const int SnareVelocityMin = 100;
        private const int SnareVelocityMax = 120;
        private const double BaseScore = 0.45; // Lower for sparing use

        public override string OperatorId => "DrumDoubleTimeFeel";

        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        // Generate dense kick + snare backbeat candidates to realize double-time feel.
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            double baseScore = ComputeScore(bar);

            // Generate dense kick pattern (8th note density)
            foreach (var kickCandidate in GenerateKickPattern(bar, seed, baseScore))
            {
                yield return kickCandidate;
            }

            // Generate backbeat snare candidates if snare is active
            if (true /* role check deferred */)
            {
                foreach (var snareCandidate in GenerateSnarePattern(bar, seed, baseScore))
                {
                    yield return snareCandidate;
                }
            }
        }

        // Produce kick candidates at downbeats and offbeats (8th-note density) deterministically.
        private IEnumerable<OperatorCandidateAddition> GenerateKickPattern(Bar bar, int seed, double baseScore)
        {
            int beatsPerBar = bar.BeatsPerBar;

            // Generate 8th note kick pattern (every beat + offbeats)
            for (int beatInt = 1; beatInt <= beatsPerBar; beatInt++)
            {
                // Downbeat kick
                int downbeatVelocity = GenerateVelocityHint(
                    KickDownbeatVelocityMin, KickDownbeatVelocityMax,
                    bar.BarNumber, beatInt,
                    seed);

                OnsetStrength downbeatStrength = beatInt == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong;

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: beatInt,
                    strength: downbeatStrength,
                    score: baseScore,
                    velocityHint: downbeatVelocity);

                // Offbeat kick (on the &)
                decimal offbeatPosition = beatInt + 0.5m;
                if (offbeatPosition <= beatsPerBar + 0.5m)
                {
                    int offbeatVelocity = GenerateVelocityHint(
                        KickOffbeatVelocityMin, KickOffbeatVelocityMax,
                        bar.BarNumber, offbeatPosition,
                        seed);

                    yield return CreateCandidate(
                        role: GrooveRoles.Kick,
                        barNumber: bar.BarNumber,
                        beat: offbeatPosition,
                        strength: OnsetStrength.Offbeat,
                        score: baseScore * 0.85, // Lower score for offbeats
                        velocityHint: offbeatVelocity);
                }
            }
        }

        // Produce snare backbeat candidates (from Bar.BackbeatBeats) with strong velocity hints.
        private IEnumerable<OperatorCandidateAddition> GenerateSnarePattern(Bar bar, int seed, double baseScore)
        {
            // Standard backbeats (2 and 4) with high velocity for double-time energy
            foreach (int backbeat in bar.BackbeatBeats)
            {
                if (backbeat > bar.BeatsPerBar)
                    continue;

                int snareVelocity = GenerateVelocityHint(
                    SnareVelocityMin, SnareVelocityMax,
                    bar.BarNumber, backbeat,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: backbeat,
                    strength: OnsetStrength.Backbeat,
                    score: baseScore,
                    velocityHint: snareVelocity);
            }
        }

        // Compute base score for this operator considering section and boundaries.
        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Boost at section boundaries
            if (bar.IsAtSectionBoundary)
                score += 0.15;

            // Boost in chorus (most natural home for double-time)
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Higher energy = more appropriate for double-time
            score *= 0.5 + 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
