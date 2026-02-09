// AI: purpose=Extracts BarPatternFingerprint from drum events in a single bar (Story 7.2a).
// AI: invariants=Deterministic hash algorithm (sorted role:bitmask pairs → SHA256 → 16 hex chars); supports variable time signatures.
// AI: deps=Consumes DrumMidiEvent list; outputs BarPatternFingerprint; uses MusicConstants for tick calculations.
// AI: change=Story 7.2a; keep hash algorithm stable; extend grid support as needed.

using Music.Generator.Drums.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Music.Generator.Drums.Diagnostics.BarAnalysis;

/// <summary>
/// Extracts pattern fingerprints from drum events for a single bar.
/// Quantizes events to grid, builds role bitmasks, and generates deterministic hash.
/// Story 7.2a: Per-Bar Pattern Fingerprint.
/// </summary>
public static class BarPatternExtractor
{
    /// <summary>
    /// Default grid resolution (16th notes).
    /// </summary>
    public const int DefaultGridResolution = 16;

    /// <summary>
    /// Extracts a fingerprint from events within a single bar.
    /// </summary>
    /// <param name="events">Drum events for this bar (should all have same BarNumber).</param>
    /// <param name="barNumber">Bar number (1-based) for the fingerprint.</param>
    /// <param name="beatsPerBar">Number of beats in this bar (from time signature).</param>
    /// <param name="gridResolution">Grid positions per bar (default: 16 for 16th notes in 4/4).</param>
    /// <returns>Fingerprint capturing pattern structure.</returns>
    public static BarPatternFingerprint Extract(
        IReadOnlyList<DrumMidiEvent> events,
        int barNumber,
        int beatsPerBar,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Filter to only events for this bar (in case caller passes mixed list)
        var barEvents = events.Where(e => e.BarNumber == barNumber).ToList();

        // Build role bitmasks
        var roleBitmasks = new Dictionary<string, long>();
        var roleVelocities = new Dictionary<string, List<int>>();
        var roleEventCounts = new Dictionary<string, int>();

        foreach (var evt in barEvents)
        {
            // Calculate grid position (0-based) from beat position (1-based)
            var gridPosition = CalculateGridPosition(evt.Beat, beatsPerBar, gridResolution);

            // Clamp to valid range
            if (gridPosition < 0 || gridPosition >= 64)
                continue; // Skip events outside representable range (long has 64 bits)

            // Update role bitmask
            if (!roleBitmasks.ContainsKey(evt.Role))
            {
                roleBitmasks[evt.Role] = 0;
                roleVelocities[evt.Role] = new List<int>();
                roleEventCounts[evt.Role] = 0;
            }

            roleBitmasks[evt.Role] |= 1L << gridPosition;
            roleVelocities[evt.Role].Add(evt.Velocity);
            roleEventCounts[evt.Role]++;
        }

        // Generate deterministic hash
        var patternHash = GeneratePatternHash(roleBitmasks);

        return new BarPatternFingerprint
        {
            BarNumber = barNumber,
            BeatsPerBar = beatsPerBar,
            RoleBitmasks = roleBitmasks.ToDictionary(k => k.Key, v => v.Value),
            RoleVelocities = roleVelocities.ToDictionary(
                k => k.Key,
                v => (IReadOnlyList<int>)v.Value),
            PatternHash = patternHash,
            RoleEventCounts = roleEventCounts.ToDictionary(k => k.Key, v => v.Value),
            GridResolution = gridResolution
        };
    }

    /// <summary>
    /// Calculates grid position (0-based) from beat position (1-based).
    /// </summary>
    /// <param name="beat">Beat position (1-based, fractional).</param>
    /// <param name="beatsPerBar">Number of beats in the bar.</param>
    /// <param name="gridResolution">Total grid positions in the bar.</param>
    /// <returns>Grid position index (0-based).</returns>
    public static int CalculateGridPosition(decimal beat, int beatsPerBar, int gridResolution)
    {
        // Convert 1-based beat to 0-based
        var beatZeroBased = beat - 1m;

        // Calculate fraction of bar
        var barFraction = beatZeroBased / beatsPerBar;

        // Map to grid position
        var gridPosition = (int)Math.Round(barFraction * gridResolution);

        // Clamp to valid range
        return Math.Clamp(gridPosition, 0, gridResolution - 1);
    }

