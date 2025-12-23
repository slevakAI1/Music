using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Helper to convert lists of MetaMidiEvent objects to PartTrack objects.
    /// </summary>
    internal static class ConvertMetaMidiEventsToSongTracks
    {
        /// <summary>
        /// Converts lists of MetaMidiEvent objects to PartTrack objects.
        /// Splits tracks by program changes - each program change segment becomes a separate songTrack.
        /// </summary>
        /// <param name="midiEventLists">Lists of MetaMidiEvent objects, one per track</param>
        /// <param name="midiInstruments">Available MIDI instruments for name lookup</param>
        /// <param name="sourceTicksPerQuarterNote">The ticks per quarter note from the source MIDI file (default 480)</param>
        public static List<PartTrack> Convert(
            List<List<MetaMidiEvent>> midiEventLists,
            short sourceTicksPerQuarterNote)
        {
            var songTracks = new List<PartTrack>();

            foreach (var midiEventList in midiEventLists)
            {
                // Split track by program changes
                var segmentedEvents = SplitByProgramChanges(midiEventList);

                foreach (var segment in segmentedEvents)
                {
                    var songTrackNoteEvents = new List<PartTrackNoteEvent>();
                    var songTrack = new PartTrack(songTrackNoteEvents);

                    // Get instrument info from this segment's program change
                    var programChangeEvent = segment.Events
                        .FirstOrDefault(e => e.Type == MidiEventType.ProgramChange);

                    // Determine if this is a drum track (channel 10/9)
                    bool isDrumTrack = segment.Events.Any(e => 
                        e.Parameters.TryGetValue("Channel", out var ch) &&
                        System.Convert.ToInt32(ch) == 9);

                    if (isDrumTrack)
                    {
                        // Drums use channel 10 (index 9) and don't have program changes
                        songTrack.MidiProgramNumber = 255; // Sentinel value for drums
                        songTrack.MidiProgramName = "Drum Set";
                    }
                    else if (programChangeEvent != null &&
                             programChangeEvent.Parameters.TryGetValue("Program", out var programObj))
                    {
                        int programNumber = System.Convert.ToInt32(programObj);
                        songTrack.MidiProgramNumber = programNumber;

                        var instrument = MidiVoices.MidiVoiceList()
                            .FirstOrDefault(i => i.ProgramNumber == programNumber);
                        songTrack.MidiProgramName = instrument?.Name ?? $"Program {programNumber}";
                    }
                    else
                    {
                        // No program change found - use default
                        songTrack.MidiProgramNumber = 0;
                        songTrack.MidiProgramName = "Acoustic Grand Piano";
                    }

                    // Calculate tick scaling factor
                    double tickScale = (double)MusicConstants.TicksPerQuarterNote / sourceTicksPerQuarterNote;

                    // Process note events
                    var noteOnEvents = new Dictionary<int, MetaMidiEvent>();

                    foreach (var midiEvent in segment.Events.OrderBy(e => e.AbsoluteTimeTicks))
                    {
                        if (midiEvent.Type == MidiEventType.NoteOn)
                        {
                            if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                                !midiEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                                continue;

                            int noteNumber = System.Convert.ToInt32(noteNumObj);
                            int velocity = System.Convert.ToInt32(velocityObj);

                            if (velocity == 0)
                            {
                                if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                                {
                                    CreateSongTrackNoteFromPair(noteOnEvent, midiEvent, songTrackNoteEvents, tickScale);
                                    noteOnEvents.Remove(noteNumber);
                                }
                            }
                            else
                            {
                                noteOnEvents[noteNumber] = midiEvent;
                            }
                        }
                        else if (midiEvent.Type == MidiEventType.NoteOff)
                        {
                            if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj))
                                continue;

                            int noteNumber = System.Convert.ToInt32(noteNumObj);

                            if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                            {
                                CreateSongTrackNoteFromPair(noteOnEvent, midiEvent, songTrackNoteEvents, tickScale);
                                noteOnEvents.Remove(noteNumber);
                            }
                        }
                    }

                    // Only add songTrack if it has notes
                    if (songTrackNoteEvents.Count > 0)
                    {
                        songTracks.Add(songTrack);
                    }
                }
            }

            return songTracks;
        }

        /// <summary>
        /// Splits a list of MIDI events by program changes.
        /// Each segment contains events from one program change to the next.
        /// </summary>
        private static List<EventSegment> SplitByProgramChanges(List<MetaMidiEvent> events)
        {
            var segments = new List<EventSegment>();
            var currentSegment = new EventSegment();

            foreach (var evt in events)
            {
                if (evt.Type == MidiEventType.ProgramChange && currentSegment.Events.Any())
                {
                    // Start new segment when we encounter a program change
                    // (but only if current segment has events)
                    segments.Add(currentSegment);
                    currentSegment = new EventSegment();
                }

                currentSegment.Events.Add(evt);
            }

            // Add final segment
            if (currentSegment.Events.Any())
            {
                segments.Add(currentSegment);
            }

            // If no program changes were found, return single segment with all events
            if (segments.Count == 0)
            {
                segments.Add(new EventSegment { Events = events });
            }

            return segments;
        }

        /// <summary>
        /// Helper class to group events by program change segments
        /// </summary>
        private class EventSegment
        {
            public List<MetaMidiEvent> Events { get; set; } = new List<MetaMidiEvent>();
        }

        /// <summary>
        /// Creates a songTrackNoteEvent from a NoteOn/NoteOff event pair.
        /// </summary>
        private static void CreateSongTrackNoteFromPair(
            MetaMidiEvent noteOnEvent,
            MetaMidiEvent noteOffEvent,
            List<PartTrackNoteEvent> songTrackNotes,
            double tickScale)
        {
            if (!noteOnEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                !noteOnEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                return;

            int noteNumber = System.Convert.ToInt32(noteNumObj);
            int velocity = System.Convert.ToInt32(velocityObj);
            
            int absolutePositionTicks = (int)Math.Round(noteOnEvent.AbsoluteTimeTicks * tickScale);
            int noteDurationTicks = (int)Math.Round((noteOffEvent.AbsoluteTimeTicks - noteOnEvent.AbsoluteTimeTicks) * tickScale);

            if (noteDurationTicks < 1)
                noteDurationTicks = 1;

            var songTrackNoteEvent = new PartTrackNoteEvent(
                noteNumber,
                absolutePositionTicks,
                noteDurationTicks,
                velocity,
                isRest: false);

            songTrackNotes.Add(songTrackNoteEvent);
        }
    }
}