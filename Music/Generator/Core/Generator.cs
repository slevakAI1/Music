// AI: purpose=Generate PartTrack using for Drums only, Section+Bar timing;
// AI: invariants=BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=MusicConstants.TicksPerQuarterNote; DrummerAgent for human-like drumming.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.
// AI: change=Story 8.1 adds optional DrummerAgent parameter for operator-based generation with fallback to DrumTrackGenerator.

using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        // AI: Story 8.1: Original signature preserved for backward compatibility; uses DrumTrackGenerator fallback.
        public static PartTrack Generate(SongContext songContext)
        {
            return Generate(songContext, drummerAgent: null);
        }

        /// <summary>
        /// Generates a drum track from the song context using the optional DrummerAgent.
        /// Story 8.1: Wire Drummer Agent into Generator.
        /// </summary>
        /// <param name="songContext">Song context with section, groove, and timing data.</param>
        /// <param name="drummerAgent">Optional drummer agent for operator-based generation.
        /// When null, falls back to groove-only DrumTrackGenerator.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <remarks>
        /// When drummerAgent is provided:
        /// - Uses operator-based candidate generation with human-like variation
        /// - Applies physicality constraints (limb conflicts, sticking rules)
        /// - Uses memory for anti-repetition across sections
        /// 
        /// When drummerAgent is null:
        /// - Falls back to existing DrumTrackGenerator (anchor patterns only)
        /// - Maintains backward compatibility
        /// </remarks>
        public static PartTrack Generate(SongContext songContext, DrummerAgent? drummerAgent)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GroovePresetDefinition);

            // When drummer agent is provided, use operator-based generation
            if (drummerAgent != null)
            {
                return drummerAgent.Generate(songContext);
            }

            // Fallback to existing groove-only generation
            int totalBars = songContext.SectionTrack.TotalBars;

            // Resolve MIDI program numbers from VoiceSet
            int drumProgramNumber = GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255);

            var drumTrack = DrumTrackGenerator.Generate(
                 songContext.BarTrack,
                 songContext.SectionTrack,
                 songContext.SegmentGrooveProfiles,
                 songContext.GroovePresetDefinition,
                 totalBars,
                 drumProgramNumber);

            return drumTrack;
        }

        // AI: returns Identity.Name of groove preset or "Default"; used as primary groove key
        private static string GetPrimaryGrooveName(GroovePresetDefinition groovePresetDefinition)
        {
            return groovePresetDefinition?.Identity?.Name ?? "Default";
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

        // ValidateSongContext: ensures caller provided a non-null SongContext; throws ArgumentNullException when null.
        private static void ValidateSongContext(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);
        }

        // ValidateGrooveTrack: ensures a preset definition exists and contains an anchor layer
        private static void ValidateGrooveTrack(GroovePresetDefinition groovePresetDefinition)
        {
            if (groovePresetDefinition == null)
                throw new ArgumentException("Groove preset definition must be provided", nameof(groovePresetDefinition));

            if (groovePresetDefinition.AnchorLayer == null)
                throw new ArgumentException("Groove preset must include an AnchorLayer", nameof(groovePresetDefinition));
        }

        #endregion

        // AI: resolves MIDI program by GrooveRole->VoiceName match (case-insensitive); returns defaultProgram on not found
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
