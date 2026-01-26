// AI: purpose=Extracts velocity dynamics including accents and ghost notes (Story 7.2b).
// AI: invariants=Histogram has 8 buckets; accent threshold = mean + 0.5*stdDev; ghost threshold = mean - 0.5*stdDev for snare.
// AI: deps=Uses DrumTrackFeatureData; outputs VelocityDynamicsData.
// AI: change=Story 7.2b; tune thresholds based on analysis feedback.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Extracts velocity dynamics from drum events.
/// Identifies accent patterns, ghost positions, and per-role velocity distributions.
/// Story 7.2b: Velocity Dynamics Data.
/// </summary>
public static class VelocityDynamicsExtractor
{
    /// <summary>
    /// Number of histogram buckets (0-15, 16-31, ..., 112-127).
    /// </summary>
    public const int HistogramBuckets = 8;

    /// <summary>
    /// Bucket size for velocity histogram.
    /// </summary>
    public const int BucketSize = 16;

    /// <summary>
    /// Grid resolution for position analysis.
    /// </summary>
    public const int GridResolution = 16;

    /// <summary>
    /// Accent threshold: mean + this factor * stdDev.
    /// </summary>
    public const double AccentThresholdFactor = 0.5;

    /// <summary>
    /// Ghost threshold: mean - this factor * stdDev.
    /// </summary>
    public const double GhostThresholdFactor = 0.5;

    /// <summary>
    /// Extracts velocity dynamics from feature data.
    /// </summary>
    /// <param name="featureData">Base feature data from Story 7.2a.</param>
    /// <returns>Velocity dynamics data.</returns>
    public static VelocityDynamicsData Extract(DrumTrackFeatureData featureData)
    {
        ArgumentNullException.ThrowIfNull(featureData);

        if (featureData.Events.Count == 0)
        {
            return CreateEmpty();
        }

        // Group events by role
        var eventsByRole = featureData.Events
            .GroupBy(e => e.Role)
            .ToDictionary(g => g.Key, g => g.ToList());

        var roleDistributions = new Dictionary<string, VelocityDistribution>();
        var roleVelocityByPosition = new Dictionary<string, IReadOnlyList<double>>();
        var accentMasks = new Dictionary<string, long>();

        foreach (var (role, events) in eventsByRole.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            var distribution = ComputeDistribution(events);
            roleDistributions[role] = distribution;

            var (positionVelocities, accentMask) = ComputePositionData(events, distribution, featureData.DefaultBeatsPerBar);
            roleVelocityByPosition[role] = positionVelocities;
            accentMasks[role] = accentMask;
        }

        // Detect ghost positions (snare only)
        var ghostPositions = DetectGhostPositions(eventsByRole, roleDistributions, featureData.DefaultBeatsPerBar);

        return new VelocityDynamicsData
        {
            RoleDistributions = roleDistributions,
            RoleVelocityByPosition = roleVelocityByPosition,
            AccentMasks = accentMasks,
            GhostPositions = ghostPositions
        };
    }

    /// <summary>
    /// Computes velocity distribution for a set of events.
    /// </summary>
    private static VelocityDistribution ComputeDistribution(List<DrumMidiEvent> events)
    {
        if (events.Count == 0)
        {
            return new VelocityDistribution(0, 0, 0, 0, new int[HistogramBuckets]);
        }

        var velocities = events.Select(e => e.Velocity).ToList();
        var mean = velocities.Average();
        var min = velocities.Min();
        var max = velocities.Max();

        var variance = velocities.Count > 1
            ? velocities.Sum(v => (v - mean) * (v - mean)) / (velocities.Count - 1)
            : 0;
        var stdDev = Math.Sqrt(variance);

        // Build histogram
        var histogram = new int[HistogramBuckets];
        foreach (var v in velocities)
        {
            var bucket = Math.Clamp(v / BucketSize, 0, HistogramBuckets - 1);
            histogram[bucket]++;
        }

        return new VelocityDistribution(mean, stdDev, min, max, histogram);
    }

    /// <summary>
    /// Computes per-position velocity data and accent mask.
    /// </summary>
    private static (IReadOnlyList<double> positionVelocities, long accentMask) ComputePositionData(
        List<DrumMidiEvent> events,
        VelocityDistribution distribution,
        int beatsPerBar)
    {
        // Group velocities by grid position
        var positionVelocities = new List<double>();
        var velocitySums = new double[GridResolution];
        var velocityCounts = new int[GridResolution];

        foreach (var evt in events)
        {
            var pos = BarPatternExtractor.CalculateGridPosition(evt.Beat, beatsPerBar, GridResolution);
            if (pos >= 0 && pos < GridResolution)
            {
                velocitySums[pos] += evt.Velocity;
                velocityCounts[pos]++;
            }
        }

        long accentMask = 0;
        var accentThreshold = distribution.Mean + AccentThresholdFactor * distribution.StdDev;

        for (int i = 0; i < GridResolution; i++)
        {
            var avgVelocity = velocityCounts[i] > 0 ? velocitySums[i] / velocityCounts[i] : 0;
            positionVelocities.Add(avgVelocity);

            if (avgVelocity > accentThreshold)
            {
                accentMask |= 1L << i;
            }
        }

        return (positionVelocities, accentMask);
    }

    /// <summary>
    /// Detects ghost note positions (snare only, low velocity).
    /// </summary>
    private static IReadOnlyList<int> DetectGhostPositions(
        Dictionary<string, List<DrumMidiEvent>> eventsByRole,
        Dictionary<string, VelocityDistribution> distributions,
        int beatsPerBar)
    {
        var ghostPositions = new HashSet<int>();

        // Look for snare ghost notes
        if (eventsByRole.TryGetValue("Snare", out var snareEvents) &&
            distributions.TryGetValue("Snare", out var snareDist))
        {
            var ghostThreshold = snareDist.Mean - GhostThresholdFactor * snareDist.StdDev;

            foreach (var evt in snareEvents)
            {
                if (evt.Velocity < ghostThreshold)
                {
                    var pos = BarPatternExtractor.CalculateGridPosition(evt.Beat, beatsPerBar, GridResolution);
                    if (pos >= 0 && pos < GridResolution)
                    {
                        ghostPositions.Add(pos);
                    }
                }
            }
        }

        return ghostPositions.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Creates empty velocity dynamics data.
    /// </summary>
    private static VelocityDynamicsData CreateEmpty()
    {
        return new VelocityDynamicsData
        {
            RoleDistributions = new Dictionary<string, VelocityDistribution>(),
            RoleVelocityByPosition = new Dictionary<string, IReadOnlyList<double>>(),
            AccentMasks = new Dictionary<string, long>(),
            GhostPositions = Array.Empty<int>()
        };
    }
}
