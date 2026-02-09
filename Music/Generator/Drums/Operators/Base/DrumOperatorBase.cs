// AI: purpose=Abstract base for all drum operators; provides helper methods.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily.
// AI: deps=IDrumOperator interface, Bar, DrumCandidate.
// AI: change=Epic DrummerContext-Dedup; removed energy thresholds; bar properties accessed directly.


using Music.Generator.Core;
using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Performance;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;
using Music.Generator.Material;

namespace Music.Generator.Drums.Operators.Base
{
    // AI: purpose=Abstract base for drum operators; supplies common Score/CreateCandidate helpers.
    // AI: invariants=Subclasses must provide OperatorId and OperatorFamily.
    public abstract class DrumOperatorBase : IDrumOperator
    {
        /// <inheritdoc/>
        public abstract string OperatorId { get; }

        /// <inheritdoc/>
        public abstract OperatorFamily OperatorFamily { get; }

        /// <inheritdoc/>
        public abstract IEnumerable<DrumCandidate> GenerateCandidates(Bar bar, int seed);

        /// <inheritdoc/>
        public virtual double Score(DrumCandidate candidate, Bar bar)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Score;
        }

        // Create a DrumCandidate populated with common operator-provided fields.
        protected DrumCandidate CreateCandidate(
            string role,
            int barNumber,
            decimal beat,
            OnsetStrength strength,
            double score,
            int? velocityHint = null,
            int? timingHint = null,
            DrumArticulation? articulationHint = null,
            FillRole fillRole = FillRole.None)
        {
            return new DrumCandidate
            {
                CandidateId = DrumCandidate.GenerateCandidateId(OperatorId, role, barNumber, beat, articulationHint),
                OperatorId = OperatorId,
                Role = role,
                BarNumber = barNumber,
                Beat = beat,
                Strength = strength,
                VelocityHint = velocityHint,
                TimingHint = timingHint,
                ArticulationHint = articulationHint,
                FillRole = fillRole,
                Score = score
            };
        }

        // Generate deterministic velocity within [min,max] using bar/beat/seed for repeatable jitter.
        protected static int GenerateVelocityHint(int min, int max, int barNumber, decimal beat, int seed)
        {
            // Simple deterministic pseudo-random within range
            int hash = HashCode.Combine(barNumber, beat, seed);
            int range = max - min + 1;
            return min + (Math.Abs(hash) % range);
        }

        // Score multiplier when motif is active. Returns 1.0 when motif absent or map null.
        // reductionFactor is fraction to subtract from 1.0 when motif active (e.g., 0.5 => 50% of score remains).
        protected static double GetMotifScoreMultiplier(MotifPresenceMap? motifPresenceMap, Bar bar, double reductionFactor)
        {
            ArgumentNullException.ThrowIfNull(bar);

            if (motifPresenceMap is null)
                return 1.0;

            if (!motifPresenceMap.IsMotifActive(bar.BarNumber))
                return 1.0;

            // Reduction factor is how much to reduce by (e.g., 0.5 = reduce by 50%)
            // Multiplier is what remains (e.g., 1.0 - 0.5 = 0.5)
            return 1.0 - reductionFactor;
        }
    }
}
