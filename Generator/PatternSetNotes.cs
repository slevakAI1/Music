using System;
using System.Collections.Generic;
using System.Linq;
using Music;
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

            // Extract and transform data from GeneratorData DTO
            var parts = (data.PartsState ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase))
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var staff = data.Staff.GetValueOrDefault();
            var startBar = data.StartBar.GetValueOrDefault();
            var endBar = data.EndBar.GetValueOrDefault(startBar);
            
            // Step is now char type - use directly, default to 'C' if '\0'
            char step = data.Step != '\0' ? data.Step : 'C';
            
            // Map accidental to alter value (MusicXml uses -1, 0, 1)
            string accidental = data.Accidental ?? "Natural";
            int? alter = accidental switch
            {
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                var s when s.Equals("Natural", StringComparison.OrdinalIgnoreCase) => 0,
                _ => 0
            };

            int octave = data.Octave;
            
            // Map NoteValue string to int denominator
            int noteValue = 4; // default
            if (data.NoteValue != null && Music.MusicConstants.NoteValueMap.TryGetValue(data.NoteValue, out var nv))
            {
                noteValue = nv;
            }
            
            var numberOfNotes = data.NumberOfNotes.GetValueOrDefault();

            // Ensure the Score has a Parts list and create missing Part entries for any selected names
            ScorePartsHelper.EnsurePartsExist(score, parts);

            // For each matching part, insert notes for each bar in the range
            foreach (var scorePart in score.Parts ?? Enumerable.Empty<Part>())
            {
                if (scorePart?.Name == null) continue;
                if (!parts.Contains(scorePart.Name)) continue;

                // Ensure Measures collection exists
                scorePart.Measures ??= new List<Measure>();

                // Ensure there are at least endBar measures (1-based)
                while (scorePart.Measures.Count < endBar)
                {
                    // NOTE: do not populate full Attributes here ? NewScore() is responsible for setting Divisions on all measures.
                    scorePart.Measures.Add(new Measure());
                }

                // Now process each measure index in the requested range
                for (int bar = startBar; bar <= endBar; bar++)
                {
                    var measure = scorePart.Measures[bar - 1];
                    if (measure == null)
                    {
                        measure = new Measure();
                        scorePart.Measures[bar - 1] = measure;
                    }

                    // Ensure MeasureElements list exists
                    measure.MeasureElements ??= new List<MeasureElement>();

                    // Determine the measure length in divisions:
                    var divisions = Math.Max(1, measure.Attributes?.Divisions ?? 1);
                    var beatsPerBar = (measure.Attributes?.Time?.Beats) ?? 4;
                    var barLengthDivisions = divisions * beatsPerBar;

                    // Sum durations of existing notes in this measure
                    long existingDuration = 0;
                    foreach (var me in measure.MeasureElements)
                    {
                        if (me == null) continue;

                        // Only consider existing notes for occupancy calculation.
                        if (me.Type == MeasureElementType.Note && me.Element is Note n && n.Duration > 0)
                        {
                            existingDuration += n.Duration;
                        }
                    }

                    // Compute single inserted note duration in divisions.
                    // Formula: durationDiv = (divisions * 4) / denom
                    var denom = noteValue;
                    var numerator = divisions * 4;
                    if (numerator % denom != 0)
                    {
                        var msg = $"Cannot represent base duration '{denom}' with measure divisions={divisions}. Resulting duration would not be an integer number of divisions.";
                        MessageBoxHelper.ShowError(msg, "Invalid Duration");
                        return;
                    }
                    int noteDuration = numerator / denom;

                    long totalNewDuration = (long)noteDuration * numberOfNotes;
                    if (existingDuration + totalNewDuration > barLengthDivisions)
                    {
                        var msg = $"Insertion would overflow bar {bar} of part '{scorePart.Name}'. Bar capacity (in divisions): {barLengthDivisions}. Existing occupied: {existingDuration}. Attempting to add: {totalNewDuration}.";
                        MessageBoxHelper.ShowError(msg, "Bar Overflow");
                        return;
                    }


                    // THIS NEEDS TO BE MODIFIED TO ALSO SUPPORT CHORDS NOW. 
                    //var a = Globals.GenerationData.rb

                    // Append notes after existing elements (per requirement)
                    for (int i = 0; i < numberOfNotes; i++)
                    {
                        var note = new Note
                        {
                            Type = DurationTypeString(denom),
                            Duration = noteDuration,
                            Voice = 1,           // voice number unspecified by requirement; default to 1
                            Staff = staff,
                            IsChordTone = false,
                            IsRest = data.IsRest ?? false,
                            Pitch = new Pitch
                            {
                                Step = char.ToUpper(step),
                                Octave = octave,
                                Alter = alter ?? 0
                            }
                        };

                        var meNote = new MeasureElement
                        {
                            Type = MeasureElementType.Note,
                            Element = note
                        };

                        measure.MeasureElements.Add(meNote);
                    }
                }
            }
            
            // Inform the user that pattern application is complete.
            MessageBoxHelper.ShowMessage("Pattern has been applied to the score.", "Apply Pattern Set Notes");
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