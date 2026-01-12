// AI: purpose=Deterministic tension planner computing section-level macro tension and micro maps (Story 7.5.2).
// AI: invariants=Deterministic by (EnergyArc + seed); tensions clamped [0..1]; drivers explain why value changed.
// AI: deps=Consumes EnergyArc (constrained energies) and Section objects; does not mutate inputs.
// AI: change=adds planner only; NeutralTensionQuery remains for fallback/testing.

namespace Music.Generator
{
    /// <summary>
    /// Deterministic tension query implementation that computes section-level macro tension
    /// using constrained energy targets from an EnergyArc plus simple, style-aware heuristics.
    /// Produces SectionTensionProfile and fallback MicroTensionMap per section.
    /// Also computes SectionTransitionHint per section boundary.
    /// </summary>
    public sealed class DeterministicTensionQuery : ITensionQuery
    {
        private readonly EnergyArc _arc;
        private readonly int _seed;
        private readonly int _sectionCount;
        private readonly Dictionary<int, SectionTensionProfile> _profiles;
        private readonly Dictionary<int, MicroTensionMap> _microMaps;
        private readonly Dictionary<int, SectionTransitionHint> _transitionHints;

        /// <summary>
        /// Creates a DeterministicTensionQuery using a resolved EnergyArc and a seed.
        /// The seed is used only for deterministic tie-breaks and minor stochastic decisions.
        /// </summary>
        public DeterministicTensionQuery(EnergyArc arc, int seed)
        {
            ArgumentNullException.ThrowIfNull(arc);
            _arc = arc;
            _seed = seed;
            _sectionCount = arc.SectionTrack.Sections.Count;
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

        /// <summary>
        /// Gets the transition hint for a section boundary (transition FROM this section TO next).
        /// Returns None for the last section.
        /// </summary>
        public SectionTransitionHint GetTransitionHint(int absoluteSectionIndex)
        {
            ValidateSectionIndex(absoluteSectionIndex);
            return _transitionHints.TryGetValue(absoluteSectionIndex, out var hint) ? hint : SectionTransitionHint.None;
        }

        private void ComputeProfiles()
        {
            var rng = new SeededRandomSource(_seed);

            for (int abs = 0; abs < _sectionCount; abs++)
            {
                var section = _arc.SectionTrack.Sections[abs];
                int sectionIndex = ComputeSectionIndex(section.SectionType, abs);

                // Get constrained energy from EnergyArc
                var target = _arc.GetTargetForSection(section, sectionIndex);
                double baseEnergy = Math.Clamp(target.Energy, 0.0, 1.0);

                TensionDriver driver = TensionDriver.None;
                double adjusted = baseEnergy;

                // Apply type-specific tension biases (distinct from energy)
                switch (section.SectionType)
                {
                    case MusicConstants.eSectionType.Intro:
                        adjusted += 0.05;
                        driver |= TensionDriver.Opening;
                        break;

                    case MusicConstants.eSectionType.Verse:
                        // Verses are typically lower tension (stable)
                        adjusted -= 0.05;
                        break;

                    case MusicConstants.eSectionType.Chorus:
                        // Chorus often releases tension vs PreChorus
                        if (abs > 0 && _arc.SectionTrack.Sections[abs - 1].SectionType == MusicConstants.eSectionType.Chorus)
                        {
                            // Repeated chorus - check if we're building or sustaining
                            var prevEnergy = _arc.GetTargetForSection(_arc.SectionTrack.Sections[abs - 1], sectionIndex - 1).Energy;
                            if (baseEnergy > prevEnergy + 0.05)
                            {
                                adjusted += 0.03; // Building toward final chorus
                                driver |= TensionDriver.Peak;
                            }
                            else
                            {
                                adjusted -= 0.08; // Release
                                driver |= TensionDriver.Resolution;
                            }
                        }
                        else
                        {
                            adjusted -= 0.08;
                            driver |= TensionDriver.Resolution;
                        }
                        break;

                    case MusicConstants.eSectionType.Bridge:
                        adjusted += 0.12;
                        driver |= TensionDriver.BridgeContrast;
                        break;

                    case MusicConstants.eSectionType.Solo:
                        adjusted += 0.07;
                        driver |= TensionDriver.Peak;
                        break;

                    case MusicConstants.eSectionType.Outro:
                        adjusted -= 0.15;
                        driver |= TensionDriver.Resolution;
                        break;
                }

                // Anticipation: if next section has higher energy, raise tension
                if (abs < _sectionCount - 1)
                {
                    int nextIdx = ComputeSectionIndex(_arc.SectionTrack.Sections[abs + 1].SectionType, abs + 1);
                    var nextTarget = _arc.GetTargetForSection(_arc.SectionTrack.Sections[abs + 1], nextIdx);
                    double nextEnergy = nextTarget.Energy;

                    if (nextEnergy - baseEnergy > 0.10)
                    {
                        double bump = Math.Min(0.15, (nextEnergy - baseEnergy) * 0.7);
                        adjusted += bump;
                        driver |= TensionDriver.Anticipation;
                    }
                }

                // PreChorus must have higher tension than preceding Verse
                if (section.SectionType == MusicConstants.eSectionType.Chorus && abs > 0)
                {
                    var prev = _arc.SectionTrack.Sections[abs - 1];
                    if (prev.SectionType == MusicConstants.eSectionType.Verse)
                    {
                        // Ensure tension rises from verse to chorus entry point
                        adjusted += 0.10;
                        driver |= TensionDriver.PreChorusBuild | TensionDriver.Anticipation;
                    }
                }

                // Deterministic jitter to avoid flat lines
                double jitter = (rng.NextDouble() - 0.5) * 0.03;
                adjusted += jitter;

                // Clamp and store
                adjusted = Math.Clamp(adjusted, 0.0, 1.0);

                var profile = SectionTensionProfile.WithMacroTension(adjusted, abs, driver);
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
                    _transitionHints[abs] = ComputeTransitionHint(abs, profile, baseEnergy);
                }
                else
                {
                    _transitionHints[abs] = SectionTransitionHint.None;
                }
            }
        }

