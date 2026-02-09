// AI: purpose=Detects structural markers like fills, crashes, section changes (Story 7.2b).
// AI: invariants=Density anomaly >= 2 std dev; pattern change similarity < 0.5; deterministic output.
// AI: deps=Uses DrumTrackFeatureData; outputs StructuralMarkerData.
// AI: change=Story 7.2b; tune thresholds based on analysis feedback.

using Music.Generator.Drums.Diagnostics;
using Music.Generator.Drums.Diagnostics.BarAnalysis;
using Music.Generator.Drums.Diagnostics.Features;

namespace Music.Generator.Drums.Diagnostics.Coordination;

/// <summary>
/// Detects structural markers in drum tracks: fills, density anomalies, crashes, pattern changes.
/// Uses heuristics based on density, pattern similarity, and role presence.
/// Story 7.2b: Structural Marker Detection.
/// </summary>
public static class StructuralMarkerDetector
{
    /// <summary>
    /// Standard deviations from mean to be considered a density anomaly.
    /// </summary>
    public const double DensityAnomalyThreshold = 2.0;

    /// <summary>
    /// Maximum similarity for a pattern change to be flagged.
    /// </summary>
    public const double PatternChangeThreshold = 0.5;

    /// <summary>
    /// Roles that indicate crash cymbal presence.
    /// </summary>
    private static readonly HashSet<string> CrashRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Crash", "Crash2", "CrashCymbal"
    };

    /// <summary>
    /// Roles that indicate tom presence (suggests fill).
    /// </summary>
    private static readonly HashSet<string> TomRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tom1", "Tom2", "FloorTom", "HighTom", "MidTom", "LowTom"
    };

    /// <summary>
    /// Detects structural markers from feature data.
    /// </summary>
    /// <param name="featureData">Base feature data from Story 7.2a.</param>
    /// <returns>Structural marker data.</returns>
    public static StructuralMarkerData Detect(DrumTrackFeatureData featureData)
    {
        ArgumentNullException.ThrowIfNull(featureData);

        if (featureData.TotalBars == 0)
        {
            return CreateEmpty();
        }

        // Detect density anomalies
        var (highDensity, lowDensity) = DetectDensityAnomalies(featureData.BarStats);

        // Detect crash bars
        var crashBars = DetectCrashBars(featureData.Events);

        // Detect pattern changes
        var patternChanges = DetectPatternChanges(featureData.BarPatterns);

        // Detect potential fills
        var potentialFills = DetectPotentialFills(
            featureData,
            highDensity,
            crashBars,
            patternChanges);

        return new StructuralMarkerData
        {
            HighDensityBars = highDensity,
            LowDensityBars = lowDensity,
            CrashBars = crashBars,
            PatternChanges = patternChanges,
            PotentialFills = potentialFills
        };
    }

    /// <summary>
    /// Detects bars with density significantly above or below mean.
    /// </summary>
    private static (IReadOnlyList<DensityAnomaly> high, IReadOnlyList<DensityAnomaly> low)
        DetectDensityAnomalies(IReadOnlyList<BarOnsetStats> barStats)
    {
        if (barStats.Count == 0)
            return (Array.Empty<DensityAnomaly>(), Array.Empty<DensityAnomaly>());

        // Calculate mean and std dev of hit counts
        var hitCounts = barStats.Select(s => (double)s.TotalHits).ToList();
        var mean = hitCounts.Average();
        var variance = hitCounts.Count > 1
            ? hitCounts.Sum(h => (h - mean) * (h - mean)) / (hitCounts.Count - 1)
            : 0;
        var stdDev = Math.Sqrt(variance);

        if (stdDev < 0.001) // No variation
            return (Array.Empty<DensityAnomaly>(), Array.Empty<DensityAnomaly>());

        var high = new List<DensityAnomaly>();
        var low = new List<DensityAnomaly>();

        foreach (var stats in barStats)
        {
            var deviation = (stats.TotalHits - mean) / stdDev;

            if (deviation >= DensityAnomalyThreshold)
            {
                high.Add(new DensityAnomaly(stats.BarNumber, stats.TotalHits, deviation));
            }
            else if (deviation <= -DensityAnomalyThreshold)
            {
                low.Add(new DensityAnomaly(stats.BarNumber, stats.TotalHits, deviation));
            }
        }

        return (
            high.OrderBy(a => a.BarNumber).ToList(),
            low.OrderBy(a => a.BarNumber).ToList()
        );
    }

    /// <summary>
    /// Detects bars containing crash cymbal hits.
    /// </summary>
    private static IReadOnlyList<int> DetectCrashBars(IReadOnlyList<DrumMidiEvent> events)
    {
        return events
            .Where(e => CrashRoles.Contains(e.Role))
            .Select(e => e.BarNumber)
            .Distinct()
            .OrderBy(b => b)
            .ToList();
    }

    /// <summary>
    /// Detects points where the pattern changes significantly.
    /// </summary>
    private static IReadOnlyList<PatternChangePoint> DetectPatternChanges(
        IReadOnlyList<BarPatternFingerprint> fingerprints)
    {
        if (fingerprints.Count < 2)
            return Array.Empty<PatternChangePoint>();

        var sorted = fingerprints.OrderBy(f => f.BarNumber).ToList();
        var changes = new List<PatternChangePoint>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];

            // Only check consecutive bars
            if (curr.BarNumber != prev.BarNumber + 1)
                continue;

            var similarity = BarPatternExtractor.CalculateSimilarity(prev, curr);

            if (similarity < PatternChangeThreshold)
            {
                changes.Add(new PatternChangePoint(
                    curr.BarNumber,
                    prev.PatternHash,
                    curr.PatternHash,
                    similarity));
            }
        }

        return changes;
    }

    /// <summary>
    /// Detects potential fill locations based on multiple indicators.
    /// </summary>
    private static IReadOnlyList<PotentialFill> DetectPotentialFills(
        DrumTrackFeatureData featureData,
        IReadOnlyList<DensityAnomaly> highDensityBars,
        IReadOnlyList<int> crashBars,
        IReadOnlyList<PatternChangePoint> patternChanges)
    {
        var fills = new List<PotentialFill>();
        var highDensitySet = highDensityBars.Select(d => d.BarNumber).ToHashSet();
        var crashSet = crashBars.ToHashSet();
        var changeSet = patternChanges.ToDictionary(p => p.BarNumber);

        // Find bars with tom activity
        var tomBars = featureData.Events
            .Where(e => TomRoles.Contains(e.Role))
            .Select(e => e.BarNumber)
            .Distinct()
            .ToHashSet();

        // Analyze each bar for fill indicators
        foreach (var stats in featureData.BarStats)
        {
            var bar = stats.BarNumber;
            var indicators = new List<string>();
            double confidence = 0;

            // High density indicator
            if (highDensitySet.Contains(bar))
            {
                indicators.Add("HighDensity");
                confidence += 0.3;
            }

            // Tom activity indicator
            if (tomBars.Contains(bar))
            {
                indicators.Add("TomActivity");
                confidence += 0.25;
            }

            // Pattern change in this bar
            if (changeSet.ContainsKey(bar))
            {
                indicators.Add("PatternChange");
                confidence += 0.2;
            }

            // Crash on next bar (indicates fill preceding section change)
            if (crashSet.Contains(bar + 1))
            {
                indicators.Add("BeforeCrash");
                confidence += 0.25;
            }

            // Only flag as potential fill if we have multiple indicators
            if (indicators.Count >= 2 && confidence >= 0.5)
            {
                fills.Add(new PotentialFill(
                    bar,
                    bar,
                    Math.Min(confidence, 1.0),
                    indicators));
            }
        }

        return fills.OrderBy(f => f.StartBar).ToList();
    }

    /// <summary>
    /// Creates empty structural marker data.
    /// </summary>
    private static StructuralMarkerData CreateEmpty()
    {
        return new StructuralMarkerData
        {
            HighDensityBars = Array.Empty<DensityAnomaly>(),
            LowDensityBars = Array.Empty<DensityAnomaly>(),
            CrashBars = Array.Empty<int>(),
            PatternChanges = Array.Empty<PatternChangePoint>(),
            PotentialFills = Array.Empty<PotentialFill>()
        };
    }
}
