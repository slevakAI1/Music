// AI: purpose=Queryable service mapping motif placements to per-bar presence/density for accompaniment ducking.
// AI: invariants=Immutable after construction; same plan+sectionTrack → same query results; density always [0..1].
// AI: deps=Consumes MotifPlacementPlan and SectionTrack; queried by DrummerPolicyProvider and operators (Story 9.3).
// AI: change=Story 9.3; extend density computation if MotifSpec gains density attribute.

using Music.Song.Material;

namespace Music.Generator.Material;

/// <summary>
/// Queryable service for determining motif presence per bar.
/// Used by accompaniment agents (drums, keys, bass, guitar) to make room for motifs.
/// Story 9.3: Motif integration with accompaniment (call/response + ducking infrastructure).
/// </summary>
/// <remarks>
/// Immutable after construction; all query methods are pure and deterministic.
/// Thread-safe for concurrent reads (immutable data).
/// </remarks>
public sealed class MotifPresenceMap
{
    private readonly MotifPlacementPlan _plan;
    private readonly SectionTrack? _sectionTrack;
    
    // Pre-computed lookup: absoluteBarNumber (1-based) → list of active placements
    private readonly Dictionary<int, List<MotifPlacement>> _barToMotifs;
    
    // Legacy lookup for section-relative queries
    private readonly Dictionary<(int section, int bar, string role), MotifPresence> _presenceByPosition;

    /// <summary>
    /// Creates a MotifPresenceMap from a placement plan and section track.
    /// </summary>
    /// <param name="plan">The motif placement plan (from MotifPlacementPlanner).</param>
    /// <param name="sectionTrack">Song section structure (for bar → section mapping).</param>
    public MotifPresenceMap(MotifPlacementPlan plan, SectionTrack sectionTrack)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(sectionTrack);

