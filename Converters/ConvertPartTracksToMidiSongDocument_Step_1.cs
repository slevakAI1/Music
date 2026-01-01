using Music.Generator;
using Music.MyMidi;

// AI: purpose=stage1: convert PartTrack notes -> absolute-time PartTrackEvent lists (note on/off + program+track name only)
// AI: invariants=output events must retain input timing; Channel removed here and must be assigned in stage2
// AI: deps=PartTrack.PartTrackNoteEvents may be null; PartTrackEvent factory methods used; consumers expect stable order
// AI: perf=O(total notes) per track; avoid changing insertion order or adding sorting here

namespace Music.Writer
{
    // AI: class=stateless stage1 converter; keep API and output event shapes stable for downstream stages
    public static class ConvertPartTracksToMidiSongDocument_Step_1
    {
        // AI: Convert: per-track independent processing; copies MidiProgramName/Number; throws on null input
        public static List<PartTrack> Convert(
            List<PartTrack> partTracks)
        {
            if (partTracks == null)
                throw new ArgumentNullException(nameof(partTracks));

            var result = new List<PartTrack>();
            foreach (var songTrack in partTracks)
            {
                var events = UpdatePartTrack_1(songTrack);
                var newTrack = new PartTrack(events)
                {
                    MidiProgramName = songTrack.MidiProgramName,
                    MidiProgramNumber = songTrack.MidiProgramNumber
                };
                result.Add(newTrack);
            }

            return result;
        }

        // AI: UpdatePartTrack_1: emits track name at t=0 and ProgramChange at t=0 with Channel removed for phase2 assignment
        // AI: empty MidiProgramName -> "Unnamed Track"; iterate PartTrackNoteEvents null-safe
        private static List<PartTrackEvent> UpdatePartTrack_1(PartTrack songTrack)
        {
            var events = new List<PartTrackEvent>();

            // Add track name event at the beginning (using instrument name)
            var trackName = string.IsNullOrWhiteSpace(songTrack.MidiProgramName) 
                ? "Unnamed Track" 
                : songTrack.MidiProgramName;
            events.Add(PartTrackEvent.CreateSequenceTrackName(0, trackName));

            // Add program change event at the beginning to set the instrument
            // Channel is null and will be assigned in Phase 2
            var programChangeEvent = PartTrackEvent.CreateProgramChange(0, 0, songTrack.MidiProgramNumber);
            // Remove the channel parameter temporarily (will be assigned in Phase 2)
            programChangeEvent.Parameters.Remove("Channel");
            events.Add(programChangeEvent);

            // Process each PartTrackEvent (note) in the PartTrack
            foreach (var songTrackNoteEvent in songTrack.PartTrackNoteEvents ?? Enumerable.Empty<PartTrackEvent>())
                    ProcessSingleNote(events, songTrackNoteEvent);

            return events;
        }

        // AI: ProcessSingleNote: create NoteOn at AbsoluteTimeTicks and NoteOff at AbsoluteTime+Duration; both have Channel param removed
        // AI: noteOffTime computed by simple addition using long; do not alter arithmetic or add rounding
        private static void ProcessSingleNote(List<PartTrackEvent> partTrackEvents, PartTrackEvent partTrackEvent)
        {
            // Create NoteOn event at the note's absolute position
            var noteOnEvent = PartTrackEvent.CreateNoteOn(
                partTrackEvent.AbsoluteTimeTicks, 
                0, 
                partTrackEvent.NoteNumber, 
                partTrackEvent.NoteOnVelocity);
            noteOnEvent.Parameters.Remove("Channel");
            partTrackEvents.Add(noteOnEvent);

            // Create NoteOff event at absolute position + duration
            long noteOffTime = partTrackEvent.AbsoluteTimeTicks + partTrackEvent.NoteDurationTicks;
            var noteOffEvent = PartTrackEvent.CreateNoteOff(noteOffTime, 0, partTrackEvent.NoteNumber, 0);
            noteOffEvent.Parameters.Remove("Channel");
            partTrackEvents.Add(noteOffEvent);
        }
    }
}
