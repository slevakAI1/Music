// AI: purpose=Abstract base for instrument operators; provides helpers for candidate creation & scoring.
// AI: invariants=Subclasses must provide OperatorId and OperatorFamily; methods must be deterministic.
// AI: deps=Bar, OperatorCandidateAddition, OperatorCandidateRemoval; adapter supplies instrument metadata.


using Music.Generator.Groove;
using Music.Generator.Material;

namespace Music.Generator.Core
{
    // AI: purpose=Abstract base for instrument operators; supplies common Score/CreateCandidate helpers.
    // AI: invariants=Subclasses must provide OperatorId and OperatorFamily; keep GenerateCandidates pure.
    public abstract class OperatorBase
    {
        // AI: deps=Instrument adapter supplies metadata; never null
        protected OperatorBase(IOperatorCandidateInstrumentAdapter instrumentAdapter)
        {
            ArgumentNullException.ThrowIfNull(instrumentAdapter);
            InstrumentAdapter = instrumentAdapter;
        }

        // AI: deps=Default adapter used by parameterless ctor; set by instrument layer before operator creation
        public static IOperatorCandidateInstrumentAdapter DefaultInstrumentAdapter { get; set; }
            = NullOperatorCandidateInstrumentAdapter.Instance;

        // AI: deps=Fallback adapter when subclass does not inject one; yields no metadata/discriminator
        protected OperatorBase() : this(DefaultInstrumentAdapter)
        {
        }

        // AI: deps=Instrument adapter for candidate metadata
        protected IOperatorCandidateInstrumentAdapter InstrumentAdapter { get; }

        // AI: purpose=Ambient song context set by applicator before GenerateCandidates; pitched operators use for harmony/groove.
        // AI: invariants=Null when no context available; operators must guard access.
        public SongContext? SongContext { get; set; }

        public abstract string OperatorId { get; }

        public abstract OperatorFamily OperatorFamily { get; }

        public abstract IEnumerable<OperatorCandidateAddition> GenerateCandidates(Bar bar, int seed);

        // AI: purpose=Optional removal targets; additive operators return empty by default.
        // AI: contract=Must return deterministic sequence; do not return null.
        public virtual IEnumerable<OperatorCandidateRemoval> GenerateRemovals(Bar bar)
        {
            ArgumentNullException.ThrowIfNull(bar);
            return Array.Empty<OperatorCandidateRemoval>();
        }

        public virtual double Score(OperatorCandidateAddition candidate, Bar bar)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            return candidate.Score;
        }

        // Create an OperatorCandidateAddition populated with common operator-provided fields.
        protected OperatorCandidateAddition CreateCandidate(
            string role,
            int barNumber,
            decimal beat,
            double score,
            int? velocityHint = null,
            int? timingHint = null,
            object? instrumentData = null,
            int? midiNote = null,
            int? durationTicks = null)
        {
            var metadata = InstrumentAdapter.BuildMetadata(instrumentData);

            return new OperatorCandidateAddition
            {
                CandidateId = OperatorCandidateAddition.GenerateCandidateId(OperatorId, role, barNumber, beat),
                OperatorId = OperatorId,
                Role = role,
                BarNumber = barNumber,
                Beat = beat,
                VelocityHint = velocityHint,
                TimingHint = timingHint,
                MidiNote = midiNote,
                DurationTicks = durationTicks,
                Score = score,
                Metadata = metadata
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
