// AI: purpose=Selection engine for agent operators; picks candidates using weighted scoring with density/cap limits.
// AI: invariants=Deterministic: same seed+inputs=same output; tie-breaking uses score desc→operatorId asc→candidateId asc.
// AI: deps=AgentMemory.GetRepetitionPenalty; Rng for randomized tie-breaks; sorted collections for determinism.
// AI: perf=O(n log n) sort; greedy selection O(n); no hotpath concerns for typical candidate counts (<100).
// AI: change=Extend with additional selection strategies (weighted random, probabilistic) if needed.

namespace Music.Generator.Core
{
    /// <summary>
    /// Metadata wrapper for a candidate during selection.
    /// Contains all scoring components and identification for tie-breaking.
    /// </summary>
    /// <typeparam name="TCandidate">The underlying candidate type.</typeparam>
    public sealed record ScoredCandidate<TCandidate>
    {
        /// <summary>The underlying candidate.</summary>
        public required TCandidate Candidate { get; init; }

        /// <summary>Stable operator ID for tie-breaking.</summary>
        public required string OperatorId { get; init; }

        /// <summary>Stable candidate ID for tie-breaking.</summary>
        public required string CandidateId { get; init; }

        /// <summary>Base score from operator (0.0-1.0).</summary>
        public required double BaseScore { get; init; }

        /// <summary>Style weight for this operator (default 0.5 if missing).</summary>
        public required double StyleWeight { get; init; }

        /// <summary>Repetition penalty from memory (0.0-1.0).</summary>
        public required double RepetitionPenalty { get; init; }

        /// <summary>Density contribution of this candidate (e.g., 1.0 for one note).</summary>
        public required double DensityContribution { get; init; }

        /// <summary>
        /// Computes final score: baseScore * styleWeight * (1.0 - repetitionPenalty).
        /// Clamped to [0.0, 1.0].
        /// </summary>
        public double FinalScore => Math.Clamp(BaseScore * StyleWeight * (1.0 - RepetitionPenalty), 0.0, 1.0);
    }

    /// <summary>
    /// Result of selection containing selected candidates and metadata.
    /// </summary>
    /// <typeparam name="TCandidate">The underlying candidate type.</typeparam>
    public sealed record SelectionResult<TCandidate>
    {
        /// <summary>Selected candidates in selection order.</summary>
        public required IReadOnlyList<ScoredCandidate<TCandidate>> Selected { get; init; }

        /// <summary>Total density of selected candidates.</summary>
        public required double TotalDensity { get; init; }

        /// <summary>Whether selection stopped due to density target reached.</summary>
        public required bool DensityTargetReached { get; init; }

        /// <summary>Whether selection stopped due to hard cap reached.</summary>
        public required bool HardCapReached { get; init; }
    }

    /// <summary>
    /// Selects candidates from operators using weighted scoring, respecting density targets and caps.
    /// Deterministic: same inputs + seed → identical output.
    /// </summary>
    /// <typeparam name="TCandidate">The candidate type produced by operators.</typeparam>
    public sealed class OperatorSelectionEngine<TCandidate>
    {
        private const double ScoreEpsilon = 0.0001;

        /// <summary>
        /// Selects candidates using greedy algorithm with scoring.
        /// 
        /// Algorithm:
        /// 1. Compute finalScore for each candidate
        /// 2. Sort by: finalScore desc → operatorId asc → candidateId asc
        /// 3. Greedily select candidates until density target or hard cap reached
        /// </summary>
        /// <param name="candidates">Scored candidates to select from.</param>
        /// <param name="densityTarget">Target density to reach (stop when >= target).</param>
        /// <param name="hardCap">Maximum number of candidates to select (never exceed).</param>
        /// <returns>Selection result with selected candidates and metadata.</returns>
        public SelectionResult<TCandidate> Select(
            IEnumerable<ScoredCandidate<TCandidate>> candidates,
            double densityTarget,
            int hardCap)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentOutOfRangeException.ThrowIfNegative(densityTarget);
            ArgumentOutOfRangeException.ThrowIfNegative(hardCap);

            // Sort candidates deterministically: score desc → operatorId asc → candidateId asc
            var sortedCandidates = candidates
                .OrderByDescending(c => c.FinalScore)
                .ThenBy(c => c.OperatorId, StringComparer.Ordinal)
                .ThenBy(c => c.CandidateId, StringComparer.Ordinal)
                .ToList();

            var selected = new List<ScoredCandidate<TCandidate>>();
            double currentDensity = 0.0;
            bool densityTargetReached = false;
            bool hardCapReached = false;

            foreach (var candidate in sortedCandidates)
            {
                // Check hard cap first
                if (selected.Count >= hardCap)
                {
                    hardCapReached = true;
                    break;
                }

                // Check density target
                if (currentDensity >= densityTarget)
                {
                    densityTargetReached = true;
                    break;
                }

                // Select this candidate
                selected.Add(candidate);
                currentDensity += candidate.DensityContribution;
            }

            // Check if we reached targets after last selection
            if (selected.Count >= hardCap)
                hardCapReached = true;
            if (currentDensity >= densityTarget)
                densityTargetReached = true;

