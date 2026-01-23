// AI: purpose=Immutable container for complete motif placement decisions across song structure.
// AI: invariants=Placements list is read-only; all placements reference valid motif IDs and section indices.
// AI: deps=Story 9.1 output; consumed by Story 9.2 renderer and Story 9.3 accompaniment ducking.

namespace Music.Song.Material;

/// <summary>
/// Complete motif placement plan for a song.
/// Deterministic output from MotifPlacementPlanner (Story 9.1).
/// Specifies WHICH motifs appear WHERE across the entire song structure.
/// </summary>
public sealed record MotifPlacementPlan
{
    /// <summary>
    /// All motif placements in the song, ordered by (AbsoluteSectionIndex, StartBarWithinSection).
    /// </summary>
    public required IReadOnlyList<MotifPlacement> Placements { get; init; }

    /// <summary>
    /// Seed used to generate this plan (for reproducibility).
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Creates an empty placement plan (no motifs placed).
    /// </summary>
    public static MotifPlacementPlan Empty(int seed = 0)
    {
        return new MotifPlacementPlan
        {
            Placements = Array.Empty<MotifPlacement>(),
            Seed = seed
        };
    }

    /// <summary>
    /// Creates a placement plan with validation and ordering.
    /// </summary>
    public static MotifPlacementPlan Create(IEnumerable<MotifPlacement> placements, int seed)
    {
        ArgumentNullException.ThrowIfNull(placements);

        var ordered = placements
            .OrderBy(p => p.AbsoluteSectionIndex)
            .ThenBy(p => p.StartBarWithinSection)
            .ToList();

        return new MotifPlacementPlan
        {
            Placements = ordered,
            Seed = seed
        };
    }

    /// <summary>
    /// Gets all placements for a specific section.
    /// </summary>
    public IReadOnlyList<MotifPlacement> GetPlacementsForSection(int absoluteSectionIndex)
    {
        return Placements
            .Where(p => p.AbsoluteSectionIndex == absoluteSectionIndex)
            .ToList();
    }

    /// <summary>
    /// Gets placement for a specific role and bar (absolute section index + bar within section).
    /// Returns null if no motif placed for this role at this position.
    /// </summary>
    public MotifPlacement? GetPlacementForRoleAndBar(string intendedRole, int absoluteSectionIndex, int barWithinSection)
    {
        return Placements.FirstOrDefault(p =>
            string.Equals(p.MotifSpec.IntendedRole, intendedRole, StringComparison.OrdinalIgnoreCase) &&
            p.AbsoluteSectionIndex == absoluteSectionIndex &&
            barWithinSection >= p.StartBarWithinSection &&
            barWithinSection < p.StartBarWithinSection + p.DurationBars);
    }

    /// <summary>
    /// Gets all placements active at a specific bar within a section.
    /// </summary>
    public IReadOnlyList<MotifPlacement> GetPlacementsForBar(int absoluteSectionIndex, int barWithinSection)
    {
        return Placements
            .Where(p =>
                p.AbsoluteSectionIndex == absoluteSectionIndex &&
                barWithinSection >= p.StartBarWithinSection &&
                barWithinSection < p.StartBarWithinSection + p.DurationBars)
            .ToList();
    }

    /// <summary>
    /// Checks if any motif is placed in a specific section.
    /// </summary>
    public bool HasMotifInSection(int absoluteSectionIndex)
    {
        return Placements.Any(p => p.AbsoluteSectionIndex == absoluteSectionIndex);
    }

    /// <summary>
    /// Gets total number of motif placements.
    /// </summary>
    public int Count => Placements.Count;
}
