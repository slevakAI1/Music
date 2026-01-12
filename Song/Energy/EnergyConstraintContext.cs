// AI: purpose=Context for energy constraint rule evaluation, providing section information and related energies.
// AI: invariants=ProposedEnergy [0..1]; all energy values [0..1]; sectionIndex >= 0.
// AI: deps=Consumed by EnergyConstraintRule implementations; provided by constraint application pipeline.

namespace Music.Generator
{
    /// <summary>
    /// Context provided to energy constraint rules for evaluation.
    /// Contains the section being evaluated and energy values of related sections.
    /// </summary>
    public sealed class EnergyConstraintContext
    {
        /// <summary>
        /// The section type being evaluated.
        /// </summary>
        public required MusicConstants.eSectionType SectionType { get; init; }

        /// <summary>
        /// 0-based index of this section instance among sections of the same type.
        /// </summary>
        public required int SectionIndex { get; init; }

        /// <summary>
        /// 0-based absolute section index in the song.
        /// </summary>
        public required int AbsoluteSectionIndex { get; init; }

        /// <summary>
        /// Proposed energy value [0..1] for this section before constraint application.
        /// </summary>
        public required double ProposedEnergy { get; init; }

        /// <summary>
        /// Energy value of the previous section instance of the same type, if any.
        /// Null if this is the first instance of this section type.
        /// </summary>
        public double? PreviousSameTypeEnergy { get; init; }

        /// <summary>
        /// Energy value of the immediately previous section (any type), if any.
        /// Null if this is the first section in the song.
        /// </summary>
        public double? PreviousAnySectionEnergy { get; init; }

        /// <summary>
        /// Section type of the immediately previous section, if any.
        /// </summary>
        public MusicConstants.eSectionType? PreviousSectionType { get; init; }

        /// <summary>
        /// Energy value of the next section (any type), if known.
        /// Null if this is the last section or energy not yet determined.
        /// </summary>
        public double? NextSectionEnergy { get; init; }

        /// <summary>
        /// Whether this is the last section of its type in the song.
        /// </summary>
        public bool IsLastOfType { get; init; }

        /// <summary>
        /// Whether this is the last section in the song (any type).
        /// </summary>
        public bool IsLastSection { get; init; }

        /// <summary>
        /// All finalized energy values for sections processed so far.
        /// Key is absolute section index, value is energy [0..1].
        /// Used for rules that need broader context.
        /// </summary>
        public required IReadOnlyDictionary<int, double> FinalizedEnergies { get; init; }

        /// <summary>
        /// Total number of sections of this type in the song.
        /// </summary>
        public int TotalSectionsOfType { get; init; }

        /// <summary>
        /// Total number of sections in the song.
        /// </summary>
        public int TotalSections { get; init; }
    }
}
