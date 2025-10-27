using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MusicXml.Domain;

namespace Music.Generate
{
    /// <summary>
    /// Inserts a repeated set of notes (beat-1 insertions) into parts / staffs / bars
    /// of a Score instance. Validates that inserted durations do not exceed the bar length.
    /// This class is form-control-agnostic; GenerateForm should call Apply() with values read
    /// from its controls.
    /// 
    /// The assumption for this method is that the parts and measures already exist in the Score!!! 
    /// Somewhere in Generate form is needs to construct the score framework first Parts/Measures.
    /// 
    /// </summary>
    public static class ApplySetNote
    {
        /// <summary>
        /// Apply the "Set Notes" operation to the provided score.
        /// The score object is mutated in-place.
        /// This overload accepts UI-style inputs (owner, step string, note value key) and will
        /// call the validation helper which shows any necessary message boxes.
        /// </summary>
        public static void Apply(
            Form? owner,
            MusicXml.Domain.Score score,
            IEnumerable<string> designPartNames,
            int staff,
            int startBar,
            int endBar,
            string? stepStr,
            string? accidental,
            int octave,
            string? noteValueKey,
            int numberOfNotes)
        {
            // Validate UI-level parameters first; this will show message boxes on failure.
            if (!ValidateApplyParameters.Validate(owner, score, designPartNames, staff, startBar, endBar, stepStr, accidental ?? "Natural", octave, noteValueKey, numberOfNotes, out var stepChar, out var noteValue))
            {
                return;
            }

            // After validation, call the core implementation (same logic as before) using mapped values.
            ApplyCore(score, designPartNames, staff, startBar, endBar, stepChar, accidental, octave, noteValue, numberOfNotes);
        }

        // Extracted core implementation that assumes validated and already-mapped parameters.
        private static void ApplyCore(
            MusicXml.Domain.Score score,
            IEnumerable<string> designPartNames,
            int staff,
            int startBar,
            int endBar,
            char step,
            string? accidental,
            int octave,
            int noteValue,
            int numberOfNotes)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (designPartNames == null) throw new ArgumentNullException(nameof(designPartNames));
            if (startBar < 1) throw new ArgumentException("startBar must be >= 1", nameof(startBar));
            if (endBar < startBar) throw new ArgumentException("endBar must be >= startBar", nameof(endBar));
            if (numberOfNotes <= 0) throw new ArgumentException("numberOfNotes must be > 0", nameof(numberOfNotes));
            if (!"ABCDEFG".Contains(char.ToUpper(step))) throw new ArgumentException("step must be a letter A-G", nameof(step));

            if (!(new[] { 1, 2, 4, 8, 16 }).Contains(noteValue))
            {
                throw new ArgumentException("denominator must be one of: 1,2,4,8,16", nameof(noteValue));
            }

            // Ensure the Score has a Parts list and create missing Part entries for any selected names.
            // This logic was extracted to a helper for reuse and clarity.

            ScorePartsHelper.EnsurePartsExist(score, designPartNames);

            //===========================================================================================
            // Map accidental to alter value (MusicXml uses -1,0,1 commonly)
            //
            //  TODO  this value should map in the form control for accidentals !!!
            //

            int? alter = accidental switch
            {
                null => 0,
                "" => 0,
                var s when s.Equals("Sharp", StringComparison.OrdinalIgnoreCase) => 1,
                var s when s.Equals("Flat", StringComparison.OrdinalIgnoreCase) => -1,
                var s when s.Equals("Natural", StringComparison.OrdinalIgnoreCase) => 0,
                _ => 0
            };

            // For each matching part, insert notes for each bar in the range.

            foreach (var scorePart in score.Parts ?? Enumerable.Empty<Part>())
            {
                if (scorePart?.Name == null) continue;
                if (!designPartNames.Contains(scorePart.Name)) continue;

                // Ensure Measures collection exists
                scorePart.Measures ??= new List<Measure>();

                // Ensure there are at least endBar measures (1-based)
                while (scorePart.Measures.Count < endBar)
                {
                    // Copy attributes from first measure if available, otherwise create reasonable defaults.
                    MeasureAttributes attrs = null!;
                    if (scorePart.Measures.Count > 0 && scorePart.Measures[0]?.Attributes != null)
                    {
                        // shallow copy reference is acceptable for attributes that describe meter/key/clef,
                        // but to be safe create a new instance and copy common fields if they exist.
                        var src = scorePart.Measures[0].Attributes;
                        attrs = new MeasureAttributes
                        {
                            Divisions = src.Divisions,
                            Key = src.Key,
                            Time = src.Time,
                            Clef = src.Clef
                        };
                    }
                    else
                    {
                        attrs = new MeasureAttributes
                        {
                            Divisions = 1,
                            Key = new Key { Fifths = 0, Mode = "major" },
                            Time = new Time { Beats = 4, Mode = "4" },
                            Clef = new Clef { Sign = "G", Line = 2 }
                        };
                    }

                    scorePart.Measures.Add(new Measure { Attributes = attrs });
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
                        throw new InvalidOperationException($"Cannot represent base duration '{denom}' with measure divisions={divisions}. Resulting duration would not be an integer number of divisions.");
                    }
                    int noteDuration = numerator / denom;

                    long totalNewDuration = (long)noteDuration * numberOfNotes;
                    if (existingDuration + totalNewDuration > barLengthDivisions)
                    {
                        throw new InvalidOperationException(
                            $"Insertion would overflow bar {bar} of part '{scorePart.Name}'. Bar capacity (in divisions): {barLengthDivisions}. Existing occupied: {existingDuration}. Attempting to add: {totalNewDuration}.");
                    }

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

    /// <summary>
    /// Validation helper that contains the UI-level checks previously in GenerateApply.
    /// This shows MessageBoxes when inputs are invalid and maps the UI strings to the
    /// primitive values used by the core Apply logic.
    /// </summary>
    internal static class ValidateApplyParameters
    {
        public static bool Validate(
            Form? owner,
            Score? score,
            IEnumerable<string>? parts,
            int staff,
            int startBar,
            int endBar,
            string? stepStr,
            string accidental,
            int octave,
            string? noteValueKey,
            int numberOfNotes,
            out char stepChar,
            out int noteValue)
        {
            stepChar = '\0';
            noteValue = 4;

            if (score == null)
            {
                MessageBox.Show(owner, "Cannnot apply to a null score", "No Score", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Validate parts
            if (parts == null || !parts.Any())
            {
                MessageBox.Show(owner, "Please select a part to apply notes to.", "No Part Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Start/End bars
            if (startBar < 1 || endBar < startBar)
            {
                MessageBox.Show(owner, "Start and End bars must be valid (Start >= 1 and End >= Start).", "Invalid Bar Range", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Step (absolute)
            if (string.IsNullOrWhiteSpace(stepStr) || stepStr.Length == 0)
            {
                MessageBox.Show(owner, "Please select a step (A-G).", "Invalid Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            stepChar = stepStr![0];

            // Base duration - map from the UI string via _noteValueMap
            if (noteValueKey == null || !Music.MusicConstants.NoteValueMap.TryGetValue(noteValueKey, out var nv))
            {
                MessageBox.Show(owner, "Please select a valid base duration.", "Invalid Duration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            noteValue = nv;

            // numberOfNotes validated by core but ensure positive here too
            if (numberOfNotes <= 0)
            {
                MessageBox.Show(owner, "Number of notes must be at least 1.", "Invalid Number", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }
    }}