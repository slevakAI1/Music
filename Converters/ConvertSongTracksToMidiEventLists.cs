using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Converts SongTrack objects to lists of MetaMidiEvent objects with absolute time positioning.
    /// This is stage 1 processing - creates NoteOn, NoteOff, and SequenceTrackName events only.
    /// Channel assignment and other processing happens in later stages.
    /// </summary>
    public static class ConvertSongTracksToMidiEventLists
    {
        /// <summary>
        /// Converts a list of songTracks to lists of MIDI events (one list per songTrack).
        /// Each songTrack is processed independently with its own event list.
        /// </summary>
        /// <param name="songTracks">List of songTracks to convert</param>
        /// <param name="ticksPerQuarterNote">MIDI time resolution (default 480 ticks per quarter note)</param>
        /// <returns>List of MetaMidiEvent lists, one per input song track</returns>
        public static List<List<MetaMidiEvent>> Convert(
            List<SongTrack> songTracks)
        {
            if (songTracks == null)
                throw new ArgumentNullException(nameof(songTracks));

            var result = new List<List<MetaMidiEvent>>();
            foreach (var songTrack in songTracks)
            {
                result.Add(ConvertSingleSongTrack(songTrack));
            }

            return result;
        }

        /// <summary>
        /// Converts a single songTrack to a list of MIDI events with absolute time positioning.
        /// </summary>
        private static List<MetaMidiEvent> ConvertSingleSongTrack(SongTrack songTrack)
        {
            var events = new List<MetaMidiEvent>();

            // Add track name event at the beginning (using instrument name)
            var trackName = string.IsNullOrWhiteSpace(songTrack.MidiProgramName) 
                ? "Unnamed Track" 
                : songTrack.MidiProgramName;
            events.Add(MetaMidiEvent.CreateSequenceTrackName(0, trackName));

            // Add program change event at the beginning to set the instrument
            // Channel is null and will be assigned in Phase 2
            var programChangeEvent = MetaMidiEvent.CreateProgramChange(0, 0, songTrack.MidiProgramNumber);
            // Remove the channel parameter temporarily (will be assigned in Phase 2)
            programChangeEvent.Parameters.Remove("Channel");
            events.Add(programChangeEvent);

            // Process each note in the songTrack
            foreach (var songTrackNoteEvent in songTrack.SongTrackNoteEvents ?? Enumerable.Empty<SongTrackNoteEvent>())
            {
                if (songTrackNoteEvent.IsRest)
                {
                    // Rests don't create MIDI events, timing is already handled by AbsolutePositionTicks
                    continue;
                }

                // Check if this note is part of a chord that needs expansion
                if (songTrackNoteEvent.songTrackChord != null && songTrackNoteEvent.songTrackChord.IsChord)
                {
                    ProcessChord(events, songTrackNoteEvent);
                }
                else
                {
                    ProcessSingleNote(events, songTrackNoteEvent);
                }
            }

            return events;
        }

        /// <summary>
        /// Processes a chord note by expanding it to individual notes using ConvertHarmonyEventToListOfPartNoteEvents.
        /// </summary>
        private static void ProcessChord(List<MetaMidiEvent> events, SongTrackNoteEvent songTrackNoteEvent)
        {
            var chord = songTrackNoteEvent.songTrackChord!;

            // Use ConvertHarmonyEventToListOfPartNoteEvents to generate individual chord notes
            var chordNotes = ConvertHarmonyEventToListOfPartNoteEvents.Convert(
                chord.ChordKey!,
                chord.ChordDegree!.Value,
                chord.ChordQuality!,
                chord.ChordBase!,
                baseOctave: songTrackNoteEvent.Octave,
                noteValue: songTrackNoteEvent.Duration);

            // Create NoteOn and NoteOff events for all chord notes
            foreach (var cn in chordNotes)
            {
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                // NoteOn at the songTrack note's absolute position
                var noteOnEvent = MetaMidiEvent.CreateNoteOn(
                    songTrackNoteEvent.AbsolutePositionTicks, 
                    0, 
                    noteNumber, 
                    songTrackNoteEvent.NoteOnVelocity);
                noteOnEvent.Parameters.Remove("Channel");
                events.Add(noteOnEvent);

                // NoteOff at absolute position + duration
                long noteOffTime = songTrackNoteEvent.AbsolutePositionTicks + songTrackNoteEvent.NoteDurationTicks;
                var noteOffEvent = MetaMidiEvent.CreateNoteOff(noteOffTime, 0, noteNumber, 0);
                noteOffEvent.Parameters.Remove("Channel");
                events.Add(noteOffEvent);
            }
        }

        /// <summary>
        /// Processes a single note event.
        /// </summary>
        private static void ProcessSingleNote(List<MetaMidiEvent> events, SongTrackNoteEvent songTrackNoteEvent)
        {
            // Create NoteOn event at the note's absolute position
            var noteOnEvent = MetaMidiEvent.CreateNoteOn(
                songTrackNoteEvent.AbsolutePositionTicks, 
                0, 
                songTrackNoteEvent.NoteNumber, 
                songTrackNoteEvent.NoteOnVelocity);
            noteOnEvent.Parameters.Remove("Channel");
            events.Add(noteOnEvent);

            // Create NoteOff event at absolute position + duration
            long noteOffTime = songTrackNoteEvent.AbsolutePositionTicks + songTrackNoteEvent.NoteDurationTicks;
            var noteOffEvent = MetaMidiEvent.CreateNoteOff(noteOffTime, 0, songTrackNoteEvent.NoteNumber, 0);
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