            return new SelectionResult<TCandidate>
            {
                Selected = selected,
                TotalDensity = currentDensity,
                DensityTargetReached = densityTargetReached,
                HardCapReached = hardCapReached
            };
        }

        /// <summary>
        /// Selects candidates with randomized tie-breaking for equal scores.
        /// Uses Rng for deterministic randomization.
        /// </summary>
        /// <param name="candidates">Scored candidates to select from.</param>
        /// <param name="densityTarget">Target density to reach.</param>
        /// <param name="hardCap">Maximum number of candidates.</param>
        /// <param name="rngPurpose">RNG purpose for tie-breaking.</param>
        /// <returns>Selection result.</returns>
        public SelectionResult<TCandidate> SelectWithRandomTieBreak(
            IEnumerable<ScoredCandidate<TCandidate>> candidates,
            double densityTarget,
            int hardCap,
            RandomPurpose rngPurpose)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentOutOfRangeException.ThrowIfNegative(densityTarget);
            ArgumentOutOfRangeException.ThrowIfNegative(hardCap);

            // Group by score (within epsilon), then shuffle within groups using RNG
            var candidateList = candidates.ToList();
            var grouped = GroupByScore(candidateList);

            var shuffledCandidates = new List<ScoredCandidate<TCandidate>>();
            foreach (var group in grouped.OrderByDescending(g => g.Key))
            {
                var groupList = group.Value;

                // Shuffle within group using Fisher-Yates with deterministic RNG
                for (int i = groupList.Count - 1; i > 0; i--)
                {
                    int j = Rng.NextInt(rngPurpose, 0, i + 1);
                    (groupList[i], groupList[j]) = (groupList[j], groupList[i]);
                }

                shuffledCandidates.AddRange(groupList);
            }

            // Now select greedily
            var selected = new List<ScoredCandidate<TCandidate>>();
            double currentDensity = 0.0;
            bool densityTargetReached = false;
            bool hardCapReached = false;

            foreach (var candidate in shuffledCandidates)
            {
                if (selected.Count >= hardCap)
                {
                    hardCapReached = true;
                    break;
                }

                if (currentDensity >= densityTarget)
                {
                    densityTargetReached = true;
                    break;
                }

                selected.Add(candidate);
                currentDensity += candidate.DensityContribution;
            }

            if (selected.Count >= hardCap)
                hardCapReached = true;
            if (currentDensity >= densityTarget)
                densityTargetReached = true;

            return new SelectionResult<TCandidate>
            {
                Selected = selected,
                TotalDensity = currentDensity,
                DensityTargetReached = densityTargetReached,
                HardCapReached = hardCapReached
            };
        }

        /// <summary>
        /// Groups candidates by score (within epsilon tolerance).
        /// </summary>
        private SortedDictionary<double, List<ScoredCandidate<TCandidate>>> GroupByScore(
            List<ScoredCandidate<TCandidate>> candidates)
        {
            var groups = new SortedDictionary<double, List<ScoredCandidate<TCandidate>>>();

            foreach (var candidate in candidates)
            {
                double score = candidate.FinalScore;

                // Find existing group within epsilon
                double? matchingKey = null;
                foreach (var key in groups.Keys)
                {
                    if (Math.Abs(key - score) < ScoreEpsilon)
                    {
                        matchingKey = key;
                        break;
                    }
                }

                if (matchingKey.HasValue)
                {
                    groups[matchingKey.Value].Add(candidate);
                }
                else
                {
                    groups[score] = new List<ScoredCandidate<TCandidate>> { candidate };
                }
            }

            return groups;
        }

        /// <summary>
        /// Creates a scored candidate from operator output with memory penalty lookup.
        /// Convenience factory method.
        /// </summary>
        public static ScoredCandidate<TCandidate> CreateScoredCandidate(
            TCandidate candidate,
            string operatorId,
            string candidateId,
            double baseScore,
            double styleWeight,
            double densityContribution,
            GeneratorMemory memory)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(operatorId);
            ArgumentNullException.ThrowIfNull(candidateId);
            ArgumentNullException.ThrowIfNull(memory);

            double penalty = memory.GetRepetitionPenalty(operatorId);

            return new ScoredCandidate<TCandidate>
            {
                Candidate = candidate,
                OperatorId = operatorId,
                CandidateId = candidateId,
                BaseScore = baseScore,
                StyleWeight = styleWeight,
                RepetitionPenalty = penalty,
                DensityContribution = densityContribution
            };
        }

        /// <summary>
        /// Creates a scored candidate with explicit penalty (for testing or when memory not available).
        /// </summary>
        public static ScoredCandidate<TCandidate> CreateScoredCandidate(
            TCandidate candidate,
            string operatorId,
            string candidateId,
            double baseScore,
            double styleWeight,
            double densityContribution,
            double repetitionPenalty)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(operatorId);
            ArgumentNullException.ThrowIfNull(candidateId);

            return new ScoredCandidate<TCandidate>
            {
                Candidate = candidate,
                OperatorId = operatorId,
                CandidateId = candidateId,
                BaseScore = baseScore,
                StyleWeight = styleWeight,
                RepetitionPenalty = repetitionPenalty,
                DensityContribution = densityContribution
            };
        }
    }
}
