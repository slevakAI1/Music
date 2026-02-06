// AI: purpose=Abstract base for all drum operators; provides common CanApply checks and helper methods.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily; base CanApply validates context type.
// AI: deps=IDrumOperator interface, DrummerContext (minimal: Bar + cross-bar state), DrumCandidate.
// AI: change=Epic DrummerContext-Dedup; removed energy thresholds; bar properties accessed via context.Bar.


using Music.Generator.Core;
using Music.Generator.Drums.Context;
using Music.Generator.Drums.Performance;
using Music.Generator.Drums.Planning;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;
using Music.Generator.Material;

namespace Music.Generator.Drums.Operators.Base
{
    // AI: purpose=Abstract base for drum operators; supplies common CanApply/Score/CreateCandidate helpers.
    // AI: invariants=Subclasses must provide OperatorId and OperatorFamily; use context.Bar for bar-derived checks.
    public abstract class DrumOperatorBase : IDrumOperator
    {
        /// <inheritdoc/>
        public abstract string OperatorId { get; }

        /// <inheritdoc/>
        public abstract OperatorFamily OperatorFamily { get; }

        // RequiredRole: when non-null operator is intended only for that role; null => no role restriction.
        protected virtual string? RequiredRole => null;

        // Requires16thGrid: set true when operator assumes 16th-note grid alignment for onsets.
        protected virtual bool Requires16thGrid => false;

        /// <inheritdoc/>
        public virtual bool CanApply(GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context is not DrummerContext drummerContext)
                return false;

            return CanApply(drummerContext);
        }

        /// <inheritdoc/>
        public virtual bool CanApply(DrummerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Required role check (if role is required, check against groove preset active roles)
            if (RequiredRole is not null)
            {
                // TODO: Access active roles from groove preset via context.Bar
                // For now, assume role is available (operators will self-gate if needed)
            }

            return true;
        }

        /// <inheritdoc/>
        public abstract IEnumerable<DrumCandidate> GenerateCandidates(GeneratorContext context);

        /// <inheritdoc/>
        public virtual IEnumerable<DrumCandidate> GenerateCandidates(DrummerContext context)
        {
            return GenerateCandidates((GeneratorContext)context);
        }

        /// <inheritdoc/>
        public virtual double Score(DrumCandidate candidate, GeneratorContext context)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Score;
        }

        /// <inheritdoc/>
        public virtual double Score(DrumCandidate candidate, DrummerContext context)
        {
            return Score(candidate, (GeneratorContext)context);
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
