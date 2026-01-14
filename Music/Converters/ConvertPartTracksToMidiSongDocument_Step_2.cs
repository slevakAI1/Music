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

                // Sort all events by absolute time using stable sort with event type priority
                // AI: sorting must be stable and happen before channel assignment so NoteOn/NoteOff pairs align
                // AI: Use event type priority for same-tick ordering, then insertion order as final tie-breaker
                mergedEvents = mergedEvents
                    .Index()
                    .OrderBy(e => e.Item.AbsoluteTimeTicks)
                    .ThenBy(e => GetEventTypePriority(e.Item.Type))
                    .ThenBy(e => e.Index)
                    .Select(e => e.Item)
                    .ToList();

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

        // AI: GetEventTypePriority: defines ordering for events at same AbsoluteTimeTicks; lower numbers sort first
        // AI: Critical ordering: NoteOff before NoteOn (allows re-trigger); setup (tempo/program) before notes; meta events grouped logically
        private static int GetEventTypePriority(PartTrackEventType type) =>
            type switch
            {
                // Priority 0-9: Critical meta events that affect timing/playback - must come first
                PartTrackEventType.SequenceNumber => 0,
                PartTrackEventType.SetTempo => 1,
                PartTrackEventType.TimeSignature => 2,
                PartTrackEventType.KeySignature => 3,
                PartTrackEventType.SmpteOffset => 4,

                // Priority 10-19: Track/instrument identification and routing
                PartTrackEventType.SequenceTrackName => 10,
                PartTrackEventType.InstrumentName => 11,
                PartTrackEventType.DeviceName => 12,
                PartTrackEventType.MidiChannelPrefix => 13,
                PartTrackEventType.MidiPort => 14,

                // Priority 20-29: Program and control setup - must happen before notes
                PartTrackEventType.ProgramChange => 20,
                PartTrackEventType.ControlChange => 21,

                // Priority 30-39: Channel voice messages (non-note)
                PartTrackEventType.ChannelPressure => 30,
                PartTrackEventType.PolyKeyPressure => 31,
                PartTrackEventType.PitchBend => 32,

                // Priority 40-49: Note events - NoteOff MUST come before NoteOn for same tick
                PartTrackEventType.NoteOff => 40,
                PartTrackEventType.NoteOn => 41,

                // Priority 50-69: Text/lyric meta events - after notes
                PartTrackEventType.Text => 50,
                PartTrackEventType.CopyrightNotice => 51,
                PartTrackEventType.Lyric => 52,
                PartTrackEventType.Marker => 53,
                PartTrackEventType.CuePoint => 54,
                PartTrackEventType.ProgramName => 55,

                // Priority 70-89: System exclusive and sequencer specific
                PartTrackEventType.SequencerSpecific => 70,
                PartTrackEventType.NormalSysEx => 71,
                PartTrackEventType.EscapeSysEx => 72,

                // Priority 90-99: System common messages
                PartTrackEventType.MtcQuarterFrame => 90,
                PartTrackEventType.SongPositionPointer => 91,
                PartTrackEventType.SongSelect => 92,
                PartTrackEventType.TuneRequest => 93,

                // Priority 100-109: System real-time messages
                PartTrackEventType.TimingClock => 100,
                PartTrackEventType.Start => 101,
                PartTrackEventType.Continue => 102,
                PartTrackEventType.Stop => 103,
                PartTrackEventType.ActiveSensing => 104,
                PartTrackEventType.SystemReset => 105,

                // Priority 110-119: End of track and unknown
                PartTrackEventType.EndOfTrack => 110,
                PartTrackEventType.UnknownMeta => 998,
                PartTrackEventType.Unknown => 999,

                // Catch-all for any future event types
                _ => 1000
            };
    }
}
