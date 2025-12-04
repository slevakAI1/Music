using System;
using System.Collections.Generic;
using Music.Domain;
using Music.Tests;

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
    public static class PhrasesToMidiDocumentConverter
    {
        public static MidiSongDocument Convert(
            List<Phrase> phrases,
            int tempo,
            int timeSignatureNumerator,
            int timeSignatureDenominator)
        {
            if (phrases == null) throw new ArgumentNullException(nameof(phrases));

            // Step 1 - convert phrases to MIDI EVENTS - Absolute positions
            var midiEventLists = ConvertPhrasesToMidiEvents.Convert(phrases);

            // Step 2 - Merge midiEventLists lists that are for the same instrument
            var mergedMidiEventLists = MergeMidiEventsByInstrument.Convert(
                midiEventLists,
                tempo: tempo,
                timeSignatureNumerator: timeSignatureNumerator,
                timeSignatureDenominator: timeSignatureDenominator);

            // Step 3 - Execute merged timed notes to MIDI document
            var midiDoc = ConvertMidiEventsToMidiDocument.Convert(
                mergedMidiEventLists,
                tempo: tempo,
                timeSignatureNumerator: timeSignatureNumerator,
                timeSignatureDenominator: timeSignatureDenominator);

            return midiDoc;
        }
    }
}