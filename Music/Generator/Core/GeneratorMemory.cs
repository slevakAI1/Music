// AI: purpose=Concrete memory implementation tracking recent decisions for anti-repetition.
// AI: invariants=Deterministic: same sequence of records = same state; uses sorted collections for stable iteration.
// AI: deps=IAgentMemory, FillShape, DecayCurve, MusicConstants.eSectionType.
// AI: perf=Circular buffer O(1) insert; usage lookup O(windowSize); sorted keys for determinism.
// AI: change=Extend with DrummerMemory for instrument-specific tracking; keep interface stable.

namespace Music.Generator.Core
{
    // AI: purpose=Tracks recent decisions for anti-repetition using a circular buffer keyed by bar
    // AI: invariants=Deterministic iteration order; windowSize controls memory; keys are 1-based bars
    public class GeneratorMemory : IGeneratorMemory
    {
        private readonly int _windowSize;
        private readonly DecayCurve _decayCurve;
        private readonly double _decayFactor;

        // Circular buffer: barNumber → list of (operatorId, candidateId)
        private readonly Dictionary<int, List<DecisionRecord>> _decisions = new();

        // Fill shape tracking
        private FillShape? _lastFillShape;

        // Section signatures: sectionType → sorted list of operator IDs
        private readonly Dictionary<MusicConstants.eSectionType, SortedSet<string>> _sectionSignatures = new();

        private int _currentBarNumber;

        // AI: ctor=windowSize>=1; decayFactor in (0,1); DecayCurve controls ComputeDecayWeight behavior
        public GeneratorMemory(int windowSize = 8, DecayCurve decayCurve = DecayCurve.Exponential, double decayFactor = 0.7)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(windowSize, 1);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(decayFactor, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(decayFactor, 1.0);

            _windowSize = windowSize;
            _decayCurve = decayCurve;
            _decayFactor = decayFactor;
        }

        // AI: prop=Latest recorded bar number; 0 when no decisions recorded
        public int CurrentBarNumber => _currentBarNumber;

        // AI: record=Add decision at barNumber; prunes entries older than window; barNumber>=1 required
        public void RecordDecision(int barNumber, string operatorId, string candidateId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);
            ArgumentNullException.ThrowIfNull(candidateId);
            ArgumentOutOfRangeException.ThrowIfLessThan(barNumber, 1);

            // Prune old entries outside window
            PruneOldEntries(barNumber);

            if (!_decisions.TryGetValue(barNumber, out var list))
            {
                list = new List<DecisionRecord>();
                _decisions[barNumber] = list;
            }

            list.Add(new DecisionRecord(operatorId, candidateId));
            _currentBarNumber = Math.Max(_currentBarNumber, barNumber);
        }

        // AI: query=Return sorted map operatorId->count over the last N bars; deterministic ordering
        public IReadOnlyDictionary<string, int> GetRecentOperatorUsage(int lastNBars)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(lastNBars, 1);

            var result = new SortedDictionary<string, int>(); // Sorted for determinism
            int startBar = Math.Max(1, _currentBarNumber - lastNBars + 1);

            foreach (var kvp in _decisions.Where(d => d.Key >= startBar).OrderBy(d => d.Key))
            {
                foreach (var record in kvp.Value)
                {
                    if (!result.TryGetValue(record.OperatorId, out int count))
                        count = 0;
                    result[record.OperatorId] = count + 1;
                }
            }

            return result;
        }

        // AI: penalty=Compute repetition penalty in [0.0,1.0] using configured decay over _windowSize bars
        public double GetRepetitionPenalty(string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);

            if (_currentBarNumber == 0)
                return 0.0;

            double totalPenalty = 0.0;
            int startBar = Math.Max(1, _currentBarNumber - _windowSize + 1);

            // Iterate through window in bar order for determinism
            foreach (var kvp in _decisions.Where(d => d.Key >= startBar).OrderBy(d => d.Key))
            {
                int age = _currentBarNumber - kvp.Key; // 0 = current bar, higher = older
                int usageInBar = kvp.Value.Count(r => r.OperatorId == operatorId);

                if (usageInBar > 0)
                {
                    double decayedWeight = ComputeDecayWeight(age);
                    totalPenalty += usageInBar * decayedWeight;
                }
            }

            // Normalize to [0.0, 1.0] — max theoretical penalty is windowSize uses at current bar
            double maxPenalty = _windowSize;
            double normalizedPenalty = Math.Min(1.0, totalPenalty / maxPenalty);

            return normalizedPenalty;
        }

        // AI: query=Returns last recorded FillShape or null when none
        public FillShape? GetLastFillShape() => _lastFillShape;

        // AI: record=Store most recent FillShape; used by agents to avoid repeating fills
        public void RecordFillShape(FillShape fillShape)
        {
            ArgumentNullException.ThrowIfNull(fillShape);
            _lastFillShape = fillShape;
        }

        // AI: query=Return sorted operator IDs for sectionType; returns a copy to preserve internal set
        public IReadOnlyList<string> GetSectionSignature(MusicConstants.eSectionType sectionType)
        {
            if (_sectionSignatures.TryGetValue(sectionType, out var signature))
                return signature.ToList(); // Return sorted copy

            return Array.Empty<string>();
        }

        // AI: record=Add operatorId to sorted signature for sectionType; idempotent and deterministic
        public void RecordSectionSignature(MusicConstants.eSectionType sectionType, string operatorId)
        {
            ArgumentNullException.ThrowIfNull(operatorId);

            if (!_sectionSignatures.TryGetValue(sectionType, out var signature))
            {
                signature = new SortedSet<string>(); // Sorted for determinism
                _sectionSignatures[sectionType] = signature;
            }

            signature.Add(operatorId);
        }

        // AI: operation=Clear all memory state including decisions, signatures and last fill
        public void Clear()
        {
            _decisions.Clear();
            _sectionSignatures.Clear();
            _lastFillShape = null;
            _currentBarNumber = 0;
        }

        // Compute decay weight for age (bars from current) per configured DecayCurve
        private double ComputeDecayWeight(int age)
        {
            return _decayCurve switch
            {
                DecayCurve.Linear => Math.Max(0.0, (_windowSize - age) / (double)_windowSize),
                DecayCurve.Exponential => Math.Pow(_decayFactor, age),
                _ => 1.0
            };
        }

        // Remove decision entries older than the sliding window
        private void PruneOldEntries(int currentBar)
        {
            int oldestAllowed = currentBar - _windowSize + 1;
            var keysToRemove = _decisions.Keys.Where(k => k < oldestAllowed).ToList();

            foreach (var key in keysToRemove)
            {
                _decisions.Remove(key);
            }
        }

        // Internal record for storing decisions: operatorId and candidateId
        private readonly record struct DecisionRecord(string OperatorId, string CandidateId);
    }
}
