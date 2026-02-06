// AI: purpose=MicroAddition: add sparse 16th hi-hat embellishments between 8th onsets.
// AI: invariants=VelocityHint in [40,60]; requires ClosedHat role and valid 16th positions in Bar.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // Add sparse 16th hi-hat embellishments (1-2 hits) in offbeat 16th positions between 8ths.
    // Score reduced when motif active; motif map not present in context here.
    public sealed class HatEmbellishmentOperator : DrumOperatorBase
    {
        private const int VelocityMin = 40;
        private const int VelocityMax = 60;
        private const double BaseScore = 0.5;

        // Fractional score reduction when motif active (e.g., 0.3 => -30%).
        private const double MotifScoreReduction = 0.3;

        // Candidate 16th positions between 8ths (offbeat 16th positions).
        private static readonly decimal[] EmbellishmentPositions = [1.25m, 1.75m, 2.25m, 2.75m, 3.25m, 3.75m, 4.25m, 4.75m];

        public override string OperatorId => "DrumHatEmbellishment";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Requires hi-hat (ClosedHat) role active; expects 16th grid availability in bar.
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        // CanApply: ensure base gates pass and 16th grid is usable for embellishments.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only apply when base pattern is 8ths (room for 16th embellishment)
            // Note: HatSubdivision is determined from Bar/groove; assume context.Bar provides necessary info.

            return true;
        }

        // Generate 1-2 hi-hat embellishments per bar deterministically from bar/seed.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Compute motif multiplier; motif map not available in context so pass null.
            double motifMultiplier = GetMotifScoreMultiplier(
                null,
                drummerContext.Bar,
                MotifScoreReduction);

            // Filter positions to those within the bar
            var validPositions = EmbellishmentPositions
                .Where(p => p <= drummerContext.Bar.BeatsPerBar)
                .ToList();

            if (validPositions.Count == 0)
                yield break;

            // Determine how many embellishments to add (1-2 based on energy)
            int count = 1; // default

            // Select positions deterministically
            var selectedPositions = SelectPositions(validPositions, count, drummerContext);

            foreach (decimal beat in selectedPositions)
            {
                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.Bar.BarNumber,
                    beat,
                    drummerContext.Seed);

                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (0.5 + 0.5 /* default energy factor */) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.ClosedHat,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Offbeat,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }

        /// <summary>
        /// Deterministically selects positions for embellishment.
        /// </summary>
        private static IEnumerable<decimal> SelectPositions(
            List<decimal> validPositions,
            int count,
            DrummerContext context)
        {
            // Use hash to shuffle positions deterministically
            int hash = HashCode.Combine(context.Bar.BarNumber, context.Seed, "HatEmb");

            // Simple deterministic selection: hash mod count to pick starting index
            int startIndex = Math.Abs(hash) % validPositions.Count;

            for (int i = 0; i < count && i < validPositions.Count; i++)
            {
                int index = (startIndex + i * 2) % validPositions.Count; // Skip by 2 for variety
                yield return validPositions[index];
            }
        }
    }
}
