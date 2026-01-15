// AI: purpose=Deterministic planner computing per-section SectionVariationPlan driven by tension/transition hints.
// AI: invariants=Same inputs yield same plans; all outputs within bounded ranges; variation intensity rises near transitions.
// AI: deps=Consumes SectionTrack, ITensionQuery, BaseReferenceSelectorRules; produces SectionVariationPlan consumed via IVariationQuery.
// AI: constraints=Conservative defaults; per-role deltas optional (null=no change); respects style/groove identity; seed used only for tie-breaks.

namespace Music.Generator;

/// <summary>
/// Deterministic planner that computes SectionVariationPlan for each section in a song.
/// Drives variation intensity and per-role deltas from tension profiles, transition hints,
/// section type/index, and groove/style.
/// </summary>
/// <remarks>
/// Acceptance criteria:
/// - Deterministic: same inputs ? same plans
/// - Driven by tension (ITensionQuery), transition hints, section type
/// - Conservative and clamped: VariationIntensity stays small by default, rises near transitions
/// - Per-role deltas bounded and optional (null = no change)
/// - Seed used only for deterministic tie-breaks
/// - No new musical behavior encoded here (planning only)
/// </remarks>
public static class SectionVariationPlanner
{
    /// <summary>
    /// Computes a complete set of variation plans for all sections in a song.
    /// </summary>
    /// <param name="sectionTrack">The song's section track.</param>
    /// <param name="tensionQuery">Tension query for transition hints.</param>
    /// <param name="grooveName">Groove/style name for deterministic decisions.</param>
    /// <param name="seed">Seed for deterministic tie-breaking.</param>
    /// <returns>List of variation plans, one per section, in section order.</returns>
    public static List<SectionVariationPlan> ComputePlans(
        SectionTrack sectionTrack,
        ITensionQuery tensionQuery,
        string grooveName,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(sectionTrack);
        ArgumentNullException.ThrowIfNull(tensionQuery);
        ArgumentNullException.ThrowIfNull(grooveName);

        var plans = new List<SectionVariationPlan>(sectionTrack.Sections.Count);
        var sections = sectionTrack.Sections;

        // Track section indices by type for proper A/A'/B indexing
        var sectionIndicesByType = new Dictionary<MusicConstants.eSectionType, int>();

        for (int absoluteIndex = 0; absoluteIndex < sections.Count; absoluteIndex++)
        {
            var section = sections[absoluteIndex];

            // Get or initialize index for this section type
            if (!sectionIndicesByType.ContainsKey(section.SectionType))
            {
                sectionIndicesByType[section.SectionType] = 0;
            }
            int sectionTypeIndex = sectionIndicesByType[section.SectionType];
            sectionIndicesByType[section.SectionType]++;

            // Select base reference (A/A'/B mapping)
            int? baseReferenceIndex = BaseReferenceSelectorRules.SelectBaseReference(
                absoluteIndex,
                sections,
                grooveName,
                seed);

            // Determine primary tag
            string primaryTag = BaseReferenceSelectorRules.DeterminePrimaryTag(
                absoluteIndex,
                baseReferenceIndex,
                sections);

            // Use fixed energy value (no longer depends on EnergyArc)
            double fixedEnergy = 0.5;

            // Get tension context (macro tension + transition hint)
            var macroTension = tensionQuery.GetMacroTension(absoluteIndex);
            var transitionHint = tensionQuery.GetTransitionHint(absoluteIndex);

            // Compute variation intensity
            double variationIntensity = ComputeVariationIntensity(
                absoluteIndex,
                baseReferenceIndex,
                fixedEnergy,
                macroTension.MacroTension,
                transitionHint,
                section.SectionType,
                sections,
                seed);

            // Compute per-role deltas
            var roleDeltas = ComputeRoleDeltas(
                variationIntensity,
                fixedEnergy,
                macroTension.MacroTension,
                transitionHint,
                section.SectionType,
                baseReferenceIndex,
                grooveName,
                seed);

            // Build additional tags
            var tags = BuildTags(
                primaryTag,
                section.SectionType,
                absoluteIndex,
                sections,
                variationIntensity,
                transitionHint);

            // Create plan
            var plan = baseReferenceIndex.HasValue
                ? SectionVariationPlan.WithReuse(
                    absoluteIndex,
                    baseReferenceIndex.Value,
                    variationIntensity,
                    roleDeltas,
                    tags)
                : SectionVariationPlan.NoReuse(absoluteIndex, primaryTag);

            // Apply tags if no reuse
            if (!baseReferenceIndex.HasValue && tags.Count > 1)
            {
                foreach (var tag in tags)
                {
                    if (tag != primaryTag)
                    {
                        plan = plan.WithTag(tag);
                    }
                }
            }

            plans.Add(plan);
        }

        return plans;
    }

