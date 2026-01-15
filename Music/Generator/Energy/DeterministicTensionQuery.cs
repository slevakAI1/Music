// AI: purpose=Deterministic tension planner computing section-level macro tension and micro maps.
// AI: invariants=Deterministic by (SectionTrack + seed); tensions clamped [0..1]; drivers explain why value changed.
// AI: deps=Consumes SectionTrack; does not mutate inputs.
// AI: change=Energy removed - uses section type for tension values.

namespace Music.Generator
{
    /// <summary>
    /// Deterministic tension query implementation that computes section-level macro tension
    /// using section type with simple, style-aware heuristics.
    /// Produces SectionTensionProfile and fallback MicroTensionMap per section.
    /// Also computes SectionTransitionHint per section boundary.
    /// </summary>
    public sealed class DeterministicTensionQuery : ITensionQuery
    {
        private readonly SectionTrack _sectionTrack;
        private readonly int _seed;
        private readonly int _sectionCount;
        private readonly Dictionary<int, SectionTensionProfile> _profiles;
        private readonly Dictionary<int, MicroTensionMap> _microMaps;
        private readonly Dictionary<int, SectionTransitionHint> _transitionHints;

        /// <summary>
        /// Creates a DeterministicTensionQuery using a SectionTrack and a seed.
        /// The seed is used only for deterministic tie-breaks and minor stochastic decisions.
        /// </summary>
        public DeterministicTensionQuery(SectionTrack sectionTrack, int seed)
        {
            ArgumentNullException.ThrowIfNull(sectionTrack);
            _sectionTrack = sectionTrack;
            _seed = seed;
            _sectionCount = sectionTrack.Sections.Count;
            _profiles = new Dictionary<int, SectionTensionProfile>(_sectionCount);
            _microMaps = new Dictionary<int, MicroTensionMap>(_sectionCount);
            _transitionHints = new Dictionary<int, SectionTransitionHint>(_sectionCount);

            ComputeProfiles();
        }

        /// <inheritdoc/>
        public SectionTensionProfile GetMacroTension(int absoluteSectionIndex)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            return _profiles[absoluteSectionIndex];
        }

