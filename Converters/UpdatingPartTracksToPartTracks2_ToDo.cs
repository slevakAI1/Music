using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Converts PartTrack objects to PartTrack objects with MetaMidiEvent objects with absolute time positioning.
    /// This is stage 1 processing - creates NoteOn, NoteOff, and SequenceTrackName events only.
    /// Channel assignment and other processing happens in later stages.
    /// </summary>
    public static class UpdatingPartTracksToPartTracks2_ToDo
    {
        /// <summary>
        /// Converts a list of songTracks to PartTrack objects with MIDI events (one PartTrack per input).
        /// Each songTrack is processed independently with its own event list.
        /// </summary>
        /// <param name="songTracks">List of songTracks to convert</param>
        /// <param name="ticksPerQuarterNote">MIDI time resolution (default 480 ticks per quarter note)</param>
        /// <returns>List of PartTrack objects with populated events, one per input song track</returns>
        public static List<PartTrack> Convert(
            List<PartTrack> songTracks)
        {
            if (songTracks == null)
                throw new ArgumentNullException(nameof(songTracks));

            var result = new List<PartTrack>();
            foreach (var songTrack in songTracks)
            {
                var events = ConvertSingleSongTrack(songTrack);
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
        /// Converts a single songTrack to a list of MIDI events with absolute time positioning.
        /// </summary>
        private static List<PartTrackEvent> ConvertSingleSongTrack(PartTrack songTrack)
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

            // Process each note in the songTrack
            foreach (var songTrackNoteEvent in songTrack.PartTrackNoteEvents ?? Enumerable.Empty<PartTrackEvent>())
                    ProcessSingleNote(events, songTrackNoteEvent);

            return events;
        }

        /// <summary>
        /// Processes a single note event.
        /// </summary>
        private static void ProcessSingleNote(List<PartTrackEvent> events, PartTrackEvent songTrackNoteEvent)
        {
            // Create NoteOn event at the note's absolute position
            var noteOnEvent = PartTrackEvent.CreateNoteOn(
                songTrackNoteEvent.AbsolutePositionTicks, 
                0, 
                songTrackNoteEvent.NoteNumber, 
                songTrackNoteEvent.NoteOnVelocity);
            noteOnEvent.Parameters.Remove("Channel");
            events.Add(noteOnEvent);

            // Create NoteOff event at absolute position + duration
            long noteOffTime = songTrackNoteEvent.AbsolutePositionTicks + songTrackNoteEvent.NoteDurationTicks;
            var noteOffEvent = PartTrackEvent.CreateNoteOff(noteOffTime, 0, songTrackNoteEvent.NoteNumber, 0);
            noteOffEvent.Parameters.Remove("Channel");
            events.Add(noteOffEvent);
        }

        /// <summary>
        /// Calculates MIDI note number from note properties.
        /// </summary>
        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
            var baseNote = char.ToUpper(step) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            return (octave + 1) * 12 + baseNote + alter;
        }
    }
}
