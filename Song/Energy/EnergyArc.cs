// AI: purpose=Deterministic energy arc selector and resolver for song-level energy planning.
// AI: invariants=Arc selection deterministic by (seed, grooveName, songFormId); energy values [0..1].
// AI: deps=Consumes EnergyArcTemplate from EnergyArcLibrary; provides targets to SectionEnergyProfile (Story 7.2).

namespace Music.Generator
{
    /// <summary>
    /// Manages energy arc selection and resolution for a song.
    /// Provides deterministic section-level energy targets based on song structure and style.
    /// </summary>
    public sealed class EnergyArc
    {
        private readonly EnergyArcTemplate _template;
        private readonly string _grooveName;
        private readonly int _seed;

        /// <summary>
        /// The selected arc template defining the energy progression.
        /// </summary>
        public EnergyArcTemplate Template => _template;

        /// <summary>
        /// Groove/style name that influenced arc selection.
        /// </summary>
        public string GrooveName => _grooveName;

        private EnergyArc(EnergyArcTemplate template, string grooveName, int seed)
        {
            _template = template;
            _grooveName = grooveName;
            _seed = seed;
        }

        /// <summary>
        /// Creates an EnergyArc by deterministically selecting a template based on song characteristics.
        /// Selection is deterministic for given (seed, grooveName, songFormId).
        /// </summary>
        /// <param name="sectionTrack">The song's section structure.</param>
        /// <param name="grooveName">Primary groove/style name (e.g., "BossaNovaBasic", "RockSteady").</param>
        /// <param name="seed">Randomization seed for deterministic tie-breaking.</param>
        /// <param name="songFormId">Optional explicit form identifier (if null, inferred from sections).</param>
        public static EnergyArc Create(
            SectionTrack sectionTrack,
            string grooveName,
            int seed,
            string? songFormId = null)
        {
            ArgumentNullException.ThrowIfNull(sectionTrack);
            ArgumentException.ThrowIfNullOrWhiteSpace(grooveName);

            // Infer form ID from section structure if not provided
            string formId = songFormId ?? InferSongFormId(sectionTrack);

            // Select template deterministically
            var template = SelectTemplate(grooveName, formId, seed);

            return new EnergyArc(template, grooveName, seed);
        }

        /// <summary>
        /// Resolves energy target for a specific section instance.
        /// </summary>
        /// <param name="section">The section to resolve energy for.</param>
        /// <param name="sectionIndex">0-based index of this section instance among sections of the same type.</param>
        public EnergySectionTarget GetTargetForSection(Section section, int sectionIndex)
        {
            ArgumentNullException.ThrowIfNull(section);
            var target = _template.GetTargetForSection(section.SectionType, sectionIndex);
            return target;
        }

        /// <summary>
        /// Resolves energy target by section type and index directly.
        /// </summary>
        public EnergySectionTarget GetTargetForSection(MusicConstants.eSectionType sectionType, int sectionIndex)
        {
            return _template.GetTargetForSection(sectionType, sectionIndex);
        }

        /// <summary>
        /// Infers a simple form identifier from section structure.
        /// Used when explicit songFormId is not provided.
        /// </summary>
        private static string InferSongFormId(SectionTrack sectionTrack)
        {
            bool hasIntro = sectionTrack.Sections.Any(s => s.SectionType == MusicConstants.eSectionType.Intro);
            bool hasVerse = sectionTrack.Sections.Any(s => s.SectionType == MusicConstants.eSectionType.Verse);
            bool hasChorus = sectionTrack.Sections.Any(s => s.SectionType == MusicConstants.eSectionType.Chorus);
            bool hasBridge = sectionTrack.Sections.Any(s => s.SectionType == MusicConstants.eSectionType.Bridge);

            // Simple form classification
            if (hasVerse && hasChorus && hasBridge)
                return "VerseChorusBridge";
            if (hasVerse && hasChorus)
                return "VerseChorus";
            if (hasIntro && hasVerse)
                return "IntroVerse";

            // Fallback
            return "Generic";
        }

        /// <summary>
        /// Selects an arc template deterministically based on groove name, form, and seed.
        /// </summary>
        private static EnergyArcTemplate SelectTemplate(string grooveName, string formId, int seed)
        {
            // Map groove name to style category for arc selection
            string styleCategory = MapGrooveToStyleCategory(grooveName);

            // Get candidate templates
            var candidates = EnergyArcLibrary.GetTemplatesForStyle(styleCategory);

            if (candidates.Count == 0)
            {
                // Fallback to generic templates
                candidates = EnergyArcLibrary.GetGenericTemplates();
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("No energy arc templates available.");
            }

            // Deterministic selection using seed
            var random = new SeededRandomSource(seed);
            int index = random.NextInt(0, candidates.Count);

            return candidates[index];
        }

        /// <summary>
        /// Maps groove name to a broad style category for arc selection.
        /// This is a simple heuristic; can be enhanced with explicit metadata.
        /// </summary>
        private static string MapGrooveToStyleCategory(string grooveName)
        {
            string lowerName = grooveName.ToLowerInvariant();

            if (lowerName.Contains("rock") || lowerName.Contains("punk") || lowerName.Contains("metal"))
                return "Rock";
            if (lowerName.Contains("pop") || lowerName.Contains("dance") || lowerName.Contains("funk"))
                return "Pop";
            if (lowerName.Contains("edm") || lowerName.Contains("house") || lowerName.Contains("techno"))
                return "EDM";
            if (lowerName.Contains("jazz") || lowerName.Contains("bossa") || lowerName.Contains("latin"))
                return "Jazz";
            if (lowerName.Contains("country") || lowerName.Contains("folk"))
                return "Country";

            // Default to Pop for unknown styles
            return "Pop";
        }
    }
}
