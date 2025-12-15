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

        // TO DO - UPDATE THIS SIGNATURE TO ACCEPT THE time signature and TEMPO data objects

        public static MidiSongDocument Convert(
            List<Phrase> phrases,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            if (phrases == null) throw new ArgumentNullException(nameof(phrases));

            // UPDATE THIS NEXT CALL AND CALLED METHOD TO PASS IN AND USE THE TEMPO EVENTS AND TIME SIGNATURE EVENTS.
            // WHEN THIS UPDATE IS COMPLETE, THE TEMPO EVENTS AND TIME SIGNATURE EVENTS WILL BE INCLUDED IN THE RETUR

            // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
            var midiEventLists = ConvertPhrasesToMidiEventLists.Convert(phrases);

            //var json1 = ObjectViewer.Json(phrases);
            //var json2 = ObjectViewer.Json(midiEventLists);

            // TO DO - UPDATE THE NEXT CALLED METHOD TO MERGE IN THE TEMPO EVENTS AND TIME SIGNATURE EVENTS BY 
            //    ABSOLUTE POSITION ALONG WITH THE PHRASES. WHEN COMPLETE THE TEMPO AND TIME SIGNATURE RETURNED META EVENTS WILL CONTAIN 
            //    THEIR ABSOLUTE POSITION SIMILAR TO HOW PHRASES WORKS.

            // Step 2 - Merge midiEventLists lists that are for the same instrument
            var mergedMidiEventLists = MergeMidiEventListsByInstrument.Convert(
                midiEventLists,
                tempo: tempo,
                timeSignatureNumerator: timeSignatureNumerator,
                timeSignatureDenominator: timeSignatureDenominator);

            //var json3 = ObjectViewer.Json(mergedMidiEventLists);


            // TO DO - IF NEEDED, UPDATE THIS NEXT CALLED METHOD TO INCLUDE THE TEMPO EVENTS AND TIME SIGNATURE EVENTS
            //     CORRECTLY IN THE MIDI DOCUMENT OUTPUT

            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertMidiEventsToMidiSongDocument.Convert(
                mergedMidiEventLists,
                tempo: tempo);

            //var json4 = ObjectViewer.Json(midiDoc);

            return midiDoc;
        }
    }
}