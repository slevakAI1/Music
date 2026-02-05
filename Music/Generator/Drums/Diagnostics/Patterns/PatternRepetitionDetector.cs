// AI: purpose=Detects pattern repetition from bar fingerprints (Story 7.2b).
// AI: invariants=Deterministic output; runs detected only for length >= 2; top 10 patterns by frequency.
// AI: deps=Consumes IReadOnlyList<BarPatternFingerprint>; outputs PatternRepetitionData.
// AI: change=Story 7.2b; tune thresholds based on analysis feedback.

using Music.Generator.Drums.Diagnostics.BarAnalysis;

namespace Music.Generator.Drums.Diagnostics.Patterns;

/// <summary>
/// Detects pattern repetition across bars using fingerprint hashes.
/// Identifies recurring patterns, consecutive runs, and most common patterns.
/// Story 7.2b: Pattern Repetition Detection.
/// </summary>
public static class PatternRepetitionDetector
{
    /// <summary>
    /// Maximum number of most common patterns to track.
    /// </summary>
    public const int MaxCommonPatterns = 10;

    /// <summary>
    /// Minimum run length to be considered a consecutive run.
    /// </summary>
    public const int MinRunLength = 2;

    /// <summary>
    /// Detects pattern repetition from a list of bar fingerprints.
    /// </summary>
    /// <param name="fingerprints">Per-bar pattern fingerprints.</param>
    /// <returns>Pattern repetition data.</returns>
    public static PatternRepetitionData Detect(IReadOnlyList<BarPatternFingerprint> fingerprints)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);

        if (fingerprints.Count == 0)
        {
            return CreateEmpty();
        }

        // Build pattern occurrences: hash → list of bar numbers
        var occurrences = new Dictionary<string, List<int>>();

        foreach (var fp in fingerprints)
        {
            if (!occurrences.TryGetValue(fp.PatternHash, out var barList))
            {
                barList = new List<int>();
                occurrences[fp.PatternHash] = barList;
            }
            barList.Add(fp.BarNumber);
        }

        // Build most common patterns (top 10)
        var mostCommon = occurrences
            .Select(kvp => new PatternFrequency(
                kvp.Key,
                kvp.Value.Count,
                kvp.Value.OrderBy(b => b).ToList()))
            .OrderByDescending(pf => pf.OccurrenceCount)
            .ThenBy(pf => pf.PatternHash, StringComparer.Ordinal) // Deterministic tie-break
            .Take(MaxCommonPatterns)
            .ToList();

        // Detect consecutive runs
        var runs = DetectConsecutiveRuns(fingerprints);

        // Build read-only occurrences dictionary
        var readOnlyOccurrences = occurrences.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<int>)kvp.Value.OrderBy(b => b).ToList());

        return new PatternRepetitionData
        {
            PatternOccurrences = readOnlyOccurrences,
            UniquePatternCount = occurrences.Count,
            MostCommonPatterns = mostCommon,
            ConsecutiveRuns = runs,
            TotalBars = fingerprints.Count
        };
    }

    /// <summary>
    /// Detects consecutive runs of the same pattern.
    /// </summary>
    private static IReadOnlyList<PatternRun> DetectConsecutiveRuns(
        IReadOnlyList<BarPatternFingerprint> fingerprints)
    {
        if (fingerprints.Count < MinRunLength)
            return Array.Empty<PatternRun>();

        var runs = new List<PatternRun>();

        // Sort fingerprints by bar number to ensure sequential processing
        var sorted = fingerprints.OrderBy(f => f.BarNumber).ToList();

        int runStart = 0;
        string currentHash = sorted[0].PatternHash;

        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];

            // Check if this continues the run
            // Must be consecutive bars AND same pattern
            bool isContinuation = curr.BarNumber == prev.BarNumber + 1 &&
                                  curr.PatternHash == currentHash;

            if (!isContinuation)
            {
                // End current run if it meets minimum length
                int runLength = i - runStart;
                if (runLength >= MinRunLength)
                {
                    runs.Add(new PatternRun(
                        currentHash,
                        sorted[runStart].BarNumber,
                        sorted[i - 1].BarNumber,
                        runLength));
                }

                // Start new run
                runStart = i;
                currentHash = curr.PatternHash;
            }
        }

        // Handle final run
        int finalRunLength = sorted.Count - runStart;
        if (finalRunLength >= MinRunLength)
        {
            runs.Add(new PatternRun(
                currentHash,
                sorted[runStart].BarNumber,
                sorted[^1].BarNumber,
                finalRunLength));
        }

        return runs;
    }

    /// <summary>
    /// Creates empty pattern repetition data.
    /// </summary>
    private static PatternRepetitionData CreateEmpty()
    {
        return new PatternRepetitionData
        {
            PatternOccurrences = new Dictionary<string, IReadOnlyList<int>>(),
            UniquePatternCount = 0,
            MostCommonPatterns = Array.Empty<PatternFrequency>(),
            ConsecutiveRuns = Array.Empty<PatternRun>(),
            TotalBars = 0
        };
    }
}
