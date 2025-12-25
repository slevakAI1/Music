using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Converts PartTrack objects to PartTrack objects with MetaMidiEvent objects with absolute time positioning.
    /// This is stage 1 processing - creates NoteOn, NoteOff, and SequenceTrackName partTrackEvents only.
    /// Channel assignment and other processing happens in later stages.
    /// </summary>
    public static class ConvertPartTracksToMidiSongDocument_Step_1
    {
        /// <summary>
        /// Converts a list of partTracks to PartTrack objects with MIDI partTrackEvents (one PartTrack per input).
        /// Each songTrack is processed independently with its own event list.
        /// </summary>
        /// <param name="partTracks">List of partTracks to convert</param>
        /// <param name="ticksPerQuarterNote">MIDI time resolution (default 480 ticks per quarter note)</param>
        /// <returns>List of PartTrack objects with populated partTrackEvents, one per input song track</returns>
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

        /// <summary>
        /// Converts a single PartTrack to a list of MIDI partTrackEvents with absolute time positioning.
        /// </summary>
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

        /// <summary>
        /// Processes a single note event.
        /// </summary>
        private static void ProcessSingleNote(List<PartTrackEvent> partTrackEvents, PartTrackEvent partTrackEvent)
        {
            // Create NoteOn event at the note's absolute position
            var noteOnEvent = PartTrackEvent.CreateNoteOn(
                partTrackEvent.AbsolutePositionTicks, 
                0, 
                partTrackEvent.NoteNumber, 
                partTrackEvent.NoteOnVelocity);
            noteOnEvent.Parameters.Remove("Channel");
            partTrackEvents.Add(noteOnEvent);

            // Create NoteOff event at absolute position + duration
            long noteOffTime = partTrackEvent.AbsolutePositionTicks + partTrackEvent.NoteDurationTicks;
            var noteOffEvent = PartTrackEvent.CreateNoteOff(noteOffTime, 0, partTrackEvent.NoteNumber, 0);
            noteOffEvent.Parameters.Remove("Channel");
            partTrackEvents.Add(noteOffEvent);
        }
    }
}
