// AI: purpose=Create a PartTrack of repeating MIDI note events for tests/fixtures; tick-based timing starting at 0.
// AI: invariants=Events are contiguous, start at tick 0, each event duration==noteDurationTicks; no overlaps by construction.
// AI: deps=Emits Music.MyMidi.PartTrack/PartTrackEvent; changing event ctor or PartTrack ordering breaks consumers.
// AI: constraints=noteNumber 0-127; noteDurationTicks should be >0; repeatCount>=0; noteOnVelocity 0-127; negative repeatCount -> 0 events.
// AI: perf=Allocates list of repeatCount events; avoid huge repeatCount in production.

using Music.MyMidi;

namespace Music.Generator
{
    // AI: contract=Produces a PartTrack of ordered PartTrackEvent starting at tick 0; ordering and event ctor are external contracts
    public static class CreateRepeatingNotes
    {
        // AI: params=noteNumber 0..127; noteDurationTicks>0; repeatCount>=0; noteOnVelocity 0..127; returns ordered PartTrack
        public static PartTrack Execute(
            int noteNumber,
            int noteDurationTicks,
            int repeatCount,
            int noteOnVelocity = 100)
        {
            var SongTrackNoteEvents = new List<PartTrackEvent>();
            int currentPosition = 0;

            for (int i = 0; i < repeatCount; i++)
            {
                var songTrackNoteEvent = new PartTrackEvent(
                    noteNumber: noteNumber,
                    absoluteTimeTicks: currentPosition,
                    noteDurationTicks: noteDurationTicks,
                    noteOnVelocity: noteOnVelocity);

                SongTrackNoteEvents.Add(songTrackNoteEvent);
                currentPosition += noteDurationTicks;
            }

            var songTrack = new PartTrack(SongTrackNoteEvents);
            return songTrack;
        }
    }
}
