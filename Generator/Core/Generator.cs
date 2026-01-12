// AI: purpose=Generate PartTracks for parts using harmony, groove, and timing; uses controlled randomness via PitchRandomizer.
// AI: invariants=Order: Harmony->Groove->Bar must align; totalBars derived from SectionTrack; tick calc uses (onsetBeat-1)*TicksPerQuarterNote.
// AI: deps=Relies on HarmonyTrack.GetActiveHarmonyEvent, GrooveTrack.GetActiveGroovePreset, BarTrack.GetBar, MusicConstants.TicksPerQuarterNote.
// AI: perf=Not real-time; called once per song generation; avoid heavy allocations in inner loops.
// TODO? confirm behavior when groove/pads onsets null vs empty; current code skips in both cases.
// IMPORTANT: Generator MUST NOT rebuild or mutate `BarTrack`.
// The `BarTrack` is considered a read-only timing "ruler" for generation and must be built
// by the caller (e.g., editor/export pipeline) via `BarTrack.RebuildFromTimingTrack(...)` before
// calling `Generator.Generate(...)`. Rebuilding `BarTrack` inside the generator would mask
// upstream integrity issues and is intentionally avoided.

using Music.MyMidi;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        // AI: Story 7.3=Now creates EnergyArc and builds section profiles to drive role energy parameters.
        public static GeneratorResult Generate(SongContext songContext)
        {
            // validate songcontext is not null
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateHarmonyTrack(songContext.HarmonyTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GrooveTrack);

            // Validate harmony events for musical correctness before generation
            var validationResult = HarmonyValidator.ValidateTrack(
                songContext.HarmonyTrack,
                new HarmonyValidationOptions
                {
                    ApplyFixes = false,
                    StrictDiatonicChordTones = false,
                    ClampInvalidBassToRoot = false,
                    AllowUnknownQuality = false
                });

            if (!validationResult.IsValid)
            {
                var errorMessage = "Harmony validation failed:\n" + string.Join("\n", validationResult.Errors);
                throw new InvalidOperationException(errorMessage);
            }

            // Get total bars from section track
            int totalBars = songContext.SectionTrack.TotalBars;

            // Use default randomization settings and harmony policy
            var settings = RandomizationSettings.Default;
            var harmonyPolicy = HarmonyPolicy.Default;

            // Story 7.3: Create energy arc and build section profiles
            var grooveName = GetPrimaryGrooveName(songContext.GrooveTrack);
            var energyArc = EnergyArc.Create(
                songContext.SectionTrack,
                grooveName,
                seed: settings.Seed);

            // Build section profiles dictionary for quick lookup
            var sectionProfiles = BuildSectionProfiles(energyArc, songContext.SectionTrack);

            // Story 7.5.x: Create deterministic tension query (used by Story 7.5.5 drums-only integration).
            ITensionQuery tensionQuery = new DeterministicTensionQuery(energyArc, settings.Seed);
            const double microTensionPhraseRampIntensity = 1.0;

            // Resolve MIDI program numbers from VoiceSet
            int bassProgramNumber = GetProgramNumberForRole(songContext.Voices, "Bass", defaultProgram: 33);
            int compProgramNumber = GetProgramNumberForRole(songContext.Voices, "Comp", defaultProgram: 27);
            int padsProgramNumber = GetProgramNumberForRole(songContext.Voices, "Pads", defaultProgram: 4);
            int drumProgramNumber = GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255);

            return new GeneratorResult
            {
                BassTrack = BassTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    sectionProfiles,
                    tensionQuery,
                    microTensionPhraseRampIntensity,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    bassProgramNumber),

                GuitarTrack = GuitarTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    sectionProfiles,
                    tensionQuery,
                    microTensionPhraseRampIntensity,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    compProgramNumber),

                KeysTrack = KeysTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    sectionProfiles,
                    tensionQuery,
                    microTensionPhraseRampIntensity,
                    totalBars,
                    settings,
                    harmonyPolicy,
                    padsProgramNumber),

                DrumTrack = DrumTrackGenerator.Generate(
                    songContext.HarmonyTrack,
                    songContext.GrooveTrack,
                    songContext.BarTrack,
                    songContext.SectionTrack,
                    sectionProfiles,
                    tensionQuery,
                    microTensionPhraseRampIntensity,
                    totalBars,
                    settings,
                    drumProgramNumber)
            };
        }

        /// <summary>
        /// Builds section energy profiles for all sections.
        /// Returns dictionary keyed by section StartBar for quick lookup.
        /// </summary>
        private static Dictionary<int, EnergySectionProfile> BuildSectionProfiles(
            EnergyArc energyArc,
            SectionTrack sectionTrack)
        {
            var profiles = new Dictionary<int, EnergySectionProfile>();
            
            // Track section indices by type for proper indexing
            var sectionIndicesByType = new Dictionary<MusicConstants.eSectionType, int>();
            
            EnergySectionProfile? previousProfile = null;

            foreach (var section in sectionTrack.Sections)
            {
                // Get or initialize index for this section type
                if (!sectionIndicesByType.ContainsKey(section.SectionType))
                {
                    sectionIndicesByType[section.SectionType] = 0;
                }
                
                int sectionIndex = sectionIndicesByType[section.SectionType];

                // Build profile for this section
                var profile = EnergyProfileBuilder.BuildProfile(
                    energyArc,
                    section,
                    sectionIndex,
                    previousProfile);

                // Store by StartBar for quick lookup during generation
                profiles[section.StartBar] = profile;

                // Increment index for this section type
                sectionIndicesByType[section.SectionType]++;

                // Track for next iteration (contrast calculation)
                previousProfile = profile;
            }

            return profiles;
        }

        /// <summary>
        /// Gets the primary groove name from the groove track (first groove event).
        /// </summary>
        private static string GetPrimaryGrooveName(GrooveTrack grooveTrack)
        {
            var firstGroove = grooveTrack.Events.FirstOrDefault();
            return firstGroove?.SourcePresetName ?? "Default";
        }

        // AI: GeneratorResult: required PartTracks returned; consumers expect these program numbers and ordering.
        public sealed class GeneratorResult
        {
            public required PartTrack BassTrack { get; init; }
            public required PartTrack GuitarTrack { get; init; }
            public required PartTrack KeysTrack { get; init; }
            public required PartTrack DrumTrack { get; init; }
        }

        #region Validation

        // AI: Validation methods throw ArgumentException when required tracks are missing; callers rely on exceptions for invalid song contexts.

        private static void ValidateSectionTrack(SectionTrack sectionTrack)
        {
            if (sectionTrack == null || sectionTrack.Sections.Count == 0)
                throw new ArgumentException("Section track must have events", nameof(sectionTrack));
        }

        private static void ValidateHarmonyTrack(HarmonyTrack harmonyTrack)
        {
            if (harmonyTrack == null || harmonyTrack.Events.Count == 0)
                throw new ArgumentException("Harmony track must have events", nameof(harmonyTrack));
        }

        private static void ValidateTimeSignatureTrack(Timingtrack timeSignatureTrack)
        {
            if (timeSignatureTrack == null || timeSignatureTrack.Events.Count == 0)
                throw new ArgumentException("Time signature track must have events", nameof(timeSignatureTrack));
        }

        private static void ValidateGrooveTrack(GrooveTrack grooveTrack)
        {
            if (grooveTrack == null || grooveTrack.Events.Count == 0)
                throw new ArgumentException("Groove track must have events", nameof(grooveTrack));
        }

        // ValidateSongContext: ensures caller provided a non-null SongContext; throws ArgumentNullException when null.
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        #endregion

        /// <summary>
        /// Resolves MIDI program number from VoiceSet by matching GrooveRole.
        /// Returns defaultProgram if role not found or voice name cannot be mapped.
        /// </summary>
        private static int GetProgramNumberForRole(VoiceSet voices, String grooveRole, int defaultProgram)
        {
            // Find voice with matching groove role
            var voice = voices.Voices.FirstOrDefault(v => 
                string.Equals(v.GrooveRole, grooveRole, StringComparison.OrdinalIgnoreCase));

            if (voice == null)
                return defaultProgram;

            // Look up MIDI program number from voice name
            var midiVoice = MidiVoices.MidiVoiceList()
                .FirstOrDefault(mv => string.Equals(mv.Name, voice.VoiceName, StringComparison.OrdinalIgnoreCase));

            return midiVoice?.ProgramNumber ?? defaultProgram;
        }
    }
}