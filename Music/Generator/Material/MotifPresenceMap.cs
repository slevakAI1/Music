// AI: purpose=Query structure for checking motif activity at (section, bar, role); used by generators for ducking/replacement decisions.
// AI: Story 9.3=Minimal implementation to support motif presence checks; density estimate for ducking magnitude.

using Music.Song.Material;

namespace Music.Generator.Material;

/// <summary>
/// Query structure for checking if motifs are active at specific song positions.
/// Story 9.3: Used by generators to check motif presence for ducking/replacement logic.
/// </summary>
public sealed class MotifPresenceMap
{
    private readonly Dictionary<(int section, int bar, string role), MotifPresence> _presenceByPosition;

    public MotifPresenceMap(MotifPlacementPlan plan)
    {
        _presenceByPosition = new Dictionary<(int section, int bar, string role), MotifPresence>();

        // Build presence map from placement plan
        foreach (var placement in plan.Placements)
        {
            for (int barOffset = 0; barOffset < placement.DurationBars; barOffset++)
            {
                int bar = placement.StartBarWithinSection + barOffset;
                var key = (placement.AbsoluteSectionIndex, bar, placement.MotifSpec.IntendedRole);

                // Estimate density from motif rhythm shape
                double density = EstimateDensity(placement.MotifSpec);

                _presenceByPosition[key] = new MotifPresence
                {
                    IsActive = true,
                    DensityEstimate = density,
                    MotifName = placement.MotifSpec.Name
                };
            }
        }
    }

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
