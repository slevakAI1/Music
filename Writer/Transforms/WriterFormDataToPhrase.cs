using Music.Designer;
using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Transforms WriterFormData to Phrase objects with MIDI tick-based timing.
    /// </summary>
    public static class WriterFormDataToPhrase
    {
        /// <summary>
        /// Converts Writer form data to a Phrase for use with the phrase control.
        /// </summary>
        public static Phrase Convert(
            WriterFormData data,
            int numberOfNotes,
            string midiProgramName,
            List<int> selectedStaffs,
            int startBar,
            int startBeat)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var phraseNotes = new List<PhraseNote>();

            // Constants
            const int ticksPerQuarterNote = 480;
            int currentPosition = 0;

            // Get tuplet settings from writer data
            bool isTuplet = !string.IsNullOrWhiteSpace(data.TupletNumber);

            if (data.IsChord ?? false)  // null = false
            {
                // Create Chord pitch events
                // Chords will be resolved into their component notes by ChordConverter
                var harmonicEvent = new HarmonicEvent
                {
                    Key = data.ChordKey ?? "C major",
                    Degree = data.ChordDegree ?? 1,
                    Quality = data.ChordQuality ?? "Major",
                    Bass = data.ChordBase ?? "root"
                };

                var chordNotes = ChordConverter.Convert(
                    harmonicEvent,
                    baseOctave: data.Octave,
                    noteValue: GetNoteValue(data.NoteValue));

                // Convert chord notes to PhraseNote with MIDI properties
                foreach (var chordNote in chordNotes)
                {
                    int noteDurationTicks = CalculateNoteDurationTicks(
                        GetNoteValue(data.NoteValue),
                        data.Dots,
                        ticksPerQuarterNote);

                    var phraseNote = new PhraseNote(
                        noteNumber: CalculateNoteNumber(chordNote.Step, chordNote.Alter, chordNote.Octave),
                        absolutePositionTicks: currentPosition,
                        noteDurationTicks: noteDurationTicks,
                        noteOnVelocity: 100,
                        isRest: false);

                    phraseNotes.Add(phraseNote);
                }

                // Advance position after chord
                currentPosition += CalculateNoteDurationTicks(
                    GetNoteValue(data.NoteValue),
                    data.Dots,
                    ticksPerQuarterNote);
            }
            else
            {
                // Single note or rest - create specified number of notes
                for (int i = 0; i < numberOfNotes; i++)
                {
                    int noteDurationTicks = CalculateNoteDurationTicks(
                        GetNoteValue(data.NoteValue),
                        data.Dots,
                        ticksPerQuarterNote);

                    int noteNumber = data.IsRest ?? false
                        ? 60 // Default middle C for rests (not used but required)
                        : CalculateNoteNumber(data.Step, GetAlter(data.Accidental), data.Octave);

                    var phraseNote = new PhraseNote(
                        noteNumber: noteNumber,
                        absolutePositionTicks: currentPosition,
                        noteDurationTicks: noteDurationTicks,
                        noteOnVelocity: 100,
                        isRest: data.IsRest ?? false);

                    phraseNotes.Add(phraseNote);

                    currentPosition += noteDurationTicks;
                }
            }

            var phrase = new Phrase(midiProgramName, phraseNotes);

            return phrase;
        }

        private static int CalculateNoteNumber(char step, int alter, int octave)
        {
            // C4 = MIDI note 60
            int baseNote = step switch
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

        private static int CalculateNoteDurationTicks(int duration, int dots, int ticksPerQuarterNote)
        {
            // Duration: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
            // Base ticks for this duration
            int baseTicks = (ticksPerQuarterNote * 4) / duration;

            // Apply dots: each dot adds half of the previous value
            int totalTicks = baseTicks;
            int dotValue = baseTicks;
            for (int i = 0; i < dots; i++)
            {
                dotValue /= 2;
                totalTicks += dotValue;
            }

            return totalTicks;
        }

        private static int GetNoteValue(string? noteValueString)
        {
            if (noteValueString != null && Music.MusicConstants.NoteValueMap.TryGetValue(noteValueString, out var nv))
            {
                return nv;
            }
            return 4; // default quarter note
        }

        private static int GetAlter(string? accidental)
        {
            return (accidental ?? "Natural") switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                _ => 0
            };
        }
    }
}