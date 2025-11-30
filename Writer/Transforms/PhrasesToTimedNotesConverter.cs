namespace Music.Writer
{
    /// <summary>
    /// Represents a phrase converted to timed notes with instrument information.
    /// </summary>
    public sealed class TimedPhrase
    {
        /// <summary>
        /// The MIDI program number for the instrument.
        /// </summary>
        public byte MidiProgramNumber { get; set; }

        /// <summary>
        /// The instrument name (MIDI part name).
        /// </summary>
        public string MidiPartName { get; set; } = string.Empty;

        /// <summary>
        /// The list of timed notes for this phrase.
        /// </summary>
        public List<TimedNote> TimedNotes { get; set; } = new();
    }

    /// <summary>
    /// Converts Phrase objects to flat lists of TimedNote objects.
    /// Sequential notes have positive delta times, while simultaneous notes (chords) have delta = 0.
    /// </summary>
    public static class PhrasesToTimedNotesConverter
    {
        /// <summary>
        /// Converts a list of phrases (which may have different instruments) to a list of TimedPhrase objects.
        /// Each TimedPhrase contains the instrument information and the converted timed notes.
        /// </summary>
        public static List<TimedPhrase> Convert(List<Phrase> phrases, short ticksPerQuarterNote = 480)
        {
            if (phrases == null)
                throw new ArgumentNullException(nameof(phrases));

            if (phrases.Count == 0)
                return new List<TimedPhrase>();

            var result = new List<TimedPhrase>();

            foreach (var phrase in phrases)
            {
                result.Add(new TimedPhrase
                {
                    MidiProgramNumber = phrase.MidiProgramNumber,
                    MidiPartName = phrase.MidiPartName ?? string.Empty,
                    TimedNotes = ConvertSinglePhrase(phrase, ticksPerQuarterNote)
                });
            }

            return result;
        }

        /// <summary>
        /// Merges timed note lists for phrases that share the same instrument (MidiProgramNumber).
        /// Multiple phrases for the same instrument are merged to play simultaneously.
        /// </summary>
        /// <param name="timedPhrases">List of timed phrases with their associated instruments.</param>
        /// <returns>A dictionary mapping MidiProgramNumber to merged TimedNote lists.</returns>
        public static Dictionary<byte, List<TimedNote>> MergeByInstrument(List<TimedPhrase> timedPhrases)
        {
            if (timedPhrases == null)
                throw new ArgumentNullException(nameof(timedPhrases));

            var result = new Dictionary<byte, List<TimedNote>>();

            // Group timed phrases by their MIDI program number (instrument)
            var groupedByInstrument = new Dictionary<byte, List<List<TimedNote>>>();

            foreach (var timedPhrase in timedPhrases)
            {
                var instrument = timedPhrase.MidiProgramNumber;
                
                if (!groupedByInstrument.ContainsKey(instrument))
                {
                    groupedByInstrument[instrument] = new List<List<TimedNote>>();
                }
                
                groupedByInstrument[instrument].Add(timedPhrase.TimedNotes);
            }

            // Merge timed note lists for each instrument
            foreach (var kvp in groupedByInstrument)
            {
                var instrument = kvp.Key;
                var listsToMerge = kvp.Value;

                if (listsToMerge.Count == 1)
                {
                    // Only one list for this instrument, no merging needed
                    result[instrument] = listsToMerge[0];
                }
                else
                {
                    // Merge multiple lists for the same instrument
                    result[instrument] = MergeSimultaneousTimedNoteLists(listsToMerge);
                }
            }

            return result;
        }

        /// <summary>
        /// Merges multiple timed note lists to play simultaneously.
        /// Notes maintain their original order within each list, and delta times are adjusted
        /// to ensure proper simultaneous playback.
        /// </summary>
        private static List<TimedNote> MergeSimultaneousTimedNoteLists(List<List<TimedNote>> lists)
        {
            var merged = new List<TimedNote>();
            var currentPositions = new int[lists.Count]; // Track position in each list
            var currentTimes = new long[lists.Count]; // Track accumulated time for each list

            bool hasMoreNotes = true;

            while (hasMoreNotes)
            {
                hasMoreNotes = false;
                long minTime = long.MaxValue;

                // Find the minimum time across all lists
                for (int i = 0; i < lists.Count; i++)
                {
                    if (currentPositions[i] < lists[i].Count)
                    {
                        hasMoreNotes = true;
                        if (currentTimes[i] < minTime)
                        {
                            minTime = currentTimes[i];
                        }
                    }
                }

                if (!hasMoreNotes)
                    break;

                long deltaFromLastNote = merged.Count == 0 ? minTime : minTime - (merged.Count > 0 ? GetLastAbsoluteTime(merged) : 0);
                bool isFirstNoteAtThisTime = true;

                // Add all notes that occur at minTime
                for (int i = 0; i < lists.Count; i++)
                {
                    while (currentPositions[i] < lists[i].Count && currentTimes[i] == minTime)
                    {
                        var note = lists[i][currentPositions[i]];
                        
                        var mergedNote = new TimedNote
                        {
                            NoteNumber = note.NoteNumber,
                            Duration = note.Duration,
                            Velocity = note.Velocity,
                            IsRest = note.IsRest,
                            Delta = isFirstNoteAtThisTime ? deltaFromLastNote : 0
                        };

                        merged.Add(mergedNote);
                        isFirstNoteAtThisTime = false;

                        // Advance position in this list
                        currentPositions[i]++;
                        
                        // Update time for next note in this list
                        if (currentPositions[i] < lists[i].Count)
                        {
                            currentTimes[i] += lists[i][currentPositions[i]].Delta;
                        }
                    }
                }
            }

            return merged;
        }

        /// <summary>
        /// Calculates the absolute time of the last note in a merged list.
        /// </summary>
        private static long GetLastAbsoluteTime(List<TimedNote> notes)
        {
            long time = 0;
            foreach (var note in notes)
            {
                time += note.Delta;
            }
            return time;
        }

        /// <summary>
        /// Converts a single Phrase object to a flat list of TimedNote objects.
        /// </summary>
        private static List<TimedNote> ConvertSinglePhrase(Phrase phrase, short ticksPerQuarterNote)
        {
            var timedNotes = new List<TimedNote>();
            long absoluteTime = 0;  // Track absolute time position
            long lastNoteStartTime = 0;  // Track when the last note/chord started

            foreach (var noteEvent in phrase.NoteEvents ?? Enumerable.Empty<NoteEvent>())
            {
                if (noteEvent.IsRest)
                {
                    // Rests add to the absolute time
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    absoluteTime += duration;
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

                    // Calculate duration once for the chord
                    var chordDuration = CalculateDuration(chordNotes[0], ticksPerQuarterNote);

                    // Convert chord notes to TimedNotes
                    for (int i = 0; i < chordNotes.Count; i++)
                    {
                        var cn = chordNotes[i];
                        var noteNumber = CalculateMidiNoteNumber(cn.Step, cn.Alter, cn.Octave);

                        // First note of the chord gets delta from last note, rest are simultaneous
                        var delta = i == 0 ? (absoluteTime - lastNoteStartTime) : 0;

                        timedNotes.Add(new TimedNote
                        {
                            Delta = delta,
                            NoteNumber = (byte)noteNumber,
                            Duration = chordDuration,
                            Velocity = 100,
                            IsRest = false
                        });
                    }

                    // Update tracking: chord started at absoluteTime and lasts for chordDuration
                    lastNoteStartTime = absoluteTime;
                    absoluteTime += chordDuration;
                }
                else
                {
                    // Handle single note
                    var duration = CalculateDuration(noteEvent, ticksPerQuarterNote);
                    var noteNumber = CalculateMidiNoteNumber(noteEvent.Step, noteEvent.Alter, noteEvent.Octave);

                    var delta = absoluteTime - lastNoteStartTime;

                    timedNotes.Add(new TimedNote
                    {
                        Delta = delta,
                        NoteNumber = (byte)noteNumber,
                        Duration = duration,
                        Velocity = 100,
                        IsRest = false
                    });

                    // Update tracking
                    lastNoteStartTime = absoluteTime;
                    absoluteTime += duration;
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