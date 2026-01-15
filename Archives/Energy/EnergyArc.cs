// AI: purpose=Deterministic energy arc selector and resolver for song-level energy planning with constraint application (Story 7.4.2).
// AI: invariants=Arc selection deterministic by (seed, grooveName, songFormId); energy values [0..1]; constraint application deterministic.
// AI: deps=Consumes EnergyArcTemplate from EnergyArcLibrary; applies EnergyConstraintPolicy; provides targets to SectionEnergyProfile.

namespace Music.Generator
{
    /// <summary>
    /// Manages energy arc selection and resolution for a song.
    /// Provides deterministic section-level energy targets based on song structure and style.
    /// Applies energy constraints after template lookup to enforce musical heuristics (Story 7.4.2).
    /// </summary>
    public sealed class EnergyArc
    {
        private readonly EnergyArcTemplate _template;
        private readonly string _grooveName;
        private readonly int _seed;
        private readonly SectionTrack _sectionTrack;
        private readonly EnergyConstraintPolicy _constraintPolicy;

        // Cache for constrained energy values (key: absolute section index)
        private readonly Dictionary<int, double> _constrainedEnergies;
        private readonly Dictionary<int, List<string>> _constraintDiagnostics;

        /// <summary>
        /// The selected arc template defining the energy progression.
        /// </summary>
        public EnergyArcTemplate Template => _template;

        /// <summary>
        /// Groove/style name that influenced arc selection.
        /// </summary>
        public string GrooveName => _grooveName;

        /// <summary>
        /// The constraint policy applied to energy values.
        /// </summary>
        public EnergyConstraintPolicy ConstraintPolicy => _constraintPolicy;

        /// <summary>
        /// The section track this arc was created for.
        /// </summary>
        public SectionTrack SectionTrack => _sectionTrack;

        private EnergyArc(
            EnergyArcTemplate template, 
            string grooveName, 
            int seed, 
            SectionTrack sectionTrack,
            EnergyConstraintPolicy constraintPolicy)
        {
            _template = template;
            _grooveName = grooveName;
            _seed = seed;
            _sectionTrack = sectionTrack;
            _constraintPolicy = constraintPolicy;
            _constrainedEnergies = new Dictionary<int, double>();
            _constraintDiagnostics = new Dictionary<int, List<string>>();

            // Pre-compute all constrained energies for all sections
            ComputeConstrainedEnergies();
        }

        /// <summary>
        /// Creates an EnergyArc by deterministically selecting a template based on song characteristics.
        /// Selection is deterministic for given (seed, grooveName, songFormId).
        /// Applies energy constraints after template lookup (Story 7.4.2).
        /// </summary>
        /// <param name="sectionTrack">The song's section structure.</param>
        /// <param name="grooveName">Primary groove/style name (e.g., "BossaNovaBasic", "RockSteady").</param>
        /// <param name="seed">Randomization seed for deterministic tie-breaking.</param>
        /// <param name="songFormId">Optional explicit form identifier (if null, inferred from sections).</param>
        /// <param name="constraintPolicy">Optional constraint policy (if null, uses default for groove style).</param>
        public static EnergyArc Create(
            SectionTrack sectionTrack,
            string grooveName,
            int seed,
            string? songFormId = null,
            EnergyConstraintPolicy? constraintPolicy = null)
        {
            ArgumentNullException.ThrowIfNull(sectionTrack);
            ArgumentException.ThrowIfNullOrWhiteSpace(grooveName);

            // Infer form ID from section structure if not provided
            string formId = songFormId ?? InferSongFormId(sectionTrack);

            // Select template deterministically
            var template = SelectTemplate(grooveName, formId, seed);

            // Select constraint policy if not provided
            var policy = constraintPolicy ?? EnergyConstraintPolicyLibrary.GetPolicyForGroove(grooveName);

            return new EnergyArc(template, grooveName, seed, sectionTrack, policy);
        }

        /// <summary>
        /// Resolves energy target for a specific section instance.
        /// Returns the constrained energy value after applying policy rules.
        /// </summary>
        /// <param name="section">The section to resolve energy for.</param>
        /// <param name="sectionIndex">0-based index of this section instance among sections of the same type.</param>
        public EnergySectionTarget GetTargetForSection(Section section, int sectionIndex)
        {
            ArgumentNullException.ThrowIfNull(section);
            
            // Find absolute section index
            int absoluteIndex = _sectionTrack.Sections.IndexOf(section);
            if (absoluteIndex < 0)
            {
                throw new ArgumentException("Section not found in section track", nameof(section));
            }

            // Get constrained energy
            if (!_constrainedEnergies.TryGetValue(absoluteIndex, out double constrainedEnergy))
            {
                // Fallback to template if not in cache
                var templateTarget = _template.GetTargetForSection(section.SectionType, sectionIndex);
                constrainedEnergy = templateTarget.Energy;
            }

            // Return target with constrained energy
            return new EnergySectionTarget
            {
                Energy = constrainedEnergy,
                SectionType = section.SectionType,
                SectionIndex = sectionIndex
            };
        }

