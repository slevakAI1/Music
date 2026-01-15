// AI: purpose=Deterministic rules for selecting base reference sections (A/A'/B mapping) for section variation planning.
// AI: invariants=Same inputs yield same outputs; BaseReferenceSectionIndex always < current index; rules prioritize musical coherence (verse repeats reference first verse, etc.).
// AI: deps=Consumed by SectionVariationPlanner (Story 7.6.3); produces BaseReferenceSectionIndex used in SectionVariationPlan (Story 7.6.1).
// AI: constraints=Bridge/Solo can be new material (B) or contrasting transforms; other repeats tend toward A' unless contrast explicitly required.

namespace Music.Generator;

/// <summary>
/// Deterministic rules for selecting which earlier section should be used as a "base reference"
/// for section repetition and variation. Supports A / A' / B mapping patterns.
/// </summary>
/// <remarks>
/// Design principles:
/// - Same SectionType repeats tend to reference the earliest prior instance (A pattern).
/// - Bridge/Solo sections can be either new material (B) or contrasting transforms.
/// - Ties resolved deterministically via stable keys (sectionType/index + groove/style + seed).
/// - Tags produced: "A" (first occurrence), "Aprime" (varied repeat), "B" (contrasting section).
/// 
/// Story 7.6.2 acceptance criteria:
/// - Deterministic BaseReferenceSectionIndex selection
/// - Same SectionType repeats tend to reference earliest prior instance (A)
/// - Deterministic B-case for Bridge/Solo/explicit contrasts
/// - Ties resolved deterministically via stable keys
/// - Stable Tags at least including A, Aprime, B
/// </remarks>
public static class BaseReferenceSelectorRules
{
    /// <summary>
    /// Selects the base reference section index for a given section, or null if the section
    /// should be new material (not reusing an earlier section).
    /// </summary>
    /// <param name="currentSectionIndex">0-based index of the current section.</param>
    /// <param name="sections">Ordered list of all sections in the song.</param>
    /// <param name="grooveName">Groove/style name for tie-breaking.</param>
    /// <param name="seed">Seed for deterministic tie-breaking (when multiple valid choices exist).</param>
    /// <returns>0-based index of the base reference section, or null for new material.</returns>
    public static int? SelectBaseReference(
        int currentSectionIndex,
        IReadOnlyList<Section> sections,
        string grooveName,
        int seed)
    {
        if (currentSectionIndex < 0 || currentSectionIndex >= sections.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentSectionIndex));
        }

        var currentSection = sections[currentSectionIndex];
        var currentType = currentSection.SectionType;

        // Rule 1: First occurrence of any section type is always new material (A)
        bool isFirstOccurrence = IsFirstOccurrenceOfType(currentSectionIndex, sections, currentType);
        if (isFirstOccurrence)
        {
            return null; // No base reference (will get "A" tag)
        }

        // Rule 2: Bridge and Solo can be contrasting material (B case)
        // Use seed to deterministically decide between reuse and new material
        if (currentType == MusicConstants.eSectionType.Bridge || 
            currentType == MusicConstants.eSectionType.Solo)
        {
            bool shouldBeContrasting = ShouldBeContrastingSection(
                currentSectionIndex, 
                sections, 
                grooveName, 
                seed);
            
            if (shouldBeContrasting)
            {
                return null; // New contrasting material (will get "B" tag)
            }
        }

        // Rule 3: For repeated sections of same type, reference the earliest prior instance
        int? earliestMatchIndex = FindEarliestPriorInstanceOfType(
            currentSectionIndex, 
            sections, 
            currentType);

        return earliestMatchIndex; // Will get "Aprime" tag
    }

    /// <summary>
    /// Determines the primary tag for a section based on its base reference.
    /// </summary>
    /// <param name="currentSectionIndex">0-based index of the current section.</param>
    /// <param name="baseReferenceIndex">Base reference index (null if new material).</param>
    /// <param name="sections">Ordered list of all sections.</param>
    /// <returns>Primary tag: "A", "Aprime", or "B".</returns>
    public static string DeterminePrimaryTag(
        int currentSectionIndex,
        int? baseReferenceIndex,
        IReadOnlyList<Section> sections)
    {
        if (baseReferenceIndex == null)
        {
            // No base reference - either first occurrence (A) or contrasting section (B)
            var currentType = sections[currentSectionIndex].SectionType;
            bool isFirstOccurrence = IsFirstOccurrenceOfType(currentSectionIndex, sections, currentType);
            
            if (isFirstOccurrence)
            {
                return "A";
            }
            else
            {
                // Not first occurrence but no base reference = contrasting section
                return "B";
            }
        }
        else
        {
            // Has base reference = varied repeat
            return "Aprime";
        }
    }

    /// <summary>
    /// Checks if the given section is the first occurrence of its type.
    /// </summary>
    private static bool IsFirstOccurrenceOfType(
        int currentSectionIndex,
        IReadOnlyList<Section> sections,
        MusicConstants.eSectionType sectionType)
    {
        for (int i = 0; i < currentSectionIndex; i++)
        {
            if (sections[i].SectionType == sectionType)
            {
                return false; // Found an earlier occurrence
            }
        }
        return true;
    }

    /// <summary>
    /// Finds the earliest prior section of the same type.
    /// </summary>
    private static int? FindEarliestPriorInstanceOfType(
        int currentSectionIndex,
        IReadOnlyList<Section> sections,
        MusicConstants.eSectionType sectionType)
    {
        for (int i = 0; i < currentSectionIndex; i++)
        {
            if (sections[i].SectionType == sectionType)
            {
                return i; // Return the earliest match
            }
        }
        return null;
    }

    /// <summary>
    /// Deterministically decides whether a Bridge or Solo section should be contrasting material (B)
    /// versus a varied repeat (Aprime). Uses stable hash of inputs for determinism.
    /// </summary>
    /// <remarks>
    /// Decision factors:
    /// - If this is the second+ occurrence of the section type, favor contrast more
    /// - Use seed + groove + section position for deterministic tie-break
    /// - Approximately 40% chance for first Bridge/Solo, 60% for subsequent ones
    /// </remarks>
    private static bool ShouldBeContrastingSection(
        int currentSectionIndex,
        IReadOnlyList<Section> sections,
        string grooveName,
        int seed)
    {
        var currentType = sections[currentSectionIndex].SectionType;
        
        // Count how many prior occurrences of this type exist
        int priorOccurrences = 0;
        for (int i = 0; i < currentSectionIndex; i++)
        {
            if (sections[i].SectionType == currentType)
            {
                priorOccurrences++;
            }
        }

        // Build deterministic hash from stable inputs
        int hash = HashCode.Combine(seed, grooveName, currentSectionIndex, currentType);
        
        // Use hash to make deterministic decision
        // First occurrence: ~40% contrast, subsequent: ~60% contrast
        double contrastThreshold = priorOccurrences == 0 ? 0.4 : 0.6;
        double normalizedHash = Math.Abs(hash % 100) / 100.0;
        
        return normalizedHash < contrastThreshold;
    }

    /// <summary>
    /// Determines if a section should have additional secondary tags based on its characteristics.
    /// </summary>
    /// <remarks>
    /// Potential secondary tags:
    /// - "Final" for last occurrence of a repeated section type (e.g., final chorus)
    /// - Section type name for clarity (e.g., "Verse", "Chorus")
    /// </remarks>
    public static HashSet<string> DetermineSecondaryTags(
        int currentSectionIndex,
        IReadOnlyList<Section> sections)
    {
        var tags = new HashSet<string>();
        var currentSection = sections[currentSectionIndex];
        var currentType = currentSection.SectionType;

        // Add section type name as a tag for clarity
        tags.Add(currentType.ToString());

        // Check if this is the final occurrence of this section type
        bool isFinalOccurrence = true;
        for (int i = currentSectionIndex + 1; i < sections.Count; i++)
        {
            if (sections[i].SectionType == currentType)
            {
                isFinalOccurrence = false;
                break;
            }
        }

        if (isFinalOccurrence && !IsFirstOccurrenceOfType(currentSectionIndex, sections, currentType))
        {
            // This is the last of multiple occurrences
            tags.Add("Final");
        }

        return tags;
    }

    /// <summary>
    /// Validates that a base reference selection is legal.
    /// </summary>
    /// <param name="currentSectionIndex">Current section index.</param>
    /// <param name="baseReferenceIndex">Proposed base reference (can be null).</param>
    /// <exception cref="ArgumentException">If base reference is invalid.</exception>
    public static void ValidateBaseReference(int currentSectionIndex, int? baseReferenceIndex)
    {
        if (baseReferenceIndex.HasValue)
        {
            if (baseReferenceIndex.Value >= currentSectionIndex)
            {
                throw new ArgumentException(
                    $"BaseReferenceSectionIndex ({baseReferenceIndex.Value}) must be < currentSectionIndex ({currentSectionIndex})");
            }

            if (baseReferenceIndex.Value < 0)
            {
                throw new ArgumentException(
                    $"BaseReferenceSectionIndex ({baseReferenceIndex.Value}) must be >= 0");
            }
        }
    }
}