    /// <summary>
    /// Computes variation intensity [0..1] from energy/tension/transition context.
    /// Conservative default: stays small unless near transitions or high-energy sections.
    /// </summary>
    private static double ComputeVariationIntensity(
        int absoluteIndex,
        int? baseReferenceIndex,
        double energy,
        double macroTension,
        SectionTransitionHint transitionHint,
        MusicConstants.eSectionType sectionType,
        IReadOnlyList<Section> sections,
        int seed)
    {
        // Base case: no reuse ? no variation intensity needed
        if (!baseReferenceIndex.HasValue)
        {
            return 0.0;
        }

        // Start with conservative base intensity
        double baseIntensity = 0.15; // Small default

        // Factor 1: Energy delta from base reference
        var baseSection = sections[baseReferenceIndex.Value];
        double baseEnergy = 0.5; // Placeholder (would need to query arc, but keeping simple for MVP)
        double energyDelta = Math.Abs(energy - baseEnergy);
        double energyFactor = Math.Clamp(energyDelta * 1.5, 0.0, 0.3); // Max +0.3 from energy

        // Factor 2: Transition hint amplifies variation near boundaries
        double transitionFactor = transitionHint switch
        {
            SectionTransitionHint.Build => 0.2,    // Building into next section
            SectionTransitionHint.Drop => 0.25,    // Dramatic drop after this section
            SectionTransitionHint.Release => 0.15, // Release variation
            SectionTransitionHint.Sustain => 0.05, // Sustain = minimal variation
            _ => 0.1
        };

        // Factor 3: Macro tension adds slight variation
        double tensionFactor = macroTension * 0.15; // Max +0.15 from tension

        // Factor 4: Section type modifiers
        double sectionTypeFactor = sectionType switch
        {
            MusicConstants.eSectionType.Chorus => 0.1,  // Chorus allows more variation
            MusicConstants.eSectionType.Bridge => 0.15, // Bridge encourages variation
            MusicConstants.eSectionType.Outro => 0.2,   // Outro can vary more
            _ => 0.0
        };

        // Factor 5: Distance from base reference (more repeats ? allow more variation)
        int repeatDistance = absoluteIndex - baseReferenceIndex.Value;
        double repeatFactor = Math.Min(repeatDistance * 0.05, 0.15); // Max +0.15 for distant repeats

        // Combine factors
        double totalIntensity = baseIntensity 
            + energyFactor 
            + transitionFactor 
            + tensionFactor 
            + sectionTypeFactor 
            + repeatFactor;

        // Apply deterministic jitter for tie-breaking (small)
        int hash = HashCode.Combine(seed, absoluteIndex, "variation_intensity");
        double jitter = ((hash % 100) / 1000.0) - 0.05; // ±0.05

        totalIntensity += jitter;

        // Clamp to [0..1], but keep practical upper bound around 0.6 for "bounded variation"
        return Math.Clamp(totalIntensity, 0.0, 0.6);
    }

