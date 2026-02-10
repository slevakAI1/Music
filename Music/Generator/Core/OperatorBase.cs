// AI: purpose=Abstract base for drum operators; provides common helpers for candidate creation & scoring.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily; methods must be deterministic.
// AI: deps=Bar, OperatorCandidate, RemovalCandidate; removed energy thresholds; bar accessed directly.


using Music.Generator.Drums.Operators.Candidates;
using Music.Generator.Drums.Planning;
using Music.Generator.Groove;
using Music.Generator.Material;

namespace Music.Generator.Core
{
    // AI: purpose=Abstract base for drum operators; supplies common Score/CreateCandidate helpers.
    // AI: invariants=Subclasses must provide OperatorId and OperatorFamily; keep GenerateCandidates pure.
    public abstract class OperatorBase
    {
        public abstract string OperatorId { get; }

        public abstract OperatorFamily OperatorFamily { get; }

        public abstract IEnumerable<OperatorCandidate> GenerateCandidates(Bar bar, int seed);

        // AI: purpose=Optional removal targets; additive operators return empty by default.
        // AI: contract=Must return deterministic sequence; do not return null.
        public virtual IEnumerable<RemovalCandidate> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);
            return Array.Empty<RemovalCandidate>();
        }

        public virtual double Score(OperatorCandidate candidate, Bar bar)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Score;
        }

        // Create a OperatorCandidate populated with common operator-provided fields.
        protected OperatorCandidate CreateCandidate(
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
            return new OperatorCandidate
            {
                CandidateId = OperatorCandidate.GenerateCandidateId(OperatorId, role, barNumber, beat, articulationHint),
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
