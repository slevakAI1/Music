using Music.MyMidi;

namespace Music.Writer
{
    public static class MergeMidiEventsByVoice
    {
        /// <summary>
        /// Merges MIDI event lists by instrument and adds tempo and time signature events.
        /// </summary>
        public static List<List<MetaMidiEvent>> Convert(
            List<List<MetaMidiEvent>> midiEventLists,
            Music.Generator.TempoTrack tempoTrack,
            Music.Generator.TimeSignatureTrack timeSignatureTrack)
        {
            if (midiEventLists == null) throw new ArgumentNullException(nameof(midiEventLists));
            if (tempoTrack == null) throw new ArgumentNullException(nameof(tempoTrack));
            if (timeSignatureTrack == null) throw new ArgumentNullException(nameof(timeSignatureTrack));

            // Create tempo events from track
            var tempoEvents = new List<MetaMidiEvent>();
            foreach (var tempoEvent in tempoTrack.Events)
            {
                // Calculate absolute ticks based on start bar (1-based to 0-based conversion)
                var absoluteTicks = (long)(tempoEvent.StartBar - 1) * MusicConstants.TicksPerQuarterNote * 4;
                tempoEvents.Add(MetaMidiEvent.CreateSetTempo(absoluteTicks, bpm: tempoEvent.TempoBpm));
            }

            // Create time signature events from track
            var timeSignatureEvents = new List<MetaMidiEvent>();
            foreach (var tsEvent in timeSignatureTrack.Events)
            {
                // Calculate absolute ticks based on start bar (1-based to 0-based conversion)
                var absoluteTicks = (long)(tsEvent.StartBar - 1) * MusicConstants.TicksPerQuarterNote * 4;
                timeSignatureEvents.Add(MetaMidiEvent.CreateTimeSignature(
                    absoluteTicks,
                    tsEvent.Numerator,
                    tsEvent.Denominator));
            }

            // Merge song track events by program number
            // FIXED: Get program number from each event list, not from individual events
            var grouped = midiEventLists
                .Select((list, index) => new 
                { 
                    ProgramNumber = GetProgramNumberFromList(list), 
                    Events = list.Where(e => e.Type != MidiEventType.SequenceTrackName).ToList() // Skip duplicate track names
                })
                .GroupBy(x => x.ProgramNumber)
                .Select(g => new
                {
                    ProgramNumber = g.Key,
                    Events = g.SelectMany(x => x.Events).ToList()
                })
                .ToList();

            var mergedLists = new List<List<MetaMidiEvent>>();

            foreach (var group in grouped)
            {
                var mergedEvents = new List<MetaMidiEvent>(group.Events);

                // Add tempo and time signature events to the first track only
                if (mergedLists.Count == 0)
                {
                    mergedEvents.AddRange(tempoEvents);
                    mergedEvents.AddRange(timeSignatureEvents);
                }

                // Sort all events by absolute time
                mergedEvents = mergedEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

                // Assign channels
                int channel = mergedLists.Count;
                if (channel >= 16) channel = 15;
                if (group.ProgramNumber == 255) channel = 9; // Drums on channel 10 (index 9)

                foreach (var evt in mergedEvents)
                {
                    if (evt.Type == MidiEventType.NoteOn || 
                        evt.Type == MidiEventType.NoteOff || 
                        evt.Type == MidiEventType.ProgramChange ||
                        evt.Type == MidiEventType.ControlChange)
                    {
                        evt.Parameters["Channel"] = channel;
                    }
                }

                mergedLists.Add(mergedEvents);
            }

            return mergedLists;
        }

        /// <summary>
        /// Gets the program number from an event list by finding the first ProgramChange event.
        /// </summary>
        private static int GetProgramNumberFromList(List<MetaMidiEvent> eventList)
        {
            var programChange = eventList.FirstOrDefault(e => e.Type == MidiEventType.ProgramChange);
            if (programChange != null && programChange.Parameters.TryGetValue("Program", out var program))
            {
                return System.Convert.ToInt32(program);
            }
            return -1;
        }
    }
}
