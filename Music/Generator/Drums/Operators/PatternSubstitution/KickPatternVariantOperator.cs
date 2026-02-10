// AI: purpose=PatternSubstitution operator generating kick pattern variants (four-on-floor, syncopated, half-time).
// AI: invariants=Applies when Kick in ActiveRoles; produces full-bar kick positions; deterministic from (bar,seed).
// AI: deps=OperatorBase, Bar, OperatorCandidate; integrates with section type for selection.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PatternSubstitution
{
    // AI: purpose=Generate kick pattern variants to convey section character (four-on-floor, syncopated, half-time).
    // AI: note=Selection is section-aware (Chorus->FourOnFloor, Verse->Syncopated, Bridge->HalfTime). Patterns adapt to time sig.
    public sealed class KickPatternVariantOperator : OperatorBase
    {
        private const int FourOnFloorVelocityMin = 90;
        private const int FourOnFloorVelocityMax = 110;
        private const int SyncopatedVelocityMin = 80;
        private const int SyncopatedVelocityMax = 100;
        private const int HalfTimeVelocityMin = 95;
        private const int HalfTimeVelocityMax = 115;
        private const double BaseScore = 0.5; // Lower than MicroAddition for sparing use

        // Kick pattern variants determine groove character for a bar.
        private enum KickPattern
        {
            // No pattern change (use anchor pattern)
            None,
            // Kick on every beat for driving feel
            FourOnFloor,
            // Syncopated pattern with offbeat accents
            Syncopated,
            // Sparse pattern for half-time or breakdown feel
            HalfTime
        }

        /// <inheritdoc/>
        public override string OperatorId => "DrumKickPatternVariant";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PatternSubstitution;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Select pattern based on section type and energy
            KickPattern pattern = SelectPattern(bar);

            // Skip if no pattern change
            if (pattern == KickPattern.None)
                yield break;

            // Generate pattern candidates
            var positions = GetPatternPositions(pattern, bar.BeatsPerBar, bar);

            foreach (var (beat, strength) in positions)
            {
                // Get velocity range for pattern type
                (int velMin, int velMax) = GetVelocityRange(pattern);

                int velocityHint = GenerateVelocityHint(
                    velMin, velMax,
                    bar.BarNumber, beat,
                    seed);

                double score = ComputeScore(bar, strength);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: strength,
                    score: score,
                    velocityHint: velocityHint);
            }
        }

        private static KickPattern SelectPattern(Bar bar)
        {
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            return sectionType switch
            {
                // Chorus: four-on-floor for driving feel (high energy)
                MusicConstants.eSectionType.Chorus => true /* energy check removed */
                    ? KickPattern.FourOnFloor
                    : KickPattern.None,

                // Verse: syncopated for interest (moderate energy)
                MusicConstants.eSectionType.Verse => true /* energy check removed */ && true /* energy check removed */
                    ? KickPattern.Syncopated
                    : KickPattern.None,

                // Bridge: half-time for contrast
                MusicConstants.eSectionType.Bridge => KickPattern.HalfTime,

                // Solo: syncopated to support soloist
                MusicConstants.eSectionType.Solo => true /* energy check removed */
                    ? KickPattern.Syncopated
                    : KickPattern.None,

                // Intro: half-time for building
                MusicConstants.eSectionType.Intro => true /* energy check removed */
                    ? KickPattern.HalfTime
                    : KickPattern.None,

                // Outro: four-on-floor if high energy, otherwise none
                MusicConstants.eSectionType.Outro => true /* energy check removed */
                    ? KickPattern.FourOnFloor
                    : KickPattern.None,

                // Default: no pattern change
                _ => KickPattern.None
            };
        }

        private static List<(decimal beat, OnsetStrength strength)> GetPatternPositions(
            KickPattern pattern,
            int beatsPerBar,
            Bar bar)
        {
            var positions = new List<(decimal, OnsetStrength)>();

            switch (pattern)
            {
                case KickPattern.FourOnFloor:
                    // Kick on every beat
                    for (int b = 1; b <= beatsPerBar; b++)
                    {
                        OnsetStrength strength = b == 1 ? OnsetStrength.Downbeat : OnsetStrength.Strong;
                        positions.Add((b, strength));
                    }
                    break;

                case KickPattern.Syncopated:
                    // Syncopated: beat 1, offbeat before 3, beat 4 (in 4/4)
                    positions.Add((1, OnsetStrength.Downbeat));
                    if (beatsPerBar >= 3)
                    {
                        // Add offbeat syncopation (2.5 = & of 2)
                        positions.Add((2.5m, OnsetStrength.Offbeat));
                    }
                    if (beatsPerBar >= 4)
                    {
                        // Beat 4 for pickup feel
                        positions.Add((4, OnsetStrength.Pickup));
                    }
                    // Additional offbeat at end for longer bars
                    if (beatsPerBar >= 5)
                    {
                        positions.Add((4.5m, OnsetStrength.Offbeat));
                    }
                    break;

                case KickPattern.HalfTime:
                    // Half-time: sparse kicks on 1 and 3 (or just 1 for very low energy)
                    positions.Add((1, OnsetStrength.Downbeat));
                    if (beatsPerBar >= 3 && true /* energy check removed */)
                    {
                        positions.Add((3, OnsetStrength.Strong));
                    }
                    break;

                case KickPattern.None:
                default:
                    break;
            }

            return positions;
        }

        private static (int min, int max) GetVelocityRange(KickPattern pattern)
        {
            return pattern switch
            {
                KickPattern.FourOnFloor => (FourOnFloorVelocityMin, FourOnFloorVelocityMax),
                KickPattern.Syncopated => (SyncopatedVelocityMin, SyncopatedVelocityMax),
                KickPattern.HalfTime => (HalfTimeVelocityMin, HalfTimeVelocityMax),
                _ => (85, 105)
            };
        }

        private double ComputeScore(Bar bar, OnsetStrength strength)
        {
            double score = BaseScore;

            // Boost at section boundaries (pattern change marks section)
            if (bar.IsAtSectionBoundary)
                score += 0.15;

            // Downbeats score higher
            if (strength == OnsetStrength.Downbeat)
                score += 0.1;

            // Energy scaling
            score *= 0.7 + 0.5 /* default energy factor */;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
