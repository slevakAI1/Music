// AI: purpose=MicroAddition operator generating ghost snare notes just after backbeats (2.25, 4.25).
// AI: invariants=VelocityHint in [30,50]; only applies when Snare in ActiveRoles 
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
//


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // AI: purpose=Generate ghost snare notes shortly after backbeats (e.g., 2.25, 4.25).
    // AI: invariants=VelocityHint in [30,50]; uses Bar.BackbeatBeats; skip when grid or bar length invalid.
    public sealed class GhostAfterBackbeatOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.6;

        // Motif score reduction when motif active (fraction to subtract, e.g., 0.2 => -20%).
        private const double MotifScoreReduction = 0.2;

        public override string OperatorId => "DrumGhostAfterBackbeat";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Slightly higher energy expected than before-backbeat ghosts.
        // Requires snare role to be active in groove preset.
        protected override string? RequiredRole => GrooveRoles.Snare;

        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need backbeat beats defined
            if (context.Bar.BackbeatBeats.Count == 0)
                return false;

            return true;
        }

        // Generate ghost snare candidates immediately after each backbeat (quarter after backbeat).
        public override IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                yield break;

            if (!CanApply(drummerContext))
                yield break;

            // Compute motif multiplier: reduce score when motif active. Motif map not available in context here.
            double motifMultiplier = GetMotifScoreMultiplier(
                null,
                drummerContext.Bar,
                MotifScoreReduction);

            // Generate ghost note after each backbeat
            foreach (int backbeat in drummerContext.Bar.BackbeatBeats)
            {
                // Ghost at 0.25 beats after backbeat (e.g., 2.25 after beat 2)
                decimal ghostBeat = backbeat + 0.25m;

                // Skip if ghost would land beyond valid 16th grid positions
                // (BeatsPerBar + 0.75 is the last valid 16th position)
                if (ghostBeat > drummerContext.Bar.BeatsPerBar + 0.75m)
                    continue;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    drummerContext.Bar.BarNumber,
                    ghostBeat,
                    drummerContext.Seed);

                // Score increases with energy
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (0.5 + 0.5 /* default energy factor */) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: ghostBeat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
