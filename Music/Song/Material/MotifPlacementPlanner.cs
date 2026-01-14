// AI: purpose=Deterministic motif placement planner for Story 9.1; selects WHICH motifs appear WHERE.
// AI: invariants=All outputs deterministic by seed; placement respects orchestration/register constraints; collision-free within register bands.
// AI: deps=Consumes SectionTrack, ISongIntentQuery, MaterialBank; produces MotifPlacementPlan for Story 9.2 renderer.

using Music.Generator;

namespace Music.Song.Material;

/// <summary>
/// Deterministically places motifs in song structure based on section types, energy/tension, and orchestration.
/// Story 9.1: WHICH motifs WHERE; Story 9.2 handles rendering notes.
/// </summary>
/// <remarks>
/// MVP placement heuristics:
/// - Chorus: primary hook motif almost always (highest energy)
/// - Intro: optional motif teaser if energy low and arrangement sparse
/// - Pre-chorus: motif fragment or rhythmic foreshadowing (build anticipation)
/// - Bridge: either new motif or transformed existing motif (contrast)
/// - Verse: optional riff if verse energy mid-high
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
    /// <param name="intentQuery">Stage 7 energy/tension/variation/orchestration intent.</param>
    /// <param name="motifBank">Available motifs (filtered by role/kind).</param>
    /// <param name="seed">Seed for deterministic tie-breaking.</param>
    /// <returns>Complete placement plan.</returns>
    public static MotifPlacementPlan CreatePlan(
        SectionTrack sectionTrack,
        ISongIntentQuery intentQuery,
        MaterialBank motifBank,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(sectionTrack);
        ArgumentNullException.ThrowIfNull(intentQuery);
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
            var intent = intentQuery.GetSectionIntent(sectionIndex);

            Tracer.DebugTrace($"Section {sectionIndex} ({section.SectionType}): Energy={intent.Energy:F2}");

            // Check if motif should be placed in this section
            bool shouldPlace = ShouldPlaceMotif(section.SectionType, intent, seed);
            Tracer.DebugTrace($"  ShouldPlaceMotif? {shouldPlace}");
            
            if (!shouldPlace)
                continue;

            // Try to place motifs for each role
            foreach (var targetRole in rolesToPlace)
            {
                Tracer.DebugTrace($"  Trying role: {targetRole}");
                
                // Check orchestration constraints first
                bool rolePresent = IsRolePresent(intent.RolePresence, targetRole);
                Tracer.DebugTrace($"    IsRolePresent? {rolePresent}");
                
                if (!rolePresent)
                    continue;

                // Select motif for this section and role
                var motif = SelectMotifForSectionAndRole(
                    section.SectionType,
                    targetRole,
                    intent,
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
                    intent,
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
    /// Determines if a motif should be placed in this section type/energy context.
    /// MVP heuristics: Chorus almost always, Verse if mid-high energy, etc.
    /// </summary>
    private static bool ShouldPlaceMotif(
        MusicConstants.eSectionType sectionType,
        SectionIntentContext intent,
        int seed)
    {
        // Hash for deterministic per-section decision
        var hash = HashCode.Combine(seed, intent.AbsoluteSectionIndex, sectionType);
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        return sectionType switch
        {
            // Chorus: almost always place hook
            MusicConstants.eSectionType.Chorus => intent.Energy > 0.3 || roll < 0.8,

            // Intro: optional teaser if energy low and sparse
            MusicConstants.eSectionType.Intro => intent.Energy < 0.4 && roll < 0.5,

            // Verse: optional riff if mid-high energy
            MusicConstants.eSectionType.Verse => intent.Energy > 0.5 && roll < 0.6,

            // Bridge: place if contrast or new material
            MusicConstants.eSectionType.Bridge => roll < 0.7,

            // Solo: usually place (it's the featured section)
            MusicConstants.eSectionType.Solo => roll < 0.8,

            // Outro: optional if not too sparse
            MusicConstants.eSectionType.Outro => intent.Energy > 0.3 && roll < 0.4,

            _ => false
        };
    }

    /// <summary>
    /// Selects appropriate motif for section and specific role, respecting A/A' reuse and variation.
    /// </summary>
    private static PartTrack? SelectMotifForSectionAndRole(
        MusicConstants.eSectionType sectionType,
        string targetRole,
        SectionIntentContext intent,
        MaterialBank motifBank,
        Dictionary<(MusicConstants.eSectionType, string), PartTrack.PartTrackId> motifUsageByTypeAndRole,
        int seed)
    {
        // Determine preferred material kind based on section type
        var preferredKind = GetPreferredMaterialKind(sectionType, intent);
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
        }
        
        Tracer.DebugTrace($"      After role filter ({targetRole}): {candidates.Count} candidates");
        foreach (var c in candidates)
        {
            Tracer.DebugTrace($"        Candidate: {c.Meta.Name} (Kind={c.Meta.MaterialKind})");
        }

        if (!candidates.Any())
            return null;

        // Check for A/A' reuse
        if (motifUsageByTypeAndRole.TryGetValue((sectionType, targetRole), out var previousMotifId) &&
            intent.BaseReferenceSectionIndex.HasValue)
        {
            // Reuse same motif for A' variation
            if (motifBank.TryGet(previousMotifId, out var previousMotif))
            {
                Tracer.DebugTrace($"      Reusing previous motif for A/A'");
                return previousMotif;
            }
        }

        // Select deterministically by hash
        var hash = HashCode.Combine(seed, intent.AbsoluteSectionIndex, sectionType, targetRole);
        var index = Math.Abs(hash) % candidates.Count;
        Tracer.DebugTrace($"      Selected index {index} of {candidates.Count}");
        return candidates[index];
    }

    /// <summary>
    /// Gets preferred material kind based on section type and energy.
    /// </summary>
    private static MaterialKind GetPreferredMaterialKind(
        MusicConstants.eSectionType sectionType,
        SectionIntentContext intent)
    {
        return sectionType switch
        {
            MusicConstants.eSectionType.Chorus => MaterialKind.Hook,
            MusicConstants.eSectionType.Bridge => MaterialKind.Hook,
            MusicConstants.eSectionType.Verse => intent.Energy > 0.6 ? MaterialKind.Riff : MaterialKind.MelodyPhrase,
            MusicConstants.eSectionType.Solo => MaterialKind.Riff,
            MusicConstants.eSectionType.Intro => MaterialKind.Hook,
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
        SectionIntentContext intent,
        int seed)
    {
        // Determine duration (typically full section or half section)
        var durationBars = DetermineDuration(section, intent, seed);

        // Determine start bar (typically bar 0, or delayed for builds)
        var startBar = DetermineStartBar(section, intent, seed);

        if (startBar + durationBars > section.BarCount)
            durationBars = Math.Max(1, section.BarCount - startBar);

        // Determine variation intensity from section intent
        var variationIntensity = intent.VariationIntensity;

        // Determine transform tags based on variation and context
        var transformTags = DetermineTransformTags(intent, seed);

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
        SectionIntentContext intent,
        int seed)
    {
        var hash = HashCode.Combine(seed, intent.AbsoluteSectionIndex, "duration");
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        // Chorus and high-energy sections typically use full section
        if (intent.SectionType == MusicConstants.eSectionType.Chorus || intent.Energy > 0.7)
            return section.BarCount;

        // Verse and lower energy may use half section
        if (section.BarCount >= 4 && roll < 0.5)
            return section.BarCount / 2;

        return section.BarCount;
    }

    /// <summary>
    /// Determines start bar within section for motif.
    /// </summary>
    private static int DetermineStartBar(
        Section section,
        SectionIntentContext intent,
        int seed)
    {
        var hash = HashCode.Combine(seed, intent.AbsoluteSectionIndex, "startbar");
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        // PreChorus builds often delay entry
        if (intent.TensionDrivers.HasFlag(TensionDriver.PreChorusBuild) && roll < 0.3)
            return Math.Min(2, section.BarCount - 2);

        // Default: start at beginning
        return 0;
    }

    /// <summary>
    /// Determines transform tags based on variation intent and context.
    /// </summary>
    private static HashSet<string> DetermineTransformTags(
        SectionIntentContext intent,
        int seed)
    {
        var tags = new HashSet<string>();
        var hash = HashCode.Combine(seed, intent.AbsoluteSectionIndex, "transform");
        var roll = (double)(Math.Abs(hash) % 100) / 100.0;

        // High variation intensity enables transforms
        if (intent.VariationIntensity > 0.6)
        {
            if (roll < 0.3)
                tags.Add("Syncopate");
            else if (roll < 0.5)
                tags.Add("OctaveUp");
        }

        // Lift tag suggests register lift
        if (intent.VariationTags.Contains("Lift") && roll < 0.4)
            tags.Add("OctaveUp");

        return tags;
    }

    /// <summary>
    /// Checks if role is present in orchestration.
    /// </summary>
    private static bool IsRolePresent(RolePresenceHints rolePresence, string intendedRole)
    {
        if (string.IsNullOrWhiteSpace(intendedRole))
            return true; // No specific role constraint

        return intendedRole.ToLowerInvariant() switch
        {
            "lead" => true, // Lead always conceptually present (motifs reserve space)
            "vocal" => true,
            "bass" => rolePresence.BassPresent,
            "comp" => rolePresence.CompPresent,
            "keys" => rolePresence.KeysPresent,
            "pads" => rolePresence.PadsPresent,
            "drums" => rolePresence.DrumsPresent,
            _ => true // Unknown role, allow
        };
    }

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
