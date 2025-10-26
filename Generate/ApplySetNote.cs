using System;
using System.Collections.Generic;
using System.Linq;
using MusicXml.Domain;

namespace Music.Generate
{
    /// <summary>
    /// Inserts a repeated set of notes (beat-1 insertions) into parts / staffs / bars
    /// of a Score instance. Validates that inserted durations do not exceed the bar length.
    /// This class is form-control-agnostic; GenerateForm should call Apply() with values read
    /// from its controls.
    /// </summary>
    public static class ApplySetNote
    {
        // Denominator values used by the Generate UI: Whole=1, Half=2, Quarter=4, etc.
        public enum BaseDuration
        {
            Whole = 1,
            Half = 2,
            Quarter = 4,
            Eighth = 8,
            Sixteenth = 16
        }

        /// <summary>
        /// Apply the "Set Notes" operation to the provided score.
        /// The score object is mutated in-place.
        /// </summary>
        /// <param name="score">Score to modify (must not be null)</param>
        /// <param name="partNames">Part names selected in the UI (one or more). Only parts with matching Name will be modified.</param>
        /// <param name="staff">Staff number (textbox value). Only this single staff will receive notes.</param>
        /// <param name="startBar">1-based start bar number (inclusive)</param>
        /// <param name="endBar">1-based end bar number (inclusive)</param>
        /// <param name="step">Absolute step letter (A..G)</param>
        /// <param name="accidental">"Natural", "Sharp", "Flat" (case-insensitive). If null/empty treated as Natural.</param>
        /// <param name="octave">Octave offset to use for the pitch (e.g. 4 for middle C)</param>
        /// <param name="baseDuration">Base duration enum</param>
        /// <param name="numberOfNotes">Number of identical notes to insert at beat 1 in each bar</param>
        /// <exception cref="ArgumentException">for invalid arguments</exception>
        /// <exception cref="InvalidOperationException">if insertion would overflow a measure</exception>
        public static void Apply(
            MusicXml.Domain.Score score,
            IEnumerable<string> partNames,
            int staff,
            int startBar,
            int endBar,
            char step,
            string? accidental,
            int octave,
            BaseDuration baseDuration,
            int numberOfNotes)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (partNames == null) throw new ArgumentNullException(nameof(partNames));
            if (startBar < 1) throw new ArgumentException("startBar must be >= 1", nameof(startBar));
            if (endBar < startBar) throw new ArgumentException("endBar must be >= startBar", nameof(endBar));
            if (numberOfNotes <= 0) throw new ArgumentException("numberOfNotes must be > 0", nameof(numberOfNotes));
            if (!"ABCDEFG".Contains(char.ToUpper(step))) throw new ArgumentException("step must be a letter A-G", nameof(step));

            var parts = new HashSet<string>(partNames.Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.OrdinalIgnoreCase);
            if (parts.Count == 0) return; // nothing selected -> nothing to do

            // Ensure the Score has a Parts list and create missing Part entries for any selected names.
            score.Parts ??= new List<Part>();

            // Add any selected part names that don't already exist in score.Parts
            var existingNames = new HashSet<string>(
                score.Parts.Where(p => !string.IsNullOrWhiteSpace(p?.Name)).Select(p => p.Name!),
                StringComparer.OrdinalIgnoreCase);

            foreach (var partName in parts)
            {
                if (existingNames.Contains(partName)) continue;

                // generate a unique part Id like "P1", "P2", ...
                int idx = 1;
                string newId;
                do
                {
                    newId = "P" + idx++;
                } while (score.Parts.Any(p => string.Equals(p?.Id, newId, StringComparison.OrdinalIgnoreCase)));

                var newPart = new Part
                {
                    Id = newId,
                    Name = partName,
                    InstrumentName = partName,
                    MidiChannel = 1,
                    Measures = new List<Measure>()
                };

                score.Parts.Add(newPart);
                existingNames.Add(partName);
            }

            // Map accidental to alter value (MusicXml uses -1,0,1 commonly)
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

            foreach (var part in score.Parts ?? Enumerable.Empty<Part>())
            {
                if (part?.Name == null) continue;
                if (!parts.Contains(part.Name)) continue;

                // Ensure Measures collection exists
                part.Measures ??= new List<Measure>();

                // Ensure there are at least endBar measures (1-based)
                while (part.Measures.Count < endBar)
                {
                    // Copy attributes from first measure if available, otherwise create reasonable defaults.
                    MeasureAttributes attrs = null!;
                    if (part.Measures.Count > 0 && part.Measures[0]?.Attributes != null)
                    {
                        // shallow copy reference is acceptable for attributes that describe meter/key/clef,
                        // but to be safe create a new instance and copy common fields if they exist.
                        var src = part.Measures[0].Attributes;
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

                    part.Measures.Add(new Measure { Attributes = attrs });
                }

                // Now process each measure index in the requested range
                for (int bar = startBar; bar <= endBar; bar++)
                {
                    var measure = part.Measures[bar - 1];
                    if (measure == null)
                    {
                        measure = new Measure();
                        part.Measures[bar - 1] = measure;
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
                    var denom = (int)baseDuration;
                    var numerator = divisions * 4;
                    if (numerator % denom != 0)
                    {
                        throw new InvalidOperationException($"Cannot represent base duration '{baseDuration}' with measure divisions={divisions}. Resulting duration would not be an integer number of divisions.");
                    }
                    int noteDuration = numerator / denom;

                    long totalNewDuration = (long)noteDuration * numberOfNotes;
                    if (existingDuration + totalNewDuration > barLengthDivisions)
                    {
                        throw new InvalidOperationException(
                            $"Insertion would overflow bar {bar} of part '{part.Name}'. Bar capacity (in divisions): {barLengthDivisions}. Existing occupied: {existingDuration}. Attempting to add: {totalNewDuration}.");
                    }

                    // Append notes after existing elements (per requirement)
                    for (int i = 0; i < numberOfNotes; i++)
                    {
                        var note = new Note
                        {
                            Type = DurationTypeString(baseDuration),
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

        private static string DurationTypeString(BaseDuration d) => d switch
        {
            BaseDuration.Whole => "whole",
            BaseDuration.Half => "half",
            BaseDuration.Quarter => "quarter",
            BaseDuration.Eighth => "eighth",
            BaseDuration.Sixteenth => "16th",
            _ => "quarter"
        };
    }
}