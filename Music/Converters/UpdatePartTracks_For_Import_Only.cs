using Music.Generator;
using Music.MyMidi;

// AI: purpose=normalize imported PartTrack event lists into note-centric PartTrack(s); split by program changes
// AI: invariants=Program 255 == drum sentinel; drum channel index==9; tickScale = MusicConstants.TicksPerQuarterNote/sourceTicksPerQuarterNote
// AI: pairing=velocity==0 treated as NoteOff; unmatched pairs ignored; preserve temporal order via AbsoluteTimeTicks
// AI: rounding=min duration 1 tick; rounding uses Math.Round on scaled values; do not change rounding logic

namespace Music.Writer
{
    // AI: internal helper used only during import; outputs one or more PartTracks per input track after splitting by program
    internal static class UpdatePartTracks_For_Import_Only
    {
        // AI: Convert: splits input track events by ProgramChange segments, maps program->name, rescales ticks, pairs NoteOn/NoteOff
        public static List<PartTrack> Convert(
            List<PartTrack> partTracks,
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
                        .FirstOrDefault(e => e.Type == PartTrackEventType.ProgramChange);

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
                        if (midiEvent.Type == PartTrackEventType.NoteOn)
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
                                    CreatePartTrackEventFromPair(noteOnEvent, midiEvent, updatedPartTrack, tickScale);
                                    noteOnEvents.Remove(noteNumber);
                                }
                            }
                            else
                            {
                                noteOnEvents[noteNumber] = midiEvent;
                            }
                        }
                        else if (midiEvent.Type == PartTrackEventType.NoteOff)
                        {
                            if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj))
                                continue;

                            int noteNumber = System.Convert.ToInt32(noteNumObj);

                            if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                            {
                                CreatePartTrackEventFromPair(noteOnEvent, midiEvent, updatedPartTrack, tickScale);
                                noteOnEvents.Remove(noteNumber);
                            }
                        }
                    }

                    // Only add updatedPartTrack if it has notes
                    if (updatedPartTrack.PartTrackNoteEvents.Count > 0)
                    {
                        // Sort by time after pairing; drum tracks may pair out of order
                        updatedPartTrack.PartTrackNoteEvents.Sort(
                            (a, b) => a.AbsoluteTimeTicks.CompareTo(b.AbsoluteTimeTicks));
                        updatedPartTracks.Add(updatedPartTrack);
                    }
                }
            }

            return updatedPartTracks;
        }

        // AI: SplitByProgramChanges: segments start at first event and split whenever a ProgramChange occurs and current segment not empty
        // AI: If no program changes found, returns single segment containing original events
        private static List<EventSegment> SplitByProgramChanges(List<PartTrackEvent> events)
        {
            var segments = new List<EventSegment>();
            var currentSegment = new EventSegment();

            foreach (var evt in events)
            {
                if (evt.Type == PartTrackEventType.ProgramChange && currentSegment.Events.Any())
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

        // AI: simple container for event segments; keep mutable list for downstream processing
        private class EventSegment
        {
            public List<PartTrackEvent> Events { get; set; } = new List<PartTrackEvent>();
        }

        // AI: CreatePartTrackEventFromPair: scales times by tickScale, rounds, clamps duration>=1, and appends PartTrackEvent
        // AI: rounding uses Math.Round for both start and duration; small negative durations clamped to 1
        private static void CreatePartTrackEventFromPair(
            PartTrackEvent noteOnEvent,
            PartTrackEvent noteOffEvent,
            PartTrack targetPartTrack,
            double tickScale)
        {
            if (!noteOnEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                !noteOnEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                return;

            int noteNumber = System.Convert.ToInt32(noteNumObj);
            int velocity = System.Convert.ToInt32(velocityObj);
            
            int absoluteTimeTicks = (int)Math.Round(noteOnEvent.AbsoluteTimeTicks * tickScale);
            int noteDurationTicks = (int)Math.Round((noteOffEvent.AbsoluteTimeTicks - noteOnEvent.AbsoluteTimeTicks) * tickScale);

            if (noteDurationTicks < 1)
                noteDurationTicks = 1;

            var songTrackNoteEvent = new PartTrackEvent(
                noteNumber,
                absoluteTimeTicks,
                noteDurationTicks,
                velocity);

            targetPartTrack.PartTrackNoteEvents.Add(songTrackNoteEvent);
        }
    }
}
