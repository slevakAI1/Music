using Music.Designer;
using Music.MyMidi;

namespace Music.Writer
{
    /// <summary>
    /// Thin wrapper that executes the existing three-stage pipeline:
    /// 1) Convert phrases -> midi event lists
    /// 2) Merge midi event lists by instrument
    /// 3) Convert merged events -> Midi document
    /// 
    /// This preserves existing logic and keeps changes minimal.
    /// </summary>
    public static class ConvertListOfPhrasesToMidiSongDocument
    {
        public static MidiSongDocument Convert(
            List<SongTrack> phrases,
            TempoTimeline tempoTimeline,
            TimeSignatureTimeline timeSignatureTimeline)
        {
            if (phrases == null) throw new ArgumentNullException(nameof(phrases));
            if (tempoTimeline == null) throw new ArgumentNullException(nameof(tempoTimeline));
            if (timeSignatureTimeline == null) throw new ArgumentNullException(nameof(timeSignatureTimeline));

            // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
            var midiEventLists = ConvertPhrasesToMidiEventLists.Convert(phrases);

            // Step 2 - Merge midiEventLists lists that are for the same instrument
            // and integrate tempo and time signature events
            var mergedMidiEventLists = MergeMidiEventListsByInstrument.Convert(
                midiEventLists,
                tempoTimeline,
                timeSignatureTimeline);

            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertMidiEventsToMidiSongDocument.Convert(mergedMidiEventLists);

            return midiDoc;
        }
    }
}