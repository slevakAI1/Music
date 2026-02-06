// AI: purpose=MicroAddition operator: produce a short cluster of ghost snare notes (2-3 hits).
// AI: invariants=VelocityHint range [30,50]; placement stays within bar; uses Bar.BeatsPerBar and ClusterStarts.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Operators.Base;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Operators.MicroAddition
{
    // Place a compact cluster (2-3 ghost notes) typically mid-bar for subtle fill.
    // Score reduced when motif active; motif map not present in context here.
    public sealed class GhostClusterOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.5;

        // Fractional score reduction when motif active (e.g., 0.5 => -50%).
        private const double MotifScoreReduction = 0.5;

        // Pre-defined cluster patterns (offsets from starting beat)
        private static readonly decimal[][] ClusterPatterns =
        [
            [0.0m, 0.25m],          // 2-note cluster
            [0.0m, 0.25m, 0.5m],    // 3-note ascending
            [0.0m, 0.5m, 0.75m]     // 3-note with gap
        ];

        // Starting beat positions for clusters (mid-bar placement)
        private static readonly decimal[] ClusterStarts = [2.25m, 2.5m, 3.25m];

        public override string OperatorId => "DrumGhostCluster";

        public override OperatorFamily OperatorFamily => OperatorFamily.MicroAddition;

        // Requires snare role active; expects BeatsPerBar >= 4 for comfortable cluster placement.
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Need enough beats for cluster placement
            if (context.Bar.BeatsPerBar < 4)
                return false;

            return true;
        }

        // Generate 2-3 ghost notes per chosen cluster pattern; deterministic selection from bar/seed.
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

            // Select cluster pattern and start position deterministically
            int hash = HashCode.Combine(drummerContext.Bar.BarNumber, drummerContext.Seed, "GhostCluster");

            int patternIndex = Math.Abs(hash) % ClusterPatterns.Length;
            int startIndex = Math.Abs(hash >> 8) % ClusterStarts.Length;

            var pattern = ClusterPatterns[patternIndex];
            decimal startBeat = ClusterStarts[startIndex];

            // Generate candidates for each note in the cluster
            for (int i = 0; i < pattern.Length; i++)
            {
                decimal beat = startBeat + pattern[i];

                // Skip if beat would exceed bar
                if (beat > drummerContext.Bar.BeatsPerBar)
                    continue;

                // Velocity decreases slightly through cluster for natural feel
                int velocityBase = VelocityMax - (i * 5);
                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    Math.Max(VelocityMin, velocityBase),
                    drummerContext.Bar.BarNumber,
                    beat,
                    drummerContext.Seed);

                // Score decreases for later notes in cluster (first note most important)
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (1.0 - (i * 0.1)) * (0.5 + 0.5 /* default energy factor */) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.Bar.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
