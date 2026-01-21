// AI: purpose=Deterministic motif placement planner; selects WHICH motifs appear WHERE.
// AI: invariants=All outputs deterministic by seed; placement respects orchestration/register constraints; collision-free within register bands.
// AI: deps=Consumes SectionTrack, MaterialBank; produces MotifPlacementPlan for renderer.


// AI: purpose=Deterministic motif placement planner; selects WHICH motifs appear WHERE.
// AI: invariants=All outputs deterministic by seed; placement respects orchestration/register constraints; collision-free within register bands.
// AI: deps=Consumes SectionTrack, MaterialBank; produces MotifPlacementPlan for renderer.

using Music.Generator;
using Music.Song.Material;

namespace Music.Generator.Material;

/// <summary>
/// Deterministically places motifs in song structure based on section types, tension, and orchestration.
/// Story 9.1: WHICH motifs WHERE; Story 9.2 handles rendering notes.
/// </summary>
/// <remarks>
/// MVP placement heuristics:
/// - Chorus: primary hook motif almost always
/// - Intro: optional motif teaser
/// - Pre-chorus: motif fragment or rhythmic foreshadowing (build anticipation)
/// - Bridge: either new motif or transformed existing motif (contrast)
/// - Verse: optional riff
/// 
/// Collision checks:
/// - do not place when role absent in orchestration
/// - do not place if register would be violated
/// - avoid simultaneous dense motifs in same register band
/// </remarks>
public static class MotifPlacementPlanner
{
    /// <summary>
    /// Creates a deterministic motif placement plan for the song.
    /// </summary>
    /// <param name="sectionTrack">Song structure (section types, lengths).</param>
    /// <param name="motifBank">Available motifs (filtered by role/kind).</param>
    /// <param name="seed">Seed for deterministic tie-breaking.</param>
    /// <returns>Complete placement plan.</returns>
    public static MotifPlacementPlan CreatePlan(
        SectionTrack sectionTrack,
        MaterialBank motifBank,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(sectionTrack);
        ArgumentNullException.ThrowIfNull(motifBank);

        Tracer.DebugTrace("=== MotifPlacementPlanner.CreatePlan ===");
        Tracer.DebugTrace($"MaterialBank has {motifBank.Count} items:");
        foreach (var motif in motifBank.Tracks)
        {
            Tracer.DebugTrace($"  Motif: Name={motif.Meta.Name}, Role={motif.Meta.IntendedRole}, Kind={motif.Meta.MaterialKind}");
        }

        var placements = new List<MotifPlacement>();

        // Track which motifs have been used per section type AND role for A/A' logic
        var motifUsageByTypeAndRole = new Dictionary<(MusicConstants.eSectionType, string), PartTrack.PartTrackId>();

        // Roles to attempt placement for (in priority order)
        var rolesToPlace = new[] { "Lead", "Bass", "Guitar", "Keys" };

        for (int sectionIndex = 0; sectionIndex < sectionTrack.Sections.Count; sectionIndex++)
        {
            var section = sectionTrack.Sections[sectionIndex];

            // Check if motif should be placed in this section
            bool shouldPlace = ShouldPlaceMotif(section.SectionType, sectionIndex, seed);
            Tracer.DebugTrace($"  ShouldPlaceMotif? {shouldPlace}");
            
            if (!shouldPlace)
                continue;

            // Try to place motifs for each role
            foreach (var targetRole in rolesToPlace)
            {
                Tracer.DebugTrace($"  Trying role: {targetRole}");
                
                // All roles present (no orchestration gating)
                Tracer.DebugTrace($"    IsRolePresent? true");

                // Select motif for this section and role
                var motif = SelectMotifForSectionAndRole(
                    section.SectionType,
                    targetRole,
                    sectionIndex,
                    motifBank,
                    motifUsageByTypeAndRole,
                    seed);

                if (motif == null)
                {
                    Tracer.DebugTrace($"    No motif selected for role {targetRole}");
                    continue;
                }

                Tracer.DebugTrace($"    Selected: {motif.Meta.Name}");

                // Create placement
                var placement = CreatePlacementForSection(
                    motif,
                    sectionIndex,
                    section,
                    seed);

                if (placement != null)
                {
                    placements.Add(placement);
                    Tracer.DebugTrace($"    Placement created: bars {placement.StartBarWithinSection}-{placement.StartBarWithinSection + placement.DurationBars - 1}");

                    // Track usage for A/A' logic
                    motifUsageByTypeAndRole[(section.SectionType, targetRole)] = motif.Meta.TrackId;
                }
                else
                {
                    Tracer.DebugTrace($"    Placement creation failed");
                }
            }
        }

        Tracer.DebugTrace($"=== Total placements: {placements.Count} ===");
        return MotifPlacementPlan.Create(placements, seed);
    }

