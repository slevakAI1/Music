// AI: purpose=Generate drum track using DrumGenerator pipeline (Story RF-4) or fallback to anchor-based generation.
// AI: deps=DrumGenerator for pipeline orchestration; DrummerAgent as data source; returns PartTrack sorted by AbsoluteTimeTicks.
// AI: change=Story RF-4: uses DrumGenerator pipeline with DrummerAgent; old anchor-based approach preserved as fallback.

using Music.Generator.Agents.Common;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
using Music.MyMidi;

namespace Music.Generator
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

    // AI: DrumOnset captures a single drum hit; minimal fields for MVP. Strength field added in Story 18 (velocity shaping).
    // AI: invariants=Beat is 1-based within bar; BarNumber is 1-based; Velocity 1-127; TickPosition computed from BarTrack.
    // AI: Story 9 adds protection flags: IsMustHit, IsNeverRemove, IsProtected for enforcement logic.
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

    // AI: DrumBarContext removed in Story G1; replaced by shared Music.Generator.BarContext.
    // AI: change=Use BarContext from Music.Generator namespace for cross-generator bar context.

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
        /// Uses DrummerAgent as data source with PopRock style configuration.
        /// </summary>
        /// <param name="songContext">Song context containing all required data.</param>
        /// <returns>Generated drum PartTrack.</returns>
        /// <exception cref="ArgumentNullException">If songContext is null.</exception>
        /// <remarks>
        /// <para>Architecture (Story RF-4):</para>
        /// <list type="bullet">
        ///   <item>Creates DrummerAgent with PopRock style (data source)</item>
        ///   <item>Creates DrumGenerator (pipeline orchestrator)</item>
        ///   <item>Generates via proper groove system with GrooveSelectionEngine</item>
        ///   <item>Enforces density targets, operator caps, weighted selection</item>
        /// </list>
        /// </remarks>
        public static PartTrack Generate(SongContext songContext)
        {
            ArgumentNullException.ThrowIfNull(songContext);


            //  OK ITS USING AGENT HERE


            // Story RF-4: Use DrumGenerator pipeline with DrummerAgent as data source
            var agent = new DrummerAgent(StyleConfigurationLibrary.PopRock);
            var generator = new DrumGenerator(agent, agent);
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

        // AI: ExtractAnchorOnsets reads kick/snare/hat patterns from GroovePreset anchor layer per bar; returns DrumOnset list with beat positions and default velocity.
        private static List<DrumOnset> ExtractAnchorOnsets(GroovePresetDefinition groovePresetDefinition, int totalBars)
        {
            var allOnsets = new List<DrumOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = groovePresetDefinition.GetActiveGroovePreset(bar);
                var anchorLayer = groovePreset.AnchorLayer;

                foreach (var beat in anchorLayer.KickOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.Kick,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }

                foreach (var beat in anchorLayer.SnareOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.Snare,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }

                foreach (var beat in anchorLayer.HatOnsets)
                {
                    allOnsets.Add(new DrumOnset(
                        Role: DrumRole.ClosedHat,
                        BarNumber: bar,
                        Beat: beat,
                        Velocity: 100,
                        TickPosition: 0));
                }
            }

            return allOnsets;
        }

        // AI: ConvertOnsetsToMidiEvents converts DrumOnset list to PartTrackEvent notes using BarTrack for tick conversion.
        private static void ConvertOnsetsToMidiEvents(List<DrumOnset> onsets, BarTrack barTrack, List<PartTrackEvent> notes)
        {
            if (onsets == null) return;
            if (barTrack == null) throw new ArgumentNullException(nameof(barTrack));
            if (notes == null) throw new ArgumentNullException(nameof(notes));

            foreach (var onset in onsets)
            {
                long absoluteTick = barTrack.ToTick(onset.BarNumber, onset.Beat);
                int midiNote = GetMidiNoteNumber(onset.Role);

                var noteEvent = new PartTrackEvent(
                    noteNumber: midiNote,
                    absoluteTimeTicks: (int)absoluteTick,
                    noteDurationTicks: 60,
                    noteOnVelocity: onset.Velocity);

                notes.Add(noteEvent);
            }

            notes.Sort((a, b) => a.AbsoluteTimeTicks.CompareTo(b.AbsoluteTimeTicks));
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

        // AI: GetDrumProgramNumber retrieves drum MIDI program from VoiceSet (defaults to 0 for standard GM drums).
        private static int GetDrumProgramNumber(VoiceSet voiceSet)
        {
            // GM drums are on channel 10 (MIDI track 10) and typically use program 0
            // Look for a voice with "Drum" in the name
            var drumVoice = voiceSet.Voices.FirstOrDefault(v =>
                v.VoiceName.Contains("Drum", StringComparison.OrdinalIgnoreCase) ||
                v.GrooveRole == GrooveRoles.DrumKit);

            // GM drums default to program 0 (Standard Kit)
            return drumVoice != null ? 0 : 0;
        }
    }
}
