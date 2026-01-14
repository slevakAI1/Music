// AI: purpose=Utility for preventing note overlap in generated tracks by trimming existing notes of same pitch.
// AI: Extracted from duplicate code in BassTrackGenerator, GuitarTrackGenerator, KeysTrackGenerator, DrumTrackGenerator.

using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// Helper for preventing note overlap across multiple generators.
    /// </summary>
    internal static class NoteOverlapHelper
    {
        /// <summary>
        /// Trims previous notes of the same pitch that would extend past the new note-on time.
        /// </summary>
        /// <param name="existingNotes">List of existing notes to check and modify</param>
        /// <param name="midiNote">The MIDI note number of the new note</param>
        /// <param name="noteStart">The start time (in ticks) of the new note</param>
        public static void PreventOverlap(List<PartTrackEvent> existingNotes, int midiNote, long noteStart)
        {
            for (int j = 0; j < existingNotes.Count; j++)
            {
                var existing = existingNotes[j];
                if (existing.Type != PartTrackEventType.NoteOn)
                    continue;

                if (existing.NoteNumber != midiNote)
                    continue;

                long existingStart = existing.AbsoluteTimeTicks;
                long existingEnd = existingStart + existing.NoteDurationTicks;

                if (existingEnd > noteStart && existingStart < noteStart)
                {
                    // Desired end is just before the new note starts
                    long desiredEnd = noteStart - 1;
                    int newDuration = (int)Math.Max(1, desiredEnd - existingStart);
                    existing.NoteDurationTicks = newDuration;
                }
            }
        }
    }
}
