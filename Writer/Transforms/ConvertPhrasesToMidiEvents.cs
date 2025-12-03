using Music.Domain;

namespace Music.Writer
{
    /// <summary>
    /// Converts Phrase objects to lists of MidiEvent objects with absolute time positioning.
    /// This is stage 1 processing - creates NoteOn, NoteOff, and SequenceTrackName events only.
    /// Channel assignment and other processing happens in later stages.
    /// </summary>
    public static class ConvertPhrasesToMidiEvents
    {
        private const short DefaultTicksPerQuarterNote = 480;

        /// <summary>
        /// Converts a list of phrases to lists of MIDI events (one list per phrase).
        /// Each phrase is processed independently with its own event list.
        /// </summary>
        /// <param name="phrases">List of phrases to convert</param>
        /// <param name="ticksPerQuarterNote">MIDI time resolution (default 480 ticks per quarter note)</param>
        /// <returns>List of MidiEvent lists, one per input phrase</returns>
        public static List<List<MidiEvent>> Convert(
            List<Phrase> phrases,
            short ticksPerQuarterNote = DefaultTicksPerQuarterNote)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            var result = new List<List<MidiEvent>>();
            foreach (var phrase in phrases)
            {
                result.Add(ConvertSinglePhrase(phrase, ticksPerQuarterNote));
            }

            return result;
        }

        /// <summary>
        /// Converts a single phrase to a list of MIDI events with absolute time positioning.
        /// </summary>
        private static List<MidiEvent> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
        {
            var events = new List<MidiEvent>();

            // Add track name event at the beginning (using instrument name)
            var trackName = string.IsNullOrWhiteSpace(phrase.MidiProgramName) 
                ? "Unnamed Track" 
                : phrase.MidiProgramName;
            events.Add(MidiEvent.CreateSequenceTrackName(0, trackName));

            // Add program change event at the beginning to set the instrument
            // Channel is null and will be assigned in Phase 2
            var programChangeEvent = MidiEvent.CreateProgramChange(0, 0, phrase.MidiProgramNumber);
            // Remove the channel parameter temporarily (will be assigned in Phase 2)
            programChangeEvent.Parameters.Remove("Channel");
            events.Add(programChangeEvent);

            // Process each note in the phrase
            foreach (var phraseNote in phrase.PhraseNotes ?? Enumerable.Empty<PhraseNote>())
            {
                if (phraseNote.IsRest)
                {
                    // Rests don't create MIDI events, timing is already handled by AbsolutePositionTicks
                    continue;
                }

                // Check if this note is part of a chord that needs expansion
                if (phraseNote.phraseChord != null && phraseNote.phraseChord.IsChord)
                {
                    ProcessChord(events, phraseNote);
                }
                else
                {
                    ProcessSingleNote(events, phraseNote);
                }
            }

            return events;
        }

        /// <summary>
        /// Processes a chord note by expanding it to individual notes using ChordConverter.
        /// </summary>
        private static void ProcessChord(List<MidiEvent> events, PhraseNote phraseNote)
        {
            var chord = phraseNote.phraseChord!;

            // Use ChordConverter to generate individual chord notes
            var chordNotes = ChordConverter.Convert(
                chord.ChordKey!,
                chord.ChordDegree!.Value,
                chord.ChordQuality!,
                chord.ChordBase!,
                baseOctave: phraseNote.Octave,
                noteValue: phraseNote.Duration);

            // Create NoteOn and NoteOff events for all chord notes
            foreach (var cn in chordNotes)
            {
                var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                // NoteOn at the phrase note's absolute position
                var noteOnEvent = MidiEvent.CreateNoteOn(
                    phraseNote.AbsolutePositionTicks, 
                    0, 
                    noteNumber, 
                    phraseNote.NoteOnVelocity);
                noteOnEvent.Parameters.Remove("Channel");
                events.Add(noteOnEvent);

                // NoteOff at absolute position + duration
                long noteOffTime = phraseNote.AbsolutePositionTicks + phraseNote.NoteDurationTicks;
                var noteOffEvent = MidiEvent.CreateNoteOff(noteOffTime, 0, noteNumber, 0);
                noteOffEvent.Parameters.Remove("Channel");
                events.Add(noteOffEvent);
            }
        }

        /// <summary>
        /// Processes a single note event.
        /// </summary>
        private static void ProcessSingleNote(List<MidiEvent> events, PhraseNote phraseNote)
        {
            // Create NoteOn event at the note's absolute position
            var noteOnEvent = MidiEvent.CreateNoteOn(
                phraseNote.AbsolutePositionTicks, 
                0, 
                phraseNote.NoteNumber, 
                phraseNote.NoteOnVelocity);
            noteOnEvent.Parameters.Remove("Channel");
            events.Add(noteOnEvent);

            // Create NoteOff event at absolute position + duration
            long noteOffTime = phraseNote.AbsolutePositionTicks + phraseNote.NoteDurationTicks;
            var noteOffEvent = MidiEvent.CreateNoteOff(noteOffTime, 0, phraseNote.NoteNumber, 0);
            noteOffEvent.Parameters.Remove("Channel");
            events.Add(noteOffEvent);
        }

        /// <summary>
        /// Calculates MIDI note number from note properties.
        /// </summary>
        private static int CalculateMidiNoteNumber(char step, int alter, int octave)
        {
            var baseNote = char.ToUpper(step) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            return (octave + 1) * 12 + baseNote + alter;
        }
    }
}
