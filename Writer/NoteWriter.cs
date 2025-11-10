using MusicXml.Domain;
using Music.Designer;
using Music;

namespace Music.Writer
{
    /// <summary>
    /// Inserts a set of notes based on user parameters.
    /// Method assumes the score has already been initialized with parts, measure, tempo, time signature.
    /// All parameters are expected to be pre-validated.
    /// </summary>
    public static class NoteWriter
    {
        /// <summary>
        /// Adds notes to the specified score based on the provided configuration.
        /// All parameters are expected to be pre-validated.
        /// </summary>
        public static void Append(Score score, SetNotesConfig config)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Notes == null || config.Notes.Count == 0) return;

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config);
            }
            
            MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
        }

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, SetNotesConfig config)
        {
            if (scorePart.Measures == null || scorePart.Measures.Count == 0)
                return;

            int currentBar = config.StartBar;
            long currentBeatPosition = 0; // Position within the current measure in divisions

            // Get initial measure info to establish beat position
            if (currentBar <= scorePart.Measures.Count)
            {
                var startMeasure = scorePart.Measures[currentBar - 1];
                var measureInfo = GetMeasureInfo(startMeasure);
                
                // Convert StartBeat to divisions offset
                if (config.StartBeat > 1)
                {
                    currentBeatPosition = (config.StartBeat - 1) * measureInfo.Divisions;
                }
            }

            // Determine the number of repetitions (use the first note's NumberOfNotes)
            int numberOfRepetitions = config.Notes[0].NumberOfNotes;

            // Group notes into chord groups (consecutive notes where IsChord follows the pattern)
            var chordGroups = GroupNotesIntoChords(config.Notes);

            // Process each repetition
            for (int repetition = 0; repetition < numberOfRepetitions; repetition++)
            {
                // Process each chord group in this repetition
                foreach (var chordGroup in chordGroups)
                {
                    if (currentBar > scorePart.Measures.Count)
                    {
                        MessageBoxHelper.ShowMessage(
                            $"Ran out of measures in part '{scorePart.Name}' at bar {currentBar}. " +
                            "Not all notes were placed.",
                            "Insufficient Measures");
                        return;
                    }

                    var measure = scorePart.Measures[currentBar - 1];
                    measure.MeasureElements ??= new List<MeasureElement>();

                    var measureInfo = GetMeasureInfo(measure);
                    int noteDuration = CalculateNoteDuration(measureInfo.Divisions, chordGroup[0].NoteValue);

                    if (noteDuration == 0)
                    {
                        continue;
                    }

                    // Check if note fits in current measure
                    long availableSpace = measureInfo.BarLengthDivisions - currentBeatPosition;

                    if (availableSpace <= 0 || currentBeatPosition >= measureInfo.BarLengthDivisions)
                    {
                        // Move to next measure
                        currentBar++;
                        currentBeatPosition = 0;

                        if (currentBar > scorePart.Measures.Count)
                        {
                            MessageBoxHelper.ShowMessage(
                                $"Ran out of measures in part '{scorePart.Name}' at bar {currentBar}. " +
                                "Not all notes were placed.",
                                "Insufficient Measures");
                            return;
                        }

                        measure = scorePart.Measures[currentBar - 1];
                        measure.MeasureElements ??= new List<MeasureElement>();
                        measureInfo = GetMeasureInfo(measure);
                        availableSpace = measureInfo.BarLengthDivisions;
                    }

                    // Insert all notes in this chord group for each selected staff
                    foreach (var staff in config.Staffs)
                    {
                        for (int noteIndex = 0; noteIndex < chordGroup.Count; noteIndex++)
                        {
                            var writerNote = chordGroup[noteIndex];

                            // First note of the chord has no <chord/> tag, subsequent notes do
                            bool isChordTone = noteIndex > 0;

                            var note = new Note
                            {
                                Type = DurationTypeString(writerNote.NoteValue),
                                Duration = noteDuration,
                                Voice = 1,
                                Staff = staff,
                                IsChordTone = isChordTone,
                                IsRest = writerNote.IsRest,
                                Pitch = new Pitch
                                {
                                    Step = char.ToUpper(writerNote.Step),
                                    Octave = writerNote.Octave,
                                    Alter = writerNote.Alter
                                }
                            };

                            measure.MeasureElements.Add(new MeasureElement
                            {
                                Type = MeasureElementType.Note,
                                Element = note
                            });
                        }
                    }

                    // Advance position (only once per chord group, not per note in the chord)
                    currentBeatPosition += noteDuration;

                    // Check if we've filled the measure and need to move to next
                    if (currentBeatPosition >= measureInfo.BarLengthDivisions)
                    {
                        currentBar++;
                        currentBeatPosition = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Groups notes into chord groups. Each group starts with a note where IsChord=false
        /// and includes all subsequent notes where IsChord=true.
        /// </summary>
        private static List<List<WriterNote>> GroupNotesIntoChords(List<WriterNote> notes)
        {
            var groups = new List<List<WriterNote>>();
            List<WriterNote>? currentGroup = null;

            foreach (var note in notes)
            {
                if (!note.IsChord)
                {
                    // Start a new chord group
                    currentGroup = new List<WriterNote> { note };
                    groups.Add(currentGroup);
                }
                else if (currentGroup != null)
                {
                    // Add to current chord group
                    currentGroup.Add(note);
                }
            }

            return groups;
        }

        private static MeasureInfo GetMeasureInfo(Measure measure)
        {
            var divisions = Math.Max(1, measure.Attributes?.Divisions ?? 1);
            var beatsPerBar = measure.Attributes?.Time?.Beats ?? 4;
            var existingDuration = CalculateExistingDuration(measure);

            return new MeasureInfo
            {
                Divisions = divisions,
                BeatsPerBar = beatsPerBar,
                BarLengthDivisions = divisions * beatsPerBar,
                ExistingDuration = existingDuration
            };
        }

        private static long CalculateExistingDuration(Measure measure)
        {
            long duration = 0;
            foreach (var me in measure.MeasureElements ?? Enumerable.Empty<MeasureElement>())
            {
                if (me?.Type == MeasureElementType.Note && 
                    me.Element is Note n && 
                    n.Duration > 0 && 
                    !n.IsChordTone)
                {
                    duration += n.Duration;
                }
            }
            return duration;
        }

        private static int CalculateNoteDuration(int divisions, int noteValue)
        {
            int numerator = divisions * 4;
            if (numerator % noteValue != 0)
            {
                // Calculate the minimum divisions needed for this note value
                // For noteValue=32, need divisions≥8; for noteValue=64, need divisions≥16
                int minDivisions = (noteValue + 3) / 4; // Round up: (32+3)/4=8, (64+3)/4=16
                
                var msg = $"Cannot represent note value '{noteValue}' (1/{noteValue} note) with divisions={divisions}.\n" +
                          $"Minimum required divisions: {minDivisions}.\n\n" +
                          $"To fix this:\n" +
                          $"1. Create a new score (divisions will be set to 8)\n" +
                          $"2. Or manually edit the score's divisions value";
                MessageBoxHelper.ShowError(msg, "Invalid Duration");
                return 0;
            }
            return numerator / noteValue;
        }

        private static string DurationTypeString(int denom) => denom switch
        {
            1 => "whole",
            2 => "half",
            4 => "quarter",
            8 => "eighth",
            16 => "16th",
            32 => "32nd",  // Added
            64 => "64th",   // Added for future support
            _ => "quarter"
        };

        private sealed class MeasureInfo
        {
            public int Divisions { get; set; }
            public int BeatsPerBar { get; set; }
            public int BarLengthDivisions { get; set; }
            public long ExistingDuration { get; set; }
        }
    }
}