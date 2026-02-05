// AI: purpose=SubdivisionTransform operator adding open hi-hat accents on specific offbeats for emphasis.
// AI: invariants=Only applies when ClosedHat/OpenHat in ActiveRoles ; places open hats on &s.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.2; adjust beat positions (1&, 3&) or velocity based on listening tests.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Performance;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.SubdivisionTransform
{
    /// <summary>
    /// Adds open hi-hat accents on specific offbeats (typically 1&amp; and 3&amp;) for rhythmic emphasis.
    /// Creates open hi-hat "splashes" that add forward momentum to the groove.
    /// Story 3.2: Subdivision Transform Operators (Timekeeping Changes).
    /// </summary>
    public sealed class OpenHatAccentOperator : DrumOperatorBase
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

        /// <summary>
        /// Requires moderate-high energy (>= 0.5) for open hat accents.
        /// </summary>

        /// <summary>
        /// Requires open hi-hat to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.OpenHat;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Requires hat mode (not ride)
            if (HatMode.Closed /* default assumption */ == HatMode.Ride)
                return false;

            // Need at least 4 beats for standard accent positions
            if (context.Bar.BeatsPerBar < 4)
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

            // Filter positions to those within the bar
            var validPositions = AccentPositions
                .Where(p => p <= drummerContext.Bar.BeatsPerBar)
                .ToList();

            // Determine how many accents based on energy
            int maxAccents = 1; // default
            int count = Math.Min(maxAccents, validPositions.Count);

            // Select positions deterministically
            var selectedPositions = SelectPositions(validPositions, count, drummerContext);

            foreach (decimal beat in selectedPositions)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin, VelocityMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                double score = ComputeScore(drummerContext, beat);

                yield return CreateCandidate(
                    role: GrooveRoles.OpenHat,
                    barNumber: drummerContext.Bar.BarNumber,
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
            DrummerContext context)
        {
            if (count >= validPositions.Count)
            {
                return validPositions;
            }

            // Deterministic selection based on bar/seed
            int hash = HashCode.Combine(context.Bar.BarNumber, context.Seed, "OpenHatAccent");
            int startIndex = Math.Abs(hash) % validPositions.Count;

            var result = new List<decimal>(count);
            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % validPositions.Count;
                result.Add(validPositions[index]);
            }
            return result;
        }

        private double ComputeScore(DrummerContext context, decimal beat)
        {
            double score = BaseScore;
            
            // Open hats work well in choruses and high-energy sections
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score *= 1.15;
            
            // Section transitions can benefit from open hat emphasis
            if (context.Bar.BarsUntilSectionEnd <= 2)
                score *= 1.1;
            
            // Energy scaling            
            // Beat 1.5 (first accent) often more important than 3.5
            if (beat == 1.5m)
                score *= 1.05;
            
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
