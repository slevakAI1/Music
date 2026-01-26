// AI: purpose=Generate PartTrack using for Drums only, Section+Bar timing;
// AI: invariants=BarTrack is read-only and must NOT be rebuilt here.
// AI: deps=MusicConstants.TicksPerQuarterNote; GrooveBasedDrumGenerator pipeline for drum generation.
// AI: perf=Single-run generation; avoid allocations in inner loops; use seed for deterministic results.
// AI: change=Story RF-3 replaces DrummerAgent.Generate() with GrooveBasedDrumGenerator pipeline architecture.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator
{
    public static class Generator
    {
        // AI: Generate: validates harmony track before generation; fast-fail on invalid data prevents silent errors.
        // AI: behavior=Runs HarmonyValidator with default options (StrictDiatonicChordTones=true) to catch F# minor crashes.
        // AI: Story RF-3: Original signature preserved for backward compatibility; uses DrumTrackGenerator fallback.
        public static PartTrack Generate(SongContext songContext)
        {
            return Generate(songContext, drummerStyle: null);
        }

        /// <summary>
        /// Generates a drum track from the song context using the optional drummer style configuration.
        /// Story RF-3: Wire GrooveBasedDrumGenerator pipeline with DrummerAgent as data source.
        /// </summary>
        /// <param name="songContext">Song context with section, groove, and timing data.</param>
        /// <param name="drummerStyle">Optional style configuration for operator-based drum generation.
        /// When null, falls back to groove-only DrumTrackGenerator.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <remarks>
        /// <para>Architecture (Story RF-3):</para>
        /// <list type="bullet">
        ///   <item>When drummerStyle is provided: Creates DrummerAgent (data source) → passes to GrooveBasedDrumGenerator (pipeline) → uses GrooveSelectionEngine for weighted selection</item>
        ///   <item>When drummerStyle is null: Falls back to existing DrumTrackGenerator (anchor patterns only)</item>
        /// </list>
        /// <para>Benefits of new architecture:</para>
        /// <list type="bullet">
        ///   <item>Enforces density targets from policy</item>
        ///   <item>Respects operator caps and weights</item>
        ///   <item>Uses GrooveSelectionEngine for proper weighted selection</item>
        ///   <item>Supports operator-based generation with physicality constraints</item>
        ///   <item>Memory system prevents robotic repetition</item>
        /// </list>
        /// </remarks>
        public static PartTrack Generate(SongContext songContext, StyleConfiguration? drummerStyle)
        {
            ValidateSongContext(songContext);
            ValidateSectionTrack(songContext.SectionTrack);
            ValidateTimeSignatureTrack(songContext.Song.TimeSignatureTrack);
            ValidateGrooveTrack(songContext.GroovePresetDefinition);

            // When drummer style is provided, use operator-based generation with pipeline architecture
            if (drummerStyle != null)
            {
                // Create DrummerAgent as data source (IGroovePolicyProvider + IGrooveCandidateSource)
                var agent = new DrummerAgent(drummerStyle);

                // Create pipeline orchestrator
                var generator = new GrooveBasedDrumGenerator(agent, agent);

                // Generate using proper groove system integration
                return generator.Generate(songContext);
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