        _plan = plan;
        _sectionTrack = sectionTrack;
        _barToMotifs = BuildBarToMotifsLookup();
        _presenceByPosition = BuildLegacyPresenceMap();
    }

    /// <summary>
    /// Legacy constructor for backward compatibility.
    /// Uses section-relative queries only (no absolute bar number support).
    /// </summary>
    public MotifPresenceMap(MotifPlacementPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        _plan = plan;
        _sectionTrack = null;
        _barToMotifs = new Dictionary<int, List<MotifPlacement>>();
        _presenceByPosition = BuildLegacyPresenceMap();
    }

    /// <summary>
    /// Creates an empty MotifPresenceMap (no motifs anywhere).
    /// </summary>
    public static MotifPresenceMap Empty => new(MotifPlacementPlan.Empty(), new SectionTrack());

    #region Absolute Bar Number Queries (Story 9.3)

    /// <summary>
    /// Checks if any motif is active in the specified bar.
    /// </summary>
    /// <param name="barNumber">1-based absolute bar number.</param>
    /// <param name="role">Optional role filter (e.g., "Lead", "Guitar"). Null = any role.</param>
    /// <returns>True if at least one motif is active in this bar (matching the role filter).</returns>
    public bool IsMotifActive(int barNumber, string? role = null)
    {
        if (barNumber < 1)
            return false;

        if (!_barToMotifs.TryGetValue(barNumber, out var motifs) || motifs.Count == 0)
            return false;

        if (role is null)
            return true;

        return motifs.Any(m => 
            string.Equals(m.MotifSpec.IntendedRole, role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the estimated motif density for a bar.
    /// </summary>
    /// <param name="barNumber">1-based absolute bar number.</param>
    /// <param name="role">Optional role filter. Null = overall density.</param>
    /// <returns>
    /// Density value [0.0-1.0]. 0.0 = no motifs; 0.5 = one motif; 1.0 = two or more motifs.
    /// </returns>
    public double GetMotifDensity(int barNumber, string? role = null)
    {
        if (barNumber < 1)
            return 0.0;

        if (!_barToMotifs.TryGetValue(barNumber, out var motifs) || motifs.Count == 0)
            return 0.0;

        int count;
        if (role is null)
        {
            count = motifs.Count;
        }
        else
        {
            count = motifs.Count(m => 
                string.Equals(m.MotifSpec.IntendedRole, role, StringComparison.OrdinalIgnoreCase));
        }

        // Simple heuristic: 0.5 per motif, capped at 1.0
        return Math.Min(count * 0.5, 1.0);
    }

    /// <summary>
    /// Gets all motif placements active in a bar.
    /// </summary>
    /// <param name="barNumber">1-based absolute bar number.</param>
    /// <returns>List of active motif placements (empty if none).</returns>
    public IReadOnlyList<MotifPlacement> GetActiveMotifs(int barNumber)
    {
        if (barNumber < 1)
            return Array.Empty<MotifPlacement>();

        if (!_barToMotifs.TryGetValue(barNumber, out var motifs))
            return Array.Empty<MotifPlacement>();

        return motifs;
    }

    /// <summary>
    /// Gets all motif placements active in a bar filtered by role.
    /// </summary>
    /// <param name="barNumber">1-based absolute bar number.</param>
    /// <param name="role">Role filter (e.g., "Lead", "Guitar").</param>
    /// <returns>List of active motif placements for the specified role.</returns>
    public IReadOnlyList<MotifPlacement> GetActiveMotifsForRole(int barNumber, string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (barNumber < 1)
            return Array.Empty<MotifPlacement>();

        if (!_barToMotifs.TryGetValue(barNumber, out var motifs))
            return Array.Empty<MotifPlacement>();

        return motifs
            .Where(m => string.Equals(m.MotifSpec.IntendedRole, role, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    #endregion

    #region Section-Relative Queries (Legacy)

    /// <summary>
    /// Checks if a motif is active for the given role at the specified position.
    /// </summary>
    public MotifPresence GetPresence(int sectionIndex, int barWithinSection, string role)
    {
        var key = (sectionIndex, barWithinSection, role);
        if (_presenceByPosition.TryGetValue(key, out var presence))
        {
            return presence;
        }

        return MotifPresence.Absent;
    }

    /// <summary>
    /// Checks if any lead/melodic motif is active (for ducking accompaniment).
    /// </summary>
    public bool HasLeadMotif(int sectionIndex, int barWithinSection)
    {
        // Check for common lead roles
        var leadRoles = new[] { "Lead", "GuitarHook", "SynthHook", "Vocal" };
        foreach (var role in leadRoles)
        {
            var presence = GetPresence(sectionIndex, barWithinSection, role);
            if (presence.IsActive)
                return true;
        }
        return false;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Builds the pre-computed bar → motifs lookup using absolute bar numbers.
    /// </summary>
    private Dictionary<int, List<MotifPlacement>> BuildBarToMotifsLookup()
    {
        var lookup = new Dictionary<int, List<MotifPlacement>>();

        if (_sectionTrack is null)
            return lookup;

        foreach (var placement in _plan.Placements)
        {
            // Convert section-relative bars to absolute bar numbers
            int sectionStartBar = GetSectionStartBar(placement.AbsoluteSectionIndex);
            if (sectionStartBar < 1)
                continue;

            // Compute absolute bar range for this placement
            int startBar = sectionStartBar + placement.StartBarWithinSection;
            int endBar = startBar + placement.DurationBars - 1;

            // Add to lookup for each covered bar
            for (int bar = startBar; bar <= endBar; bar++)
            {
                if (!lookup.TryGetValue(bar, out var list))
                {
                    list = new List<MotifPlacement>();
                    lookup[bar] = list;
                }
                list.Add(placement);
            }
        }

        return lookup;
    }

    /// <summary>
    /// Builds the legacy section-relative presence map.
    /// </summary>
    private Dictionary<(int section, int bar, string role), MotifPresence> BuildLegacyPresenceMap()
    {
        var presenceMap = new Dictionary<(int section, int bar, string role), MotifPresence>();

        foreach (var placement in _plan.Placements)
        {
            for (int barOffset = 0; barOffset < placement.DurationBars; barOffset++)
            {
                int bar = placement.StartBarWithinSection + barOffset;
                var key = (placement.AbsoluteSectionIndex, bar, placement.MotifSpec.IntendedRole);

                // Estimate density from motif rhythm shape
                double density = EstimateDensity(placement.MotifSpec);

                presenceMap[key] = new MotifPresence
                {
                    IsActive = true,
                    DensityEstimate = density,
                    MotifName = placement.MotifSpec.Name
                };
            }
        }

        return presenceMap;
    }

    /// <summary>
    /// Gets the absolute start bar (1-based) for a section by index.
    /// </summary>
    private int GetSectionStartBar(int absoluteSectionIndex)
    {
        if (_sectionTrack is null)
            return -1;

        if (absoluteSectionIndex < 0 || absoluteSectionIndex >= _sectionTrack.Sections.Count)
            return -1;

        return _sectionTrack.Sections[absoluteSectionIndex].StartBar;
    }

    /// <summary>
    /// Estimates motif density from rhythm shape (for ducking magnitude).
    /// </summary>
    private static double EstimateDensity(MotifSpec spec)
    {
        // Simple heuristic: more onsets = higher density
        int onsetCount = spec.RhythmShape.Count;
        double density = Math.Clamp(onsetCount / 8.0, 0.1, 1.0);
        return density;
    }

    #endregion
}

/// <summary>
/// Presence information for a motif at a specific position.
/// </summary>
public record MotifPresence
{
    public bool IsActive { get; init; }
    public double DensityEstimate { get; init; }
    public string MotifName { get; init; } = string.Empty;

    public static readonly MotifPresence Absent = new()
    {
        IsActive = false,
        DensityEstimate = 0.0,
        MotifName = string.Empty
    };
}
