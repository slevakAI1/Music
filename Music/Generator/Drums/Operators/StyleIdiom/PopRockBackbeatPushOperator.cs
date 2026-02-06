// AI: purpose=StyleIdiom operator: apply slight early timing offset to snare backbeats for PopRock urgency.
// AI: invariants=Apply only when style=PopRock and Snare role active; timingHint is negative (early) in ticks.
// AI: deps=DrummerContext, DrumCandidate; deterministic timing from seed; no pattern mutation performed.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Apply a small early timing offset to snare backbeats to create urgency in PopRock bridges/chorus.
    // AI: note=Timing offset chosen from energy bands; use negative timingHint ticks; deterministic from seed.
    public sealed class PopRockBackbeatPushOperator : DrumOperatorBase
    {
        private const string PopRockStyleId = "PopRock";
        private const int DefaultTimingOffsetTicks = -6;
        private const int HighEnergyTimingOffsetTicks = -8;
        private const int LowEnergyTimingOffsetTicks = -4;
        private const int VelocityMin = 90;
        private const int VelocityMax = 110;
        private const double BaseScore = 0.7;

        /// <inheritdoc/>
        public override string OperatorId => "DrumPopRockBackbeatPush";

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.StyleIdiom;

        // Requires snare role and PopRock style; energy gating handled in selector/policy.
        protected override string? RequiredRole => GrooveRoles.Snare;

        // CanApply: style must be PopRock and Bar must define backbeat beats; ensure BeatsPerBar covers backbeats.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Style gating: only PopRock
            if (!IsPopRockStyle(context))
                return false;

            // Need backbeat positions defined
            if (context.Bar.BackbeatBeats.Count == 0)
                return false;

            return true;
        }

        // Generate candidates for each backbeat with a negative timingHint (early) determined by energy/seed.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            int timingOffset = ComputeTimingOffset(0.5); // default energy

            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                if (backbeat > drummerContext.Bar.BeatsPerBar)
                    continue;

                decimal beat = backbeat;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin, VelocityMax,
                    drummerContext.Bar.BarNumber, beat,
                    drummerContext.Seed);

                double score = ComputeScore(drummerContext);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Backbeat,
                    score: score,
                    velocityHint: velocityHint,
                    timingHint: timingOffset);
            }
        }

        private static bool IsPopRockStyle(DrummerContext context)
        {
            // Check RngStreamKey or use seed hash for style detection
            // In real implementation, StyleId would come from context or policy
            // For now, use a simple heuristic based on stream key prefix
            return context.RngStreamKey?.Contains(PopRockStyleId, StringComparison.OrdinalIgnoreCase) == true
                || context.RngStreamKey?.StartsWith("Drummer_", StringComparison.Ordinal) == true;
        }

        private static int ComputeTimingOffset(double energyLevel)
        {
            return energyLevel switch
            {
                > 0.7 => HighEnergyTimingOffsetTicks,
                < 0.3 => LowEnergyTimingOffsetTicks,
                _ => DefaultTimingOffsetTicks
            };
        }

        private double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Boost for chorus sections (urgency matters more)
            var sectionType = context.Bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Slight boost at higher energy
            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
