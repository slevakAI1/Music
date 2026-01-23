// AI: purpose=MicroAddition operator generating 2-3 ghost notes as a mini-fill cluster.
// AI: invariants=VelocityHint in [30,50]; only applies when Snare in ActiveRoles, energy >= 0.5, not in fill window.
// AI: deps=DrumOperatorBase, DrummerContext, DrumCandidate; registered in DrumOperatorRegistry.
// AI: change=Story 3.1, 9.3; adjust cluster patterns and placement based on listening tests; reduces score when motif active.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Operators.MicroAddition
{
    /// <summary>
    /// Generates a cluster of 2-3 ghost notes as a mini-fill embellishment.
    /// Typically placed mid-bar (around beat 2.5-3) for subtle rhythmic interest.
    /// Story 3.1: Micro-Addition Operators (Ghost Notes &amp; Embellishments).
    /// Story 9.3: Reduces score by 50% when motif is active (avoid clutter).
    /// </summary>
    public sealed class GhostClusterOperator : DrumOperatorBase
    {
        private const int VelocityMin = 30;
        private const int VelocityMax = 50;
        private const double BaseScore = 0.5;

        /// <summary>
        /// Story 9.3: Score reduction when motif is active (50% = 0.5).
        /// </summary>
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

        /// <inheritdoc/>
        public override string OperatorId => "DrumGhostCluster";

        /// <inheritdoc/>
        public override Common.OperatorFamily OperatorFamily => Common.OperatorFamily.MicroAddition;

        /// <summary>
        /// Requires moderate-high energy for ghost clusters.
        /// </summary>
        protected override double MinEnergyThreshold => 0.5;

        /// <summary>
        /// Requires snare to be in active roles.
        /// </summary>
        protected override string? RequiredRole => GrooveRoles.Snare;

        /// <inheritdoc/>
        public override bool CanApply(DrummerContext context)
        {
            if (!base.CanApply(context))
                return false;

            // Suppress during fill windows - actual fills handle this
            if (context.IsFillWindow)
                return false;

            // Need enough beats for cluster placement
            if (context.BeatsPerBar < 4)
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

            // Story 9.3: Get motif score multiplier (50% reduction when motif active)
            double motifMultiplier = GetMotifScoreMultiplier(drummerContext, MotifScoreReduction);

            // Select cluster pattern and start position deterministically
            int hash = HashCode.Combine(drummerContext.BarNumber, drummerContext.Seed, "GhostCluster");

            int patternIndex = Math.Abs(hash) % ClusterPatterns.Length;
            int startIndex = Math.Abs(hash >> 8) % ClusterStarts.Length;

            var pattern = ClusterPatterns[patternIndex];
            decimal startBeat = ClusterStarts[startIndex];

            // Generate candidates for each note in the cluster
            for (int i = 0; i < pattern.Length; i++)
            {
                decimal beat = startBeat + pattern[i];

                // Skip if beat would exceed bar
                if (beat > drummerContext.BeatsPerBar)
                    continue;

                // Velocity decreases slightly through cluster for natural feel
                int velocityBase = VelocityMax - (i * 5);
                int velocityHint = GenerateVelocityHint(
                    VelocityMin,
                    Math.Max(VelocityMin, velocityBase),
                    drummerContext.BarNumber,
                    beat,
                    drummerContext.Seed);

                // Score decreases for later notes in cluster (first note most important)
                // Story 9.3: Apply motif multiplier to reduce score when motif active
                double score = BaseScore * (1.0 - (i * 0.1)) * (0.5 + 0.5 * drummerContext.EnergyLevel) * motifMultiplier;

                yield return CreateCandidate(
                    role: GrooveRoles.Snare,
                    barNumber: drummerContext.BarNumber,
                    beat: beat,
                    strength: OnsetStrength.Ghost,
                    score: Math.Clamp(score, 0.0, 1.0),
                    velocityHint: velocityHint);
            }
        }
    }
}
