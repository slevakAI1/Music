// AI: purpose=Generate drum track using DrumGenerator pipeline or fallback to anchor-based generation.
// AI: deps=DrumGenerator for pipeline orchestration; candidate source built from operator registry; returns PartTrack sorted by AbsoluteTimeTicks.
// AI: change= uses DrumGenerator pipeline with operator registry + DrummerCandidateSource; old anchor-based approach preserved as fallback.

using Music.Generator.Core;
using Music.Generator.Drums.Operators;
using Music.Generator.Drums.Selection.Candidates;
using Music.Generator.Groove;

namespace Music.Generator.Drums.Generation
{
    // AI: DrumRole identifies drum instrument for MIDI mapping and onset processing; extend for additional kit pieces.
    public enum DrumRole
    {
        Kick,
        Snare,
        ClosedHat,
        OpenHat,
        Ride,
        Crash,
        TomHigh,
        TomMid,
        TomLow
    }

    // AI: DrumOnset captures a single drum hit; minimal fields for MVP. Strength field.
    // AI: invariants=Beat is 1-based within bar; BarNumber is 1-based; Velocity 1-127; TickPosition computed from BarTrack.
    // AI: adds protection flags: IsMustHit, IsNeverRemove, IsProtected for enforcement logic.
    public sealed record DrumOnset(
        DrumRole Role,
        int BarNumber,
        decimal Beat,
        int Velocity,
        long TickPosition)
    {
        public bool IsMustHit { get; set; }
        public bool IsNeverRemove { get; set; }
        public bool IsProtected { get; set; }
    }

    // AI: Bar holds bar context; avoid reintroducing BarContext/DrumBarContext types.

    public static class DrumTrackGenerator
    {
        // AI: MIDI drum note numbers (General MIDI standard); extend mapping as roles are added.
        private const int KickMidiNote = 36;
        private const int SnareMidiNote = 38;
        private const int ClosedHatMidiNote = 42;
        private const int OpenHatMidiNote = 46;
        private const int RideMidiNote = 51;
        private const int CrashMidiNote = 49;
        private const int TomHighMidiNote = 50;
        private const int TomMidMidiNote = 47;
        private const int TomLowMidiNote = 45;

        /// <summary>
        /// Generates drum track using DrumGenerator pipeline (Story RF-4).
        /// Uses operator registry + DrummerCandidateSource with PopRock style configuration.
        /// </summary>
        /// <param name="songContext">Song context containing all required data.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <exception cref="ArgumentNullException">If songContext is null.</exception>
        /// <remarks>
        /// <para>Architecture (Story RF-4):</para>
        /// <list type="bullet">
        ///   <item>Builds operator registry + DrummerCandidateSource with PopRock style</item>
        ///   <item>Creates DrumGenerator (pipeline orchestrator)</item>
        ///   <item>Generates via proper groove system with GrooveSelectionEngine</item>
        ///   <item>Enforces density targets, operator caps, weighted selection</item>
        /// </list>
        /// </remarks>
        public static PartTrack Generate(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);


            //  OK ITS USING AGENT HERE


            // Story RF-4: Use DrumGenerator pipeline with operator registry as data source
            var registry = DrumOperatorRegistryBuilder.BuildComplete();
            var candidateSource = new DrummerCandidateSource(
                registry,
                diagnosticsCollector: null,
                settings: null);
            var generator = new DrumPhraseGenerator(candidateSource);
            return generator.Generate(songContext);
        }

        /// <summary>
        /// Original Generate method signature preserved for backward compatibility.
        /// Builds SongContext and uses DrumGenerator pipeline.
        /// Story RF-4: Updated to use new pipeline architecture.
        /// </summary>
        public static PartTrack Generate(
            BarTrack barTrack,
            SectionTrack sectionTrack,
            IReadOnlyList<object>? segmentProfiles,
            GroovePresetDefinition groovePresetDefinition,
            int totalBars,
            int midiProgramNumber)
        {
            // Build minimal SongContext for pipeline
            var songContext = new SongContext
            {
                BarTrack = barTrack,
                SectionTrack = sectionTrack,
                GroovePresetDefinition = groovePresetDefinition
            };

            // Use new pipeline entry point
            return Generate(songContext);
        }

        // AI: GetMidiNoteNumber maps DrumRole to General MIDI note; throws for unknown roles.
        public static int GetMidiNoteNumber(DrumRole role) => role switch
        {
            DrumRole.Kick => KickMidiNote,
            DrumRole.Snare => SnareMidiNote,
            DrumRole.ClosedHat => ClosedHatMidiNote,
            DrumRole.OpenHat => OpenHatMidiNote,
            DrumRole.Ride => RideMidiNote,
            DrumRole.Crash => CrashMidiNote,
            DrumRole.TomHigh => TomHighMidiNote,
            DrumRole.TomMid => TomMidMidiNote,
            DrumRole.TomLow => TomLowMidiNote,
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown drum role: {role}")
        };
    }
}
