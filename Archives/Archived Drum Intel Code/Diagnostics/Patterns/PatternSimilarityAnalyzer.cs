// AI: purpose=Analyzes pattern similarity using Jaccard distance on bitmasks (Story 7.2b).
// AI: invariants=Threshold >= 0.7 for "similar"; families built via greedy clustering; deterministic output.
// AI: deps=Uses BarPatternExtractor.CalculateSimilarity; outputs PatternSimilarityData.
// AI: change=Story 7.2b; tune threshold and clustering based on analysis feedback.


// AI: purpose=Analyzes pattern similarity using Jaccard distance on bitmasks (Story 7.2b).
// AI: invariants=Threshold >= 0.7 for "similar"; families built via greedy clustering; deterministic output.
// AI: deps=Uses BarPatternExtractor.CalculateSimilarity; outputs PatternSimilarityData.
// AI: change=Story 7.2b; tune threshold and clustering based on analysis feedback.

using Music.Generator.Drums.Diagnostics.BarAnalysis;

namespace Music.Generator.Drums.Diagnostics.Patterns;

/// <summary>
/// Analyzes pattern similarity using Jaccard similarity on role bitmasks.
/// Identifies similar pattern pairs and groups patterns into families.
/// Story 7.2b: Pattern Similarity Analysis.
/// </summary>
public static class PatternSimilarityAnalyzer
{
    /// <summary>
    /// Minimum similarity to be considered "similar".
    /// </summary>
    public const double SimilarityThreshold = 0.7;

    /// <summary>
    /// Analyzes pattern similarity from a list of bar fingerprints.
    /// </summary>
    /// <param name="fingerprints">Per-bar pattern fingerprints.</param>
    /// <returns>Pattern similarity analysis data.</returns>
    public static PatternSimilarityData Analyze(IReadOnlyList<BarPatternFingerprint> fingerprints)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);

        if (fingerprints.Count == 0)
        {
            return CreateEmpty();
        }

        // Get unique patterns with their fingerprints
        var uniquePatterns = fingerprints
            .GroupBy(f => f.PatternHash)
            .ToDictionary(g => g.Key, g => g.First());

        var patternHashes = uniquePatterns.Keys.OrderBy(h => h, StringComparer.Ordinal).ToList();

        // Find similar pairs
        var similarPairs = FindSimilarPairs(uniquePatterns, patternHashes);

        // Build pattern families
        var families = BuildPatternFamilies(fingerprints, similarPairs);

        return new PatternSimilarityData
        {
            SimilarPairs = similarPairs,
            PatternFamilies = families
        };
    }

    /// <summary>
    /// Finds all pairs of patterns with similarity above threshold.
    /// </summary>
    private static IReadOnlyList<SimilarPatternPair> FindSimilarPairs(
        Dictionary<string, BarPatternFingerprint> uniquePatterns,
        List<string> patternHashes)
    {
        var pairs = new List<SimilarPatternPair>();

        // Compare all unique pattern pairs
        for (int i = 0; i < patternHashes.Count; i++)
        {
            for (int j = i + 1; j < patternHashes.Count; j++)
            {
                var hashA = patternHashes[i];
                var hashB = patternHashes[j];

                // Skip identical patterns
                if (hashA == hashB)
                    continue;

                var fpA = uniquePatterns[hashA];
                var fpB = uniquePatterns[hashB];

                var similarity = BarPatternExtractor.CalculateSimilarity(fpA, fpB);

                if (similarity >= SimilarityThreshold)
                {
                    pairs.Add(new SimilarPatternPair(hashA, hashB, similarity));
                }
            }
        }

        // Sort by similarity descending, then by hash for determinism
        return pairs
            .OrderByDescending(p => p.Similarity)
            .ThenBy(p => p.PatternHashA, StringComparer.Ordinal)
            .ThenBy(p => p.PatternHashB, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Builds pattern families using greedy clustering.
    /// The most common pattern in each cluster is the base.
    /// </summary>
    private static IReadOnlyList<PatternFamily> BuildPatternFamilies(
        IReadOnlyList<BarPatternFingerprint> fingerprints,
        IReadOnlyList<SimilarPatternPair> similarPairs)
    {
        if (similarPairs.Count == 0)
            return Array.Empty<PatternFamily>();

        // Count occurrences of each pattern
        var patternCounts = fingerprints
            .GroupBy(f => f.PatternHash)
            .ToDictionary(g => g.Key, g => g.Count());

        // Track which bars have each pattern
        var patternBars = fingerprints
            .GroupBy(f => f.PatternHash)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.BarNumber).OrderBy(b => b).ToList());

        // Build adjacency list from similar pairs
        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var pair in similarPairs)
        {
            if (!adjacency.ContainsKey(pair.PatternHashA))
                adjacency[pair.PatternHashA] = new HashSet<string>();
            if (!adjacency.ContainsKey(pair.PatternHashB))
                adjacency[pair.PatternHashB] = new HashSet<string>();

            adjacency[pair.PatternHashA].Add(pair.PatternHashB);
            adjacency[pair.PatternHashB].Add(pair.PatternHashA);
        }

        // Greedy clustering: find connected components
        var visited = new HashSet<string>();
        var families = new List<PatternFamily>();

        foreach (var seed in adjacency.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (visited.Contains(seed))
                continue;

            // BFS to find all connected patterns
            var cluster = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!cluster.Add(current))
                    continue;

                visited.Add(current);

                if (adjacency.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (!cluster.Contains(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }
            }

            // Find base pattern (most common in cluster)
            var baseHash = cluster
                .OrderByDescending(h => patternCounts.GetValueOrDefault(h, 0))
                .ThenBy(h => h, StringComparer.Ordinal) // Deterministic tie-break
                .First();

            var variants = cluster
                .Where(h => h != baseHash)
                .OrderBy(h => h, StringComparer.Ordinal)
                .ToList();

            // Collect all bar numbers for this family
            var allBars = cluster
                .SelectMany(h => patternBars.GetValueOrDefault(h, new List<int>()))
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            families.Add(new PatternFamily(baseHash, variants, allBars));
        }

        // Sort families by size (number of patterns) descending
        return families
            .OrderByDescending(f => 1 + f.VariantHashes.Count)
            .ThenBy(f => f.BasePatternHash, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Creates empty pattern similarity data.
    /// </summary>
    private static PatternSimilarityData CreateEmpty()
    {
        return new PatternSimilarityData
        {
            SimilarPairs = Array.Empty<SimilarPatternPair>(),
            PatternFamilies = Array.Empty<PatternFamily>()
        };
    }
}
