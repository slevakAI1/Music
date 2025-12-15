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

        // TODO - FIX THIS SIGNATURE TO ACCEPT THE TEMPO ROW

        public static MidiSongDocument Convert(
            List<Phrase> phrases,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            if (phrases == null) throw new ArgumentNullException(nameof(phrases));

            // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
            var midiEventLists = ConvertPhrasesToMidiEventLists.Convert(phrases);

            //var json1 = ObjectViewer.Json(phrases);
            //var json2 = ObjectViewer.Json(midiEventLists);


            // TO DO - FIX THIS METHOD TO MERGE IN THE TEMPO ROW EVENTS
            // TO DO - FIX THIS METHOD TO MERGE IN THE TIME SIGNATURE ROW EVENTS


            // Step 2 - Merge midiEventLists lists that are for the same instrument
            var mergedMidiEventLists = MergeMidiEventListsByInstrument.Convert(
                midiEventLists,
                tempo: tempo,
                timeSignatureNumerator: timeSignatureNumerator,
                timeSignatureDenominator: timeSignatureDenominator);

            //var json3 = ObjectViewer.Json(mergedMidiEventLists);


            // TO DO - FIX THIS METHOD IF NEEDED TO INCLUDE THE TEMPO EVENTS IN THE MIDI DOCUMENT

            // TO DO - FIX THIS METHOD IF NEEDED TO INCLUDE THE TIME SIGNATURE EVENTS IN THE MIDI DOCUMENT


            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertMidiEventsToMidiSongDocument.Convert(
                mergedMidiEventLists,
                tempo: tempo);

            //var json4 = ObjectViewer.Json(midiDoc);

            return midiDoc;
        }
    }
}