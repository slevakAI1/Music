using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Phase 2 of MIDI event conversion:
    /// - Merges MIDI event lists by instrument
    /// - Adds track 0 global events (tempo, time signature)
    /// - Assigns track numbers (track 0 for globals, track 10 for drums, sequential for others)
    /// - Sorts events by absolute time position and event type priority
    /// Delta time calculation happens in Phase 3.
    /// </summary>
    public static class PhrasesToMidiEventsConverter_Phase_2
    {
        private const int GlobalTrack = 0;
        private const int DrumTrack = 10;
        private const byte DrumSetProgramNumber = 255; // Sentinel value for drum sets

        /// <summary>
        /// Converts phase 1 output to phase 2 output with merged, sorted, and track-assigned events.
        /// </summary>
        /// <param name="midiEventLists">Output from Phase 1 - one list per phrase</param>
        /// <param name="tempo">Tempo in BPM for track 0</param>
        /// <param name="timeSignatureNumerator">Time signature numerator for track 0</param>
        /// <param name="timeSignatureDenominator">Time signature denominator for track 0</param>
        /// <returns>List of MIDI event lists - track 0 + one per instrument, sorted by absolute time</returns>
        public static List<List<MidiEvent>> Convert(
            List<List<MidiEvent>> midiEventLists,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            if (midiEventLists == null)
                throw new ArgumentNullException(nameof(midiEventLists));
            if (tempo <= 0)
                throw new ArgumentException("Tempo must be greater than 0", nameof(tempo));
            if (timeSignatureNumerator <= 0)
                throw new ArgumentException("Time signature numerator must be greater than 0", nameof(timeSignatureNumerator));
            if (timeSignatureDenominator <= 0)
                throw new ArgumentException("Time signature denominator must be greater than 0", nameof(timeSignatureDenominator));

            var result = new List<List<MidiEvent>>();

            // Step 1: Create track 0 with global events
            var track0 = CreateGlobalTrack(tempo, timeSignatureNumerator, timeSignatureDenominator);
            result.Add(track0);

            // Step 2: Group events by instrument (based on track name)
            var eventsByInstrument = GroupEventsByInstrument(midiEventLists);

            // Step 3: Assign track numbers and create final track lists
            var tracksByInstrument = AssignTracksAndSort(eventsByInstrument);

            // Step 4: Add all instrument tracks to result
            result.AddRange(tracksByInstrument);

            return result;
        }

        /// <summary>
        /// Creates track 0 with global MIDI events (tempo, time signature, end of track).
        /// </summary>
        private static List<MidiEvent> CreateGlobalTrack(
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            var track0 = new List<MidiEvent>();

            // Add track name event at absolute time 0
            track0.Add(MidiEvent.TrackName(0, "Global Events"));

            // Add tempo event at absolute time 0
            track0.Add(MidiEvent.SetTempoBpm(0, tempo));

            // Add time signature event at absolute time 0
            track0.Add(MidiEvent.TimeSignature(
                0,
                timeSignatureNumerator,
                timeSignatureDenominator));

            // Add end of track event at absolute time 0 (will be adjusted later if needed)
            track0.Add(MidiEvent.EndOfTrack(0));

            // Sort track 0 events
            return SortEventsByPriority(track0);
        }

        /// <summary>
        /// Groups MIDI events by instrument name (extracted from SequenceTrackName event).
        /// Only keeps the first SequenceTrackName event per instrument.
        /// </summary>
        private static Dictionary<string, List<MidiEvent>> GroupEventsByInstrument(
            List<List<MidiEvent>> midiEventLists)
        {
            var eventsByInstrument = new Dictionary<string, List<MidiEvent>>();

            foreach (var eventList in midiEventLists)
            {
                if (eventList == null || eventList.Count == 0)
                    continue;

                // Find the track name (should be first event in the list)
                var trackNameEvent = eventList.FirstOrDefault(e => e.Type == MidiEventType.SequenceTrackName);
                var instrumentName = trackNameEvent?.Text ?? "Unknown Instrument";

                // Initialize list for this instrument if not exists
                if (!eventsByInstrument.ContainsKey(instrumentName))
                {
                    eventsByInstrument[instrumentName] = new List<MidiEvent>();
                    // Add the track name event only once (from the first list for this instrument)
                    if (trackNameEvent != null)
                    {
                        eventsByInstrument[instrumentName].Add(trackNameEvent);
                    }
                }

                // Add all non-track-name events from this list
                eventsByInstrument[instrumentName].AddRange(
                    eventList.Where(e => e.Type != MidiEventType.SequenceTrackName));
            }

            return eventsByInstrument;
        }

        /// <summary>
        /// Assigns track numbers to instruments and sorts events by absolute time and priority.
        /// </summary>
        private static List<List<MidiEvent>> AssignTracksAndSort(
            Dictionary<string, List<MidiEvent>> eventsByInstrument)
        {
            var result = new List<List<MidiEvent>>();
            var nextAvailableTrack = 1; // Track 0 is reserved for globals

            // Separate drums from other instruments
            var drumEvents = eventsByInstrument
                .Where(kvp => IsDrumSet(kvp.Key))
                .ToList();

            var melodicEvents = eventsByInstrument
                .Where(kvp => !IsDrumSet(kvp.Key))
                .ToList();

            // Process melodic instruments first (assign tracks 1, 2, 3, ... skipping 10)
            foreach (var kvp in melodicEvents.OrderBy(x => x.Key))
            {
                var instrumentName = kvp.Key;
                var events = kvp.Value;

                // Skip track 10 (reserved for drums)
                if (nextAvailableTrack == DrumTrack)
                    nextAvailableTrack++;

                // Sort events by absolute time and priority
                var sortedEvents = SortEventsByPriority(events);

                result.Add(sortedEvents);
                nextAvailableTrack++;
            }

            // Process drum sets (assign to track 10)
            foreach (var kvp in drumEvents)
            {
                var instrumentName = kvp.Key;
                var events = kvp.Value;

                // Sort events by absolute time and priority
                var sortedEvents = SortEventsByPriority(events);

                result.Add(sortedEvents);
            }

            return result;
        }

        /// <summary>
        /// Sorts MIDI events by absolute time, then by event type priority.
        /// Priority order (for events at the same tick):
        /// 1. Meta events (tempo, time signature, markers, track name)
        /// 2. ProgramChange
        /// 3. ControlChange, PitchBend, ChannelPressure, PolyPressure
        /// 4. NoteOff
        /// 5. NoteOn
        /// 6. EndOfTrack (last)
        /// </summary>
        private static List<MidiEvent> SortEventsByPriority(List<MidiEvent> events)
        {
            return events.OrderBy(e => e.AbsoluteTimeTicks)
                         .ThenBy(e => GetEventPriority(e))
                         .ToList();
        }

        /// <summary>
        /// Returns the priority order for event types at the same absolute time.
        /// Lower numbers = higher priority (earlier in sequence).
        /// </summary>
        private static int GetEventPriority(MidiEvent evt)
        {
            return evt.Type switch
            {
                // Meta events first
                MidiEventType.SequenceTrackName => 0,
                MidiEventType.SetTempo => 1,
                MidiEventType.TimeSignature => 2,
                MidiEventType.KeySignature => 3,
                MidiEventType.Marker => 4,
                MidiEventType.Text => 6,
                MidiEventType.Lyric => 7,

                // Program/bank changes
                MidiEventType.ProgramChange => 10,

                // Controllers and modulation
                MidiEventType.ControlChange => 20,
                MidiEventType.PitchBend => 21,
                MidiEventType.ChannelPressure => 22,
                MidiEventType.PolyKeyPressure => 23,

                // Note off before note on (critical for same-pitch re-triggering)
                MidiEventType.NoteOff => 30,
                MidiEventType.NoteOn => 31,

                // SysEx
                MidiEventType.SysEx => 40,

                // End of track last
                MidiEventType.EndOfTrack => 999,

                // Unknown/other events
                _ => 50
            };
        }

        /// <summary>
        /// Determines if an instrument name represents a drum set.
        /// </summary>
        private static bool IsDrumSet(string instrumentName)
        {
            if (string.IsNullOrWhiteSpace(instrumentName))
                return false;

            // Check for common drum set names
            var drumKeywords = new[] { "drum", "drums", "drum set", "percussion", "kit" };
            var lowerName = instrumentName.ToLowerInvariant();

            return drumKeywords.Any(keyword => lowerName.Contains(keyword));
        }
    }
}