        /// <inheritdoc/>
        public double GetMicroTension(int absoluteSectionIndex, int barIndexWithinSection)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            var map = _microMaps[absoluteSectionIndex];
            return map.GetTension(barIndexWithinSection);
        }

        /// <inheritdoc/>
        public MicroTensionMap GetMicroTensionMap(int absoluteSectionIndex)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            return _microMaps[absoluteSectionIndex];
        }

        /// <inheritdoc/>
        public (bool IsPhraseEnd, bool IsSectionEnd, bool IsSectionStart) GetPhraseFlags(
            int absoluteSectionIndex,
            int barIndexWithinSection)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            var map = _microMaps[absoluteSectionIndex];
            return map.GetFlags(barIndexWithinSection);
        }

        /// <inheritdoc/>
        public bool HasTensionData(int absoluteSectionIndex)
        {
            return absoluteSectionIndex >= 0 && absoluteSectionIndex < _sectionCount;
        }

        /// <inheritdoc/>
        public int SectionCount => _sectionCount;

        /// <inheritdoc/>
        public SectionTransitionHint GetTransitionHint(int absoluteSectionIndex)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            return _transitionHints.TryGetValue(absoluteSectionIndex, out var hint) ? hint : SectionTransitionHint.None;
        }

        /// <inheritdoc/>
        public TensionContext GetTensionContext(int absoluteSectionIndex, int barIndexWithinSection)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            return TensionContext.Create(this, absoluteSectionIndex, barIndexWithinSection);
        }

        private void ComputeProfiles()
        {
            var rng = new SeededRandomSource(_seed);

            for (int abs = 0; abs < _sectionCount; abs++)
            {
                var section = _sectionTrack.Sections[abs];

                TensionDriver driver = TensionDriver.None;
                double baseTension = 0.5; // Default neutral tension

                // Apply type-specific tension values (simplified, no energy dependency)
                switch (section.SectionType)
                {
                    case MusicConstants.eSectionType.Intro:
                        baseTension = 0.55;
                        driver |= TensionDriver.Opening;
                        break;

                    case MusicConstants.eSectionType.Verse:
                        baseTension = 0.45;
                        break;

                    case MusicConstants.eSectionType.Chorus:
                        baseTension = 0.42;
                        driver |= TensionDriver.Resolution;
                        break;

                    case MusicConstants.eSectionType.Bridge:
                        baseTension = 0.62;
                        driver |= TensionDriver.BridgeContrast;
                        break;

                    case MusicConstants.eSectionType.Solo:
                        baseTension = 0.57;
                        driver |= TensionDriver.Peak;
                        break;

                    case MusicConstants.eSectionType.Outro:
                        baseTension = 0.35;
                        driver |= TensionDriver.Resolution;
                        break;
                }

                // Anticipation: if next section is Chorus after Verse, raise tension
                if (abs < _sectionCount - 1)
                {
                    var nextSection = _sectionTrack.Sections[abs + 1];
                    if (section.SectionType == MusicConstants.eSectionType.Verse && 
                        nextSection.SectionType == MusicConstants.eSectionType.Chorus)
                    {
                        baseTension += 0.10;
                        driver |= TensionDriver.PreChorusBuild | TensionDriver.Anticipation;
                    }
                    else if (nextSection.SectionType == MusicConstants.eSectionType.Chorus ||
                             nextSection.SectionType == MusicConstants.eSectionType.Bridge)
                    {
                        baseTension += 0.05;
                        driver |= TensionDriver.Anticipation;
                    }
                }

                // Deterministic jitter to avoid flat lines
                double jitter = (rng.NextDouble() - 0.5) * 0.03;
                baseTension += jitter;

                // Clamp and store
                baseTension = Math.Clamp(baseTension, 0.0, 1.0);

                var profile = SectionTensionProfile.WithMacroTension(baseTension, abs, driver);
                _profiles[abs] = profile;

                // Build micro tension map using Story 7.5.3 builder
                int perSectionSeed = _seed ^ (abs * 397);
                var micro = MicroTensionMap.Build(
                    Math.Max(1, section.BarCount),
                    profile.MacroTension,
                    profile.MicroTensionDefault,
                    phraseLength: null,
                    seed: perSectionSeed);
                _microMaps[abs] = micro;

                // Compute transition hint
                if (abs < _sectionCount - 1)
                {
                    _transitionHints[abs] = ComputeTransitionHint(abs, profile, baseTension);
                }
                else
                {
                    _transitionHints[abs] = SectionTransitionHint.None;
                }
            }
        }

        private SectionTransitionHint ComputeTransitionHint(int absoluteIndex, SectionTensionProfile currentProfile, double currentTension)
        {
            if (absoluteIndex >= _sectionCount - 1)
                return SectionTransitionHint.None;

            var nextSection = _sectionTrack.Sections[absoluteIndex + 1];

            var nextProfile = _profiles.ContainsKey(absoluteIndex + 1) 
                ? _profiles[absoluteIndex + 1] 
                : SectionTensionProfile.Neutral(absoluteIndex + 1);

            double tensionDelta = nextProfile.MacroTension - currentProfile.MacroTension;

            // Build: tension increasing
            if (tensionDelta > 0.05)
                return SectionTransitionHint.Build;

            // Drop: significant tension decrease
            if (tensionDelta < -0.15)
                return SectionTransitionHint.Drop;

            // Release: moderate tension drop
            if (tensionDelta < -0.08)
                return SectionTransitionHint.Release;

            // Sustain: minimal change
            if (Math.Abs(tensionDelta) < 0.08)
                return SectionTransitionHint.Sustain;

            return SectionTransitionHint.Sustain;
        }

        private void ValidateSectionIndex(int index)
        {
            if (index < 0 || index >= _sectionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Section index {index} out of range [0..{_sectionCount - 1}]");
            }
        }
    }
}