        private SectionTransitionHint ComputeTransitionHint(int absoluteIndex, SectionTensionProfile currentProfile, double currentEnergy)
        {
            if (absoluteIndex >= _sectionCount - 1)
                return SectionTransitionHint.None;

            var nextSection = _arc.SectionTrack.Sections[absoluteIndex + 1];
            int nextIdx = ComputeSectionIndex(nextSection.SectionType, absoluteIndex + 1);
            var nextTarget = _arc.GetTargetForSection(nextSection, nextIdx);
            double nextEnergy = nextTarget.Energy;

            var nextProfile = _profiles.ContainsKey(absoluteIndex + 1) 
                ? _profiles[absoluteIndex + 1] 
                : SectionTensionProfile.Neutral(absoluteIndex + 1);

            double energyDelta = nextEnergy - currentEnergy;
            double tensionDelta = nextProfile.MacroTension - currentProfile.MacroTension;

            // Build: both energy and tension increasing
            if (energyDelta > 0.08 && tensionDelta > 0.05)
                return SectionTransitionHint.Build;

            // Drop: significant energy/tension decrease
            if (energyDelta < -0.12 || tensionDelta < -0.15)
                return SectionTransitionHint.Drop;

            // Release: tension drops but energy may sustain
            if (tensionDelta < -0.08)
                return SectionTransitionHint.Release;

            // Sustain: minimal change
            if (Math.Abs(energyDelta) < 0.08 && Math.Abs(tensionDelta) < 0.08)
                return SectionTransitionHint.Sustain;

            // Default: Build if moving toward higher energy
            if (energyDelta > 0)
                return SectionTransitionHint.Build;

            return SectionTransitionHint.Sustain;
        }

        private int ComputeSectionIndex(MusicConstants.eSectionType sectionType, int absoluteIndex)
        {
            int count = 0;
            for (int i = 0; i <= absoluteIndex; i++)
            {
                if (_arc.SectionTrack.Sections[i].SectionType == sectionType)
                {
                    if (i == absoluteIndex)
                        return count;
                    count++;
                }
            }
            return 0;
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
