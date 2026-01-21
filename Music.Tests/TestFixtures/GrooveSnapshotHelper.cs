// AI: purpose=Story H1 snapshot helpers for H2 golden test preparation.
// AI: invariants=Deterministic serialization; compact format; essential fields only.
// AI: deps=GrooveBarPlan, GrooveOnset, System.Text.Json.

using System.Text.Json;
using System.Text.Json.Serialization;
using Music.Generator;

namespace Music.Tests.TestFixtures;

/// <summary>
/// Story H1/H2: Helpers for creating and comparing groove snapshots.
/// Used to lock deterministic behavior in golden/regression tests.
/// </summary>
public static class GrooveSnapshotHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Compact snapshot representation of a bar plan.
    /// Contains only the essential fields for determinism verification.
    /// </summary>
    public record BarPlanSnapshot
    {
        public int BarNumber { get; init; }
        public string Role { get; init; } = "";
        public List<OnsetSnapshot> Onsets { get; init; } = new();
    }

    /// <summary>
    /// Compact snapshot of a single onset.
    /// </summary>
    public record OnsetSnapshot
    {
        public decimal Beat { get; init; }
        public int? Velocity { get; init; }
        public int? TimingOffsetTicks { get; init; }
        public string? Strength { get; init; }
        public string? Source { get; init; } // Anchor or Variation
    }

    /// <summary>
    /// Creates a compact snapshot from a GrooveBarPlan.
    /// </summary>
    /// <param name="plan">The bar plan to snapshot.</param>
    /// <param name="role">The role being tracked (for multi-role disambiguation).</param>
    /// <returns>A compact, serializable snapshot.</returns>
    public static BarPlanSnapshot CreateSnapshot(GrooveBarPlan plan, string role)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new BarPlanSnapshot
        {
            BarNumber = plan.BarNumber,
            Role = role,
            Onsets = plan.FinalOnsets
                .Where(o => o.Role == role)
                .OrderBy(o => o.Beat)
                .Select(o => new OnsetSnapshot
                {
                    Beat = o.Beat,
                    Velocity = o.Velocity,
                    TimingOffsetTicks = o.TimingOffsetTicks,
                    Strength = o.Strength?.ToString(),
                    Source = o.Provenance?.Source.ToString()
                })
                .ToList()
        };
    }

    /// <summary>
    /// Serializes a bar plan snapshot to JSON.
    /// </summary>
    public static string SerializeSnapshot(BarPlanSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    /// <summary>
    /// Deserializes a bar plan snapshot from JSON.
    /// </summary>
    public static BarPlanSnapshot? DeserializeSnapshot(string json)
    {
        return JsonSerializer.Deserialize<BarPlanSnapshot>(json, SerializerOptions);
    }

    /// <summary>
    /// Creates a snapshot directly from a GrooveBarPlan and serializes to JSON.
    /// </summary>
    public static string SerializeBarPlan(GrooveBarPlan plan, string role)
    {
        var snapshot = CreateSnapshot(plan, role);
        return SerializeSnapshot(snapshot);
    }

    /// <summary>
    /// Compares two snapshots for equality.
    /// Returns true if all onset properties match exactly.
    /// </summary>
    public static bool SnapshotsEqual(BarPlanSnapshot expected, BarPlanSnapshot actual)
    {
        if (expected.BarNumber != actual.BarNumber) return false;
        if (expected.Role != actual.Role) return false;
        if (expected.Onsets.Count != actual.Onsets.Count) return false;

        for (int i = 0; i < expected.Onsets.Count; i++)
        {
            var e = expected.Onsets[i];
            var a = actual.Onsets[i];

            if (e.Beat != a.Beat) return false;
            if (e.Velocity != a.Velocity) return false;
            if (e.TimingOffsetTicks != a.TimingOffsetTicks) return false;
            if (e.Strength != a.Strength) return false;
            // Source is optional - don't fail on mismatch
        }

        return true;
    }

    /// <summary>
    /// Gets detailed differences between two snapshots.
    /// </summary>
    public static List<string> GetSnapshotDifferences(BarPlanSnapshot expected, BarPlanSnapshot actual)
    {
        var differences = new List<string>();

        if (expected.BarNumber != actual.BarNumber)
            differences.Add($"BarNumber: expected {expected.BarNumber}, actual {actual.BarNumber}");

        if (expected.Role != actual.Role)
            differences.Add($"Role: expected '{expected.Role}', actual '{actual.Role}'");

        if (expected.Onsets.Count != actual.Onsets.Count)
        {
            differences.Add($"Onset count: expected {expected.Onsets.Count}, actual {actual.Onsets.Count}");
            return differences; // Can't compare further
        }

        for (int i = 0; i < expected.Onsets.Count; i++)
        {
            var e = expected.Onsets[i];
            var a = actual.Onsets[i];

            if (e.Beat != a.Beat)
                differences.Add($"Onset[{i}].Beat: expected {e.Beat}, actual {a.Beat}");

            if (e.Velocity != a.Velocity)
                differences.Add($"Onset[{i}].Velocity: expected {e.Velocity}, actual {a.Velocity}");

            if (e.TimingOffsetTicks != a.TimingOffsetTicks)
                differences.Add($"Onset[{i}].TimingOffsetTicks: expected {e.TimingOffsetTicks}, actual {a.TimingOffsetTicks}");

            if (e.Strength != a.Strength)
                differences.Add($"Onset[{i}].Strength: expected '{e.Strength}', actual '{a.Strength}'");
        }

        return differences;
    }

    /// <summary>
    /// Creates a multi-bar snapshot collection for a complete groove.
    /// </summary>
    public static List<BarPlanSnapshot> CreateMultiBarSnapshot(
        IEnumerable<GrooveBarPlan> plans,
        string role)
    {
        return plans
            .Select(p => CreateSnapshot(p, role))
            .OrderBy(s => s.BarNumber)
            .ToList();
    }

    /// <summary>
    /// Serializes a multi-bar snapshot collection to JSON.
    /// </summary>
    public static string SerializeMultiBarSnapshot(List<BarPlanSnapshot> snapshots)
    {
        return JsonSerializer.Serialize(snapshots, SerializerOptions);
    }

    /// <summary>
    /// Deserializes a multi-bar snapshot collection from JSON.
    /// </summary>
    public static List<BarPlanSnapshot>? DeserializeMultiBarSnapshot(string json)
    {
        return JsonSerializer.Deserialize<List<BarPlanSnapshot>>(json, SerializerOptions);
    }
}