        /// <summary>
        /// Resolves energy target by section type and index directly.
        /// Note: This method doesn't have section context, so it returns unconstrained energy from template.
        /// Prefer using GetTargetForSection(Section, int) for constrained values.
        /// </summary>
        public EnergySectionTarget GetTargetForSection(MusicConstants.eSectionType sectionType, int sectionIndex)
        {
            return _template.GetTargetForSection(sectionType, sectionIndex);
        }

        /// <summary>
        /// Gets constraint diagnostics for a specific section (if available).
        /// </summary>
        public IReadOnlyList<string> GetConstraintDiagnostics(int absoluteSectionIndex)
        {
            if (_constraintDiagnostics.TryGetValue(absoluteSectionIndex, out var diagnostics))
            {
                return diagnostics;
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Pre-computes all constrained energy values for all sections.
        /// This ensures constraints can look at surrounding sections and maintain determinism.
        /// </summary>
        private void ComputeConstrainedEnergies()
        {
            if (!_constraintPolicy.IsEnabled || _sectionTrack.Sections.Count == 0)
            {
                // No constraints - use template values directly
                for (int i = 0; i < _sectionTrack.Sections.Count; i++)
                {
                    var section = _sectionTrack.Sections[i];
                    int sectionIndex = GetSectionIndex(section.SectionType, i);
                    var templateTarget = _template.GetTargetForSection(section.SectionType, sectionIndex);
                    _constrainedEnergies[i] = templateTarget.Energy;
                    _constraintDiagnostics[i] = new List<string> { "No constraints applied" };
                }
                return;
            }

            // Process sections in order, building context as we go
            for (int absoluteIndex = 0; absoluteIndex < _sectionTrack.Sections.Count; absoluteIndex++)
            {
                var section = _sectionTrack.Sections[absoluteIndex];
                int sectionIndex = GetSectionIndex(section.SectionType, absoluteIndex);

                // Get template energy
                var templateTarget = _template.GetTargetForSection(section.SectionType, sectionIndex);
                double templateEnergy = templateTarget.Energy;

                // Build constraint context
                var context = BuildConstraintContext(section, sectionIndex, absoluteIndex, templateEnergy);

                // Apply constraints
                var (constrainedEnergy, diagnostics) = _constraintPolicy.Apply(context);

                // Store results
                _constrainedEnergies[absoluteIndex] = constrainedEnergy;
                _constraintDiagnostics[absoluteIndex] = diagnostics;
            }
        }

        /// <summary>
        /// Builds constraint context for a section.
        /// </summary>
        private EnergyConstraintContext BuildConstraintContext(
            Section section,
            int sectionIndex,
            int absoluteIndex,
            double proposedEnergy)
        {
            // Find previous section of same type
            double? previousSameTypeEnergy = null;
            for (int i = absoluteIndex - 1; i >= 0; i--)
            {
                if (_sectionTrack.Sections[i].SectionType == section.SectionType)
                {
                    previousSameTypeEnergy = _constrainedEnergies.TryGetValue(i, out double energy) ? energy : null;
                    break;
                }
            }

            // Find previous section (any type)
            double? previousAnySectionEnergy = null;
            MusicConstants.eSectionType? previousSectionType = null;
            if (absoluteIndex > 0)
            {
                previousAnySectionEnergy = _constrainedEnergies.TryGetValue(absoluteIndex - 1, out double energy) ? energy : null;
                previousSectionType = _sectionTrack.Sections[absoluteIndex - 1].SectionType;
            }

            // Find next section energy (if available)
            double? nextSectionEnergy = null;
            if (absoluteIndex < _sectionTrack.Sections.Count - 1)
            {
                nextSectionEnergy = _constrainedEnergies.TryGetValue(absoluteIndex + 1, out double energy) ? energy : null;
            }

            // Count sections of same type
            int totalOfType = _sectionTrack.Sections.Count(s => s.SectionType == section.SectionType);
            int remainingOfType = _sectionTrack.Sections.Skip(absoluteIndex + 1).Count(s => s.SectionType == section.SectionType);
            bool isLastOfType = remainingOfType == 0;

            return new EnergyConstraintContext
            {
                SectionType = section.SectionType,
                SectionIndex = sectionIndex,
                AbsoluteSectionIndex = absoluteIndex,
                ProposedEnergy = proposedEnergy,
                PreviousSameTypeEnergy = previousSameTypeEnergy,
                PreviousAnySectionEnergy = previousAnySectionEnergy,
                PreviousSectionType = previousSectionType,
                NextSectionEnergy = nextSectionEnergy,
                IsLastOfType = isLastOfType,
                IsLastSection = absoluteIndex == _sectionTrack.Sections.Count - 1,
                FinalizedEnergies = new Dictionary<int, double>(_constrainedEnergies), // Snapshot
                TotalSectionsOfType = totalOfType,
                TotalSections = _sectionTrack.Sections.Count
            };
        }

        /// <summary>
        /// Gets the 0-based section index for a section type (Nth instance of that type).
        /// </summary>
        private int GetSectionIndex(MusicConstants.eSectionType sectionType, int absoluteIndex)
        {
            int count = 0;
            for (int i = 0; i <= absoluteIndex; i++)
            {
                if (_sectionTrack.Sections[i].SectionType == sectionType)
                {
                    if (i == absoluteIndex)
                        return count;
                    count++;
                }
            }
            return 0;
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
