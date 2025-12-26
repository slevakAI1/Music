using Music.Designer;
using Music.Generator;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Thin wrapper that executes the existing three-stage pipeline:
    /// 1) Convert PartTrack -> midi event lists
    /// 2) Merge midi event lists by instrument
    /// 3) Convert merged events -> Midi document
    /// 
    /// This preserves existing logic and keeps changes minimal.
    /// </summary>
    public static class ConvertPartTracksToMidiSongDocument_For_Play_And_Export
    {
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