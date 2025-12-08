using System;
using System.Collections.Generic;
using Music.Domain;
using Music.Tests;
using Music;

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
            List<Phrase> phrases,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            if (phrases == null) throw new ArgumentNullException(nameof(phrases));

            // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
            var midiEventLists = ConvertPhrasesToMidiEventLists.Convert(phrases);

            var json1 = Helpers.DebugObject(phrases);
            var json2 = Helpers.DebugObject(midiEventLists);

            // Step 2 - Merge midiEventLists lists that are for the same instrument
            var mergedMidiEventLists = MergeMidiEventListsByInstrument.Convert(
                midiEventLists,
                tempo: tempo,
                timeSignatureNumerator: timeSignatureNumerator,
                timeSignatureDenominator: timeSignatureDenominator);

            var json3 = Helpers.DebugObject(mergedMidiEventLists);

            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertMidiEventsToMidiSongDocument.Convert(
                mergedMidiEventLists,
                tempo: tempo);

            var json43 = Helpers.DebugObject(midiDoc);


            return midiDoc;
        }
    }
}