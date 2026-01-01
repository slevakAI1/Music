// AI: purpose=orchestrate 3-step pipeline to produce MidiSongDocument from PartTrack data; keeps steps isolated
// AI: invariants=Step_1 output=absolute-time events; Step_2 merges by instrument and injects tempo/time sig; Step_3 emits MidiSongDocument
// AI: deps=ConvertPartTracksToMidiSongDocument_Step_1/Step_2/Step_3; consumers rely on absolute event times and merged ordering
// AI: errors=throws ArgNull for null inputs; preserve exception types and messages; do not swallow exceptions
// AI: perf=single-thread caller expected; O(total events); avoid adding allocations in pipeline steps
// AI: change=when modifying timing or merging rules update all 3 steps and associated unit tests for time integrity
using Music.Designer;
using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    // AI: class=thin coordinator; no state; keep method signatures stable for external callers
    public static class ConvertPartTracksToMidiSongDocument_For_Play_And_Export
    {
        // AI: Convert: validate inputs then run step1->step2->step3; maintain null checks and step ordering
        public static MidiSongDocument Convert(
            List<PartTrack> songTracks,
            TempoTrack tempoTrack,
            Timingtrack timeSignatureTrack)
        {
            if (songTracks == null) throw new ArgumentNullException(nameof(songTracks));
            if (tempoTrack == null) throw new ArgumentNullException(nameof(tempoTrack));
            if (timeSignatureTrack == null) throw new ArgumentNullException(nameof(timeSignatureTrack));

            // Step 1 - convert songTracks to MIDI EVENTS - Absolute positions
            var partTracks = ConvertPartTracksToMidiSongDocument_Step_1.Convert(songTracks);

            // Step 2 - Merge Part Tracks that are for the same instrument
            //    and integrate tempo and time signature events
            var mergedPartTracks = ConvertPartTracksToMidiSongDocument_Step_2.Convert(
                partTracks,
                tempoTrack,
                timeSignatureTrack);

            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertPartTracksToMidiSongDocument_Step_3.Convert(mergedPartTracks);

            return midiDoc;
        }
    }
}