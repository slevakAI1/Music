// AI: purpose=MicroAddition operator generating ghost snare notes just before backbeats (e.g., 1.75→2).
// AI: invariants=VelocityHint in [30,50]; uses Bar.BackbeatBeats; skips ghosts outside valid 16th grid.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // Generate ghost snare notes immediately before backbeats (quarter before backbeat).
    // Score reduced by motif multiplier when motif active; motif map not present in context here.
    public sealed class GhostBeforeBackbeatOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.7;

        // Fractional score reduction to apply when motif is active (e.g., 0.2 = -20%).
        private const double MotifScoreReduction = 0.2;

        public override string OperatorId => "DrumGhostBeforeBackbeat";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Generate ghost snare candidates immediately before each backbeat (0.25 beats before).
        // Skip ghosts that would fall before beat 1 or outside valid 16th grid positions.
        public override IEnumerable<DrumCandidate> GenerateCandidates(Bar bar, int seed)
        {
            ArgumentNullException.ThrowIfNull(bar);

            // Compute motif multiplier; motif map not available in context so pass null.
            double motifMultiplier = GetMotifScoreMultiplier(
                null,
                bar,
                MotifScoreReduction);

            // Generate ghost note before each backbeat
            foreach (int backbeat in bar.BackbeatBeats)
            {
                // Ghost at 0.25 beats before backbeat (e.g., 1.75 before beat 2)
                decimal ghostBeat = backbeat - 0.25m;

                // Skip if ghost would land before beat 1
                if (ghostBeat < 1.0m)
                    continue;

                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    VelocityMax,
                    bar.BarNumber,
                    ghostBeat,
                    seed);

                // Score increases with energy (more appropriate at higher energy)
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (0.5 + 0.5 /* default energy factor */) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: bar.BarNumber,
                    beat: ghostBeat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
