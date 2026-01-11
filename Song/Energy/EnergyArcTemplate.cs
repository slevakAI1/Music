// AI: purpose=Template defining energy progression across a song's structural form.
// AI: invariants=SectionTargets map is keyed by (SectionType, SectionIndex); energy values [0..1]; must be deterministic.
// AI: deps=Used by EnergyArc to generate section-specific targets; consumed by SectionEnergyProfile in Story 7.2.

namespace Music.Generator
{
    /// <summary>
    /// Template defining the energy arc (energy map) for a song form.
    /// Maps section types and indices to energy targets, creating the emotional "graph" of the song.
    /// </summary>
    public sealed class EnergyArcTemplate
    {
        /// <summary>
        /// Human-readable name for the arc template (e.g., "PopStandard", "RockBuild", "EDMDrop").
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Description of the arc's emotional shape and intended use.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Default section energy targets keyed by section type.
        /// These are used when a specific section instance target is not defined.
        /// </summary>
        public required Dictionary<MusicConstants.eSectionType, double> DefaultEnergiesBySectionType { get; init; }

        /// <summary>
        /// Specific energy targets for individual section instances.
        /// Key format: "(SectionType, Index)" where Index is 0-based (e.g., Verse 1 = index 0, Verse 2 = index 1).
        /// If not present, falls back to DefaultEnergiesBySectionType.
        /// </summary>
        public Dictionary<(MusicConstants.eSectionType Type, int Index), EnergySectionTarget> SectionTargets { get; init; } = new();

        /// <summary>
        /// Resolves energy target for a specific section instance.
        /// Returns instance-specific target if defined, otherwise falls back to default for section type.
        /// </summary>
        public EnergySectionTarget GetTargetForSection(MusicConstants.eSectionType sectionType, int sectionIndex)
        {
            // Check for instance-specific target
            var key = (sectionType, sectionIndex);
            if (SectionTargets.TryGetValue(key, out var target))
            {
                return target;
            }

            // Fall back to default energy for section type
            if (DefaultEnergiesBySectionType.TryGetValue(sectionType, out var defaultEnergy))
            {
                return EnergySectionTarget.Uniform(defaultEnergy);
            }

            // Final fallback: medium energy
            return EnergySectionTarget.Uniform(0.5);
        }
    }
}
