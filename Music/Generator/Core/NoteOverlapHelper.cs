// AI: purpose=Trim prior same-pitch notes so new note-ons do not produce overlapping note durations.
// AI: invariants=Mutates existingNotes in-place; ensures NoteDurationTicks>=1; checks PartTrackEventType.NoteOn only.
// AI: deps=Relies on Music.MyMidi.PartTrackEvent, PartTrackEventType and AbsoluteTimeTicks semantics.
// AI: perf=O(n) over existingNotes; avoid huge existingNotes lists in hot paths.

using Music.MyMidi;

namespace Music.Generator
{
    // AI: purpose=Prevent overlap by trimming prior notes of same pitch that extend into new noteStart
    internal static class NoteOverlapHelper
    {
        // AI: behavior=For each NoteOn of midiNote, shorten duration so it ends before noteStart; min duration 1
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
