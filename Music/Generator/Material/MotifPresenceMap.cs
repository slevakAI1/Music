// AI: purpose=Map motif placements to per-bar presence/density for accompaniment ducking and queries
// AI: invariants=Immutable after construction; same plan+sectionTrack => same results; density in [0,1]
// AI: deps=Consumes MotifPlacementPlan and SectionTrack; used by operators and policy providers for ducking
using Music.Song.Material;

namespace Music.Generator.Material;

// AI: contract=Immutable query service; thread-safe for concurrent reads; returns empty results for out-of-range bars
public sealed class MotifPresenceMap
{
    private readonly MotifPlacementPlan _plan;
    private readonly SectionTrack? _sectionTrack;
    
    // Pre-computed lookup: absoluteBarNumber (1-based) → list of active placements
    private readonly Dictionary<int, List<MotifPlacement>> _barToMotifs;
    
    // Legacy lookup for section-relative queries
    private readonly Dictionary<(int section, int bar, string role), MotifPresence> _presenceByPosition;

    // AI: ctor=Build map from MotifPlacementPlan and SectionTrack; throws on null inputs
    public MotifPresenceMap(MotifPlacementPlan plan, SectionTrack sectionTrack)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(sectionTrack);

        _plan = plan;
        _sectionTrack = sectionTrack;
        _barToMotifs = BuildBarToMotifsLookup();
        _presenceByPosition = BuildLegacyPresenceMap();
    }

    // AI: ctor_legacy=Legacy ctor supports section-relative queries only; absolute queries return empty
    public MotifPresenceMap(MotifPlacementPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        _plan = plan;
        _sectionTrack = null;
        _barToMotifs = new Dictionary<int, List<MotifPlacement>>();
        _presenceByPosition = BuildLegacyPresenceMap();
    }

    // AI: sentinel=Empty map with no motifs; useful for tests and default fallbacks
    public static MotifPresenceMap Empty => new(MotifPlacementPlan.Empty(), new SectionTrack());

    #region Absolute Bar Number Queries (Story 9.3)

    // AI: query=Return true when at least one motif active at absolute 1-based barNumber; optional role filter
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

    // AI: query=Estimate density in [0,1]; naive heuristic 0.5 per motif capped at 1.0; role filter optional
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

    // AI: query=Return list of MotifPlacement active at absolute barNumber; empty list when none
    public IReadOnlyList<MotifPlacement> GetActiveMotifs(int barNumber)
    {
        if (barNumber < 1)
            return Array.Empty<MotifPlacement>();

        if (!_barToMotifs.TryGetValue(barNumber, out var motifs))
            return Array.Empty<MotifPlacement>();

        return motifs;
    }

    // AI: query=Return active MotifPlacement for role at absolute barNumber; role is required and case-insensitive
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

    // AI: legacy_query=Return MotifPresence for section-relative (sectionIndex, barWithinSection, role)
    public MotifPresence GetPresence(int sectionIndex, int barWithinSection, string role)
    {
        var key = (sectionIndex, barWithinSection, role);
        if (_presenceByPosition.TryGetValue(key, out var presence))
        {
            return presence;
        }

        return MotifPresence.Absent;
    }

    // AI: legacy_query=Return true when common lead roles active at section-relative position
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

    // AI: build=Precompute absoluteBar->placements lookup using SectionTrack; returns empty when SectionTrack null
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

    // AI: build_legacy=Build section-relative presence map with density estimates per placement
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

    // AI: helper=Return 1-based absolute section start bar or -1 when invalid or sectionTrack missing
    private int GetSectionStartBar(int absoluteSectionIndex)
    {
        if (_sectionTrack is null)
            return -1;

        if (absoluteSectionIndex < 0 || absoluteSectionIndex >= _sectionTrack.Sections.Count)
            return -1;

        return _sectionTrack.Sections[absoluteSectionIndex].StartBar;
    }

    // AI: heuristics=Estimate density from MotifSpec rhythm shape; onsetCount->density clamped [0.1,1.0]
    private static double EstimateDensity(MotifSpec spec)
    {
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
