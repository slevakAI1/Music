using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts Phrase objects to flat lists of TimedNote objects.
    /// Sequential notes have positive delta times, while simultaneous notes (chords) have delta = 0.
    /// </summary>
    public static class PhrasesToTimedNotesConverter
    {
        /// <summary>
        /// Converts a list of phrases (which may have different instruments) to a list of TimedNote lists,
        /// one for each input phrase.
        /// </summary>
        public static List<List<TimedNote>> Convert(List<Phrase> phrases, short ticksPerQuarterNote = 480)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            if (phrases.Count == 0)
                return new List<List<TimedNote>>();

            var result = new List<List<TimedNote>>();

            foreach (var phrase in phrases)
            {
                result.Add(ConvertSinglePhrase(phrase, ticksPerQuarterNote));
            }

            return result;
        }

        /// <summary>
        /// Converts a single Phrase object to a flat list of TimedNote objects.
        /// </summary>
        private static List<TimedNote> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
        {
            var timedNotes = new List<TimedNote>();
            long currentTime = 0;

            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);

                if (noteEvent.IsRest)
                {
                    // Rests add to the delta for the next note
                    currentTime += duration;
                    continue;
                }

                // For chords, the first note gets the accumulated delta, subsequent notes get 0
                bool isFirstNoteInChord = true;

                // Handle chord notes (if IsChord is true, this note is part of a chord)
                var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);
                
                timedNotes.Add(new TimedNote
                {
                    Delta = isFirstNoteInChord ? currentTime : 0,
                    NoteNumber = (byte)noteNumber,
                    Duration = duration,
                    Velocity = 100,
                    IsRest = false
                });

                if (isFirstNoteInChord)
                {
                    currentTime = 0; // Reset after first note
                    isFirstNoteInChord = false;
                }

                // After processing a note (or chord), accumulate time for the next note
                if (!noteEvent.IsChord)
                {
                    currentTime += duration;
                }
            }

            return timedNotes;
        }

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

        private static long CalculateDuration(NoteEvent noteEvent, short ticksPerQuarterNote)
        {
            var baseDuration = (ticksPerQuarterNote * 4.0) / noteEvent.Duration;

            var dottedMultiplier = 1.0;
            var dotValue = 0.5;
            for (int i = 0; i < noteEvent.Dots; i++)
            {
                dottedMultiplier += dotValue;
                dotValue /= 2;
            }

            baseDuration *= dottedMultiplier;

            if (noteEvent.TupletActualNotes > 0 && noteEvent.TupletNormalNotes > 0)
                baseDuration *= (double)noteEvent.TupletNormalNotes / noteEvent.TupletActualNotes;

            return (long)Math.Round(baseDuration);
        }
    }
}