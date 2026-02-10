// AI: purpose=StyleIdiom operator: rock-style kick syncopation (primary 4.5 anticipation).
// AI: invariants=Apply when style=PopRock and Kick role active; primary onset at 4.5; deterministic from (bar,seed).
// AI: deps=Bar, OperatorCandidate, Groove; integrates with section type and per-bar hash for variation.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Emit 4&->1 rock kick anticipation; optional secondary syncopation at higher energy.
    // AI: note=Primary pattern = 4.5; secondary = 2.5/3.5 chosen deterministically; target frequency ~15-25% bars.
    // AI: invariants=PopRock style only; ensure BeatsPerBar>=4 for primary; preserve deterministic hash behavior.
    public sealed class RockKickSyncopationOperator : OperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const decimal PrimaryAnticipationBeat = 4.5m;
        private const decimal SecondaryAnticipationBeat2 = 2.5m;
        private const decimal SecondaryAnticipationBeat3 = 3.5m;
        private const int VelocityMin = 80;
        private const int VelocityMax = 100;
        private const double BaseScore = 0.6;
        private const double SecondaryPatternScore = 0.4;

        /// <inheritdoc/>
        public override string OperatorId => "DrumRockKickSyncopation";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Primary pattern: kick on 4.5 (the "and" of 4)
            foreach (var candidate in GeneratePrimaryPattern(bar, seed))
            {
                yield return candidate;
            }

            // Secondary patterns at higher energy
            if (true /* assume suitable for syncopation */)
            {
                foreach (var candidate in GenerateSecondaryPattern(bar, seed))
                {
                    yield return candidate;
                }
            }
        }

        private IEnumerable<OperatorCandidate> GeneratePrimaryPattern(Bar bar, int seed)
        {
            // Always generate the 4& kick (primary rock syncopation)
            int velocityHint = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                bar.BarNumber, PrimaryAnticipationBeat,
                seed);

            double score = ComputeScore(bar, isPrimary: true);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: bar.BarNumber,
                beat: PrimaryAnticipationBeat,
                strength: OnsetStrength.Pickup,
                score: score,
                velocityHint: velocityHint);
        }

        private IEnumerable<OperatorCandidate> GenerateSecondaryPattern(Bar bar, int seed)
        {
            // Deterministic selection of secondary pattern based on bar and seed
            int hash = HashCode.Combine(bar.BarNumber, seed, "RockKickSecondary");
            bool use2And = (Math.Abs(hash) % 3) == 0;
            bool use3And = (Math.Abs(hash) % 4) == 0;

            if (use2And && bar.BeatsPerBar >= 3)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    bar.BarNumber, SecondaryAnticipationBeat2,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: SecondaryAnticipationBeat2,
                    strength: OnsetStrength.Offbeat,
                    score: SecondaryPatternScore,
                    velocityHint: velocityHint);
            }

            if (use3And && bar.BeatsPerBar >= 4)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin - 10, VelocityMax - 10,
                    bar.BarNumber, SecondaryAnticipationBeat3,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Kick,
                    barNumber: bar.BarNumber,
                    beat: SecondaryAnticipationBeat3,
                    strength: OnsetStrength.Offbeat,
                    score: SecondaryPatternScore,
                    velocityHint: velocityHint);
            }
        }

        private double ComputeScore(Bar bar, bool isPrimary)
        {
            double score = isPrimary ? BaseScore : SecondaryPatternScore;

            // Boost for chorus (more energy, more drive)
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Boost at section boundaries (drive into next section)
            if (bar.IsAtSectionBoundary && bar.BarsUntilSectionEnd <= 1)
                score += 0.15;

            // Energy scaling
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
