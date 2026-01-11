// AI: purpose=Prevent overlapping notes by trimming previous notes of the same pitch that would extend past a new note-on event.
// AI: used by track generators (Bass, Guitar, Keys, Drums) to avoid MIDI note overlap artifacts.

using Music.MyMidi;

namespace Music.Generator
{
    /// <summary>
    /// Prevents overlapping MIDI notes by trimming previous notes that would extend past new note-on events.
    /// </summary>
    internal static class NoteOverlapPreventer
    {
        /// <summary>
        /// Trims previous notes of the same pitch that would overlap with a new note starting at the specified time.
        /// </summary>
        /// <param name="notes">List of existing notes to check and potentially modify.</param>
        /// <param name="midiNoteNumber">The MIDI note number of the new note being added.</param>
        /// <param name="newNoteStartTick">The absolute tick time when the new note starts.</param>
        public static void TrimOverlappingNotes(List<PartTrackEvent> notes, int midiNoteNumber, long newNoteStartTick)
        {
            for (int j = 0; j < notes.Count; j++)
            {
                var existing = notes[j];
                if (existing.Type != PartTrackEventType.NoteOn)
                    continue;

                if (existing.NoteNumber != midiNoteNumber)
                    continue;

                long existingStart = existing.AbsoluteTimeTicks;
                long existingEnd = existingStart + existing.NoteDurationTicks;

                // If the existing note would overlap with the new note
                if (existingEnd > newNoteStartTick && existingStart < newNoteStartTick)
                {
                    // Trim the existing note to end just before the new note starts
                    long desiredEnd = newNoteStartTick - 1;
                    int newDuration = (int)Math.Max(1, desiredEnd - existingStart);
                    existing.NoteDurationTicks = newDuration;
                }
            }
        }
    }
}
