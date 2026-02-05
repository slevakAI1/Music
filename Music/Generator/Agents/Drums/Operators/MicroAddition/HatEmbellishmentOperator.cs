// AI: purpose=MicroAddition operator adding sparse 16th hi-hat notes for rhythmic interest.
// AI: invariants=VelocityHint in [40,60]; only applies when ClosedHat in ActiveRoles and 16th grid available.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1, 9.3; adjust position selection and count based on listening tests; reduces score when motif active.


using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates sparse 16th note hi-hat embellishments for added rhythmic interest.
    /// Places 1-2 extra 16th hits in positions not covered by the base 8th pattern.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// Story 9.3: Reduces score by 30% when motif is active.
    /// </summary>
    public sealed class HatEmbellishmentOperator : DrumOperatorBase
    {
        private const int VelocityMin = 40;
        private const int VelocityMax = 60;
        private const double BaseScore = 0.5;

        /// <summary>
        /// Story 9.3: Score reduction when motif is active (30% = 0.3).
        /// </summary>
        private const double MotifScoreReduction = 0.3;

        // Candidate 16th positions that fall between 8ths (offbeats of offbeats)
        private static readonly decimal[] EmbellishmentPositions = [1.25m, 1.75m, 2.25m, 2.75m, 3.25m, 3.75m, 4.25m, 4.75m];

        /// <inheritdoc/>
        public override string OperatorId => "DrumHatEmbellishment";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires moderate energy for embellishments.
        /// </summary>
        protected override double MinEnergyThreshold => 0.45;

        /// <summary>
        /// Requires hi-hat to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.ClosedHat;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Only apply when base pattern is 8ths (room for 16th embellishment)
            if (context.HatSubdivision != HatSubdivision.Eighth)
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

            // Story 9.3: Get motif score multiplier (30% reduction when motif active)
            double motifMultiplier = GetMotifScoreMultiplier(drummerContext, MotifScoreReduction);

            // Filter positions to those within the bar
            var validPositions = EmbellishmentPositions
                .Where(p => p <= drummerContext.BeatsPerBar)
                .ToList();

            if (validPositions.Count == 0)
                yield break;

            // Determine how many embellishments to add (1-2 based on energy)
            int count = drummerContext.EnergyLevel >= 0.7 ? 2 : 1;

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
                double score = BaseScore * (0.5 + 0.5 * drummerContext.EnergyLevel) * motifMultiplier;

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
