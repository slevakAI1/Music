


using Music.Designer;
using Music.Generator;

namespace Music.Writer
{
    /// <summary>
    /// Converts HarmonyEvent objects to lists of PhraseNote compatible with Writer.
    /// </summary>
    public static class ConvertHarmonyEventToListOfPhraseNotes
    {
        /// <summary>
        /// Converts a HarmonyEvent to a list of notes representing the chord.
        /// </summary>
        /// <param name="harmonyEvent">The harmony event containing key, degree, quality, and bass.</param>
        /// <param name="baseOctave">The base octave for the root note (default: 4).</param>
        /// <param name="noteValue">The note value (duration) for all notes in the chord.</param>
        /// <returns>A list of WriterNote objects representing the chord voicing.</returns>
        /// <exception cref="ArgumentNullException">When harmonyEvent is null.</exception>
        /// <exception cref="InvalidOperationException">When the chord cannot be constructed.</exception>
        public static List<PhraseNote> Convert(HarmonyEvent harmonyEvent, int baseOctave = 4, int noteValue = 4)
        {
            if (harmonyEvent == null)
                throw new ArgumentNullException(nameof(harmonyEvent));

            return Convert(harmonyEvent.Key, harmonyEvent.Degree, harmonyEvent.Quality, harmonyEvent.Bass, baseOctave, noteValue);
        }

        /// <summary>
        /// Converts harmony parameters to a list of notes representing the chord.
        /// </summary>
        /// <param name="key">The key (e.g., "C major", "F# minor").</param>
        /// <param name="degree">The scale degree (1-7).</param>
        /// <param name="quality">The chord quality (e.g., "Major", "Minor7").</param>
        /// <param name="bass">The bass note option (e.g., "root", "3rd", "5th").</param>
        /// <param name="baseOctave">The base octave for the root note (default: 4).</param>
        /// <param name="noteValue">The note value (duration) for all notes in the chord.</param>
        /// <returns>A list of WriterNote objects representing the chord voicing.</returns>
        /// <exception cref="ArgumentException">When parameters are invalid.</exception>
        /// <exception cref="InvalidOperationException">When the chord cannot be constructed.</exception>
        public static List<PhraseNote> Convert(string key, int degree, string quality, string bass, int baseOctave = 4, int noteValue = 4)
        {
            // Use shared helper to generate chord MIDI notes
            var chordMidiNotes = ChordVoicingHelper.GenerateChordMidiNotes(key, degree, quality, bass, baseOctave);
            
            // Calculate note duration in ticks based on note value
            int noteDurationTicks = CalculateNoteDurationTicks(noteValue);
            
            // Create PhraseChord metadata object
            var phraseChord = new PhraseChord(
                isChord: true,
                chordKey: key,
                chordDegree: degree,
                chordQuality: quality,
                chordBase: bass);
            
            // Convert MIDI notes to PhraseNotes
            var result = new List<PhraseNote>();
            foreach (var noteNumber in chordMidiNotes)
            {
                var phraseNote = new PhraseNote(
                    noteNumber: noteNumber,
                    absolutePositionTicks: 0, // Will be set by the calling code
                    noteDurationTicks: noteDurationTicks,
                    noteOnVelocity: 100,
                    isRest: false);
                
                // Attach chord metadata
                phraseNote.phraseChord = phraseChord;
                
                result.Add(phraseNote);
            }
            
            return result;
        }

        /// <summary>
        /// Calculates note duration in ticks based on note value.
        /// Duration: 1=whole, 2=half, 4=quarter, 8=eighth, etc.
        /// </summary>
        private static int CalculateNoteDurationTicks(int noteValue)
        {
            return (MusicConstants.TicksPerQuarterNote * 4) / noteValue;
        }
    }
}