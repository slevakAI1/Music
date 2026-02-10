// AI: purpose=PhrasePunctuation operator creating brief dropout (sparse accents) to emphasize transitions.
// AI: invariants=During dropout generate only sparse accents (kick/snare); absence of hats produces "dropout".
// AI: deps=Bar; downstream systems interpret fewer candidates as thinning; deterministic from seed.
using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Emit sparse accents (kick on 1, optional snare on 3) during fill windows to create stop-time.
    // AI: note=Produces no hat candidates; keep behavior deterministic and minimal; use FillRole.None for accents.
    public sealed class StopTimeOperator : OperatorBase
    {
        private const int VelocityMin = 90;
        private const int VelocityMax = 115;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => DrumFillOperatorIds.StopTime;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        // Generate sparse accents (kick on 1, optional snare on 3) to create a dropout; no hat candidates emitted.
        public override IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Stop-time: generate only sparse accents (kick on 1, snare on 3)
            // The absence of dense hat/fill candidates creates the "dropout" effect

            // Kick accent on beat 1
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                bar.BarNumber, 1.0m,
                seed);

            double score = ComputeScore(bar);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: score,
                velocityHint: kickVelocity,
                fillRole: FillRole.None); // Stop-time isn't really a "fill"

            // Snare accent on beat 3 (if 4/4 or longer)
            if (bar.BeatsPerBar >= 4 && true /* role check deferred */)
            {
                int snareVelocity = GenerateVelocityHint(
                    VelocityMin - 5, VelocityMax - 5,
                    bar.BarNumber, 3.0m,
                    seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: 3.0m,
                    strength: OnsetStrength.Backbeat,
                    score: score * 0.95,
                    velocityHint: snareVelocity,
                    fillRole: FillRole.None);
            }

            // NO hat candidates generated = the "dropout" effect
            // Other operators generating dense hats will NOT fire during this bar
            // because IsFillWindow is true and they suppress during fills
        }

        private static double ComputeScore(Bar bar)
        {
            double score = BaseScore;

            // Stop-time is more effective near section boundaries
            if (bar.IsAtSectionBoundary)
                score *= 1.1;

            // Moderate energy sweet spot
            double energyFromCenter = 0.05; // default variance
            score *= (1.0 - energyFromCenter * 0.3);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
