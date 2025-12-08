using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Helper to convert lists of MidiEvent objects to Phrase objects.
    /// Extracted from WriterForm to keep UI code focused.
    /// </summary>
    internal static class MidiEventListConverter
    {
        private const int StandardTicksPerQuarterNote = 480;

        /// <summary>
        /// Converts lists of MidiEvent objects to Phrase objects.
        /// Each list becomes one phrase with instrument information extracted from ProgramChange events.
        /// </summary>
        /// <param name="midiEventLists">Lists of MidiEvent objects, one per track</param>
        /// <param name="midiInstruments">Available MIDI instruments for name lookup</param>
        /// <param name="sourceTicksPerQuarterNote">The ticks per quarter note from the source MIDI file (default 480)</param>
        public static List<Phrase> ConvertMidiEventListsToPhrases(
            List<List<MidiEvent>> midiEventLists,
            List<MidiInstrument> midiInstruments,
            short sourceTicksPerQuarterNote = StandardTicksPerQuarterNote)
        {
            var phrases = new List<Phrase>();

            foreach (var midiEventList in midiEventLists)
            {
                var phraseNotes = new List<PhraseNote>();
                var phrase = new Phrase(phraseNotes);

                // Extract instrument information from ProgramChange event
                var programChangeEvents = midiEventList
                    .Where(e => e.Type == MidiEventType.ProgramChange)
                    .ToList();

                if (programChangeEvents.Count > 1)
                {
                    // Multiple program changes detected - show error
                    var programNumbers = string.Join(", ", programChangeEvents
                        .Select(e => e.Parameters.TryGetValue("Program", out var p) ? p.ToString() : "unknown"));
                    MessageBox.Show(
                        $"Multiple ProgramChange events detected in a single track.\n" +
                        $"Program numbers: {programNumbers}\n" +
                        $"Each track should have only one instrument assignment.",
                        "Multiple Instruments Detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                var programChangeEvent = programChangeEvents.FirstOrDefault();

                if (programChangeEvent != null &&
                    programChangeEvent.Parameters.TryGetValue("Program", out var programObj))
                {
                    int programNumber = Convert.ToInt32(programObj);
                    phrase.MidiProgramNumber = programNumber;

                    // Find the matching instrument name using the provided instrument list
                    var instrument = midiInstruments
                        .FirstOrDefault(i => i.ProgramNumber == programNumber);
                    phrase.MidiProgramName = instrument?.Name ?? $"Program {programNumber}";
                }
                else
                {
                    // No program change found - use default
                    phrase.MidiProgramNumber = 0;
                    phrase.MidiProgramName = "Acoustic Grand Piano";
                }

                // Calculate tick scaling factor to normalize to 480 ticks per quarter note
                double tickScale = (double)StandardTicksPerQuarterNote / sourceTicksPerQuarterNote;

                // Process note events - pair NoteOn with NoteOff events
                var noteOnEvents = new Dictionary<int, MidiEvent>(); // Key: note number, Value: NoteOn event

                foreach (var midiEvent in midiEventList.OrderBy(e => e.AbsoluteTimeTicks))
                {
                    if (midiEvent.Type == MidiEventType.NoteOn)
                    {
                        if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                            !midiEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                            continue;

                        int noteNumber = Convert.ToInt32(noteNumObj);
                        int velocity = Convert.ToInt32(velocityObj);

                        // Velocity 0 is treated as NoteOff
                        if (velocity == 0)
                        {
                            if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                            {
                                CreatePhraseNoteFromPair(noteOnEvent, midiEvent, phraseNotes, tickScale);
                                noteOnEvents.Remove(noteNumber);
                            }
                        }
                        else
                        {
                            // Store the NoteOn event
                            noteOnEvents[noteNumber] = midiEvent;
                        }
                    }
                    else if (midiEvent.Type == MidiEventType.NoteOff)
                    {
                        if (!midiEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj))
                            continue;

                        int noteNumber = Convert.ToInt32(noteNumObj);

                        if (noteOnEvents.TryGetValue(noteNumber, out var noteOnEvent))
                        {
                            CreatePhraseNoteFromPair(noteOnEvent, midiEvent, phraseNotes, tickScale);
                            noteOnEvents.Remove(noteNumber);
                        }
                    }
                }

                phrases.Add(phrase);
            }

            return phrases;
        }

        /// <summary>
        /// Creates a PhraseNote from a NoteOn/NoteOff event pair.
        /// </summary>
        /// <param name="noteOnEvent">The NoteOn event</param>
        /// <param name="noteOffEvent">The NoteOff event</param>
        /// <param name="phraseNotes">The list to add the created PhraseNote to</param>
        /// <param name="tickScale">Scale factor to normalize ticks to 480 per quarter note</param>
        private static void CreatePhraseNoteFromPair(
            MidiEvent noteOnEvent,
            MidiEvent noteOffEvent,
            List<PhraseNote> phraseNotes,
            double tickScale)
        {
            if (!noteOnEvent.Parameters.TryGetValue("NoteNumber", out var noteNumObj) ||
                !noteOnEvent.Parameters.TryGetValue("Velocity", out var velocityObj))
                return;

            int noteNumber = Convert.ToInt32(noteNumObj);
            int velocity = Convert.ToInt32(velocityObj);
            
            // Scale the timing values to standard 480 ticks per quarter note
            int absolutePositionTicks = (int)Math.Round(noteOnEvent.AbsoluteTimeTicks * tickScale);
            int noteDurationTicks = (int)Math.Round((noteOffEvent.AbsoluteTimeTicks - noteOnEvent.AbsoluteTimeTicks) * tickScale);

            // Ensure minimum duration of 1 tick
            if (noteDurationTicks < 1)
                noteDurationTicks = 1;

            // Create the PhraseNote - constructor will calculate metadata fields
            var phraseNote = new PhraseNote(
                noteNumber,
                absolutePositionTicks,
                noteDurationTicks,
                velocity,
                isRest: false);

            phraseNotes.Add(phraseNote);
        }
    }
}