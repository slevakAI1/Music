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
        public static void Insert(Score score, SetNotesConfig config)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (config == null) throw new ArgumentNullException(nameof(config));

            ScorePartsHelper.EnsurePartsExist(score, config.Parts);

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
            scorePart.Measures ??= new List<Measure>();
            //EnsureMeasureCount(scorePart, config.EndBar);



            //               T O D O    !!  -     this should be placing notes down
            // starting at start bar/beat until all notes in list are placed.
            // There needs to be a mechanism to track last bar beat position?? - a method should do it

            for (int bar = config.StartBar; bar <= config.EndBar; bar++)    
            {
                ProcessMeasure(scorePart, bar, config);
            }


        }

        //private static void EnsureMeasureCount(Part part, int requiredCount)
        //{
        //    while (part.Measures.Count < requiredCount)
        //    {
        //        part.Measures.Add(new Measure());
        //    }
        //}

        private static void ProcessMeasure(Part scorePart, int barNumber, SetNotesConfig config)
        {
            var measure = scorePart.Measures[barNumber - 1];
            if (measure == null)
            {
                measure = new Measure();
                scorePart.Measures[barNumber - 1] = measure;
            }

            measure.MeasureElements ??= new List<MeasureElement>();

            // Get note data from first item in list
            if (config.Notes.Count == 0) return;

            var measureInfo = GetMeasureInfo(measure);
            int noteDuration = CalculateNoteDuration(measureInfo.Divisions, config.Notes[0].NoteValue);
            
            if (!ValidateCapacity(measure, measureInfo, noteDuration, config.Notes[0].NumberOfNotes, barNumber, scorePart.Name))
            {
                return;
            }

            InsertNotes(measure, config, noteDuration);
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
                var msg = $"Cannot represent base duration '{noteValue}' with measure divisions={divisions}. Resulting duration would not be an integer number of divisions.";
                MessageBoxHelper.ShowError(msg, "Invalid Duration");
                return 0;
            }
            return numerator / noteValue;
        }

        private static bool ValidateCapacity(Measure measure, MeasureInfo info, int noteDuration, 
            int numberOfNotes, int barNumber, string? partName)
        {
            if (noteDuration == 0) return false;

            long totalNewDuration = (long)noteDuration * numberOfNotes;
            if (info.ExistingDuration + totalNewDuration > info.BarLengthDivisions)
            {
                var msg = $"Insertion would overflow bar {barNumber} of part '{partName}'. " +
                          $"Bar capacity (in divisions): {info.BarLengthDivisions}. " +
                          $"Existing occupied: {info.ExistingDuration}. " +
                          $"Attempting to add: {totalNewDuration}.";
                MessageBoxHelper.ShowError(msg, "Bar Overflow");
                return false;
            }
            return true;
        }

        private static void InsertNotes(Measure measure, SetNotesConfig config, int noteDuration)
        {
            var firstNote = config.Notes[0];
            
            for (int i = 0; i < firstNote.NumberOfNotes; i++)
            {
                // Insert notes for each selected staff
                foreach (var staff in config.Staffs)
                {
                    // Insert all notes from the list (single note or chord)
                    foreach (var writerNote in config.Notes)
                    {
                        var note = new Note
                        {
                            Type = DurationTypeString(writerNote.NoteValue),
                            Duration = noteDuration,
                            Voice = 1,
                            Staff = staff,
                            IsChordTone = writerNote.IsChord,
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
            }
        }

        private static string DurationTypeString(int denom) => denom switch
        {
            1 => "whole",
            2 => "half",
            4 => "quarter",
            8 => "eighth",
            16 => "16th",
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

    /// <summary>
    /// Helper extracted from ApplySetNote.Apply to ensure score parts exist for the requested part names.
    /// Callers may reuse this helper when parts need to be created before further modification.
    /// </summary>
    public static class ScorePartsHelper
    {
        public static void EnsurePartsExist(Score score, IEnumerable<string> designPartNames)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (designPartNames == null) throw new ArgumentNullException(nameof(designPartNames));

            // TODO this is outside scope. Adding parts should be a separate operation.
            if (designPartNames.Count() == 0)
            {
                throw new ArgumentException("At least one part must be selected.", nameof(designPartNames));
            }

            score.Parts ??= new List<Part>();

            // Build a case-insensitive set of existing part names for quick lookup.
            var scorePartNames = new HashSet<string>(
                score.Parts.Where(p => !string.IsNullOrWhiteSpace(p?.Name)).Select(p => p.Name!),
                StringComparer.OrdinalIgnoreCase);

            int count = 0;
            foreach (var partName in designPartNames)
            {
                count++;  // THIS ASSUMES that parts are added in sequence 1,2,3,...and some may not be affected
                          // by this operation
                if (scorePartNames.Contains(partName)) continue;
                var newPart = new Part
                {
                    Id = count.ToString(),
                    Name = partName,
                    InstrumentName = partName,
                    MidiChannel = count,
                    Measures = new List<Measure>()
                };

                score.Parts.Add(newPart);
                scorePartNames.Add(partName);
            }
        }
    }
}