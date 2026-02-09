// AI: purpose=Trim prior same-pitch notes so new note-ons do not produce overlapping durations.
// AI: invariants=Mutates notes in-place; only touches NoteOn events; ensures NoteDurationTicks>=1.
// AI: deps=Relies on PartTrackEvent.AbsoluteTimeTicks and NoteDurationTicks semantics from Music.MyMidi.
// AI: perf=O(n) over notes; avoid very large lists in hot paths.
using Music.MyMidi;

namespace Music.Generator
{
    // AI: purpose=Prevent overlap by trimming prior notes of same pitch that extend into newNoteStartTick
    internal static class NoteOverlapPreventer
    {
        // AI: behavior=Shorten existing NoteOn of same pitch so it ends before newNoteStartTick; min duration 1
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

                if (existingEnd > newNoteStartTick && existingStart < newNoteStartTick)
                {
                    // Trim the existing note to end just before the new note starts (min duration 1)
                    long desiredEnd = newNoteStartTick - 1;
                    int newDuration = (int)Math.Max(1, desiredEnd - existingStart);
                    existing.NoteDurationTicks = newDuration;
                }
            }
        }
    }
}