    /// <summary>
    /// Computes per-role variation deltas based on variation intensity and context.
    /// Returns null for roles that should not vary, or conservative deltas when variation is desired.
    /// </summary>
    private static VariationRoleDeltas ComputeRoleDeltas(
        double variationIntensity,
        double energy,
        double macroTension,
        SectionTransitionHint transitionHint,
        MusicConstants.eSectionType sectionType,
        int? baseReferenceIndex,
        string grooveName,
        int seed)
    {
        // No variation needed if intensity is very low or no base reference
        if (variationIntensity < 0.1 || !baseReferenceIndex.HasValue)
        {
            return VariationRoleDeltas.Neutral();
        }

        // Decide which roles should vary based on section type and intensity
        bool varyBass = DetermineRoleVariation("Bass", variationIntensity, sectionType, seed);
        bool varyComp = DetermineRoleVariation("Comp", variationIntensity, sectionType, seed);
        bool varyKeys = DetermineRoleVariation("Keys", variationIntensity, sectionType, seed);
        bool varyPads = DetermineRoleVariation("Pads", variationIntensity, sectionType, seed);
        bool varyDrums = DetermineRoleVariation("Drums", variationIntensity, sectionType, seed);

        // Compute delta magnitudes scaled by variation intensity
        // Conservative: small deltas that scale with intensity
        double densityDeltaMagnitude = variationIntensity * 0.2;  // Max ±0.2 (so 0.8-1.2 range)
        int velocityDeltaMagnitude = (int)(variationIntensity * 8); // Max ±8 MIDI units
        int registerDeltaMagnitude = (int)(variationIntensity * 6); // Max ±6 semitones
        double busyDeltaMagnitude = variationIntensity * 0.15;     // Max ±0.15

        // Direction bias: Build ? lift/louder, Drop ? thin/quieter, Release ? varied
        int directionBias = transitionHint switch
        {
            SectionTransitionHint.Build => 1,   // Positive deltas
            SectionTransitionHint.Drop => -1,   // Negative deltas
            SectionTransitionHint.Release => 0, // Mixed
            _ => 0
        };

        // Per-role delta construction with deterministic sign selection
        RoleVariationDelta? bassDelta = varyBass
            ? CreateRoleDelta("Bass", densityDeltaMagnitude, velocityDeltaMagnitude, 0, busyDeltaMagnitude, directionBias, seed)
            : null;

        RoleVariationDelta? compDelta = varyComp
            ? CreateRoleDelta("Comp", densityDeltaMagnitude, velocityDeltaMagnitude, registerDeltaMagnitude, busyDeltaMagnitude, directionBias, seed)
            : null;

        RoleVariationDelta? keysDelta = varyKeys
            ? CreateRoleDelta("Keys", densityDeltaMagnitude, velocityDeltaMagnitude, registerDeltaMagnitude, busyDeltaMagnitude, directionBias, seed)
            : null;

        RoleVariationDelta? padsDelta = varyPads
            ? CreateRoleDelta("Pads", densityDeltaMagnitude, velocityDeltaMagnitude, registerDeltaMagnitude, busyDeltaMagnitude, directionBias, seed)
            : null;

        RoleVariationDelta? drumsDelta = varyDrums
            ? CreateRoleDelta("Drums", densityDeltaMagnitude, velocityDeltaMagnitude, 0, busyDeltaMagnitude, directionBias, seed)
            : null;

        return new VariationRoleDeltas
        {
            Bass = bassDelta,
            Comp = compDelta,
            Keys = keysDelta,
            Pads = padsDelta,
            Drums = drumsDelta
        };
    }

    /// <summary>
    /// Deterministically decides whether a role should vary in this section.
    /// Higher variation intensity ? more roles vary.
    /// </summary>
    private static bool DetermineRoleVariation(
        string roleName,
        double variationIntensity,
        MusicConstants.eSectionType sectionType,
        int seed)
    {
        // Hash-based deterministic decision
        int hash = HashCode.Combine(seed, roleName, sectionType);
        double normalizedHash = Math.Abs(hash % 100) / 100.0;

        // Threshold: higher intensity ? easier to cross threshold ? more roles vary
        double threshold = 1.0 - variationIntensity; // Intensity 0.3 ? threshold 0.7 (30% chance)
        
        return normalizedHash >= threshold;
    }

