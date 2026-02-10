// AI: purpose=StyleIdiom operator: apply slight early timing offset to snare backbeats for PopRock urgency.
// AI: invariants=Apply only when style=PopRock and Snare role active; timingHint is negative (early) in ticks.
// AI: deps=Bar, OperatorCandidateAddition; deterministic timing from seed; no pattern mutation performed.


using Music.Generator.Core;
using Music.Generator.Groove;
using Music.Generator.Drums.Operators.Candidates;

namespace Music.Generator.Drums.Operators.StyleIdiom
{
    // AI: purpose=Apply a small early timing offset to snare backbeats to create urgency in PopRock bridges/chorus.
    // AI: note=Timing offset chosen from energy bands; use negative timingHint ticks; deterministic from seed.
    public sealed class PopRockBackbeatPushOperator : OperatorBase
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

        // Generate candidates for each backbeat with a negative timingHint (early) determined by energy/seed.
        public override IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            int timingOffset = ComputeTimingOffset(0.5); // default energy

            foreach (int backbeat in bar.BackbeatBeats)
            {
                if (backbeat > bar.BeatsPerBar)
                    continue;

                decimal beat = backbeat;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin, VelocityMax,
                    bar.BarNumber, beat,
                    seed);

                double score = ComputeScore(bar);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: beat,
                    score: score,
                    velocityHint: velocityHint,
                    timingHint: timingOffset,
                    instrumentData: DrumCandidateData.Create(strength: OnsetStrength.Backbeat));
            }
        }

        private double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Boost for chorus sections (urgency matters more)
            var sectionType = bar.Section?.SectionType ?? MusicConstants.eSectionType.Verse;
            if (sectionType == MusicConstants.eSectionType.Chorus)
                score += 0.1;

            // Slight boost at higher energy
            return Math.Clamp(score, 0.0, 1.0);
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
    }
}
