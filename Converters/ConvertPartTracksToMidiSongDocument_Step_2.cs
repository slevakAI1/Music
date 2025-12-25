using Music.MyMidi;

namespace Music.Writer
{
    public static class ConvertPartTracksToMidiSongDocument_Step_2
    {
        /// <summary>
        /// Merges MIDI event lists by instrument and adds tempo and time signature events.
        /// </summary>
        public static List<Generator.PartTrack> Convert(
            List<Generator.PartTrack> partTracks,
            Music.Generator.TempoTrack tempoTrack,
            Music.Generator.TimeSignatureTrack timeSignatureTrack)
        {
            if (partTracks == null) throw new ArgumentNullException(nameof(partTracks));
            if (tempoTrack == null) throw new ArgumentNullException(nameof(tempoTrack));
            if (timeSignatureTrack == null) throw new ArgumentNullException(nameof(timeSignatureTrack));

            // Create tempo events from track
            var tempoEvents = new List<PartTrackEvent>();
            foreach (var tempoEvent in tempoTrack.Events)
            {
                // Calculate absolute ticks based on start bar (1-based to 0-based conversion)
                var absoluteTicks = (long)(tempoEvent.StartBar - 1) * MusicConstants.TicksPerQuarterNote * 4;
                tempoEvents.Add(PartTrackEvent.CreateSetTempo(absoluteTicks, bpm: tempoEvent.TempoBpm));
            }

            // Create time signature events from track
            var timeSignatureEvents = new List<PartTrackEvent>();
            foreach (var tsEvent in timeSignatureTrack.Events)
            {
                // Calculate absolute ticks based on start bar (1-based to 0-based conversion)
                var absoluteTicks = (long)(tsEvent.StartBar - 1) * MusicConstants.TicksPerQuarterNote * 4;
                timeSignatureEvents.Add(PartTrackEvent.CreateTimeSignature(
                    absoluteTicks,
                    tsEvent.Numerator,
                    tsEvent.Denominator));
            }

            // Merge song track events by program number
            var grouped = partTracks
                .Select((track, index) => new 
                { 
                    ProgramNumber = track.MidiProgramNumber, 
                    Events = track.PartTrackNoteEvents.Where(e => e.Type != MidiEventType.SequenceTrackName).ToList() // Skip duplicate track names
                })
                .GroupBy(x => x.ProgramNumber)
                .Select(g => new
                {
                    ProgramNumber = g.Key,
                    Events = g.SelectMany(x => x.Events).ToList()
                })
                .ToList();

            var mergedTracks = new List<Generator.PartTrack>();

            foreach (var group in grouped)
            {
                var mergedEvents = new List<PartTrackEvent>(group.Events);

                // Add tempo and time signature events to the first track only
                if (mergedTracks.Count == 0)
                {
                    mergedEvents.AddRange(tempoEvents);
                    mergedEvents.AddRange(timeSignatureEvents);
                }

                // Sort all events by absolute time
                mergedEvents = mergedEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

                // Assign channels
                int channel = mergedTracks.Count;
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

                var mergedTrack = new Generator.PartTrack(mergedEvents)
                {
                    MidiProgramNumber = group.ProgramNumber
                };
                mergedTracks.Add(mergedTrack);
            }

            return mergedTracks;
        }
    }
}
