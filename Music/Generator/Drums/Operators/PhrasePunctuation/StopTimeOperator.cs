// AI: purpose=PhrasePunctuation operator creating brief dropout (sparse accents) to emphasize transitions.
// AI: invariants=During dropout generate only sparse accents (kick/snare); absence of hats produces "dropout".
// AI: deps=DrummerContext; downstream systems interpret fewer candidates as thinning; deterministic from seed.
using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.PhrasePunctuation
{
    // AI: purpose=Emit sparse accents (kick on 1, optional snare on 3) during fill windows to create stop-time.
    // AI: note=Produces no hat candidates; keep behavior deterministic and minimal; use FillRole.None for accents.
    public sealed class StopTimeOperator : DrumOperatorBase
    {
        private const int VelocityMin = 90;
        private const int VelocityMax = 115;
        private const double BaseScore = 0.6;

        /// <inheritdoc/>
        public override string OperatorId => FillOperatorIds.StopTime;

        /// <inheritdoc/>
        public override OperatorFamily OperatorFamily => OperatorFamily.PhrasePunctuation;

        // Requires kick role for accents; snare optional. Energy gating is handled by selector/policy.
        protected override string? RequiredRole => GrooveRoles.Kick;

        // CanApply: apply only in fill windows with at least 3 beats to make stop-time musical.
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Stop-time is a fill window technique
            if (!context.Bar.IsFillWindow)
                return false;

            // Need at least 3 beats for stop-time to make sense
            if (context.Bar.BeatsPerBar < 3)
                return false;

            return true;
        }

        // Generate sparse accents (kick on 1, optional snare on 3) to create a dropout; no hat candidates emitted.
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Stop-time: generate only sparse accents (kick on 1, snare on 3)
            // The absence of dense hat/fill candidates creates the "dropout" effect

            // Kick accent on beat 1
            int kickVelocity = GenerateVelocityHint(
                VelocityMin, VelocityMax,
                drummerContext.Bar.BarNumber, 1.0m,
                drummerContext.Seed);

            double score = ComputeScore(drummerContext);

            yield return CreateCandidate(
                role: GrooveRoles.Kick,
                barNumber: drummerContext.Bar.BarNumber,
                beat: 1.0m,
                strength: OnsetStrength.Downbeat,
                score: score,
                velocityHint: kickVelocity,
                fillRole: FillRole.None); // Stop-time isn't really a "fill"

            // Snare accent on beat 3 (if 4/4 or longer)
            if (drummerContext.Bar.BeatsPerBar >= 4 && true /* role check deferred */)
            {
                int snareVelocity = GenerateVelocityHint(
                    VelocityMin - 5, VelocityMax - 5,
                    drummerContext.Bar.BarNumber, 3.0m,
                    drummerContext.Seed);

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
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

        private static double ComputeScore(DrummerContext context)
        {
            double score = BaseScore;

            // Stop-time is more effective near section boundaries
            if (context.Bar.IsAtSectionBoundary)
                score *= 1.1;

            // Moderate energy sweet spot
            double energyFromCenter = 0.05; // default variance
            score *= (1.0 - energyFromCenter * 0.3);

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
