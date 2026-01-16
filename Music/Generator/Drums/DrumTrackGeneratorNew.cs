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
    // AI: invariants=Beat is 1-based within bar; Velocity 1-127; TickPosition computed from BarTrack.
    public sealed record DrumOnset(
        DrumRole Role,
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

            // Story 1: Scaffold complete - returns empty PartTrack
            // Story 2 will add anchor extraction
            // Story 3 will add MIDI emission

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }

        // AI: GetMidiNoteNumber maps DrumRole to General MIDI note; throws for unknown roles.
        public static int GetMidiNoteNumber(DrumRole role) => role switch
        {
            DrumRole.Kick => KickMidiNote,
            DrumRole.Snare => SnareMidiNote,
            DrumRole.ClosedHat => ClosedHatMidiNote,
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown drum role: {role}")
        };
    }
}
