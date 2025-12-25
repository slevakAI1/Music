using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Helper to convert lists of MetaMidiEvent objects to PartTrack objects.
    /// </summary>
    internal static class UpdatePartTracks_For_Import_Only
    {
        /// <summary>
        /// Converts lists of MetaMidiEvent objects to PartTrack objects.
        /// Splits tracks by program changes - each program change segment becomes a separate updatedPartTrack.
        /// </summary>
        /// <param name="partTracks"></param>
        /// <param name="midiInstruments">Available MIDI instruments for name lookup</param>
        /// <param name="sourceTicksPerQuarterNote">The ticks per quarter note from the source MIDI file (default 480)</param>
        public static List<PartTrack> Convert(
            List<Generator.PartTrack> partTracks,
            short sourceTicksPerQuarterNote)
        {
            var updatedPartTracks = new List<PartTrack>();

            foreach (var partTrack in partTracks)
            {
                // Split track by program changes
                var segmentedEvents = SplitByProgramChanges(partTrack.PartTrackNoteEvents);

                foreach (var segment in segmentedEvents)
                {
                    var partTrackEvents = new List<PartTrackEvent>();
                    var updatedPartTrack = new PartTrack(partTrackEvents);

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
                        updatedPartTrack.MidiProgramNumber = 255; // Sentinel value for drums
                        updatedPartTrack.MidiProgramName = "Drum Set";
                    }
                    else if (programChangeEvent != null &&
                             programChangeEvent.Parameters.TryGetValue("Program", out var programObj))
                    {
                        int programNumber = System.Convert.ToInt32(programObj);
                        updatedPartTrack.MidiProgramNumber = programNumber;

                        var instrument = MidiVoices.MidiVoiceList()
                            .FirstOrDefault(i => i.ProgramNumber == programNumber);
                        updatedPartTrack.MidiProgramName = instrument?.Name ?? $"Program {programNumber}";
                    }
                    else
                    {
                        // No program change found - use default
                        updatedPartTrack.MidiProgramNumber = 0;
                        updatedPartTrack.MidiProgramName = "Acoustic Grand Piano";
                    }

                    // Calculate tick scaling factor
                    double tickScale = (double)MusicConstants.TicksPerQuarterNote / sourceTicksPerQuarterNote;

                    // Process note events
                    var noteOnEvents = new Dictionary<int, PartTrackEvent>();

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
                                    CreatePartTrackEventFromPair(noteOnEvent, midiEvent, partTrackEvents, tickScale);
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
                                CreatePartTrackEventFromPair(noteOnEvent, midiEvent, partTrackEvents, tickScale);
                                noteOnEvents.Remove(noteNumber);
                            }
                        }
                    }

                    // Only add updatedPartTrack if it has notes
                    if (partTrackEvents.Count > 0)
                    {
                        updatedPartTracks.Add(updatedPartTrack);
                    }
                }
            }

            return updatedPartTracks;
        }

        /// <summary>
        /// Splits a list of MIDI events by program changes.
        /// Each segment contains events from one program change to the next.
        /// </summary>
        private static List<EventSegment> SplitByProgramChanges(List<PartTrackEvent> events)
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
            public List<PartTrackEvent> Events { get; set; } = new List<PartTrackEvent>();
        }

        /// <summary>
        /// Creates a PartTrackEvent from a NoteOn/NoteOff event pair.
        /// </summary>
        private static void CreatePartTrackEventFromPair(
            PartTrackEvent noteOnEvent,
            PartTrackEvent noteOffEvent,
            List<PartTrackEvent> partTrackEvents,  // to do,
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

            var songTrackNoteEvent = new PartTrackEvent(
                noteNumber,
                absolutePositionTicks,
                noteDurationTicks,
                velocity);

            partTrackEvents.Add(songTrackNoteEvent);
        }
    }
}