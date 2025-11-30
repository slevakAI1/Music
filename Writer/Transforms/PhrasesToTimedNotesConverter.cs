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
                if (noteEvent.IsRest)
                {
                    // Rests add to the delta for the next note
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    currentTime += duration;
                    continue;
                }

                // Check if this is a chord that needs to be expanded
                if (!string.IsNullOrWhiteSpace(noteEvent.ChordKey) && 
                    noteEvent.ChordDegree.HasValue && 
                    !string.IsNullOrWhiteSpace(noteEvent.ChordQuality) && 
                    !string.IsNullOrWhiteSpace(noteEvent.ChordBase))
                {
                    // Use ChordConverter to generate chord notes
                    var chordNotes = ChordConverter.Convert(
                        noteEvent.ChordKey,
                        noteEvent.ChordDegree.Value,
                        noteEvent.ChordQuality,
                        noteEvent.ChordBase,
                        baseOctave: noteEvent.Octave,
                        noteValue: noteEvent.Duration);

                    // Apply dots and tuplet settings to chord notes
                    foreach (var cn in chordNotes)
                    {
                        cn.Dots = noteEvent.Dots;
                        if (!string.IsNullOrWhiteSpace(noteEvent.TupletNumber))
                        {
                            cn.TupletNumber = noteEvent.TupletNumber;
                            cn.TupletActualNotes = noteEvent.TupletActualNotes;
                            cn.TupletNormalNotes = noteEvent.TupletNormalNotes;
                        }
                    }

                    // Convert chord notes to TimedNotes
                    for (int i = 0; i < chordNotes.Count; i++)
                    {
                        var cn = chordNotes[i];
                        var duration = CalculateDuration(cn, ticksPerQuarterNote);
                        var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                        timedNotes.Add(new TimedNote
                        {
                            Delta = i == 0 ? currentTime : 0, // First note gets accumulated time, rest are simultaneous
                            NoteNumber = (byte)noteNumber,
                            Duration = duration,
                            Velocity = 100,
                            IsRest = false
                        });
                    }

                    // Advance time for the entire chord
                    var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);
                    currentTime = chordDuration;
                }
                else
                {
                    // Handle single note
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

                    timedNotes.Add(new TimedNote
                    {
                        Delta = currentTime,
                        NoteNumber = (byte)noteNumber,
                        Duration = duration,
                        Velocity = 100,
                        IsRest = false
                    });

                    // Advance time for the next note
                    currentTime = duration;
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