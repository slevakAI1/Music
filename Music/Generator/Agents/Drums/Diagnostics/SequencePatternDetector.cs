// AI: purpose=Detects multi-bar sequences (2-bar, 4-bar) and evolving patterns (Story 7.2b).
// AI: invariants=Sequences require >= 2 occurrences; evolution threshold 0.6-0.95; deterministic output.
// AI: deps=Uses BarPatternFingerprint; outputs SequencePatternData.
// AI: change=Story 7.2b; extend to 8-bar phrases if needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Detects multi-bar sequences and evolving patterns in drum tracks.
/// Identifies common 2-bar and 4-bar phrase structures.
/// Story 7.2b: Multi-Bar Sequence Detection.
/// </summary>
public static class SequencePatternDetector
{
    /// <summary>
    /// Minimum occurrences for a sequence to be reported.
    /// </summary>
    public const int MinOccurrences = 2;

    /// <summary>
    /// Minimum similarity for an evolution step.
    /// </summary>
    public const double MinEvolutionSimilarity = 0.6;

    /// <summary>
    /// Maximum similarity for an evolution step (must be different from base).
    /// </summary>
    public const double MaxEvolutionSimilarity = 0.95;

    /// <summary>
    /// Minimum steps for an evolving sequence.
    /// </summary>
    public const int MinEvolutionSteps = 3;

    /// <summary>
    /// Detects multi-bar sequences from a list of fingerprints.
    /// </summary>
    /// <param name="fingerprints">Per-bar pattern fingerprints.</param>
    /// <returns>Sequence pattern data.</returns>
    public static SequencePatternData Detect(IReadOnlyList<BarPatternFingerprint> fingerprints)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);

        if (fingerprints.Count < 2)
        {
            return CreateEmpty();
        }

        // Sort by bar number
        var sorted = fingerprints.OrderBy(f => f.BarNumber).ToList();

        // Detect fixed-length sequences
        var twoBarSequences = DetectNBarSequences(sorted, 2);
        var fourBarSequences = DetectNBarSequences(sorted, 4);

        // Detect evolving sequences
        var evolvingSequences = DetectEvolvingSequences(sorted);

        return new SequencePatternData
        {
            TwoBarSequences = twoBarSequences,
            FourBarSequences = fourBarSequences,
            EvolvingSequences = evolvingSequences
        };
    }

    /// <summary>
    /// Detects N-bar sequences that repeat in the track.
    /// </summary>
    private static IReadOnlyList<MultiBarSequence> DetectNBarSequences(
        List<BarPatternFingerprint> sorted,
        int n)
    {
        if (sorted.Count < n)
            return Array.Empty<MultiBarSequence>();

        // Build a dictionary of sequence hash → start bars
        var sequenceOccurrences = new Dictionary<string, List<int>>();

        for (int i = 0; i <= sorted.Count - n; i++)
        {
            // Check if we have N consecutive bars
            bool consecutive = true;
            for (int j = 1; j < n; j++)
            {
                if (sorted[i + j].BarNumber != sorted[i + j - 1].BarNumber + 1)
                {
                    consecutive = false;
                    break;
                }
            }

            if (!consecutive)
                continue;

            // Build sequence hash from concatenated pattern hashes
            var patternHashes = new List<string>();
            for (int j = 0; j < n; j++)
            {
                patternHashes.Add(sorted[i + j].PatternHash);
            }

            var sequenceKey = string.Join("|", patternHashes);
            var startBar = sorted[i].BarNumber;

            if (!sequenceOccurrences.TryGetValue(sequenceKey, out var occurrences))
            {
                occurrences = new List<int>();
                sequenceOccurrences[sequenceKey] = occurrences;
            }

            // Only add if not overlapping with previous occurrence
            if (occurrences.Count == 0 || startBar >= occurrences[^1] + n)
            {
                occurrences.Add(startBar);
            }
        }

        // Filter to sequences with enough occurrences
        var result = new List<MultiBarSequence>();

        foreach (var (sequenceKey, occurrences) in sequenceOccurrences)
        {
            if (occurrences.Count >= MinOccurrences)
            {
                var patternHashes = sequenceKey.Split('|').ToList();
                result.Add(new MultiBarSequence(
                    patternHashes,
                    occurrences.OrderBy(b => b).ToList(),
                    n));
            }
        }

        // Sort by occurrence count descending, then by first occurrence
        return result
            .OrderByDescending(s => s.Occurrences.Count)
            .ThenBy(s => s.Occurrences[0])
            .ThenBy(s => string.Join("|", s.PatternHashes), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Detects sequences showing gradual pattern evolution.
    /// </summary>
    private static IReadOnlyList<EvolvingSequence> DetectEvolvingSequences(
        List<BarPatternFingerprint> sorted)
    {
        if (sorted.Count < MinEvolutionSteps)
            return Array.Empty<EvolvingSequence>();

        var evolving = new List<EvolvingSequence>();
        var usedBars = new HashSet<int>();

        for (int startIdx = 0; startIdx <= sorted.Count - MinEvolutionSteps; startIdx++)
        {
            var baseFp = sorted[startIdx];

            if (usedBars.Contains(baseFp.BarNumber))
                continue;

            var steps = new List<EvolutionStep>
            {
                new(baseFp.BarNumber, baseFp.PatternHash, 1.0)
            };

            int prevBar = baseFp.BarNumber;

            // Look for consecutive bars with decreasing similarity
            for (int i = startIdx + 1; i < sorted.Count; i++)
            {
                var currFp = sorted[i];

                // Must be consecutive
                if (currFp.BarNumber != prevBar + 1)
                    break;

                var similarity = BarPatternExtractor.CalculateSimilarity(baseFp, currFp);

                // Must be similar but not identical
                if (similarity >= MinEvolutionSimilarity && similarity <= MaxEvolutionSimilarity)
                {
                    steps.Add(new EvolutionStep(currFp.BarNumber, currFp.PatternHash, similarity));
                    prevBar = currFp.BarNumber;
                }
                else
                {
                    break;
                }
            }

            if (steps.Count >= MinEvolutionSteps)
            {
                // Check that similarity actually decreases (evolution trend)
                bool isEvolving = true;
                for (int j = 2; j < steps.Count; j++)
                {
                    if (steps[j].SimilarityToBase > steps[j - 1].SimilarityToBase + 0.1)
                    {
                        isEvolving = false;
                        break;
                    }
                }

                if (isEvolving)
                {
                    evolving.Add(new EvolvingSequence(
                        baseFp.PatternHash,
                        steps,
                        steps[^1].BarNumber - steps[0].BarNumber + 1));

                    // Mark bars as used
                    foreach (var step in steps)
                        usedBars.Add(step.BarNumber);
                }
            }
        }

        return evolving
            .OrderByDescending(e => e.TotalBarsSpanned)
            .ThenBy(e => e.Steps[0].BarNumber)
            .ToList();
    }

    /// <summary>
    /// Creates empty sequence pattern data.
    /// </summary>
    private static SequencePatternData CreateEmpty()
    {
        return new SequencePatternData
        {
            TwoBarSequences = Array.Empty<MultiBarSequence>(),
            FourBarSequences = Array.Empty<MultiBarSequence>(),
            EvolvingSequences = Array.Empty<EvolvingSequence>()
        };
    }
}
