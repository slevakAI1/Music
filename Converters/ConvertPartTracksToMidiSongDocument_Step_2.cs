using Music.Generator;
using Music.MyMidi;

// AI: purpose=merge per-track events by program and inject tempo/time-signature; output consumed by Step_3 to build MidiSongDocument
// AI: invariants=absolute timing preserved; tempo/time-signature absoluteTicks uses (StartBar-1)*TicksPerQuarterNote*4 (StartBar is 1-based)
// AI: deps=MusicConstants.TicksPerQuarterNote, PartTrackEvent factory; consumers expect Channel param present for note/program/control events
// AI: change=when modifying channel assignment, drum mapping, or tick calc update Step_3 and playback/export tests

namespace Music.Writer
{
    public static class ConvertPartTracksToMidiSongDocument_Step_2
    {
        // AI: Convert: validates inputs; builds tempo & time-sig events, groups by MidiProgramNumber, sorts and assigns channels
        public static List<Generator.PartTrack> Convert(
            List<Generator.PartTrack> partTracks,
            Music.Generator.TempoTrack tempoTrack,
            Music.Generator.Timingtrack timeSignatureTrack)
        {
            if (partTracks == null) throw new ArgumentNullException(nameof(partTracks));
            if (tempoTrack == null) throw new ArgumentNullException(nameof(tempoTrack));
            if (timeSignatureTrack == null) throw new ArgumentNullException(nameof(timeSignatureTrack));

            // Create tempo events from track
            var tempoEvents = new List<PartTrackEvent>();
            foreach (var tempoEvent in tempoTrack.Events)
            {
                // AI: tempo: StartBar is 1-based; conversion yields 0 for StartBar==1; CreateSetTempo expects bpm param
                var absoluteTicks = (long)(tempoEvent.StartBar - 1) * MusicConstants.TicksPerQuarterNote * 4;
                tempoEvents.Add(PartTrackEvent.CreateSetTempo(absoluteTicks, bpm: tempoEvent.TempoBpm));
            }

            // Create time signature events from track
            var timeSignatureEvents = new List<PartTrackEvent>();
            foreach (var tsEvent in timeSignatureTrack.Events)
            {
                // AI: time sig: Denominator forwarded verbatim; consumers interpret format consistently with CreateTimeSignature
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
                    Events = track.PartTrackNoteEvents.Where(e => e.Type != PartTrackEventType.SequenceTrackName).ToList() // Skip duplicate track names
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
                // AI: sorting must be stable and happen before channel assignment so NoteOn/NoteOff pairs align
                mergedEvents = mergedEvents.OrderBy(e => e.AbsoluteTimeTicks).ToList();

                // Assign channels
                int channel = mergedTracks.Count;
                if (channel >= 16) channel = 15;
                if (group.ProgramNumber == 255) channel = 9; // Drums on channel 10 (index 9)

                foreach (var evt in mergedEvents)
                {
                    if (evt.Type == PartTrackEventType.NoteOn || 
                        evt.Type == PartTrackEventType.NoteOff || 
                        evt.Type == PartTrackEventType.ProgramChange ||
                        evt.Type == PartTrackEventType.ControlChange)
                    {
                        // AI: only assign Channel for these types; other event types keep their parameters untouched
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
