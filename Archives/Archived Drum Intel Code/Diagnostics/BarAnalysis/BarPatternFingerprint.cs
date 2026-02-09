// AI: purpose=Per-bar pattern fingerprint with role bitmasks and deterministic hash (Story 7.2a).
// AI: invariants=PatternHash is deterministic (same events → same hash); RoleBitmasks indexed by grid position.
// AI: deps=Populated by BarPatternExtractor; used for pattern repetition detection and similarity analysis.
// AI: change=Story 7.2a; keep hash algorithm stable for pattern comparison across analysis runs.

namespace Music.Generator.Drums.Diagnostics.BarAnalysis;

/// <summary>
/// Fingerprint of a single bar's drum pattern for pattern matching and analysis.
/// Contains role presence bitmasks and a deterministic hash for quick comparison.
/// Story 7.2a: Per-Bar Pattern Fingerprint.
/// </summary>
public sealed record BarPatternFingerprint
{
    /// <summary>
    /// Bar number (1-based) this fingerprint represents.
    /// </summary>
    public required int BarNumber { get; init; }

    /// <summary>
    /// Number of beats per bar (based on time signature).
    /// </summary>
    public required int BeatsPerBar { get; init; }

    /// <summary>
    /// Role presence bitmap per beat position (quantized to 16th note grid).
    /// Key: role name, Value: bitmask where bit N = onset at grid position N.
    /// For 4/4 time with 16th note grid: 16 positions (bits 0-15).
    /// </summary>
    public required IReadOnlyDictionary<string, long> RoleBitmasks { get; init; }

    /// <summary>
    /// Velocity profile per role (average velocity at each hit position).
    /// Key: role name, Value: list of velocities at each hit position in order.
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<int>> RoleVelocities { get; init; }

    /// <summary>
    /// Combined hash for quick pattern comparison.
    /// SHA256 truncated to 16 hex chars for storage efficiency.
    /// Same pattern → same hash (deterministic).
    /// </summary>
    public required string PatternHash { get; init; }

    /// <summary>
    /// Event count per role in this bar.
    /// </summary>
    public required IReadOnlyDictionary<string, int> RoleEventCounts { get; init; }

    /// <summary>
    /// Total number of hits in this bar across all roles.
    /// </summary>
    public int TotalHits => RoleEventCounts.Values.Sum();

    /// <summary>
    /// Grid resolution used for bitmask quantization.
    /// Default: 16 (16th notes for 4/4 = 16 positions per bar).
    /// </summary>
    public int GridResolution { get; init; } = 16;
}
