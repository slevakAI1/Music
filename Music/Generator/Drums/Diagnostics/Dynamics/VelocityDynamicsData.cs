// AI: purpose=Velocity dynamics data for analyzing accent and ghost patterns (Story 7.2b).
// AI: invariants=VelocityDistribution values in [0,127]; histogram has 8 buckets; AccentMasks indexed by grid position.
// AI: deps=Populated by VelocityDynamicsExtractor; uses DrumMidiEvent velocity data.
// AI: change=Story 7.2b; add additional velocity metrics as needed.

namespace Music.Generator.Drums.Diagnostics.Dynamics;

/// <summary>
/// Distribution of velocities for statistical analysis.
/// Story 7.2b: Velocity Dynamics Data.
/// </summary>
/// <param name="Mean">Mean velocity value.</param>
/// <param name="StdDev">Standard deviation.</param>
/// <param name="Min">Minimum velocity (0 if no events).</param>
/// <param name="Max">Maximum velocity (0 if no events).</param>
/// <param name="Histogram">8 buckets: 0-15, 16-31, ..., 112-127.</param>
public sealed record VelocityDistribution(
    double Mean,
    double StdDev,
    int Min,
    int Max,
    IReadOnlyList<int> Histogram);

/// <summary>
/// Data about velocity dynamics in a drum track.
/// Captures accent patterns, ghost positions, and per-role dynamics.
/// Story 7.2b: Velocity Dynamics Data.
/// </summary>
public sealed record VelocityDynamicsData
{
    /// <summary>
    /// Velocity distribution per role.
    /// Key: role name, Value: distribution statistics.
    /// </summary>
    public required IReadOnlyDictionary<string, VelocityDistribution> RoleDistributions { get; init; }

    /// <summary>
    /// Average velocity by beat position (0-based grid) per role.
    /// Key: role name, Value: list of average velocities at each grid position.
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<double>> RoleVelocityByPosition { get; init; }

    /// <summary>
    /// Accent position masks per role.
    /// Bit N = 1 means grid position N has above-average velocity.
    /// </summary>
    public required IReadOnlyDictionary<string, long> AccentMasks { get; init; }

    /// <summary>
    /// Grid positions with ghost note activity (snare only, low velocity).
    /// </summary>
    public required IReadOnlyList<int> GhostPositions { get; init; }

    /// <summary>
    /// Overall velocity range across all roles.
    /// </summary>
    public int OverallVelocityRange
    {
        get
        {
            if (RoleDistributions.Count == 0) return 0;
            var min = RoleDistributions.Values.Where(d => d.Min > 0).Select(d => d.Min).DefaultIfEmpty(0).Min();
            var max = RoleDistributions.Values.Select(d => d.Max).DefaultIfEmpty(0).Max();
            return max - min;
        }
    }

    /// <summary>
    /// Total ghost positions detected.
    /// </summary>
    public int GhostCount => GhostPositions.Count;
}
