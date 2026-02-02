// AI: purpose=Extracts timing feel including swing detection and pocket analysis (Story 7.2b).
// AI: invariants=SwingRatio computed from 8th note pair timings; consistency inverted from stdDev.
// AI: deps=Uses DrumTrackFeatureData; outputs TimingFeelData.
// AI: change=Story 7.2b; improve swing detection algorithm as needed.

using Music.Generator.Groove;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Extracts timing feel data from drum events.
/// Detects swing ratio, pocket feel, and per-role timing characteristics.
/// Story 7.2b: Timing Feel Data.
/// </summary>
public static class TimingFeelExtractor
{
    /// <summary>
    /// Number of histogram buckets for timing distribution.
    /// Covers -20 to +20 ticks in 5-tick increments (9 buckets).
    /// </summary>
    public const int HistogramBuckets = 9;

    /// <summary>
    /// Width of each histogram bucket in ticks.
    /// </summary>
    public const int BucketWidth = 5;

    /// <summary>
    /// Center offset for histogram (bucket 4 = 0 offset).
    /// </summary>
    public const int HistogramCenter = 4;

    /// <summary>
    /// Maximum standard deviation for "high consistency" (used for scaling).
    /// </summary>
    public const double MaxStdDevForConsistency = 15.0;

    /// <summary>
    /// Extracts timing feel from feature data.
    /// </summary>
    /// <param name="featureData">Base feature data from Story 7.2a.</param>
    /// <returns>Timing feel data.</returns>
    public static TimingFeelData Extract(DrumTrackFeatureData featureData)
    {
        ArgumentNullException.ThrowIfNull(featureData);

        if (featureData.Events.Count == 0)
        {
            return CreateEmpty();
        }

        // Filter to events with timing offset data
        var eventsWithTiming = featureData.Events
            .Where(e => e.TimingOffsetTicks.HasValue)
            .ToList();

        if (eventsWithTiming.Count == 0)
        {
            return CreateEmpty();
        }

        // Group events by role
        var eventsByRole = eventsWithTiming
            .GroupBy(e => e.Role)
            .ToDictionary(g => g.Key, g => g.ToList());

        var roleAverageOffset = new Dictionary<string, double>();
        var roleDistributions = new Dictionary<string, TimingDistribution>();

        foreach (var (role, events) in eventsByRole.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            var offsets = events.Select(e => e.TimingOffsetTicks!.Value).ToList();
            var distribution = ComputeDistribution(offsets);

            roleAverageOffset[role] = distribution.Mean;
            roleDistributions[role] = distribution;
        }

        // Compute overall timing stats
        var allOffsets = eventsWithTiming.Select(e => e.TimingOffsetTicks!.Value).ToList();
        var aheadBehindScore = allOffsets.Average();
        var overallStdDev = ComputeStdDev(allOffsets);
        var timingConsistency = Math.Max(0, 1.0 - (overallStdDev / MaxStdDevForConsistency));

        // Detect swing ratio
        var swingRatio = DetectSwingRatio(featureData);

        return new TimingFeelData
        {
            RoleAverageOffset = roleAverageOffset,
            RoleTimingDistributions = roleDistributions,
            SwingRatio = swingRatio,
            AheadBehindScore = aheadBehindScore,
            TimingConsistency = timingConsistency
        };
    }

    /// <summary>
    /// Computes timing distribution from offsets.
    /// </summary>
    private static TimingDistribution ComputeDistribution(List<int> offsets)
    {
        if (offsets.Count == 0)
        {
            return new TimingDistribution(0, 0, 0, 0, new int[HistogramBuckets]);
        }

        var mean = offsets.Average();
        var min = offsets.Min();
        var max = offsets.Max();
        var stdDev = ComputeStdDev(offsets);

        // Build histogram: -20 to +20 in 5-tick buckets
        var histogram = new int[HistogramBuckets];
        foreach (var offset in offsets)
        {
            var bucket = (offset / BucketWidth) + HistogramCenter;
            bucket = Math.Clamp(bucket, 0, HistogramBuckets - 1);
            histogram[bucket]++;
        }

        return new TimingDistribution(mean, stdDev, min, max, histogram);
    }

    /// <summary>
    /// Computes standard deviation.
    /// </summary>
    private static double ComputeStdDev(List<int> values)
    {
        if (values.Count <= 1) return 0;

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Detects swing ratio from 8th note pair timings.
    /// Swing = long-short pattern where first 8th is longer than second.
    /// </summary>
    private static double DetectSwingRatio(DrumTrackFeatureData featureData)
    {
        // Look for hi-hat or ride patterns on 8th notes
        var hatEvents = featureData.Events
            .Where(e => e.Role == "ClosedHat" || e.Role == "OpenHat" || e.Role == "Ride")
            .OrderBy(e => e.AbsoluteTimeTicks)
            .ToList();

        if (hatEvents.Count < 4)
            return 1.0; // Not enough data, assume straight

        // Calculate expected 8th note spacing
        var ticksPerBeat = MusicConstants.TicksPerQuarterNote;
        var ticksPerEighth = ticksPerBeat / 2;

        var longShortRatios = new List<double>();

        // Look for consecutive pairs
        for (int i = 0; i < hatEvents.Count - 1; i++)
        {
            var current = hatEvents[i];
            var next = hatEvents[i + 1];

            var gap = next.AbsoluteTimeTicks - current.AbsoluteTimeTicks;

            // Check if this looks like an 8th note pair (gap is roughly 1 8th)
            if (gap > ticksPerEighth * 0.6 && gap < ticksPerEighth * 1.6)
            {
                // Look for the next gap to form a pair
                if (i + 2 < hatEvents.Count)
                {
                    var nextNext = hatEvents[i + 2];
                    var secondGap = nextNext.AbsoluteTimeTicks - next.AbsoluteTimeTicks;

                    // Both gaps should sum to roughly a quarter note
                    var totalGap = gap + secondGap;
                    if (totalGap > ticksPerBeat * 0.8 && totalGap < ticksPerBeat * 1.2)
                    {
                        // This is a valid pair
                        if (secondGap > 0)
                        {
                            var ratio = (double)gap / secondGap;
                            if (ratio > 0.5 && ratio < 3.0)
                            {
                                longShortRatios.Add(ratio);
                            }
                        }
                    }
                }
            }
        }

        if (longShortRatios.Count == 0)
            return 1.0;

        // Average the ratios
        return longShortRatios.Average();
    }

    /// <summary>
    /// Creates empty timing feel data.
    /// </summary>
    private static TimingFeelData CreateEmpty()
    {
        return new TimingFeelData
        {
            RoleAverageOffset = new Dictionary<string, double>(),
            RoleTimingDistributions = new Dictionary<string, TimingDistribution>(),
            SwingRatio = 1.0,
            AheadBehindScore = 0,
            TimingConsistency = 1.0
        };
    }
}
