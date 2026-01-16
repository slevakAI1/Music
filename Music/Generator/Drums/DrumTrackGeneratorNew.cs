// AI: purpose=Generate bass track using BassPatternLibrary for pattern selection and BassChordChangeDetector for approach notes.
// AI: keep MIDI program number 33; patterns replace randomizer for more structured bass lines (Story 5.1 + 5.2); returns sorted by AbsoluteTimeTicks.
// AI: uses fixed approach note probability; no tension/energy variation.

using Music.MyMidi;
using Music.Song.Material;

namespace Music.Generator
{
    internal static class DrumTrackGeneratorNew
    {
        /// <summary>
        /// Generates bass track: pattern-based bass lines with optional approach notes to chord changes.
        /// Uses fixed approach note probability (slot-gated).
        /// Uses MotifRenderer when motif placed for Bass role.
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

            for (int bar = 1; bar <= totalBars; bar++)
            {
                //var groovePreset = grooveTrack.GetActiveGroovePreset(bar);

                // Get section
                var section = (Section?)null;
                sectionTrack.GetActiveSection(bar, out section);


                int barIndexWithinSection = section != null ? (bar - section.StartBar) : 0;
            }

            return new PartTrack(notes) { MidiProgramNumber = midiProgramNumber };
        }
    }

}
