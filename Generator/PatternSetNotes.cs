using MusicXml.Domain;

namespace Music.Generator
{
    /// <summary>
    /// Inserts a set of notes based on user parameters.
    /// Method assumes the score has already been initialized with parts, measure, tempo, time signature.
    /// All parameters are expected to be pre-validated.
    /// </summary>
    public static class PatternSetNotes
    {
        /// <summary>
        /// Apply the "Set Notes" operation to the provided score.
        /// The score object is updated in-place.
        /// All parameters are expected to be pre-validated.
        /// </summary>
        public static void Apply(Score score, GeneratorData data)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var config = ExtractConfiguration(data);
            ScorePartsHelper.EnsurePartsExist(score, config.Parts);

            foreach (var scorePart in GetTargetParts(score, config.Parts))
            {
                ProcessPart(scorePart, config);
            }
            
            MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
        }

        private static PatternConfiguration ExtractConfiguration(GeneratorData data)
        {
            var parts = (data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase))
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var config = new PatternConfiguration
            {
                Parts = parts,
                Staff = data.Staff.GetValueOrDefault(),
                StartBar = data.StartBar.GetValueOrDefault(),
                EndBar = data.EndBar.GetValueOrDefault(data.StartBar.GetValueOrDefault()),
                Step = data.Step != '\0' ? data.Step : 'C',
                Octave = data.Octave,
                NoteValue = GetNoteValue(data.NoteValue),
                NumberOfNotes = data.NumberOfNotes.GetValueOrDefault(),
                IsChord = data.IsChord ?? false,
                IsRest = data.IsRest ?? false,
                Alter = GetAlter(data.Accidental)
            };

            if (config.IsChord)
            {
                config.ChordNotes = HarmonicChordConverter.Convert(
                    data.ChordKey,
                    (int)data.ChordDegree,
                    data.ChordQuality,
                    data.ChordBase,
                    baseOctave: config.Octave);
            }

            return config;
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

        private static IEnumerable<Part> GetTargetParts(Score score, List<string> partNames)
        {
            return (score.Parts ?? Enumerable.Empty<Part>())
                .Where(p => p?.Name != null && partNames.Contains(p.Name));
        }

        private static void ProcessPart(Part scorePart, PatternConfiguration config)
        {
            scorePart.Measures ??= new List<Measure>();
            EnsureMeasureCount(scorePart, config.EndBar);

            for (int bar = config.StartBar; bar <= config.EndBar; bar++)
            {
                ProcessMeasure(scorePart, bar, config);
            }
        }

        private static void EnsureMeasureCount(Part part, int requiredCount)
        {
            while (part.Measures.Count < requiredCount)
            {
                part.Measures.Add(new Measure());
            }
        }

        private static void ProcessMeasure(Part scorePart, int barNumber, PatternConfiguration config)
        {
            var measure = scorePart.Measures[barNumber - 1];
            if (measure == null)
            {
                measure = new Measure();
                scorePart.Measures[barNumber - 1] = measure;
            }

            measure.MeasureElements ??= new List<MeasureElement>();

            var measureInfo = GetMeasureInfo(measure);
            int noteDuration = CalculateNoteDuration(measureInfo.Divisions, config.NoteValue);
            
            if (!ValidateCapacity(measure, measureInfo, noteDuration, config, barNumber, scorePart.Name))
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
            PatternConfiguration config, int barNumber, string? partName)
        {
            if (noteDuration == 0) return false;

            long totalNewDuration = (long)noteDuration * config.NumberOfNotes;
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

        private static void InsertNotes(Measure measure, PatternConfiguration config, int noteDuration)
        {
            for (int i = 0; i < config.NumberOfNotes; i++)
            {
                if (config.IsChord && config.ChordNotes != null)
                {
                    InsertChord(measure, config, noteDuration);
                }
                else
                {
                    InsertSingleNote(measure, config, noteDuration);
                }
            }
        }

        private static void InsertChord(Measure measure, PatternConfiguration config, int noteDuration)
        {
            for (int j = 0; j < config.ChordNotes!.Count; j++)
            {
                var chordNote = config.ChordNotes[j];
                var note = new Note
                {
                    Type = DurationTypeString(config.NoteValue),
                    Duration = noteDuration,
                    Voice = 1,
                    Staff = config.Staff,
                    IsChordTone = j > 0,
                    IsRest = false,
                    Pitch = new Pitch
                    {
                        Step = char.ToUpper(chordNote.Step),
                        Octave = chordNote.Octave,
                        Alter = chordNote.Alter
                    }
                };

                measure.MeasureElements.Add(new MeasureElement
                {
                    Type = MeasureElementType.Note,
                    Element = note
                });
            }
        }

        private static void InsertSingleNote(Measure measure, PatternConfiguration config, int noteDuration)
        {
            var note = new Note
            {
                Type = DurationTypeString(config.NoteValue),
                Duration = noteDuration,
                Voice = 1,
                Staff = config.Staff,
                IsChordTone = false,
                IsRest = config.IsRest,
                Pitch = new Pitch
                {
                    Step = char.ToUpper(config.Step),
                    Octave = config.Octave,
                    Alter = config.Alter
                }
            };

            measure.MeasureElements.Add(new MeasureElement
            {
                Type = MeasureElementType.Note,
                Element = note
            });
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

        // Configuration extracted from GeneratorData for easier processing
        private sealed class PatternConfiguration
        {
            public List<string> Parts { get; set; } = new();
            public int Staff { get; set; }
            public int StartBar { get; set; }
            public int EndBar { get; set; }
            public char Step { get; set; }
            public int Octave { get; set; }
            public int NoteValue { get; set; }
            public int NumberOfNotes { get; set; }
            public bool IsChord { get; set; }
            public bool IsRest { get; set; }
            public int Alter { get; set; }
            public List<HarmonicChordConverter.ChordNote>? ChordNotes { get; set; }
        }

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