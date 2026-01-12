// AI: purpose=Represents energy target for a single section instance, including per-section and phrase-level targets.
// AI: invariants=Energy values are [0..1] scale where 0=minimal, 1=maximum; phrase targets optional.
// AI: deps=Used by EnergyArc and later by SectionEnergyProfile; changing scale requires updating all energy consumers.

namespace Music.Generator
{
    /// <summary>
    /// Energy target for a specific section instance, supporting both section-level and phrase-level targets.
    /// Energy values use [0..1] scale where 0 represents minimal energy and 1 represents maximum energy.
    /// </summary>
    public sealed class EnergySectionTarget
    {
        /// <summary>
        /// Overall energy target for the entire section [0..1].
        /// This is the primary energy value that drives arrangement decisions.
        /// </summary>
        public required double Energy { get; init; }

        /// <summary>
        /// The section type this target applies to.
        /// </summary>
        public required MusicConstants.eSectionType SectionType { get; init; }

        /// <summary>
        /// 0-based index of this section instance among sections of the same type.
        /// </summary>
        public required int SectionIndex { get; init; }

        /// <summary>
        /// Optional phrase-level energy targets within the section.
        /// If null or empty, the section uses uniform energy throughout.
        /// Values represent relative positions within phrases: Start, Middle, Peak, Cadence.
        /// </summary>
        public EnergyPhraseTargets? PhraseTargets { get; init; }

        /// <summary>
        /// Creates a section target with uniform energy (no phrase variation).
        /// </summary>
        public static EnergySectionTarget Uniform(double energy, MusicConstants.eSectionType sectionType, int sectionIndex)
        {
            return new EnergySectionTarget
            {
                Energy = energy,
                SectionType = sectionType,
                SectionIndex = sectionIndex,
                PhraseTargets = null
            };
        }

        /// <summary>
        /// Creates a section target with phrase-level micro-arcs.
        /// </summary>
        public static EnergySectionTarget WithPhraseMicroArc(
            double baseEnergy,
            MusicConstants.eSectionType sectionType,
            int sectionIndex,
            double startOffset = 0.0,
            double middleOffset = 0.0,
            double peakOffset = 0.0,
            double cadenceOffset = 0.0)
        {
            return new EnergySectionTarget
            {
                Energy = baseEnergy,
                SectionType = sectionType,
                SectionIndex = sectionIndex,
                PhraseTargets = new EnergyPhraseTargets
                {
                    StartOffset = startOffset,
                    MiddleOffset = middleOffset,
                    PeakOffset = peakOffset,
                    CadenceOffset = cadenceOffset
                }
            };
        }
    }

    /// <summary>
    /// Phrase-level energy offsets relative to section base energy.
    /// Offsets are applied additively to the section's base energy value.
    /// </summary>
    public sealed class EnergyPhraseTargets
    {
        /// <summary>Energy offset at phrase start (first bar(s)).</summary>
        public double StartOffset { get; init; }

        /// <summary>Energy offset at phrase middle.</summary>
        public double MiddleOffset { get; init; }

        /// <summary>Energy offset at phrase peak (highest energy point).</summary>
        public double PeakOffset { get; init; }

        /// <summary>Energy offset at phrase cadence (resolution point).</summary>
        public double CadenceOffset { get; init; }
    }
}
