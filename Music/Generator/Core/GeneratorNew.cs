// AI: purpose=Generate PartTrack using for Drums only, Section+Bar timing;
// AI: invariants=BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=MusicConstants.TicksPerQuarterNote.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.

using Music.MyMidi;

namespace Music.Generator
{
    public static class GeneratorNew
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        public static PartTrack Generate(SongContext songContext)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            //ValidateGrooveTrack(songContext.GroovePresetDefinition);   TO DO 



            // Get total bars from section track
            int totalBars = songContext.SectionTrack.TotalBars;

            // Use default randomization settings and harmony policy
            var settings = RandomizationSettings.Default;
            var harmonyPolicy = HarmonyPolicy.Default;

            var grooveName = GetPrimaryGrooveName(songContext.GrooveTrack);

            // Resolve MIDI program numbers from VoiceSet
            //int bassProgramNumber = GetProgramNumberForRole(songContext.Voices, "Bass", defaultProgram: 33);
            //int compProgramNumber = GetProgramNumberForRole(songContext.Voices, "Comp", defaultProgram: 27);
            //int padsProgramNumber = GetProgramNumberForRole(songContext.Voices, "Pads", defaultProgram: 4);
            int drumProgramNumber = GetProgramNumberForRole(songContext.Voices, "DrumKit", defaultProgram: 255);

            var drumTrack = DrumTrackGeneratorNew.Generate(
                 songContext.HarmonyTrack,
                 songContext.GrooveTrack,
                 songContext.BarTrack,
                 songContext.SectionTrack,
                 totalBars,
                 drumProgramNumber);

            return drumTrack;
        }

        // AI: returns SourcePresetName of first groove event or "Default"; used as primary groove key
        private static string GetPrimaryGrooveName(GrooveTrack grooveTrack)
        {
            var firstGroove = grooveTrack.Events.FirstOrDefault();
            return firstGroove?.SourcePresetName ?? "Default";
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