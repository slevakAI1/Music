// AI: purpose=Concrete memory implementation tracking recent decisions for anti-repetition.
// AI: invariants=Deterministic: same sequence of records = same state; uses sorted collections for stable iteration.
// AI: deps=IAgentMemory, FillShape, DecayCurve, MusicConstants.eSectionType.
// AI: perf=Circular buffer O(1) insert; usage lookup O(windowSize); sorted keys for determinism.
// AI: change=Extend with DrummerMemory for instrument-specific tracking; keep interface stable.

namespace Music.Generator.Agents.Common
{
    /// <summary>
    /// Tracks agent decisions to prevent repetition.
    /// Uses circular buffer for efficient last-N-bars tracking.
    /// Human musicians don't repeat the exact same pattern 8 times—this creates variation.
    /// </summary>
    public class AgentMemory : IAgentMemory
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

        /// <summary>
        /// Creates a new agent memory with configurable window and decay.
        /// </summary>
        /// <param name="windowSize">Number of recent bars to track (default 8).</param>
        /// <param name="decayCurve">How penalty decays over the window (default Exponential).</param>
        /// <param name="decayFactor">Base for exponential decay (default 0.7); ignored for Linear.</param>
        public AgentMemory(int windowSize = 8, DecayCurve decayCurve = DecayCurve.Exponential, double decayFactor = 0.7)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(windowSize, 1);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(decayFactor, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(decayFactor, 1.0);

            _windowSize = windowSize;
            _decayCurve = decayCurve;
            _decayFactor = decayFactor;
        }

        /// <inheritdoc />
        public int CurrentBarNumber => _currentBarNumber;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// Computes repetition penalty for an operator based on recent usage.
        /// Returns 0.0 if never used, up to 1.0 for heavy recent use.
        /// </summary>
        /// <param name="operatorId">The operator to check.</param>
        /// <returns>Penalty in range [0.0, 1.0].</returns>
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

        /// <inheritdoc />
        public FillShape? GetLastFillShape() => _lastFillShape;

        /// <inheritdoc />
        public void RecordFillShape(FillShape fillShape)
        {
            ArgumentNullException.ThrowIfNull(fillShape);
            _lastFillShape = fillShape;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetSectionSignature(MusicConstants.eSectionType sectionType)
        {
            if (_sectionSignatures.TryGetValue(sectionType, out var signature))
                return signature.ToList(); // Return sorted copy

            return Array.Empty<string>();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Clear()
        {
            _decisions.Clear();
            _sectionSignatures.Clear();
            _lastFillShape = null;
            _currentBarNumber = 0;
        }

        /// <summary>
        /// Computes decay weight for a given age (bars from current).
        /// </summary>
        private double ComputeDecayWeight(int age)
        {
            return _decayCurve switch
            {
                DecayCurve.Linear => Math.Max(0.0, (_windowSize - age) / (double)_windowSize),
                DecayCurve.Exponential => Math.Pow(_decayFactor, age),
                _ => 1.0
            };
        }

        /// <summary>
        /// Removes entries outside the memory window.
        /// </summary>
        private void PruneOldEntries(int currentBar)
        {
            int oldestAllowed = currentBar - _windowSize + 1;
            var keysToRemove = _decisions.Keys.Where(k => k < oldestAllowed).ToList();

            foreach (var key in keysToRemove)
            {
                _decisions.Remove(key);
            }
        }

        /// <summary>
        /// Internal record for storing decisions.
        /// </summary>
        private readonly record struct DecisionRecord(string OperatorId, string CandidateId);
    }
}