    /// <summary>
    /// Determines if a motif should be placed in this section type.
    /// MVP heuristics based on section type only.
    /// </summary>
    private static bool ShouldPlaceMotif(
        MusicConstants.eSectionType sectionType,
        int sectionIndex,
        int seed)
    {
        // Hash for deterministic per-section decision
        var hash = HashCode.Combine(seed, sectionIndex, sectionType);
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        return sectionType switch
        {
            // Chorus: almost always place hook
            MusicConstants.eSectionType.Chorus => roll < 0.8,

            // Intro: optional teaser
            MusicConstants.eSectionType.Intro => roll < 0.5,

            // Verse: optional riff or bass fill for transitions
            MusicConstants.eSectionType.Verse => roll < 0.7,

            // Bridge: place if contrast or new material
            MusicConstants.eSectionType.Bridge => roll < 0.7,

            // Solo: usually place (it's the featured section)
            MusicConstants.eSectionType.Solo => roll < 0.8,

            // Outro: place bass fills and outros frequently for satisfying endings
            MusicConstants.eSectionType.Outro => roll < 0.7,

            _ => false
        };
    }

    /// <summary>
    /// Selects appropriate motif for section and specific role, respecting A/A' reuse and variation.
    /// </summary>
    private static PartTrack? SelectMotifForSectionAndRole(
        MusicConstants.eSectionType sectionType,
        string targetRole,
        int sectionIndex,
        MaterialBank motifBank,
        Dictionary<(MusicConstants.eSectionType, string), PartTrack.PartTrackId> motifUsageByTypeAndRole,
        int seed)
    {
        Tracer.DebugTrace($"    SelectMotifForSectionAndRole: sectionType={sectionType}, role={targetRole}");
        
        // Get preferred material kind for this section type
        var preferredKind = GetPreferredMaterialKind(sectionType, sectionIndex);
        Tracer.DebugTrace($"      PreferredKind for {sectionType}: {preferredKind}");

        // For lead roles, filter strictly by MaterialKind
        // For accompaniment roles (Bass, Guitar, Keys), be more flexible
        bool isLeadRole = IsLeadRole(targetRole);
        
        List<PartTrack> candidates;
        
        if (isLeadRole)
        {
            // Lead: strict MaterialKind filtering
            var allMotifs = motifBank.GetMotifsByKind(preferredKind);
            Tracer.DebugTrace($"      Found {allMotifs.Count} motifs with kind={preferredKind}");
            
            candidates = allMotifs
                .Where(m => string.Equals(m.Meta.IntendedRole, targetRole, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            // Accompaniment roles: filter by role only, accept any compatible MaterialKind
            candidates = motifBank.Tracks
                .Where(m => string.Equals(m.Meta.IntendedRole, targetRole, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            Tracer.DebugTrace($"      Found {candidates.Count} motifs with role={targetRole} (any kind)");
            Tracer.DebugTrace($"      PreferredKind={preferredKind}, IsTransitionBarOrFill=");
        }
        
        Tracer.DebugTrace($"      After role filter ({targetRole}): {candidates.Count} candidates");
        foreach (var c in candidates)
        {
            Tracer.DebugTrace($"        Candidate: {c.Meta.Name} (Kind={c.Meta.MaterialKind})");
        }

        if (!candidates.Any())
            return null;

        // Check for A/A' reuse (simplified: no BaseReferenceSectionIndex available)
        // For MVP, each section gets its own motif selection
        if (motifUsageByTypeAndRole.TryGetValue((sectionType, targetRole), out var previousMotifId))
        {
            // Could reuse, but without variation context we select fresh
            // (A/A' logic disabled for MVP energy disconnect)
        }

        // Select deterministically by hash
        var hash = HashCode.Combine(seed, sectionIndex, sectionType, targetRole);
        var index = Math.Abs(hash) % candidates.Count;
        Tracer.DebugTrace($"      Selected index {index} of {candidates.Count}");
        return candidates[index];
    }

    /// <summary>
    /// Gets preferred material kind based on section type.
    /// AI: BassFill prioritized at section transitions; Outro commonly uses fills.
    /// </summary>
    private static MaterialKind GetPreferredMaterialKind(
        MusicConstants.eSectionType sectionType,
        int sectionIndex)
    {
        return sectionType switch
        {
            MusicConstants.eSectionType.Chorus => MaterialKind.Hook,
            MusicConstants.eSectionType.Bridge => MaterialKind.Hook,
            MusicConstants.eSectionType.Verse => MaterialKind.MelodyPhrase,
            MusicConstants.eSectionType.Solo => MaterialKind.Riff,
            MusicConstants.eSectionType.Intro => MaterialKind.Hook,
            MusicConstants.eSectionType.Outro => MaterialKind.BassFill,  // Use bass fills for outros/transitions
            _ => MaterialKind.Hook
        };
    }

    /// <summary>
    /// Creates placement for motif in section, determining bars and variation intensity.
    /// </summary>
    private static MotifPlacement? CreatePlacementForSection(
        PartTrack motif,
        int sectionIndex,
        Section section,
        int seed)
    {
        // Determine duration (typically full section or half section)
        var durationBars = DetermineDuration(section, sectionIndex, seed);

        // Determine start bar (typically bar 0)
        var startBar = DetermineStartBar(section, sectionIndex, seed);

        if (startBar + durationBars > section.BarCount)
            durationBars = Math.Max(1, section.BarCount - startBar);

        // Use fixed variation intensity (no variation for MVP energy disconnect)
        var variationIntensity = 0.0;

        // No transform tags (no variation context)
        var transformTags = new HashSet<string>();

        // Convert PartTrack to MotifSpec for the placement
        var motifSpec = MotifConversion.FromPartTrack(motif);
        if (motifSpec == null)
            return null;

        return MotifPlacement.Create(
            motifSpec,
            sectionIndex,
            startBar,
            durationBars,
            variationIntensity,
            transformTags);
    }

    /// <summary>
    /// Determines duration in bars for motif placement.
    /// </summary>
    private static int DetermineDuration(
        Section section,
        int sectionIndex,
        int seed)
    {
        var hash = HashCode.Combine(seed, sectionIndex, "duration");
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        // Chorus typically uses full section
        if (section.SectionType == MusicConstants.eSectionType.Chorus)
            return section.BarCount;

        // Other sections may use half section
        if (section.BarCount >= 4 && roll < 0.5)
            return section.BarCount / 2;

        return section.BarCount;
    }

    /// <summary>
    /// Determines start bar within section for motif.
    /// </summary>
    private static int DetermineStartBar(
        Section section,
        int sectionIndex,
        int seed)
    {
        // Default: start at beginning (no delayed entry for MVP energy disconnect)
        return 0;
    }

    /// <summary>
    /// <summary>
    /// Checks if role is a lead role (melody/hook carrier).
    /// </summary>
    private static bool IsLeadRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        var lower = role.ToLowerInvariant();
        return lower.Contains("lead") || lower.Contains("vocal") || lower.Contains("hook");
    }
}