    /// <summary>
    /// Creates a role variation delta with deterministic signs based on direction bias.
    /// </summary>
    private static RoleVariationDelta CreateRoleDelta(
        string roleName,
        double densityMagnitude,
        int velocityMagnitude,
        int registerMagnitude,
        double busyMagnitude,
        int directionBias,
        int seed)
    {
        // Hash for deterministic sign selection
        int hash = HashCode.Combine(seed, roleName, "delta_signs");

        // Apply direction bias, but allow deterministic variation around it
        double densityMultiplier = directionBias switch
        {
            1 => 1.0 + densityMagnitude,  // Build: increase density
            -1 => 1.0 - densityMagnitude, // Drop: decrease density
            _ => ((hash % 2) == 0) ? 1.0 + densityMagnitude : 1.0 - densityMagnitude
        };

        int velocityBias = directionBias switch
        {
            1 => velocityMagnitude,       // Build: louder
            -1 => -velocityMagnitude,     // Drop: quieter
            _ => (((hash >> 1) % 2) == 0) ? velocityMagnitude : -velocityMagnitude
        };

        int registerLift = directionBias switch
        {
            1 => registerMagnitude,       // Build: lift register
            -1 => -registerMagnitude,     // Drop: lower register
            _ => (((hash >> 2) % 2) == 0) ? registerMagnitude : -registerMagnitude
        };

        double busyProbability = directionBias switch
        {
            1 => busyMagnitude,           // Build: busier
            -1 => -busyMagnitude,         // Drop: sparser
            _ => (((hash >> 3) % 2) == 0) ? busyMagnitude : -busyMagnitude
        };

        return RoleVariationDelta.Create(
            densityMultiplier: densityMultiplier,
            velocityBias: velocityBias,
            registerLiftSemitones: registerMagnitude > 0 ? registerLift : null,
            busyProbability: busyProbability);
    }

    /// <summary>
    /// Builds a complete set of tags for a section variation plan.
    /// </summary>
    private static HashSet<string> BuildTags(
        string primaryTag,
        MusicConstants.eSectionType sectionType,
        int absoluteIndex,
        IReadOnlyList<Section> sections,
        double variationIntensity,
        SectionTransitionHint transitionHint)
    {
        var tags = new HashSet<string> { primaryTag };

        // Add section type name for clarity
        tags.Add(sectionType.ToString());

        // Add intensity descriptors
        if (variationIntensity >= 0.4)
        {
            tags.Add("HighVariation");
        }
        else if (variationIntensity >= 0.2)
        {
            tags.Add("ModerateVariation");
        }
        else if (variationIntensity > 0.0)
        {
            tags.Add("SubtleVariation");
        }

        // Add transition-based descriptors
        if (transitionHint == SectionTransitionHint.Build)
        {
            tags.Add("Lift");
        }
        else if (transitionHint == SectionTransitionHint.Drop)
        {
            tags.Add("Thin");
        }
        else if (transitionHint == SectionTransitionHint.Release)
        {
            tags.Add("Release");
        }

        // Add positional descriptors
        if (absoluteIndex == sections.Count - 1)
        {
            tags.Add("Final");
        }

        // Check if this is the last occurrence of this section type
        bool isLastOfType = true;
        for (int i = absoluteIndex + 1; i < sections.Count; i++)
        {
            if (sections[i].SectionType == sectionType)
            {
                isLastOfType = false;
                break;
            }
        }
        if (isLastOfType && absoluteIndex > 0)
        {
            tags.Add("LastOfType");
        }

        return tags;
    }
}
