using Music.Domain;
using Music.Tests;

namespace Music.Writer
{
    /// <summary>
    /// Converts a list of Phrase objects (with the same instrument) to a flat list of TimedNote objects.
    /// Sequential notes have positive delta times, while simultaneous notes (chords) have delta = 0.
    /// </summary>
    public static class PhrasesToTimedNotesConverter
    {
        public static List<TimedNote> Convert(List<Phrase> phrases, short ticksPerQuarterNote = 480)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            if (phrases.Count == 0)
                return new List<TimedNote>();

            // Verify all phrases have the same program number
            if (phrases.Count > 1)
            {
                var firstProgram = phrases[0].MidiProgramNumber;
                if (phrases.Any(p => p.MidiProgramNumber != firstProgram))
                    throw new ArgumentException("All phrases must have the same MidiProgramNumber.", nameof(phrases));
            }

            var timedNotes = new List<TimedNote>();
            long currentTime = 0;

            foreach (var phrase in phrases)
            {
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