    /// <summary>
    /// Generates deterministic hash from role bitmasks.
    /// Algorithm: sorted concatenation of role:bitmask pairs → SHA256 → first 16 hex chars.
    /// </summary>
    private static string GeneratePatternHash(Dictionary<string, long> roleBitmasks)
    {
        // Empty pattern has a stable empty hash
        if (roleBitmasks.Count == 0)
            return "0000000000000000";

        // Build deterministic string: sorted by role name
        var sb = new StringBuilder();
        foreach (var kvp in roleBitmasks.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append(kvp.Key);
            sb.Append(':');
            sb.Append(kvp.Value.ToString("X16")); // Hex representation
            sb.Append(';');
        }

        // SHA256 hash
        var inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hashBytes = SHA256.HashData(inputBytes);

        // Take first 8 bytes (16 hex chars) for storage efficiency
        return Convert.ToHexString(hashBytes, 0, 8);
    }

    /// <summary>
    /// Calculates Jaccard similarity between two fingerprints.
    /// Compares role bitmask overlap across all roles.
    /// </summary>
    /// <param name="a">First fingerprint.</param>
    /// <param name="b">Second fingerprint.</param>
    /// <returns>Similarity score 0.0-1.0 (1.0 = identical patterns).</returns>
    public static double CalculateSimilarity(BarPatternFingerprint a, BarPatternFingerprint b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Get all roles present in either pattern
        var allRoles = a.RoleBitmasks.Keys.Union(b.RoleBitmasks.Keys).ToList();

        if (allRoles.Count == 0)
            return 1.0; // Both empty = identical

        // Calculate Jaccard similarity per role, then average
        var totalIntersection = 0;
        var totalUnion = 0;

        foreach (var role in allRoles)
        {
            var maskA = a.RoleBitmasks.TryGetValue(role, out var va) ? va : 0L;
            var maskB = b.RoleBitmasks.TryGetValue(role, out var vb) ? vb : 0L;

            var intersection = maskA & maskB;
            var union = maskA | maskB;

            totalIntersection += CountBits(intersection);
            totalUnion += CountBits(union);
        }

        return totalUnion == 0 ? 1.0 : (double)totalIntersection / totalUnion;
    }

    /// <summary>
    /// Counts the number of set bits in a long (population count).
    /// </summary>
    private static int CountBits(long value)
    {
        return System.Numerics.BitOperations.PopCount((ulong)value);
    }

    /// <summary>
    /// Extracts fingerprints for all bars in the event list.
    /// </summary>
    /// <param name="events">All drum events.</param>
    /// <param name="barTrack">BarTrack for time signature info.</param>
    /// <param name="gridResolution">Grid resolution for quantization.</param>
    /// <returns>List of fingerprints, one per bar with events.</returns>
    public static IReadOnlyList<BarPatternFingerprint> ExtractAllBars(
        IReadOnlyList<DrumMidiEvent> events,
        BarTrack barTrack,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(barTrack);

        var fingerprints = new List<BarPatternFingerprint>();
        var eventsByBar = events.GroupBy(e => e.BarNumber).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var bar in barTrack.Bars)
        {
            var barEvents = eventsByBar.TryGetValue(bar.BarNumber, out var evts)
                ? evts
                : new List<DrumMidiEvent>();

            var fingerprint = Extract(
                barEvents,
                bar.BarNumber,
                bar.BeatsPerBar,
                gridResolution);

            fingerprints.Add(fingerprint);
        }

        return fingerprints;
    }
}
