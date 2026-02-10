// AI: purpose=SubdivisionTransform: add open hi-hat accents on selected offbeats for emphasis.
// AI: invariants=Apply when OpenHat role active; positions limited to Bar.BeatsPerBar; deterministic selection.
// AI: deps=Bar, OperatorCandidateAddition, DrumArticulation; integrates with groove section type and energy.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    // AI: purpose=Place open hi-hat "splash" accents on offbeats (e.g., 1.5, 3.5) to add forward momentum.
    // AI: note=Select up to N accents deterministically from AccentPositions; use DrumArticulation.OpenHat.
    public sealed class OpenHatAccentOperator : OperatorBase
    {
        private const int VelocityMin = 85;
        private const int VelocityMax = 105;
        private const double BaseScore = 0.6;

        // Accent positions: offbeats of beats 1 and 3 (1.5 and 3.5)
        private static readonly decimal[] AccentPositions = [1.5m, 3.5m];

        /// <inheritdoc/>
        public override string OperatorId => "DrumOpenHatAccent";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.SubdivisionTransform;

        /// <inheritdoc/>
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Filter positions to those within the bar
            var validPositions = AccentPositions
                .Where(p => p <= bar.BeatsPerBar)
                .ToList();

            // Determine how many accents based on energy
            int maxAccents = 1; // default
            int count = Math.Min(maxAccents, validPositions.Count);

            // Select positions deterministically
            var selectedPositions = SelectPositions(validPositions, count, bar, seed);

            foreach (decimal beat in selectedPositions)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin, VelocityMax,
                    bar.BarNumber, beat,
                    seed);

                double score = ComputeScore(bar, beat);

                yield return CreateCandidate(
                    role: GrooveRoles.OpenHat,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Offbeat,
                    score: score,
                    velocityHint: velocityHint,
                    articulationHint: DrumArticulation.OpenHat);
            }
        }

        private static IEnumerable<decimal> SelectPositions(
            List<decimal> validPositions,
            int count,
            Bar bar,
            int seed)
        {
            if (count >= validPositions.Count)
            {
                return validPositions;
            }

            // Deterministic selection based on bar/seed
            int hash = HashCode.Combine(bar.BarNumber, seed, "OpenHatAccent");
            int startIndex = Math.Abs(hash) % validPositions.Count;

            var result = new List<decimal>(count);
            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % validPositions.Count;
                result.Add(validPositions[index]);
            }
            return result;
        }

        private double ComputeScore(Bar bar, decimal beat)
        {
            double score = BaseScore;

            // Open hats work well in choruses and high-energy sections
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score *= 1.15;

            // Section transitions can benefit from open hat emphasis
            if (bar.BarsUntilSectionEnd <= 2)
                score *= 1.1;
            
            // Energy scaling            
            // Beat 1.5 (first accent) often more important than 3.5
            if (beat == 1.5m)
                score *= 1.05;
            
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
