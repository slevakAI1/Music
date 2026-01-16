// AI: purpose=Generate drum track from GroovePresetDefinition anchor patterns; MVP for Story 1 of GroovePlan.
// AI: deps=GrooveTrack for preset lookup; BarTrack for tick conversion; returns PartTrack sorted by AbsoluteTimeTicks.
// AI: change=Phase 1 MVP; subsequent stories add velocity shaping, timing, variations per GroovePlan.md.

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
    public sealed record DrumOnset(
        DrumRole Role,
        int BarNumber,
        decimal Beat,
        int Velocity,
        long TickPosition);

    internal static class DrumTrackGeneratorNew
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
        /// Generates drum track from groove preset anchor patterns.
        /// MVP: extracts anchor onsets and emits MIDI events with default velocity.
        /// </summary>
        public static PartTrack Generate(
            HarmonyTrack harmonyTrack,
            GrooveTrack grooveTrack,
            BarTrack barTrack,
            SectionTrack sectionTrack,
            int totalBars,
            int midiProgramNumber)
        {
            var notes = new List<PartTrackEvent>();

            // Story 2: Extract anchor patterns from GroovePreset per bar
            var allOnsets = ExtractAnchorOnsets(grooveTrack, totalBars);

            // Story 3: Convert onsets to MIDI events
            ConvertOnsetsToMidiEvents(allOnsets, barTrack, notes);

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: ExtractAnchorOnsets reads kick/snare/hat patterns from GroovePreset anchor layer per bar; returns DrumOnset list with beat positions and default velocity.
        private static List<DrumOnset> ExtractAnchorOnsets(GrooveTrack grooveTrack, int totalBars)
        {
            var allOnsets = new List<DrumOnset>();

            for (int bar = 1; bar <= totalBars; bar++)
            {
                var groovePreset = grooveTrack.GetActiveGroovePreset(bar);
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
    }
}
